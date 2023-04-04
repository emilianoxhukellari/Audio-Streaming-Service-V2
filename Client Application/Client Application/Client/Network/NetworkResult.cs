using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Client_Application.Client.Core;

namespace Client_Application.Client.Network
{
    public enum ResultType
    {
        Authentication,
        Registration,
        Disconnect,
        FoundSongs,
        SynchronizationStart,
        SynchronizationUpdate
    }
    public sealed class SyncDiff
    {
        public List<int>? DeletePlaylists;
        public List<(int playlistId, string playlistName)>? AddPlaylists;
        public List<(int playlistId, string newName)>? RenamePlaylists;
        public List<(int playlistId, int songId)>? DeleteSongs;
        public List<(int playlistId, List<Song> songs)>? AddSongs;
    }

    public class Result
    {
        public bool ValidAuthentication { get; set; }
        public List<string>? RegistrationErrors { get; set; }
        public bool Disconnected { get; set; }
        public int PlaylistId { get; set; }
        public SyncDiff? SyncDiff { get; set; }
        public List<Song>? FoundSongs { get; set; }

    }
    public sealed class NetworkResult
    {
        private AutoResetEvent _event = new AutoResetEvent(false);
        private Result _result;
        public ResultType ResultType { get; }
        private object _lock = new();

        public NetworkResult(ResultType resultType)
        {
            ResultType = resultType;
            _result = new Result();
            _event.Reset();
        }

        public void UpdateSyncUpdate()
        {
            _event.Set();
        }

        public void UpdateSyncDiff(SyncDiff syncDiff)
        {
            lock (_lock)
            {
                _result.SyncDiff = syncDiff;
                _event.Set();
            }
        }

        public void UpdateNewPlaylistId(int playlistId)
        {
            lock (_lock)
            {
                _result.PlaylistId = playlistId;
                _event.Set();
            }
        }

        public void UpdateRegisterResult(List<string>? errors)
        {
            lock (_lock)
            {
                _result.RegistrationErrors = errors;
                _event.Set();   
            }
        }
        public void UpdateAuthenticationResult(bool success)
        {
            lock (_lock)
            {
                _result.ValidAuthentication = success;
                _event.Set();
            }
        }

        public void UpdateDisconnectResult(bool success)
        {
            lock (_lock)
            {
                _result.Disconnected = success;
                _event.Set();
            }
        }

        public void UpdateFoundSongs(List<Song> foundSongs)
        {
            lock (_lock)
            {
                _result.FoundSongs = foundSongs;
                _event.Set();
            }
        }

        public Result Wait()
        {
            _event.WaitOne();
            lock (_lock)
            {
                return _result;
            }
        }
    }
}
