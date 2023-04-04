using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Client_Application.Client.Network
{
    public sealed class SslClient : IDisposable
    {
        private readonly Socket? _socket;
        private NetworkStream? _networkStream;
        private SslStream? _sslStream;
        private readonly IPEndPoint _iPEndPoint;
        private readonly string _HASH = Config.Config.GetCertificateHash();
        public bool IsReadyForSSL { get; private set; }
        public bool IsSocketConnected { get; set; }


        public SslClient(IPEndPoint iPEndPoint)
        {
            _iPEndPoint = iPEndPoint;
            _socket = new Socket(_iPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        ~SslClient()
        {
            Dispose();
        }

        public void SetReadTimeout(int timeout)
        {
            if (_sslStream != null)
            {
                _sslStream.ReadTimeout = timeout;
            }
        }

        public void SetWriteTimeout(int timeout)
        {
            if (_sslStream != null)
            {
                _sslStream.WriteTimeout = timeout;
            }
        }

        public void ResetReadTimeout()
        {
            if (_sslStream != null)
            {
                _sslStream.ReadTimeout = Timeout.Infinite;
            }
        }

        public void ResetWriteTimeout()
        {
            if (_sslStream != null)
            {
                _sslStream.WriteTimeout = Timeout.Infinite;
            }
        }

        public SslStream? GetSslStream()
        {
            if (IsReadyForSSL)
            {
                return _sslStream;
            }
            else
            {
                return null;
            }
        }

        public byte[] ReceiveSSL(int size)
        {
            if (!IsReadyForSSL || _sslStream == null)
            {
                throw new ExceptionSSL("SSL not ready for receive.");
            }

            byte[] packet = new byte[size];
            int bytesReceived = 0;
            int x;
            while (bytesReceived < size)
            {
                byte[] buffer = new byte[size - bytesReceived];
                x = _sslStream!.Read(buffer);
                Buffer.BlockCopy(buffer, 0, packet, bytesReceived, x);
                bytesReceived += x;
            }
            return packet;
        }

        public void SendSSL(byte[] data, int size)
        {
            if (!IsReadyForSSL || _sslStream == null)
            {
                throw new ExceptionSSL("SSL not ready for send.");
            }
            _sslStream.Write(data, 0, size);
        }

        public void Connect(IPEndPoint iPEndPoint)
        {
            if (_socket != null)
            {
                _socket.Connect(iPEndPoint);
                IsSocketConnected = true;
            }
            else
            {
                throw new Exception("Not Connected");
            }
        }

        public void InitializeSSL()
        {
            if (IsSocketConnected && _socket != null)
            {
                _networkStream = new NetworkStream(_socket, true);
                _sslStream = new SslStream(_networkStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate!), null);
                _sslStream.AuthenticateAsClient(_iPEndPoint.Address.ToString());
                IsReadyForSSL = true;
            }
            else
            {
                throw new Exception("Internal Socket is not connected");
            }
        }

        public void Dispose()
        {
            IsSocketConnected = false;
            IsReadyForSSL = false;
            _sslStream?.Dispose();
            _networkStream?.Dispose();
            _socket?.Dispose();
        }

        public void ForceDisconnect()
        {
            _sslStream?.Close();
            Dispose();
        }

        public void GracefulDisconnect()
        {
            Dispose();
        }

        public bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (certificate.GetCertHashString() == _HASH)
            {
                return true;
            }

            return false;
        }
    }
}
