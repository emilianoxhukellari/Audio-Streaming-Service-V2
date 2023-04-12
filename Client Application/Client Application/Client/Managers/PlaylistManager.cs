using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Client_Application.Client.Core;
using Client_Application.Client.Network;

namespace Client_Application.Client.Managers
{
    /// <summary>
    /// This class represents a playlist with the necessary data for synchronization.
    /// </summary>
    public sealed class PlaylistSyncData
    {
        public int PlaylistId { get; private set; }
        public string PlaylistName { get; set; }
        public List<int> SongIds { get; set; }

        public int NumberOfSongs
        {
            get { return SongIds.Count; }
        }
        public PlaylistSyncData(int playlistId, string playlistName, List<int> songIds)
        {
            PlaylistId = playlistId;
            PlaylistName = playlistName;
            SongIds = songIds;
        }
    }

    /// <summary>
    /// This class represents the data of local playlists.
    /// </summary>
    public sealed class PlaylistData
    {
        public int PlaylistId { get; set; }

        public PlaylistData(int playlistId)
        {
            PlaylistId = playlistId;
        }

        public PlaylistData(byte[] serialized)
        {
            PlaylistId = BitConverter.ToInt32(serialized);
        }
        public byte[] GetBytes()
        {
            byte[] data = BitConverter.GetBytes(PlaylistId);
            return data;
        }

    }
    public enum PlaylistResult
    {
        Success,
        Error,
        AlreadyExists,
        None,
        EmptyName,
        SongAlreadyInPlaylist
    }

    /// <summary>
    /// This class represents a playlist manager. 
    /// </summary>
    public sealed class PlaylistManager
    {
        private static volatile PlaylistManager? _instance;
        private static readonly object _syncLock = new object();
        private static readonly ManualResetEvent _isInitialized = new ManualResetEvent(false);

        private readonly string _playlistsRelateivePath;
        private readonly CommunicationManager _communicationManager;
        public string CurrentPlaylist { get; set; }

        private PlaylistManager()
        {
            _playlistsRelateivePath = Config.Config.GetPlaylistsRelativePath();
            _communicationManager = CommunicationManager.GetInstance();
            CurrentPlaylist = "";
        }

        /// <summary>
        /// Wait until the singleton is created.
        /// </summary>
        public static void WaitForInstance()
        {
            _isInitialized.WaitOne();
        }

        /// <summary>
        /// Initialize the singleton. This method is thread-safe.
        /// </summary>
        /// <returns></returns>
        public static PlaylistManager InitializeSingleton()
        {
            if (_instance == null)
            {
                lock (_syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new PlaylistManager();
                        _isInitialized.Set();
                    }
                }
            }
            return _instance;
        }

        /// <summary>
        /// Get the singleton instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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

        /// <summary>
        /// Synchronize all playlists from server.
        /// </summary>
        /// <returns></returns>
        public PlaylistResult Sync()
        {
            {
                string[] playlists = GetPlaylistLinks();
                List<PlaylistSyncData> playlistsUp = new List<PlaylistSyncData>();

                foreach (string playlist in playlists)
                {
                    List<Song> songs = GetPlaylistSongs(playlist);
                    List<int> songIds = songs.Select(song => song.SongId).ToList();
                    playlistsUp.Add(new PlaylistSyncData(GetPlaylistId(playlist), playlist, songIds));
                }

                SyncDiff syncDiff = _communicationManager.SyncStart(playlistsUp);

                if (syncDiff.DeletePlaylists != null)
                {
                    foreach (var element in syncDiff.DeletePlaylists)
                    {
                        DeletePlaylist(GetPlaylistNameFromId(element), false);
                    }
                }

                if (syncDiff.AddPlaylists != null)
                {
                    foreach (var element in syncDiff.AddPlaylists)
                    {
                        string name = element.playlistName;

                        while(true)
                        {
                            PlaylistResult result = CreateNewPlaylist(element.playlistId, name, false);

                            if (result == PlaylistResult.AlreadyExists)
                            {
                                name += "@";
                            }
                            else
                            {
                                if(name != element.playlistName)
                                {
                                    _communicationManager.SyncRenamePlaylist(element.playlistId, name);
                                }
                                break;
                            }
                        }
                    }
                }

                if (syncDiff.RenamePlaylists != null)
                {
                    foreach (var element in syncDiff.RenamePlaylists)
                    {
                        RenamePlaylist(element.playlistId, element.newName, false);
                    }
                }

                if (syncDiff.DeleteSongs != null)
                {
                    foreach (var element in syncDiff.DeleteSongs)
                    {
                        RemoveSongFromPlaylist(element.songId, element.playlistId, false);
                    }
                }

                if (syncDiff.AddSongs != null)
                {
                    foreach (var element in syncDiff.AddSongs)
                    {
                        string playlistName = GetPlaylistNameFromId(element.playlistId);
                        foreach (var song in element.songs)
                        {
                            AddSongToPlaylist(song, playlistName, false);
                        }
                    }
                }

                return PlaylistResult.Success;
            }
        }

        public PlaylistResult RemoveSongFromPlaylist(int songId, int playlistId, bool syncWithServer = true)
        {
            string playlistName = GetPlaylistNameFromId(playlistId);
            List<Song> songs = GetPlaylistSongs(playlistName);
            foreach (var song in songs)
            {
                if (song.SongId == songId)
                {
                    RemoveSongFromPlaylist(song, playlistName);
                    if (syncWithServer)
                    {
                        _communicationManager.SyncDeleteSongFromPlaylist(playlistId, songId);
                    }
                    return PlaylistResult.Success;
                }
            }
            return PlaylistResult.Error;
        }

        /// <summary>
        /// Get the names of all local playlists.
        /// </summary>
        /// <returns></returns>
        public string[] GetPlaylistLinks()
        {
            return Directory.GetDirectories(_playlistsRelateivePath).Select(d => Path.GetRelativePath(_playlistsRelateivePath, d)).ToArray();
        }

        private int GetPlaylistId(string playlistLink)
        {
            byte[] dataBytes = File.ReadAllBytes(@$"{_playlistsRelateivePath}{playlistLink}\playlist_data.bytes");
            return new PlaylistData(dataBytes).PlaylistId;
        }

        private void WritePlaylistData(string playlistLink, int playlistId)
        {
            File.WriteAllBytes(@$"{_playlistsRelateivePath}{playlistLink}\playlist_data.bytes", new PlaylistData(playlistId).GetBytes());
        }

        private string GetPlaylistNameFromId(int id)
        {
            foreach (var playlist in GetPlaylistLinks())
            {
                if (GetPlaylistId(playlist) == id)
                {
                    return playlist;
                }
            }
            return "";
        }

        /// <summary>
        /// Create a new playlist with playlistName.
        /// </summary>
        /// <param name="playlistName"></param>
        /// <param name="syncWithServer"></param>
        /// <returns></returns>
        public PlaylistResult CreateNewPlaylist(string playlistName, bool syncWithServer = true)
        {
            if (playlistName != "")
            {
                if (Directory.Exists($"{_playlistsRelateivePath}{playlistName}"))
                {
                    return PlaylistResult.AlreadyExists;
                }
                else
                {
                    Directory.CreateDirectory($"{_playlistsRelateivePath}{playlistName}");
                    if (syncWithServer)
                    {
                        int id = _communicationManager.SyncAddPlaylist(playlistName);
                        WritePlaylistData(playlistName, id);
                    }
                    CurrentPlaylist = playlistName;
                    return PlaylistResult.Success;
                }
            }
            return PlaylistResult.EmptyName;
        }

        private PlaylistResult CreateNewPlaylist(int playlistId, string playlistName, bool syncWithServer = true)
        {
            if (playlistName != "")
            {

                if (Directory.Exists($"{_playlistsRelateivePath}{playlistName}"))
                {
                    return PlaylistResult.AlreadyExists;
                }
                else
                {
                    Directory.CreateDirectory($"{_playlistsRelateivePath}{playlistName}");
                    if (syncWithServer)
                    {
                        playlistId = _communicationManager.SyncAddPlaylist(playlistName);
                    }
                    WritePlaylistData(playlistName, playlistId);
                    return PlaylistResult.Success;
                }
            }
            return PlaylistResult.EmptyName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newName"></param>
        /// <param name="syncWithServer"></param>
        /// <returns></returns>
        public PlaylistResult RenameCurrentPlaylist(string newName, bool syncWithServer = true)
        {
            string source = $"{_playlistsRelateivePath}{CurrentPlaylist}";
            string destination = $"{_playlistsRelateivePath}{newName}";
            if (!Directory.Exists(destination))
            {
                Directory.Move(source, destination);
                CurrentPlaylist = newName;
                if (syncWithServer)
                {
                    _communicationManager.SyncRenamePlaylist(GetPlaylistId(CurrentPlaylist), newName);
                }
                return PlaylistResult.Success;
            }
            else
            {
                return PlaylistResult.AlreadyExists;
            }
        }

        public PlaylistResult RenamePlaylist(int playlistId, string newName, bool syncWithServer = true)
        {
            string playlistName = GetPlaylistNameFromId(playlistId);
            string source = $"{_playlistsRelateivePath}{playlistName}";
            string destination = $"{_playlistsRelateivePath}{newName}";
            if (!Directory.Exists(destination) && Directory.Exists(source))
            {
                Directory.Move(source, destination);
                CurrentPlaylist = newName;
                if (syncWithServer)
                {
                    _communicationManager.SyncRenamePlaylist(playlistId, newName);
                }
                return PlaylistResult.Success;
            }
            else
            {
                return PlaylistResult.Error;
            }
        }

        /// <summary>
        /// Delete all playlists locally only.
        /// </summary>
        /// <returns></returns>
        public PlaylistResult DeleteAllPlaylistsLocal()
        {
            string[] playlists = GetPlaylistLinks();

            foreach (var playlist in playlists)
            {
                PlaylistResult result = DeletePlaylist(playlist, false);
                if (result == PlaylistResult.Error)
                {
                    return PlaylistResult.Error;
                }
            }
            return PlaylistResult.Success;
        }

        private PlaylistResult DeletePlaylist(string playlist, bool syncWithServer = true)
        {
            try
            {
                string target = @$"{_playlistsRelateivePath}{playlist}\";
                int playlistId = GetPlaylistId(playlist);
                Directory.Delete(target, true);
                if (syncWithServer)
                {
                    _communicationManager.SyncDeletePlaylist(playlistId);
                }
                return PlaylistResult.Success;
            }
            catch { return PlaylistResult.Error; }
        }

        /// <summary>
        /// Delete current playlist. Current playlist is the playlist that the client has accessed.
        /// </summary>
        /// <param name="syncWithServer"></param>
        /// <returns></returns>
        public PlaylistResult DeleteCurrentPlaylist(bool syncWithServer = true)
        {
            try
            {
                string target = @$"{_playlistsRelateivePath}{CurrentPlaylist}\";
                int playlistId = GetPlaylistId(CurrentPlaylist);
                Directory.Delete(target, true);
                if (syncWithServer)
                {
                    _communicationManager.SyncDeletePlaylist(playlistId);
                }
                return PlaylistResult.Success;
            }
            catch { return PlaylistResult.Error; }
        }

        /// <summary>
        /// Add song to playlist with playlistLink.
        /// </summary>
        /// <param name="song"></param>
        /// <param name="playlistLink"></param>
        /// <param name="syncWithServer"></param>
        /// <returns></returns>
        public PlaylistResult AddSongToPlaylist(Song song, string playlistLink, bool syncWithServer = true)
        {
            string fullSongPath = GetFullSongPath(song, playlistLink);

            if (File.Exists(fullSongPath))
            {
                return PlaylistResult.SongAlreadyInPlaylist;
            }
            else
            {
                File.WriteAllBytes(fullSongPath, song.GetSerialized());
                if (syncWithServer)
                {
                    _communicationManager.SyncAddSongToPlaylist(GetPlaylistId(playlistLink), song.SongId);
                }
                return PlaylistResult.Success;
            }
        }

        /// <summary>
        /// Remove song from playlist with playlistLink.
        /// </summary>
        /// <param name="song"></param>
        /// <param name="playlistLink"></param>
        /// <param name="syncWithServer"></param>
        /// <returns></returns>
        public PlaylistResult RemoveSongFromPlaylist(Song song, string playlistLink, bool syncWithServer = true)
        {
            string fullSongPath = GetFullSongPath(song, playlistLink);
            if (File.Exists(fullSongPath))
            {
                File.Delete(fullSongPath);
                if (syncWithServer)
                {
                    _communicationManager.SyncDeleteSongFromPlaylist(GetPlaylistId(playlistLink), song.SongId);
                }
                return PlaylistResult.Success;
            }
            return PlaylistResult.Error;
        }

        private string GetFullSongPath(Song song, string playlistLink)
        {
            string fileName = $"{song.SongName} by {song.ArtistName}.bytes";
            return @$"{_playlistsRelateivePath}{playlistLink}\{fileName}";
        }

        /// <summary>
        /// Search for songs based on searchString. It will ask the server to send songs that match the pattern on
        /// song name or artist name.
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public List<Song> SearchPlaylistSongs(string searchString)
        {
            string serializedString = GetNormalized(searchString);


            if (serializedString == "") // If empty display all songs
            {
                return GetPlaylistSongs(CurrentPlaylist);
            }

            List<Song> results = new List<Song>();
            List<Song> playlistSongs = GetPlaylistSongs(CurrentPlaylist);

            for (int i = 0; i < playlistSongs.Count; i++)
            {
                if (GetNormalized(playlistSongs[i].SongName).Contains(serializedString) ||
                    GetNormalized(playlistSongs[i].ArtistName).Contains(serializedString))
                {
                    results.Add(playlistSongs[i]);
                }
            }
            return results;
        }

        /// <summary>
        /// Returns an Upper Case string with no white spaces.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private string GetNormalized(string text)
        {
            return new string(text.ToCharArray().Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpper();
        }

        /// <summary>
        /// Get all songs of playlist with playlistLink.
        /// </summary>
        /// <param name="playlistLink"></param>
        /// <returns></returns>
        public List<Song> GetPlaylistSongs(string playlistLink)
        {
            List<Song> songs = new List<Song>(0);
            if (Directory.Exists(@$"{_playlistsRelateivePath}{playlistLink}\"))
            {
                foreach (string file in Directory.EnumerateFiles(@$"{_playlistsRelateivePath}{playlistLink}\"))
                {
                    if (Path.GetFileName(file) != "playlist_data.bytes")
                    {
                        songs.Add(new Song(File.ReadAllBytes(file)));
                    }
                }
            }
            return songs;
        }

        /// <summary>
        /// Get all songs of current playlist.
        /// </summary>
        /// <returns></returns>
        public List<Song> GetCurrentPlaylistSongs()
        {
            List<Song> songs = new List<Song>(0);
            if (Directory.Exists(@$"{_playlistsRelateivePath}{CurrentPlaylist}\"))
            {
                foreach (string file in Directory.EnumerateFiles(@$"{_playlistsRelateivePath}{CurrentPlaylist}\"))
                {
                    if (Path.GetFileName(file) != "playlist_data.bytes")
                    {
                        songs.Add(new Song(File.ReadAllBytes(file)));
                    }
                }
            }
            return songs;
        }
    }
}
