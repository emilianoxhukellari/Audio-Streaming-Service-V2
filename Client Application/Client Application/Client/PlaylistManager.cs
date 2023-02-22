using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Client_Application.Client
{
    
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
        private static readonly object syncLock = new object();

        private readonly string _playlistsRelateivePath;
        public string CurrentPlaylist { get; set; }

        private PlaylistManager()
        {
            _playlistsRelateivePath = Config.Config.GetPlaylistsRelativePath();
            CurrentPlaylist = "";
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

        public string[] GetPlaylistLinks()
        {
            return Directory.GetDirectories(_playlistsRelateivePath).Select(d => Path.GetRelativePath(_playlistsRelateivePath, d)).ToArray();
        }


        public PlaylistResult CreateNewPlaylist(string playlistName)
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
                    CurrentPlaylist = playlistName;
                    return PlaylistResult.Success;
                }
            }
            return PlaylistResult.EmptyName;
        }

        public PlaylistResult RenameCurrentPlaylist(string newName)
        {
            string source = $"{_playlistsRelateivePath}{CurrentPlaylist}";
            string destination = $"{_playlistsRelateivePath}{newName}";
            if (!Directory.Exists(destination))
            {
                Directory.Move(source, destination);
                CurrentPlaylist = newName;
                return PlaylistResult.Success;
            }
            else
            {
                return PlaylistResult.AlreadyExists;
            }
        }

        public PlaylistResult DeleteCurrentPlaylist()
        {
            try
            {
                string target = @$"{_playlistsRelateivePath}{CurrentPlaylist}\";
                Directory.Delete(target, true);
                return PlaylistResult.Success;
            }
            catch { return PlaylistResult.Error; }
        }

        public PlaylistResult AddSongToPlaylist(Song song, string playlistLink)
        {
            string fullSongPath = GetFullSongPath(song, playlistLink);

            if (File.Exists(fullSongPath))
            {
                return PlaylistResult.SongAlreadyInPlaylist;
            }
            else
            {
                File.WriteAllBytes(fullSongPath, song.GetSerialized());
                return PlaylistResult.Success;
            }
        }

        public PlaylistResult RemoveSongFromPlaylist(Song song, string playlistLink)
        {
            string fullSongPath = GetFullSongPath(song, playlistLink);
            if (File.Exists(fullSongPath))
            {
                File.Delete(fullSongPath);
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
                    songs.Add(new Song(File.ReadAllBytes(file)));
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
                    songs.Add(new Song(File.ReadAllBytes(file)));
                }
            }
            return songs;
        }
    }
}
