using AudioEngine.Services;
using DataAccess.Contexts;
using DataAccess.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Server_Application.Server
{
    /// <summary>
    /// This class is the main class of the server. It handles connection requests and assigns ClientHandlers to the connected clients.
    /// It also controls data manupulation on songs and images. It also handles the database connection.
    /// </summary>
    public sealed class Controller
    {
        private Dictionary<string, SslStream> _clientStreamingSockets;
        private Dictionary<string, SslStream> _clientCommunicationSockets;
        private List<ClientHandler> _clients;
        private readonly int _portCommunication;
        private readonly int _portStreaming;
        private readonly string _host;
        private int _clientCountLimit;
        private Thread _listenerCommunicationThread;
        private Thread _listenerStreamingThread;
        private Thread _handleInternalRequestsThread;
        private Task _createClientHandlerTask;
        private Socket? _communicationSocket;
        private Socket? _streamingSocket;
        private readonly AutoResetEvent _newClient;
        private readonly AutoResetEvent _newInternalRequest;
        private readonly ServerListener _serverListener;
        private readonly Queue<InternalRequest> _internalRequestQueue;
        private readonly object _lock = new object();
        private readonly object _engineStateLock = new object();
        private readonly object _criticalCommunicationListeningOperation = new object();
        private readonly object _criticalStreamingListeningOperation = new object();
        private readonly object _clientCountLimitChangeLock = new object();

        private readonly IServiceProvider _serviceProvider;
        private readonly IAudioEngineConfigurationService _audioEngineConfigurationService;
        private readonly X509Certificate _x509Certificate;

        private volatile bool _exitCommunicationListeningLoop;
        private volatile bool _exitStreamingListeningLoop;
        private volatile bool _exitCreateClientHandlerLoop;
        private volatile bool _exitHandleInternalRequestLoop;

        public bool IsRunning { get; private set; }
        public int ConnectedCount { get { return _clients.Count; } }
        public int ClientCountLimit { get { return _clientCountLimit; } }

        private enum TerminateClientsMode
        {
            All,
            Limit
        }

        public Controller(IServiceProvider serviceProvider, IAudioEngineConfigurationService audioEngineConfigurationService)
        {
            _serviceProvider = serviceProvider;
            _audioEngineConfigurationService = audioEngineConfigurationService;
            _clientStreamingSockets = new Dictionary<string, SslStream>();
            _clientCommunicationSockets = new Dictionary<string, SslStream>();
            _listenerCommunicationThread = new Thread(CommuncationListeningLoop);
            _listenerCommunicationThread.IsBackground = true;
            _listenerStreamingThread = new Thread(StreamingListeningLoop);
            _listenerStreamingThread.IsBackground = true;
            _createClientHandlerTask = new Task(CreateClientHandlerLoop);
            _handleInternalRequestsThread = new Thread(InternalRequestLoop);
            _handleInternalRequestsThread.IsBackground = true;
            _newClient = new AutoResetEvent(false);
            _newInternalRequest = new AutoResetEvent(false);
            _clients = new List<ClientHandler>();
            _host = _audioEngineConfigurationService.Host;
            _portCommunication = _audioEngineConfigurationService.PortCommunication;
            _portStreaming = _audioEngineConfigurationService.PortStreaming;
            _x509Certificate = _audioEngineConfigurationService.X509Certificate;
            _communicationSocket = CreateSocket(_host, _portCommunication);
            _streamingSocket = CreateSocket(_host, _portStreaming);
            _serverListener = new ServerListener();
            _internalRequestQueue = new Queue<InternalRequest>();
            IsRunning = false;
            InitializeClientCountLimit(ref _clientCountLimit);
            Listen(EventType.InternalRequest, new ServerEventCallback(AddInternalRequest));
        }

        private void InitializeClientCountLimit(ref int clientCountLimit)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dataAccessConfigurationService = scope.ServiceProvider.GetRequiredService<IDataAccessConfigurationService>();
                clientCountLimit = dataAccessConfigurationService.DesktopAppClientCountLimit;
            }
        }

        /// <summary>
        /// This method will forcibly terminate clients. 
        /// </summary>
        /// <param name="mode"></param>
        private void TerminateClients(TerminateClientsMode mode)
        {
            if (mode == TerminateClientsMode.All)
            {
                while (_clients.Count > 0)
                {
                    try
                    {
                        _clients[0]?.TerminateClientHandler();
                    }
                    catch { }
                }
            }

            else if (mode == TerminateClientsMode.Limit)
            {
                while (_clients.Count > _clientCountLimit)
                {
                    try
                    {
                        _clients[0]?.TerminateClientHandler();
                    }
                    catch { }
                }
            }
        }

        private void AddInternalRequest(params object[] parameters)
        {
            _internalRequestQueue.Enqueue(new InternalRequest(parameters));
            _newInternalRequest.Set();
        }

        private void ExecuteInternalRequest(InternalRequest internalRequest)
        {
            if (internalRequest.Type == InternalRequestType.RemoveClientHandler)
            {
                ExecuteRemoveClientHandler(internalRequest.Parameters);
            }
        }

        /// <summary>
        /// This method is called to remove a client handler when it is terminated.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteRemoveClientHandler(object[] parameters)
        {
            ClientHandler handler = (ClientHandler)parameters[0];
            _clients.Remove(handler);
            GC.Collect();
        }

        private void Listen(EventType eventType, ServerEventCallback serverEventCallback)
        {
            _serverListener.Listen(eventType, serverEventCallback);
        }


        private void StartInternalRequestLoop()
        {
            _handleInternalRequestsThread = new Thread(InternalRequestLoop);
            _handleInternalRequestsThread.IsBackground = true;
            _exitHandleInternalRequestLoop = false;
            _handleInternalRequestsThread.Start();
        }

        private void ExitInternalRequestLoop()
        {
            _exitHandleInternalRequestLoop = true;
            _newInternalRequest.Set();
            _handleInternalRequestsThread.Join();
        }

        private void InternalRequestLoop()
        {
            while (!_exitHandleInternalRequestLoop)
            {
                if (_internalRequestQueue.Count == 0)
                {
                    _newInternalRequest.WaitOne();

                    if (_exitHandleInternalRequestLoop)
                    {
                        break;
                    }
                }

                InternalRequest? internalRequest;

                if (_internalRequestQueue.TryDequeue(out internalRequest))
                {
                    ExecuteInternalRequest(internalRequest);
                }
            }
        }

        private void ExitStreamingListeningLoop()
        {
            _exitStreamingListeningLoop = true;
            _streamingSocket?.Close();
            _streamingSocket?.Dispose();
            _streamingSocket = null;
            _listenerStreamingThread.Join();
        }

        private void StartStreamingListeningLoop()
        {
            _listenerStreamingThread = new Thread(StreamingListeningLoop);
            _listenerStreamingThread.IsBackground = true;
            _exitStreamingListeningLoop = false;

            if (_streamingSocket == null || !_streamingSocket.IsBound)
            {
                _streamingSocket = CreateSocket(_host, _portStreaming);
            }

            _listenerStreamingThread.Start();
        }

        /// <summary>
        /// Listens for clients trying to connect to the streaming port.
        /// </summary>
        /// 
        private void StreamingListeningLoop()
        {
            try
            {
                while (!_exitStreamingListeningLoop)
                {
                    if (_streamingSocket != null)
                    {
                        lock (_criticalStreamingListeningOperation)
                        {
                            if (ConnectedCount >= ClientCountLimit)
                            {
                                Thread.Sleep(300);
                                continue;
                            }

                            Socket socket = _streamingSocket.Accept();
                            IPEndPoint? ipEndPoint = (IPEndPoint?)socket.RemoteEndPoint;
                            SslStream sslStream = new SslStream(new NetworkStream(socket, true), false);
                            sslStream.AuthenticateAsServer(_x509Certificate, false, true);

                            string clientId = Encoding.UTF8.GetString(ReceiveTCP(6, sslStream));
                            string clientFullId = GetClientFullId(clientId, ipEndPoint);
                            lock (_lock)
                            {
                                _clientStreamingSockets.Add(clientFullId, sslStream);
                                _newClient.Set();
                            }
                        }
                    }
                }
            }
            catch { }

            finally
            {
                _streamingSocket?.Close();
            }
        }

        private void StartCommunicationListeningLoop()
        {
            _listenerCommunicationThread = new Thread(CommuncationListeningLoop);
            _listenerCommunicationThread.IsBackground = true;
            _exitCommunicationListeningLoop = false;
            if (_communicationSocket == null || !_communicationSocket.IsBound)
            {
                _communicationSocket = CreateSocket(_host, _portCommunication);
            }
            _listenerCommunicationThread.Start();
        }

        private void ExitCommunicationListeningLoop()
        {
            _exitCommunicationListeningLoop = true;
            _communicationSocket?.Close();
            _communicationSocket?.Dispose();
            _communicationSocket = null;
            _listenerCommunicationThread.Join();
        }

        /// <summary>
        /// Listens for clients trying to connect to communication port.
        /// </summary>
        /// 

        private void CommuncationListeningLoop()
        {
            try
            {
                while (!_exitCommunicationListeningLoop)
                {
                    if (_communicationSocket != null)
                    {

                        lock (_criticalCommunicationListeningOperation)
                        {
                            if (ConnectedCount >= ClientCountLimit)
                            {
                                Thread.Sleep(300);
                                continue;
                            }

                            Socket socket = _communicationSocket.Accept();
                            IPEndPoint? ipEndPoint = (IPEndPoint?)socket.RemoteEndPoint;
                            SslStream sslStream = new SslStream(new NetworkStream(socket, true), false);
                            sslStream.AuthenticateAsServer(_x509Certificate, false, false);

                            string clientId = Encoding.UTF8.GetString(ReceiveTCP(6, sslStream));
                            string clientFullId = GetClientFullId(clientId, ipEndPoint);
                            lock (_lock)
                            {
                                _clientCommunicationSockets.Add(clientFullId, sslStream);
                                _newClient.Set();
                            }
                        }
                    }
                }
            }
            catch { }

            finally
            {
                _communicationSocket?.Close();
            }
        }


        private void StartCreateClientHandlerLoop()
        {
            _createClientHandlerTask = new Task(CreateClientHandlerLoop);
            _exitCreateClientHandlerLoop = false;
            _createClientHandlerTask.Start();
        }

        private void ExitCreateClientHandlerLoop()
        {
            _exitCreateClientHandlerLoop = true;
            _newClient.Set();
            _createClientHandlerTask.Wait();
        }

        /// <summary>
        /// Creates a client handler after two sockets from the same client are received - streaming and communication.
        /// </summary>
        private void CreateClientHandlerLoop()
        {
            while (true)
            {
                _newClient.WaitOne();
                if (_exitCreateClientHandlerLoop)
                {
                    break;
                }
                lock (_lock)
                {
                    _newClient.Reset();
                    foreach (var key in _clientStreamingSockets.Keys)
                    {
                        if (_clientCommunicationSockets.ContainsKey(key))
                        {
                            if (_clients.Count < _clientCountLimit)
                            {
                                _clients.Add(new ClientHandler(key,
                                _clientStreamingSockets[key],
                                _clientCommunicationSockets[key],
                                _serviceProvider.CreateScope()));
                                _clientCommunicationSockets.Remove(key);
                                _clientStreamingSockets.Remove(key);
                            }
                            else
                            {
                                try
                                {
                                    _clientCommunicationSockets[key]?.Close();
                                    _clientCommunicationSockets[key]?.Dispose();
                                    _clientCommunicationSockets.Remove(key);
                                    _clientStreamingSockets[key]?.Close();
                                    _clientStreamingSockets[key]?.Dispose();
                                    _clientStreamingSockets.Remove(key);
                                }
                                catch { }
                            }
                            break;
                        }
                    }
                }
            }
        }

        private string GetClientFullId(string ClientId, IPEndPoint? iPEndPoint)
        {
            if (iPEndPoint != null)
            {
                return $"{ClientId}@{iPEndPoint.Address.ToString()}";
            }
            return $"{ClientId}@";
        }

        /// <summary>
        /// This method will change the limit count for clients. This represents the maximum number of clients
        /// connected at one time. If there are more clients connected than the new limit, the excessive clients
        /// will be terminated.
        /// </summary>
        /// <param name="limit"></param>
        public void ChangeClientCountLimit(int limit)
        {
            bool acquiredLock = false;
            try
            {
                Monitor.TryEnter(_clientCountLimitChangeLock, ref acquiredLock);
                if (acquiredLock)
                {
                    _clientCountLimit = limit;
                    TerminateClients(TerminateClientsMode.Limit);
                }
            }
            finally
            {
                Monitor.Exit(_clientCountLimitChangeLock);
            }
        }

        /// <summary>
        /// Starts the controller: 1) Start communication listening thread. 2) Start streaming listening thread. 3) Start create client
        /// handler thread. 4) Start internal request thread.
        /// </summary>
        public void Start()
        {
            bool acquiredLock = false;
            try
            {
                Monitor.TryEnter(_engineStateLock, ref acquiredLock);
                if (acquiredLock)
                {
                    if (!IsRunning)
                    {
                        StartCommunicationListeningLoop();
                        StartStreamingListeningLoop();
                        StartCreateClientHandlerLoop();
                        StartInternalRequestLoop();
                        IsRunning = true;
                    }
                }
            }
            finally
            {
                Monitor.Exit(_engineStateLock);
            }
        }

        /// <summary>
        /// Stop the controller: 1) Terminate communication listening thread. 2) Terminate streaming listening thread. 3) Terminate create client
        /// handler thread. 4) Terminate all clients. 5) Terminate internal request thread.
        /// </summary>
        public void Stop()
        {
            bool acquiredLock = false;
            try
            {
                Monitor.TryEnter(_engineStateLock, ref acquiredLock);
                if (acquiredLock)
                {
                    if (IsRunning)
                    {
                        ExitCommunicationListeningLoop();
                        ExitStreamingListeningLoop();
                        ExitCreateClientHandlerLoop();
                        TerminateClients(TerminateClientsMode.All);
                        ExitInternalRequestLoop();
                        IsRunning = false;
                    }
                }
            }
            finally
            {
                Monitor.Exit(_engineStateLock);
            }
        }

        private Socket CreateSocket(string ipAddress, int port)
        {
            IPAddress IP = IPAddress.Parse(ipAddress);
            IPEndPoint ipe = new IPEndPoint(IP, port);
            Socket socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(ipe);
            socket.Listen(100);
            return socket;
        }

        /// <summary>
        /// Makes sure all the data is received.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="socket"></param>
        /// <returns></returns>
        /// <exception cref="SocketException"></exception>
        public static byte[] ReceiveTCP(int size, SslStream? sslStream)
        {
            if (sslStream == null)
            {
                throw new SocketException();
            }
            byte[] packet = new byte[size];
            int bytesReceived = 0;
            int x;
            while (bytesReceived < size)
            {
                byte[] buffer = new byte[size - bytesReceived];
                x = sslStream.Read(buffer);
                Buffer.BlockCopy(buffer, 0, packet, bytesReceived, x);
                bytesReceived += x;
            }
            return packet;
        }
    }
}
