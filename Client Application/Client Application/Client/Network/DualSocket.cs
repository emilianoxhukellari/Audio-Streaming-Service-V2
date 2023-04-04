using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Printing.IndexedProperties;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Client_Application.Client.Event;

namespace Client_Application.Client.Network
{

    /// <summary>
    /// This class represents a dual socket design. One socket is dedicated to MediaPlayer, and the other to Controller.
    /// If the Dual Socket receives a Reconnect() command, it will reconnect both sockets.
    /// </summary>
    /// 
    public sealed class DualSocket
    {
        public SslClient StreamingSSL { get; private set; }
        public SslClient CommunicationSSL { get; private set; }
        public bool Connected
        {
            get { return _connected; }
            private set { _connected = value; ConnectionStateUpdate(_connected); }
        }

        private volatile bool _connected;
        private volatile bool _communicationConnected;
        private volatile bool _streamingConnected;
        private readonly ManualResetEvent _reconnectFlag;
        private readonly ManualResetEvent _communicationConnectedFlag;
        private readonly ManualResetEvent _streamingConnectedFlag;
        private readonly string _clientId;
        private readonly IPEndPoint _communicationIPE;
        private readonly IPEndPoint _streamingIPE;
        private readonly Task _connectionTaskCommunication;
        private readonly Task _connectionTaskStreaming;
        private readonly CallbackRecoverSession RecoverSession;
        private readonly CallbackConnectionStateUpdate ConnectionStateUpdate;
        private readonly object _lockDisconnected = new object();
        private readonly object _lockConnected = new object();

        public DualSocket(IPEndPoint communicaitonIPE,
            IPEndPoint streamingIPE,
            string clientId,
            CallbackRecoverSession callbackSessionRecover,
            CallbackConnectionStateUpdate callbackConnectionStateUpdate)
        {
            _streamingIPE = streamingIPE;
            _communicationIPE = communicaitonIPE;

            _communicationIPE = communicaitonIPE;
            _streamingIPE = streamingIPE;
            _reconnectFlag = new ManualResetEvent(false);
            _communicationConnectedFlag = new ManualResetEvent(false);
            _streamingConnectedFlag = new ManualResetEvent(false);
            _clientId = clientId;

            StreamingSSL = new SslClient(_streamingIPE);
            CommunicationSSL = new SslClient(_communicationIPE);

            _connectionTaskStreaming = new Task(ConnectToServerStreaming);
            _connectionTaskCommunication = new Task(ConnectToServerCommunication);
            _connectionTaskStreaming.Start();
            _connectionTaskCommunication.Start();
            RecoverSession = callbackSessionRecover;
            ConnectionStateUpdate = callbackConnectionStateUpdate;
        }

        ~DualSocket()
        {
            StreamingSSL?.Dispose();
            CommunicationSSL?.Dispose();
        }

        public void UpdateDisconnected()
        {
            Connected = false;
        }

        public void ForceDisconnect()
        {
            StreamingSSL.ForceDisconnect();
            CommunicationSSL.ForceDisconnect();
            Connected = false;
        }

        public void Reconnect()
        {
            _communicationConnectedFlag.Reset();
            _streamingConnectedFlag.Reset();
            _reconnectFlag.Set();
            Task.Run(() =>
            {
                _communicationConnectedFlag.WaitOne();
                _streamingConnectedFlag.WaitOne();
                RecoverSession();
            });
        }

        public void Connect()
        {
            if(!Connected) 
            {
                Trace.WriteLine("Hereeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee");
                _communicationConnectedFlag.Reset();
                _streamingConnectedFlag.Reset();
                _reconnectFlag.Set();
                _communicationConnectedFlag.WaitOne();
                _streamingConnectedFlag.WaitOne();
            }
        }

        private void ConnectToServerCommunication()
        {
            while (true)
            {
                _reconnectFlag.WaitOne();
                CommunicationSSL?.Dispose();
                CommunicationSSL = new SslClient(_communicationIPE);
                lock (_lockDisconnected)
                {
                    _communicationConnected = false;
                    if (!_communicationConnected && !_streamingConnected)
                    {
                        Connected = false;
                    }
                }
                while (true)
                {
                    try
                    {
                        CommunicationSSL.Connect(_communicationIPE);
                        CommunicationSSL.InitializeSSL();
                        CommunicationSSL.SendSSL(Encoding.UTF8.GetBytes(_clientId), 6);
                        _communicationConnectedFlag.Set();
                        lock (_lockConnected)
                        {
                            _communicationConnected = true;
                            if (_communicationConnected && _streamingConnected)
                            {
                                Connected = true;
                            }
                        }
                        break;
                    }
                    catch (Exception ex) when (ex is IOException or ExceptionSSL or SocketException) { }
                }
                _reconnectFlag.Reset();
            }
        }

        private void ConnectToServerStreaming()
        {
            while (true)
            {
                _reconnectFlag.WaitOne();
                StreamingSSL?.Dispose();
                StreamingSSL = new SslClient(_streamingIPE);
                lock (_lockDisconnected)
                {
                    _streamingConnected = false;
                    if (!_communicationConnected && !_streamingConnected)
                    {
                        Connected = false;
                    }
                }
                while (true)
                {
                    try
                    {
                        StreamingSSL.Connect(_streamingIPE);
                        StreamingSSL.InitializeSSL();
                        StreamingSSL.SendSSL(Encoding.UTF8.GetBytes(_clientId), 6);
                        _streamingConnectedFlag.Set();
                        lock (_lockConnected)
                        {
                            _streamingConnected = true;
                            if (_communicationConnected && _streamingConnected)
                            {
                                Connected = true;
                            }
                        }
                        break;
                    }
                    catch (Exception ex) when (ex is IOException or ExceptionSSL or SocketException) { }
                }
                _reconnectFlag.Reset();
            }
        }
    }
}
