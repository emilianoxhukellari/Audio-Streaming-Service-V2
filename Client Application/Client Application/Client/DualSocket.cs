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

namespace Client_Application.Client
{

    /// <summary>
    /// This class represents a dual socket design. One socket is dedicated to MediaPlayer, and the other to Controller.
    /// If the Dual Socket receives a Reconnect() command, it will reconnect both sockets.
    /// </summary>
    /// 
    public sealed class DualSocket
    {
        public SslClient MediaPlayerStreamSSL { get; private set; }
        public SslClient ControllerStreamSSL { get; private set; }
        public bool Connected
        {
            get { return _connected; }
            private set { _connected = value; ConnectionStateUpdate(_connected); }
        }

        private volatile bool _connected;
        private volatile bool _controllerConnected;
        private volatile bool _mediaPlayerConnected;
        private readonly ManualResetEvent _reconnectFlag;
        private readonly ManualResetEvent _controllerConnectedFlag;
        private readonly ManualResetEvent _mediaPlayerConnectedFlag;
        private readonly string _clientId;
        private readonly IPEndPoint _controllerIPE;
        private readonly IPEndPoint _mediaPlayerIPE;
        private readonly Task _connectionTaskController;
        private readonly Task _connectionTaskMediaPlayer;
        private readonly CallbackRecoverSession RecoverSession;
        private readonly CallbackConnectionStateUpdate ConnectionStateUpdate;
        private readonly object _lockDisconnected = new object();
        private readonly object _lockConnected = new object();

        public DualSocket(IPEndPoint controllerIPE, 
            IPEndPoint mediaPlayerIPE, 
            string clientId, 
            CallbackRecoverSession callbackSessionRecover,
            CallbackConnectionStateUpdate callbackConnectionStateUpdate)
        {
            _mediaPlayerIPE = mediaPlayerIPE;
            _controllerIPE = controllerIPE;

            _controllerIPE = controllerIPE;
            _mediaPlayerIPE = mediaPlayerIPE;
            _reconnectFlag = new ManualResetEvent(false);
            _controllerConnectedFlag = new ManualResetEvent(false);
            _mediaPlayerConnectedFlag = new ManualResetEvent(false);
            _clientId = clientId;

            MediaPlayerStreamSSL = new SslClient(_mediaPlayerIPE);
            ControllerStreamSSL = new SslClient(_controllerIPE);

            _connectionTaskMediaPlayer = new Task(ConnectToServerMediaPlayer);
            _connectionTaskController = new Task(ConnectToServerController);
            _connectionTaskMediaPlayer.Start();
            _connectionTaskController.Start();
            RecoverSession = callbackSessionRecover;
            ConnectionStateUpdate = callbackConnectionStateUpdate;
            _reconnectFlag.Set();
        }

        ~DualSocket()
        {
            MediaPlayerStreamSSL?.Dispose();
            ControllerStreamSSL?.Dispose();    
        }

        public void ForceDisconnect()
        {
            MediaPlayerStreamSSL.ForceDisconnect();
            ControllerStreamSSL.ForceDisconnect();
        }

        public void Reconnect()
        {
            _controllerConnectedFlag.Reset();
            _mediaPlayerConnectedFlag.Reset();
            _reconnectFlag.Set();
            new Task(() =>
            {
                _controllerConnectedFlag.WaitOne();
                _mediaPlayerConnectedFlag.WaitOne();
                RecoverSession();
            }).Start();
        }

        public void Connect()
        {
            _controllerConnectedFlag.Reset();
            _mediaPlayerConnectedFlag.Reset();
            _reconnectFlag.Set();
            _controllerConnectedFlag.WaitOne();
            _mediaPlayerConnectedFlag.WaitOne();
        }

        private void ConnectToServerController()
        {
            while (true)
            {
                _reconnectFlag.WaitOne();
                ControllerStreamSSL?.Dispose();
                ControllerStreamSSL = new SslClient(_controllerIPE);

                lock (_lockDisconnected)
                {
                    _controllerConnected = false;
                    if(!_controllerConnected && !_mediaPlayerConnected)
                    {
                        Trace.WriteLine("DISCONNECTED");
                        Connected = false;
                    }
                }

                while (true)
                {
                    try
                    {
                        ControllerStreamSSL.Connect(_controllerIPE);
                        ControllerStreamSSL.InitializeSSL();
                        ControllerStreamSSL.SendSSL(Encoding.UTF8.GetBytes(_clientId), 6);
                        _controllerConnectedFlag.Set();
                        lock(_lockConnected)
                        {
                            _controllerConnected = true;
                            if(_controllerConnected && _mediaPlayerConnected)
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

        private void ConnectToServerMediaPlayer()
        {
            while (true)
            {
                _reconnectFlag.WaitOne();
                MediaPlayerStreamSSL?.Dispose();
                MediaPlayerStreamSSL = new SslClient(_mediaPlayerIPE);
                lock (_lockDisconnected)
                {
                    _mediaPlayerConnected = false;
                    if (!_controllerConnected && !_mediaPlayerConnected)
                    {
                        Trace.WriteLine("DISCONNECTED");
                        Connected = false;
                    }
                }

                while (true)
                {
                    try
                    {
                        MediaPlayerStreamSSL.Connect(_mediaPlayerIPE);
                        MediaPlayerStreamSSL.InitializeSSL();
                        MediaPlayerStreamSSL.SendSSL(Encoding.UTF8.GetBytes(_clientId), 6);
                        _mediaPlayerConnectedFlag.Set();
                        lock (_lockConnected)
                        {
                            _mediaPlayerConnected = true;
                            if (_controllerConnected && _mediaPlayerConnected)
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
