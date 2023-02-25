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

namespace Client_Application.Client
{

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

        public static void WaitForInstance()
        {
            _isInitialized.WaitOne();
        }

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
                        CreateNewPlaylist(element.playlistId, element.playlistName, false);
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
                    foreach(var element in syncDiff.DeleteSongs)
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

        public PlaylistResult RemoveSongFromPlaylist(int songId, int playlistId, bool syncWithServer=true)
        {
            string playlistName = GetPlaylistNameFromId(playlistId);
            List<Song> songs = GetPlaylistSongs(playlistName);
            foreach(var song in songs)
            {
                if(song.SongId == songId)
                {
                    RemoveSongFromPlaylist(song, playlistName);
                    if(syncWithServer)
                    {
                        _communicationManager.SyncDeleteSongFromPlaylist(playlistId, songId);
                    }
                    return PlaylistResult.Success;
                }
            }
            return PlaylistResult.Error;
        }

        public string[] GetPlaylistLinks()
        {
            return Directory.GetDirectories(_playlistsRelateivePath).Select(d => Path.GetRelativePath(_playlistsRelateivePath, d)).ToArray();
        }

        public int GetPlaylistId(string playlistLink)
        {
            byte[] dataBytes = File.ReadAllBytes(@$"{_playlistsRelateivePath}{playlistLink}\playlist_data.bytes");
            return new PlaylistData(dataBytes).PlaylistId;
        }

        public void WritePlaylistData(string playlistLink, int playlistId)
        {
            File.WriteAllBytes(@$"{_playlistsRelateivePath}{playlistLink}\playlist_data.bytes", new PlaylistData(playlistId).GetBytes());
        }

        public string GetPlaylistNameFromId(int id)
        {
            foreach(var playlist in GetPlaylistLinks())
            {
                if(GetPlaylistId(playlist) == id)
                {
                    return playlist;
                }
            }
            return "";
        }

        public PlaylistResult CreateNewPlaylist(string playlistName, bool syncWithServer=true)
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
                    if(syncWithServer)
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

        public PlaylistResult CreateNewPlaylist(int playlistId, string playlistName, bool syncWithServer=true)
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
                    if(syncWithServer)
                    {
                        playlistId = _communicationManager.SyncAddPlaylist(playlistName);
                    }
                    WritePlaylistData(playlistName, playlistId);
                    return PlaylistResult.Success;
                }
            }
            return PlaylistResult.EmptyName;
        }

        public PlaylistResult RenameCurrentPlaylist(string newName, bool syncWithServer=true)
        {
            string source = $"{_playlistsRelateivePath}{CurrentPlaylist}";
            string destination = $"{_playlistsRelateivePath}{newName}";
            if (!Directory.Exists(destination))
            {
                Directory.Move(source, destination);
                CurrentPlaylist = newName;
                if(syncWithServer)
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

        public PlaylistResult RenamePlaylist(int playlistId, string newName, bool syncWithServer=true)
        {
            string playlistName = GetPlaylistNameFromId(playlistId);
            string source = $"{_playlistsRelateivePath}{playlistName}";
            string destination = $"{_playlistsRelateivePath}{newName}";
            if (!Directory.Exists(destination) && Directory.Exists(source))
            {
                Directory.Move(source, destination);
                CurrentPlaylist = newName;
                if(syncWithServer)
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

        public PlaylistResult DeleteAllPlaylistsLocal()
        {
            string[] playlists = GetPlaylistLinks();

            foreach(var playlist in playlists)
            {
                PlaylistResult result = DeletePlaylist(playlist, false);
                if(result == PlaylistResult.Error)
                {
                    return PlaylistResult.Error;
                }
            }
            return PlaylistResult.Success;
        }

        public PlaylistResult DeletePlaylist(string playlist, bool syncWithServer=true)
        {
            try
            {
                string target = @$"{_playlistsRelateivePath}{playlist}\";
                int playlistId = GetPlaylistId(playlist);
                Directory.Delete(target, true);
                if(syncWithServer)
                {
                    _communicationManager.SyncDeletePlaylist(playlistId);
                }
                return PlaylistResult.Success;
            }
            catch { return PlaylistResult.Error; }
        }

        public PlaylistResult DeleteCurrentPlaylist(bool syncWithServer=true)
        {
            try
            {
                string target = @$"{_playlistsRelateivePath}{CurrentPlaylist}\";
                int playlistId = GetPlaylistId(CurrentPlaylist);
                Directory.Delete(target, true);
                if(syncWithServer)
                {
                    _communicationManager.SyncDeletePlaylist(playlistId);
                }
                return PlaylistResult.Success;
            }
            catch { return PlaylistResult.Error; }
        }

        public PlaylistResult AddSongToPlaylist(Song song, string playlistLink, bool syncWithServer=true)
        {
            string fullSongPath = GetFullSongPath(song, playlistLink);

            if (File.Exists(fullSongPath))
            {
                return PlaylistResult.SongAlreadyInPlaylist;
            }
            else
            {
                File.WriteAllBytes(fullSongPath, song.GetSerialized());
                if(syncWithServer)
                {
                    _communicationManager.SyncAddSongToPlaylist(GetPlaylistId(playlistLink), song.SongId);
                }
                return PlaylistResult.Success;
            }
        }

        public PlaylistResult RemoveSongFromPlaylist(Song song, string playlistLink, bool syncWithServer=true)
        {
            string fullSongPath = GetFullSongPath(song, playlistLink);
            if (File.Exists(fullSongPath))
            {
                File.Delete(fullSongPath);
                if(syncWithServer)
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

        public List<Song> SearchPlaylistSong(string searchString)
        {
            string serializedString = new string(searchString.ToCharArray().Where(c => !char.IsWhiteSpace(c)).ToArray());

            if (serializedString == "") // If empty display all songs
            {
                return GetPlaylistSongs(CurrentPlaylist);
            }

            List<Song> results = new List<Song>();
            List<Song> playlistSongs = GetPlaylistSongs(CurrentPlaylist);

            for (int i = 0; i < playlistSongs.Count; i++)
            {
                if (Regex.Match(playlistSongs[i].SongName, serializedString, RegexOptions.IgnoreCase).Success ||
                        Regex.Match(playlistSongs[i].ArtistName, serializedString, RegexOptions.IgnoreCase).Success)
                {
                    results.Add(playlistSongs[i]);
                }
            }
            return results;
        }

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

        public List<Song> GetCurrentPlaylistSongs()
        {
            List<Song> songs = new List<Song>(0);
            if (Directory.Exists(@$"{_playlistsRelateivePath}{CurrentPlaylist}\"))
            {
                foreach (string file in Directory.EnumerateFiles(@$"{_playlistsRelateivePath}{CurrentPlaylist}\"))
                {
                    if(Path.GetFileName(file) != "playlist_data.bytes")
                    {
                        songs.Add(new Song(File.ReadAllBytes(file)));
                    }
                }
            }
            return songs;
        }
    }
}
