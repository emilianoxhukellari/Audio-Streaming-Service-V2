using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client_Application.Client
{
    
    public sealed class PlaylistManager
    {
        private static volatile PlaylistManager? _instance;
        private static readonly object syncLock = new object();
        private PlaylistManager()
        {

        }

        public static PlaylistManager InitializeSingleton()
        {
            if (_instance == null)
            {
                lock (syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new PlaylistManager();
                    }
                }
            }
            return _instance;
        }

        public static PlaylistManager GetInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }
            else
            {
                throw new InvalidOperationException("Playlist Manager is not Initialized.");
            }
        }


    }
}
