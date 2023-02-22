using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client_Application.Client
{
    public enum ResultType
    {
        Authentication,
        Disconnect,
        FoundSongs
    }
    public class Result
    {
        public bool ValidAuthentication { get; set; }
        public bool Disconnected { get; set; }
        public  List<Song>? FoundSongs { get; set; }
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
        
        public void UpdateAuthenticationResult(bool success)
        {
            lock(_lock)
            {
                _result.ValidAuthentication = success;
                _event.Set();
            }
        }

        public void UpdateDisconnectResult(bool success)
        {
            lock(_lock)
            {
                _result.Disconnected = success;
                _event.Set();
            }
        }

        public void UpdateFoundSongs(List<Song> foundSongs)
        {
            lock(_lock)
            {
                _result.FoundSongs = foundSongs;
                _event.Set();
            }
        }

        public Result Wait()
        {
            _event.WaitOne();
            lock(_lock)
            {
                return _result;
            }
        }
    }
}
