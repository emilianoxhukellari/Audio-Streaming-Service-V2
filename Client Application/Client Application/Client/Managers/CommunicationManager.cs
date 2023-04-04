using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Printing.IndexedProperties;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Client_Application.Client.Core;
using Client_Application.Client.Network;

namespace Client_Application.Client.Managers
{
    public sealed class CommunicationManager
    {
        private static volatile CommunicationManager? _instance;
        private static readonly object _syncLock = new object();
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
                lock (_syncLock)
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
            else if (networkRequest.Type == NetworkRequestType.RegisterToServer)
            {
                ExecuteRegisterToServer(networkRequest.Parameters);
            }
            else if (networkRequest.Type == NetworkRequestType.Disconnect)
            {
                ExecuteDisconnect(networkRequest.Parameters);
            }
            else if (networkRequest.Type == NetworkRequestType.SynchronizeStart)
            {
                ExecuteSynchronizeStart(networkRequest.Parameters);
            }
            else if (networkRequest.Type == NetworkRequestType.SynchronizeDeletePlaylist)
            {
                ExecuteSyncDeletePlaylist(networkRequest.Parameters);
            }
            else if (networkRequest.Type == NetworkRequestType.SynchronizeDeletePlaylist)
            {
                ExecuteSyncDeletePlaylist(networkRequest.Parameters);
            }
            else if (networkRequest.Type == NetworkRequestType.SynchronizeAddPlaylist)
            {
                ExecuteSyncAddPlaylist(networkRequest.Parameters);
            }
            else if (networkRequest.Type == NetworkRequestType.SynchronizeRenamePlaylist)
            {
                ExecuteSyncRenamePlaylist(networkRequest.Parameters);
            }
            else if (networkRequest.Type == NetworkRequestType.SynchronizeDeleteSong)
            {
                ExecuteSyncDeleteSongFromPlaylist(networkRequest.Parameters);
            }
            else if (networkRequest.Type == NetworkRequestType.SynchronizeAddSong)
            {
                ExecuteSyncAddSongToPlaylist(networkRequest.Parameters);
            }
        }

        private void ExecuteSyncDeletePlaylist(params object[] parameters)
        {
            NetworkResult networkResult = (NetworkResult)parameters[0];
            int playlistId = (int)parameters[1];

            string request = "SYNC_DELETE_PLAYLIST@";
            byte[] requestBytes = Encoding.UTF8.GetBytes(request);
            int requestLength = requestBytes.Length;
            byte[] requestLengthBytes = BitConverter.GetBytes(requestLength);

            _dualSocket.CommunicationSSL.SendSSL(requestLengthBytes, 4);
            _dualSocket.CommunicationSSL.SendSSL(requestBytes, requestLength);
            _dualSocket.CommunicationSSL.SendSSL(BitConverter.GetBytes(playlistId), 4);

            networkResult.UpdateSyncUpdate();
        }

        private void ExecuteSyncAddPlaylist(params object[] parameters)
        {
            NetworkResult networkResult = (NetworkResult)parameters[0];
            string playlistName = (string)parameters[1];

            string request = "SYNC_ADD_PLAYLIST@";
            byte[] requestBytes = Encoding.UTF8.GetBytes(request);
            int requestLength = requestBytes.Length;
            byte[] requestLengthBytes = BitConverter.GetBytes(requestLength);

            _dualSocket.CommunicationSSL.SendSSL(requestLengthBytes, 4);
            _dualSocket.CommunicationSSL.SendSSL(requestBytes, requestLength);

            byte[] playlistNameBytes = Encoding.UTF8.GetBytes(playlistName);
            int playlistNameLength = playlistNameBytes.Length;
            byte[] playlistNameLengthBytes = BitConverter.GetBytes(playlistNameLength);

            _dualSocket.CommunicationSSL.SendSSL(playlistNameLengthBytes, 4);
            _dualSocket.CommunicationSSL.SendSSL(playlistNameBytes, playlistNameLength);

            byte[] playlistIdBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
            int playlistId = BitConverter.ToInt32(playlistIdBytes);

            networkResult.UpdateNewPlaylistId(playlistId);
        }

        private void ExecuteSyncRenamePlaylist(params object[] parameters)
        {
            NetworkResult networkResult = (NetworkResult)parameters[0];
            int playlistId = (int)parameters[1];
            string newName = (string)parameters[2];

            string request = "SYNC_RENAME_PLAYLIST@";
            byte[] requestBytes = Encoding.UTF8.GetBytes(request);
            int requestLength = requestBytes.Length;
            byte[] requestLengthBytes = BitConverter.GetBytes(requestLength);

            _dualSocket.CommunicationSSL.SendSSL(requestLengthBytes, 4);
            _dualSocket.CommunicationSSL.SendSSL(requestBytes, requestLength);

            byte[] playlistIdBytes = BitConverter.GetBytes(playlistId);
            _dualSocket.CommunicationSSL.SendSSL(playlistIdBytes, 4);
            byte[] newNameBytes = Encoding.UTF8.GetBytes(newName);
            int newNameLength = newNameBytes.Length;
            byte[] newNameLengthBytes = BitConverter.GetBytes(newNameLength);
            _dualSocket.CommunicationSSL.SendSSL(newNameLengthBytes, 4);
            _dualSocket.CommunicationSSL.SendSSL(newNameBytes, newNameLength);

            networkResult.UpdateSyncUpdate();
        }

        private void ExecuteSyncDeleteSongFromPlaylist(params object[] parameters)
        {
            NetworkResult networkResult = (NetworkResult)parameters[0];
            int playlistId = (int)parameters[1];
            int songId = (int)parameters[2];

            string request = "SYNC_DELETE_SONG@";
            byte[] requestBytes = Encoding.UTF8.GetBytes(request);
            int requestLength = requestBytes.Length;
            byte[] requestLengthBytes = BitConverter.GetBytes(requestLength);

            _dualSocket.CommunicationSSL.SendSSL(requestLengthBytes, 4);
            _dualSocket.CommunicationSSL.SendSSL(requestBytes, requestLength);

            byte[] playlistIdBytes = BitConverter.GetBytes(playlistId);
            byte[] songIdBytes = BitConverter.GetBytes(songId);
            _dualSocket.CommunicationSSL.SendSSL(playlistIdBytes, 4);
            _dualSocket.CommunicationSSL.SendSSL(songIdBytes, 4);

            networkResult.UpdateSyncUpdate();
        }

        private void ExecuteSyncAddSongToPlaylist(params object[] parameters)
        {
            NetworkResult networkResult = (NetworkResult)parameters[0];
            int playlistId = (int)parameters[1];
            int songId = (int)parameters[2];

            string request = "SYNC_ADD_SONG@";
            byte[] requestBytes = Encoding.UTF8.GetBytes(request);
            int requestLength = requestBytes.Length;
            byte[] requestLengthBytes = BitConverter.GetBytes(requestLength);

            _dualSocket.CommunicationSSL.SendSSL(requestLengthBytes, 4);
            _dualSocket.CommunicationSSL.SendSSL(requestBytes, requestLength);

            byte[] playlistIdBytes = BitConverter.GetBytes(playlistId);
            byte[] songIdBytes = BitConverter.GetBytes(songId);
            _dualSocket.CommunicationSSL.SendSSL(playlistIdBytes, 4);
            _dualSocket.CommunicationSSL.SendSSL(songIdBytes, 4);

            networkResult.UpdateSyncUpdate();
        }

        public SyncDiff SyncStart(List<PlaylistSyncData> playlistsUp)
        {
            NetworkResult networkResult = new NetworkResult(ResultType.SynchronizationStart);
            AddNetworkRequest(NetworkRequestType.SynchronizeStart, networkResult, playlistsUp);
            return networkResult.Wait().SyncDiff!;
        }

        public void SyncRenamePlaylist(int playlistId, string newName)
        {
            NetworkResult networkResult = new NetworkResult(ResultType.SynchronizationUpdate);
            AddNetworkRequest(NetworkRequestType.SynchronizeRenamePlaylist, networkResult, playlistId, newName);
            networkResult.Wait();
        }

        public void SyncDeletePlaylist(int playlistId)
        {
            NetworkResult networkResult = new NetworkResult(ResultType.SynchronizationUpdate);
            AddNetworkRequest(NetworkRequestType.SynchronizeDeletePlaylist, networkResult, playlistId);
            networkResult.Wait();
        }

        public void SyncAddSongToPlaylist(int playlistId, int songId)
        {
            NetworkResult networkResult = new NetworkResult(ResultType.SynchronizationUpdate);
            AddNetworkRequest(NetworkRequestType.SynchronizeAddSong, networkResult, playlistId, songId);
            networkResult.Wait();
        }

        public void SyncDeleteSongFromPlaylist(int playlistId, int songId)
        {
            NetworkResult networkResult = new NetworkResult(ResultType.SynchronizationUpdate);
            AddNetworkRequest(NetworkRequestType.SynchronizeDeleteSong, networkResult, playlistId, songId);
            networkResult.Wait();
        }

        /// <summary>
        /// Returns the id of the new playlist supplied by the server.
        /// </summary>
        /// <param name="playlistName"></param>
        /// <returns></returns>
        public int SyncAddPlaylist(string playlistName)
        {
            NetworkResult networkResult = new NetworkResult(ResultType.SynchronizationUpdate);
            AddNetworkRequest(NetworkRequestType.SynchronizeAddPlaylist, networkResult, playlistName);
            return networkResult.Wait().PlaylistId;
        }

        public bool AuthenticateToServer(string email, string password)
        {
            NetworkResult networkResult = new NetworkResult(ResultType.Authentication);
            AddNetworkRequest(NetworkRequestType.AuthenticateToServer, networkResult, email, password);
            return networkResult.Wait().ValidAuthentication;
        }

        public List<string>? RegisterToServer(string email, string password)
        {
            NetworkResult networkResult = new NetworkResult(ResultType.Registration);
            AddNetworkRequest(NetworkRequestType.RegisterToServer, networkResult, email, password);
            return networkResult.Wait().RegistrationErrors;
        }

        public void DisconnectFromServer()
        {
            NetworkResult networkResult = new NetworkResult(ResultType.Disconnect);
            AddNetworkRequest(NetworkRequestType.Disconnect, networkResult);
            networkResult.Wait();
        }

        public List<Song> SearchSongsServer(string search)
        {
            NetworkResult networkResult = new NetworkResult(ResultType.FoundSongs);
            AddNetworkRequest(NetworkRequestType.SearchSongOrArtist, networkResult, search);
            List<Song>? foundSongs = networkResult.Wait().FoundSongs;

            if (foundSongs != null)
            {
                return foundSongs;
            }
            return new List<Song>(0);
        }

        public void TerminateSongDataReceive()
        {
            AddNetworkRequest(NetworkRequestType.TerminateSongDataReceive);
        }


        private void ReceiveDeletePlaylistsReply(ref SyncDiff diff)
        {
            string deletePlaylistsReply = ReceiveDiffPropertyReply();

            if (deletePlaylistsReply == "NULL")
            {
                diff.DeletePlaylists = null;
            }
            else if (deletePlaylistsReply == "DELETE_PLAYLISTS")
            {
                List<int> deletePlaylists = new(0);
                byte[] playlistIdsCountBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                int playlistIdsCount = BitConverter.ToInt32(playlistIdsCountBytes);

                for (int i = 0; i < playlistIdsCount; i++)
                {
                    deletePlaylists.Add(BitConverter.ToInt32(_dualSocket.CommunicationSSL.ReceiveSSL(4)));
                }

                diff.DeletePlaylists = deletePlaylists;
            }
        }

        private void ReceiveAddPlaylistsReply(ref SyncDiff diff)
        {
            string addPlaylistsReply = ReceiveDiffPropertyReply();

            if (addPlaylistsReply == "NULL")
            {
                diff.AddPlaylists = null;
            }
            else if (addPlaylistsReply == "ADD_PLAYLISTS")
            {
                List<(int playlistId, string playlistName)> addPlaylists = new(0);
                byte[] countBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                int count = BitConverter.ToInt32(countBytes);

                for (int i = 0; i < count; i++)
                {
                    byte[] playlistIdBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                    int playlistId = BitConverter.ToInt32(playlistIdBytes);

                    byte[] playlistNameLengthBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                    int playlistNameLength = BitConverter.ToInt32(playlistNameLengthBytes);

                    byte[] playlistNameBytes = _dualSocket.CommunicationSSL.ReceiveSSL(playlistNameLength);
                    string playlistName = Encoding.UTF8.GetString(playlistNameBytes);

                    addPlaylists.Add((playlistId, playlistName));
                }

                diff.AddPlaylists = addPlaylists;
            }
        }

        private void ReceiveRenamePlaylistsReply(ref SyncDiff diff)
        {
            string renamePlaylistsReply = ReceiveDiffPropertyReply();

            if (renamePlaylistsReply == "NULL")
            {
                diff.RenamePlaylists = null;
            }
            else if (renamePlaylistsReply == "RENAME_PLAYLISTS")
            {
                List<(int playlistId, string newName)> renamePlaylists = new(0);
                byte[] countBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                int count = BitConverter.ToInt32(countBytes);

                for (int i = 0; i < count; i++)
                {
                    byte[] playlistIdBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                    int playlistId = BitConverter.ToInt32(playlistIdBytes);

                    byte[] newNameLengthBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                    int newNameLength = BitConverter.ToInt32(newNameLengthBytes);

                    byte[] newNameBytes = _dualSocket.CommunicationSSL.ReceiveSSL(newNameLength);
                    string newName = Encoding.UTF8.GetString(newNameBytes);

                    renamePlaylists.Add((playlistId, newName));
                }

                diff.RenamePlaylists = renamePlaylists;
            }
        }

        private void ReceiveDeleteSongsReply(ref SyncDiff diff)
        {
            string deleteSongsReply = ReceiveDiffPropertyReply();

            if (deleteSongsReply == "NULL")
            {
                diff.DeleteSongs = null;
            }
            else if (deleteSongsReply == "DELETE_SONGS")
            {
                List<(int playlistId, int songId)> deleteSongs = new(0);
                byte[] countBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                int count = BitConverter.ToInt32(countBytes);

                for (int i = 0; i < count; i++)
                {
                    byte[] playlistIdBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                    int playlistId = BitConverter.ToInt32(playlistIdBytes);
                    byte[] songIdBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                    int songId = BitConverter.ToInt32(songIdBytes);

                    deleteSongs.Add((playlistId, songId));
                }

                diff.DeleteSongs = deleteSongs;
            }
        }

        private void ReceiveAddSongsReply(ref SyncDiff diff)
        {
            string addSongsReply = ReceiveDiffPropertyReply();

            if (addSongsReply == "NULL")
            {
                diff.AddSongs = null;
            }
            else if (addSongsReply == "ADD_SONGS")
            {
                List<(int playlistId, List<Song> songs)> addSongs = new(0);
                byte[] countBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                int count = BitConverter.ToInt32(countBytes);

                for (int i = 0; i < count; i++)
                {
                    List<Song> songs = new(0);
                    byte[] playlistIdBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                    int playlistId = BitConverter.ToInt32(playlistIdBytes);

                    byte[] songsCountBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                    int songsCount = BitConverter.ToInt32(songsCountBytes);
                    for (int j = 0; j < songsCount; j++)
                    {

                        byte[] packetsCountBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                        int packetsCount = BitConverter.ToInt32(packetsCountBytes);

                        byte[] lastPacketLengthBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                        int lastPacketLength = BitConverter.ToInt32(lastPacketLengthBytes);

                        byte[] songBytes = new byte[packetsCount * 1024 + lastPacketLength];
                        byte[] lastPacket = _dualSocket.CommunicationSSL.ReceiveSSL(lastPacketLength);

                        Buffer.BlockCopy(lastPacket, 0, songBytes, songBytes.Length - lastPacketLength, lastPacketLength);

                        int index = 0;
                        for (int x = 0; x < packetsCount - 1; x++)
                        {
                            Buffer.BlockCopy(_dualSocket.CommunicationSSL.ReceiveSSL(1024), 0, songBytes, index, 1024);
                            index += 1024;
                        }
                        songs.Add(new Song(songBytes));
                    }
                    addSongs.Add((playlistId, songs));
                }
                diff.AddSongs = addSongs;
            }
        }

        private void ExecuteSynchronizeStart(params object[] parameters)
        {
            NetworkResult networkResult = (NetworkResult)parameters[0];
            List<PlaylistSyncData> playlistsUpstream = (List<PlaylistSyncData>)parameters[1];

            string request = "SYNC_UP@";
            byte[] requestBytes = Encoding.UTF8.GetBytes(request);

            int requestLength = requestBytes.Length;
            byte[] requestLengthBytes = BitConverter.GetBytes(requestLength);

            _dualSocket.CommunicationSSL.SendSSL(requestLengthBytes, 4);
            _dualSocket.CommunicationSSL.SendSSL(requestBytes, requestLength);

            int numberOfPlaylists = playlistsUpstream.Count;
            byte[] numberOfPlaylistsBytes = BitConverter.GetBytes(numberOfPlaylists);
            _dualSocket.CommunicationSSL.SendSSL(numberOfPlaylistsBytes, 4);

            for (int i = 0; i < numberOfPlaylists; i++)
            {
                byte[] playlistIdBytes = BitConverter.GetBytes(playlistsUpstream[i].PlaylistId);
                _dualSocket.CommunicationSSL.SendSSL(playlistIdBytes, 4);

                byte[] playlistNameBytes = Encoding.UTF8.GetBytes(playlistsUpstream[i].PlaylistName);
                int playlistNameLength = playlistNameBytes.Length;
                byte[] playlistNameLengthBytes = BitConverter.GetBytes(playlistNameLength);
                _dualSocket.CommunicationSSL.SendSSL(playlistNameLengthBytes, 4);
                _dualSocket.CommunicationSSL.SendSSL(playlistNameBytes, playlistNameLength);


                int numberOfSongs = playlistsUpstream[i].SongIds.Count;
                byte[] numberOfSongsBytes = BitConverter.GetBytes(numberOfSongs);
                _dualSocket.CommunicationSSL.SendSSL(numberOfSongsBytes, 4);

                for (int j = 0; j < numberOfSongs; j++)
                {
                    int songId = playlistsUpstream[i].SongIds[j];
                    byte[] songIdBytes = BitConverter.GetBytes(songId);

                    _dualSocket.CommunicationSSL.SendSSL(songIdBytes, 4);
                }
            }

            SyncDiff diff = new SyncDiff();

            ReceiveDeletePlaylistsReply(ref diff);
            ReceiveAddPlaylistsReply(ref diff);
            ReceiveRenamePlaylistsReply(ref diff);
            ReceiveDeleteSongsReply(ref diff);
            ReceiveAddSongsReply(ref diff);

            networkResult.UpdateSyncDiff(diff);
        }

        private string ReceiveDiffPropertyReply()
        {
            byte[] replyLengthBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
            int replyLength = BitConverter.ToInt32(replyLengthBytes);
            byte[] replyBytes = _dualSocket.CommunicationSSL.ReceiveSSL(replyLength);
            return Encoding.UTF8.GetString(replyBytes);
        }

        private void ExecuteRegisterToServer(params object[] parameters)
        {
            NetworkResult networkResult = (NetworkResult)parameters[0];
            string email = (string)parameters[1];
            string password = (string)parameters[2];
            
            try
            {
                string request = "REGISTER@";
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

                byte[] errorCountBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                int errorCount = BitConverter.ToInt32(errorCountBytes);

                if (errorCount == 0)
                {
                    networkResult.UpdateRegisterResult(null); // No errors, successful registration
                }
                else if (errorCount > 0) // Errors were found, unsuccessful registration
                {
                    List<string> errors = new List<string>(errorCount);
                    for (int i = 0; i < errorCount; i++)
                    {
                        byte[] errorLengthBytes = _dualSocket.CommunicationSSL.ReceiveSSL(4);
                        int errorLength = BitConverter.ToInt32(errorLengthBytes);
                        byte[] errorBytes = _dualSocket.CommunicationSSL.ReceiveSSL(errorLength);
                        string error = Encoding.UTF8.GetString(errorBytes);
                        errors.Add(error);
                    }
                    networkResult.UpdateRegisterResult(errors);
                }
            }
            catch (Exception ex) when (ex is IOException or ExceptionSSL or SocketException)
            {
                _dualSocket.Reconnect();
                networkResult.UpdateRegisterResult(new List<string> { "An error occurred." });
            }
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
                    _dualSocket.UpdateDisconnected();
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
