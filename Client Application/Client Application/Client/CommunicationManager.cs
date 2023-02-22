using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client_Application.Client
{
    public sealed class CommunicationManager
    {
        private static volatile CommunicationManager? _instance;
        private static readonly object syncLock = new object();
        private static readonly ManualResetEvent _isInitialized = new ManualResetEvent(false);
        private readonly Thread _communicationThread;
        private readonly Queue<NetworkRequest> _networkRequestQueue;
        private readonly AutoResetEvent _newNetworkRequestFlag;
        private readonly DualSocket _dualSocket;

        private CommunicationManager(DualSocket dualSocket)
        {
            _networkRequestQueue = new Queue<NetworkRequest>();
            _newNetworkRequestFlag = new AutoResetEvent(false);
            _communicationThread = new Thread(CommunicationLoop);
            _communicationThread.IsBackground = true;
            _dualSocket = dualSocket;

            _communicationThread.Start();
        }

        public static void WaitForInstance()
        {
            _isInitialized.WaitOne();
        }

        public static CommunicationManager InitializeSingleton(DualSocket dualSocket)
        {
            if (_instance == null)
            {
                lock (syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new CommunicationManager(dualSocket);
                        _isInitialized.Set();
                    }
                }
            }
            return _instance;
        }

        public static CommunicationManager GetInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }
            else
            {
                throw new InvalidOperationException("Communication Manager is not Initialized.");
            }
        }

        private void AddNetworkRequest(params object[] parameters)
        {
            _networkRequestQueue.Enqueue(new NetworkRequest(parameters));
            _newNetworkRequestFlag.Set();
        }

        private void CommunicationLoop()
        {
            while (true)
            {
                if (_networkRequestQueue.Count == 0)
                {
                    _newNetworkRequestFlag.WaitOne();
                }

                NetworkRequest? networkRequest;

                if (_networkRequestQueue.TryDequeue(out networkRequest))
                {
                    ExecuteNetworkRequest(networkRequest);
                }
            }
        }


        private void ExecuteNetworkRequest(NetworkRequest networkRequest)
        {
            if (networkRequest.Type == NetworkRequestType.SearchSongOrArtist)
            {
                ExecuteSearchRequest(networkRequest.Parameters);
            }
            else if (networkRequest.Type == NetworkRequestType.TerminateSongDataReceive)
            {
                ExecuteTerminateSongDataReceiveRequest();
            }
            else if (networkRequest.Type == NetworkRequestType.AuthenticateToServer)
            {
                ExecuteAuthenticateToServer(networkRequest.Parameters);
            }
            else if (networkRequest.Type == NetworkRequestType.Disconnect)
            {
                ExecuteDisconnect(networkRequest.Parameters);
            }
        }

        public bool AuthenticateToServer(string email, string password)
        {
            NetworkResult networkResult = new NetworkResult(ResultType.Authentication);
            AddNetworkRequest(NetworkRequestType.AuthenticateToServer, networkResult, email, password);
            return networkResult.Wait().ValidAuthentication;
        }

        public void DisconnectFromServer()
        {
            NetworkResult networkResult = new NetworkResult(ResultType.Disconnect);
            AddNetworkRequest(NetworkRequestType.Disconnect, networkResult);
            networkResult.Wait();
        }

        public List<Song> SearchSongOrArtist(string search)
        {
            NetworkResult networkResult = new NetworkResult(ResultType.FoundSongs);
            AddNetworkRequest(NetworkRequestType.SearchSongOrArtist, networkResult, search);
            List<Song>? foundSongs = networkResult.Wait().FoundSongs;

            if(foundSongs != null)
            {
                return foundSongs;
            }
            return new List<Song>(0);
        }

        public void TerminateSongDataReceive()
        {
            AddNetworkRequest(NetworkRequestType.TerminateSongDataReceive);
        }

        private void ExecuteAuthenticateToServer(params object[] parameters)
        {
            NetworkResult networkResult = (NetworkResult)parameters[0];
            string email = (string)parameters[1];
            string password = (string)parameters[2];

            try
            {
                string request = "LOGIN@";
                byte[] requestBytes = Encoding.UTF8.GetBytes(request);

                int requestLength = requestBytes.Length;
                byte[] requestLengthBytes = BitConverter.GetBytes(requestLength);

                _dualSocket.CommunicationSSL.SendSSL(requestLengthBytes, 4);
                _dualSocket.CommunicationSSL.SendSSL(requestBytes, requestLength);

                byte[] emailBytes = Encoding.UTF8.GetBytes(email);
                int emailLength = emailBytes.Length;
                byte[] emailLengthBytes = BitConverter.GetBytes(emailLength);
                _dualSocket.CommunicationSSL.SendSSL(emailLengthBytes, 4);
                _dualSocket.CommunicationSSL.SendSSL(emailBytes, emailLength);

                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                int passwordLength = passwordBytes.Length;
                byte[] passwordLengthBytes = BitConverter.GetBytes(passwordLength);
                _dualSocket.CommunicationSSL.SendSSL(passwordLengthBytes, 4);
                _dualSocket.CommunicationSSL.SendSSL(passwordBytes, passwordLength);

                byte[] replyLengthBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                int replyLength = BitConverter.ToInt32(replyLengthBytes);
                byte[] replyBytes = _dualSocket.CommunicationSSL.ReceiveSSL(replyLength);
                string reply = Encoding.UTF8.GetString(replyBytes);

                if (reply == "VALID")
                {
                    networkResult.UpdateAuthenticationResult(true);
                }
                else if (reply == "INVALID")
                {
                    networkResult.UpdateAuthenticationResult(false);
                }
            }
            catch (Exception ex) when (ex is IOException or ExceptionSSL or SocketException)
            {
                _dualSocket.Reconnect();
                networkResult.UpdateAuthenticationResult(false);
            }
        }

        private void ExecuteDisconnect(params object[] parameters)
        {
            NetworkResult networkResult = (NetworkResult)parameters[0];

            string reply = "";

            try
            {
                string request = "DISCONNECT@";
                byte[] requestBytes = Encoding.UTF8.GetBytes(request);

                int length = requestBytes.Length;
                byte[] lengthBytes = BitConverter.GetBytes(length);

                _dualSocket.CommunicationSSL.SetWriteTimeout(1000);
                _dualSocket.CommunicationSSL.SendSSL(lengthBytes, 4);
                _dualSocket.CommunicationSSL.SendSSL(requestBytes, length);

                _dualSocket.CommunicationSSL.SetReadTimeout(1000);
                byte[] replyLengthBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                int replyLength = BitConverter.ToInt32(replyLengthBytes);
                byte[] replyBytes = _dualSocket.CommunicationSSL.ReceiveSSL(replyLength);
                reply = Encoding.UTF8.GetString(replyBytes);

                byte[] ackBytes = Encoding.UTF8.GetBytes("ACK");
                int ackLength = ackBytes.Length;
                byte[] ackLengthBytes = BitConverter.GetBytes(ackLength);
                _dualSocket.CommunicationSSL.SendSSL(ackLengthBytes, 4);
                _dualSocket.CommunicationSSL.SendSSL(ackBytes, ackLength);
            }
            catch { }

            finally
            {
                _dualSocket.CommunicationSSL.ResetWriteTimeout();
                _dualSocket.CommunicationSSL.ResetReadTimeout();
                if (reply == "OK")
                {
                    networkResult.UpdateDisconnectResult(true);
                }
                else
                {
                    _dualSocket.ForceDisconnect();
                    networkResult.UpdateDisconnectResult(true);
                }
            }
        }

        private void ExecuteTerminateSongDataReceiveRequest()
        {
            string request = "TERMINATE_SONG_DATA_RECEIVE@";
            byte[] requestBytes = Encoding.UTF8.GetBytes(request);

            int length = requestBytes.Length;
            byte[] lengthBytes = BitConverter.GetBytes(length);

            try
            {
                _dualSocket.CommunicationSSL.SendSSL(lengthBytes, 4);
                _dualSocket.CommunicationSSL.SendSSL(requestBytes, length);
            }
            catch (Exception ex) when (ex is IOException or ExceptionSSL or SocketException)
            {
                _dualSocket.Reconnect();
            }
        }

        private void ExecuteSearchRequest(object[] parameters)
        {
            NetworkResult networkResult = (NetworkResult)parameters[0];
            string searchString = (string)parameters[1];
            string searchSongSerialized = string.Concat(searchString.Where(c => !char.IsWhiteSpace(c)));
            List<Song> foundSongs = new List<Song>(0);

            if (searchSongSerialized == "")
            {
                networkResult.UpdateFoundSongs(foundSongs);
            }

            else
            {
                try
                {
                    string request = $"SEARCH@{searchSongSerialized}";
                    byte[] requestBytes = Encoding.UTF8.GetBytes(request);

                    int length = requestBytes.Length;
                    byte[] lengthBytes = BitConverter.GetBytes(length);

                    _dualSocket.CommunicationSSL.SendSSL(lengthBytes, 4);
                    _dualSocket.CommunicationSSL.SendSSL(requestBytes, length);

                    byte[] numberOfSongsBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);

                    int numberOfSongs = BitConverter.ToInt32(numberOfSongsBytes);

                    for (int i = 0; i < numberOfSongs; i++)
                    {

                        byte[] packetsCountBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);

                        int packetsCount = BitConverter.ToInt32(packetsCountBytes);


                        byte[] lastPacketLengthBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4); // This is the last packet which is likely to be less than 1024 bytes
                        int lastPacketLength = BitConverter.ToInt32(lastPacketLengthBytes);

                        byte[] songBytes = new byte[packetsCount * 1024 + lastPacketLength]; // Full packets + last packet
                        byte[] lastPacket = _dualSocket.CommunicationSSL.ReceiveSSL(lastPacketLength);

                        Buffer.BlockCopy(lastPacket, 0, songBytes, songBytes.Length - lastPacketLength, lastPacketLength); // Add last packet at the end 

                        int index = 0;

                        for (int j = 0; j < packetsCount - 1; j++)
                        {
                            Buffer.BlockCopy(_dualSocket.CommunicationSSL.ReceiveSSL(1024), 0, songBytes, index, 1024);
                            index += 1024;
                        }
                        foundSongs.Add(new Song(songBytes));
                    }
                }
                catch (Exception ex) when (ex is IOException or ExceptionSSL or SocketException)
                {
                    _dualSocket.Reconnect();
                }
                finally
                {
                    networkResult.UpdateFoundSongs(foundSongs);
                }
            }
        }
    }
}
