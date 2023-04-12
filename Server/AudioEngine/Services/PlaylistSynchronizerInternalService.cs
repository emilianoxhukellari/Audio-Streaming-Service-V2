using DataAccess.Contexts;
using DataAccess.Services;
using Microsoft.Extensions.DependencyInjection;
using Server_Application.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioEngine.Services
{
    public class PlaylistSynchronizerInternalService
    {
        private readonly IPlaylistManagerService _playlistManagerService;
        private readonly ISongManagerService _songManagerService;
        private readonly IServiceScope _serviceScope;
        public string? UserId { get; set; }
        public PlaylistSynchronizerInternalService(IServiceProvider serviceProvider)
        {
            _serviceScope = serviceProvider.CreateScope();
            _playlistManagerService = _serviceScope.ServiceProvider.GetRequiredService<IPlaylistManagerService>();
            _songManagerService = _serviceScope.ServiceProvider.GetRequiredService<ISongManagerService>();
        }

        ~PlaylistSynchronizerInternalService() 
        {
            _serviceScope.Dispose();
        }

        public void SetUserId(string userId)
        {
            UserId = userId;
        }

        public void SyncDeletePlaylist(int playlistId)
        {
            _playlistManagerService.SoftDeletePlaylist(playlistId);
        }

        public int SyncAddPlaylist(string playlistName)
        {
            if(UserId == null)
            {
                throw new ArgumentNullException("User id is not set.");
            }
            return _playlistManagerService.AddPlaylist(playlistName, UserId);
        }

        public void SyncRenamePlaylist(int playlistId, string newName)
        {
            if(UserId == null)
            {
                throw new ArgumentNullException("User id is not set.");
            }
            _playlistManagerService.RenamePlaylist(playlistId, newName);
        }

        public void SyncDeleteSongFromPlaylist(int playlistId, int songId)
        {
            if(UserId == null)
            {
                throw new ArgumentNullException("User id is not set.");
            }
            _playlistManagerService.DeleteSongFromPlaylist(playlistId, songId);
        }

        public void SyncAddSongToPlaylist(int playlistId, int songId)
        {
            if(UserId == null)
            {
                throw new ArgumentNullException("User id is not set.");
            }
            _playlistManagerService.AddSongToPlaylist(playlistId, songId);
        }

        public SyncDiff GetDiff(List<PlaylistSyncData> playlistsUp)
        {
            if(UserId == null)
            {
                throw new ArgumentNullException("User id is not set.");
            }

            List<int> serverPlaylists = _playlistManagerService.GetPlaylistIds(UserId);
            List<int> clientPlaylists = playlistsUp.Select(p => p.PlaylistId).ToList();
            int[] intersectPlaylists = clientPlaylists.Intersect(serverPlaylists).ToArray();


            List<int> deletePlaylists = clientPlaylists.Except(serverPlaylists).ToList(); //DELETE

            int[] addPlaylistIds = serverPlaylists.Except(clientPlaylists).ToArray(); //ADD
            Trace.WriteLine($"Number of playlists to add: {addPlaylistIds.Length}");
            List<(int playlistId, string playlistName)> addPlaylists = new();
            foreach(int playlistId in addPlaylistIds)
            {
                addPlaylists.Add((playlistId, _playlistManagerService.GetPlaylistName(playlistId)!));
            }

            List<(int playlistId, string playlistName)> renamePlaylists = new(); //RENAME
            foreach (int playlistId in intersectPlaylists)
            {
                string? playlistName = _playlistManagerService.GetPlaylistName(playlistId);
                if (playlistName != playlistsUp.Where(p => p.PlaylistId == playlistId).Select(p => p.PlaylistName).FirstOrDefault())
                {
                    renamePlaylists.Add((playlistId, playlistName!));
                }
            }

            List<(int playlistId, int songId)> deleteSongs = new(); // DELETE SONGS
            foreach(int playlistId in intersectPlaylists)
            {
                List<int> clientSongIds = playlistsUp.Where(p => p.PlaylistId == playlistId).Select(p => p.SongIds).FirstOrDefault()!;
                var deleteSongForPlaylist = clientSongIds.Except(_songManagerService.GetSongIds(playlistId));
                foreach(var deleteSong in deleteSongForPlaylist)
                {
                    deleteSongs.Add((playlistId, deleteSong));
                }
            }

            List<(int playlistId, List<Song> songs)> addSongs = new(); // ADD SONGS

            foreach (int playlistId in intersectPlaylists) // Add songs in existing playlists
            {
                List<Song> songsToAdd = new(0);
                List<int> clientSongIds = playlistsUp.Where(p => p.PlaylistId == playlistId).Select(p => p.SongIds).FirstOrDefault()!;
                var addSongForPlaylistIds = _songManagerService.GetSongIds(playlistId).Except(clientSongIds);
                foreach (var addSongId in addSongForPlaylistIds)
                {
                    DataAccess.Models.Song? song = _songManagerService.GetSongFromDatabase(addSongId);
                    if(song != null)
                    {
                        songsToAdd.Add(new Song(song.SongId, song.SongName, song.ArtistName, song.Duration, File.ReadAllBytes(song.ImageFileName)));
                    }
                }

                if(songsToAdd.Count > 0)
                {
                    addSongs.Add((playlistId, songsToAdd));
                }
            }


            foreach(int playlistId in addPlaylistIds) // Add songs of playlists that the client did not have
            {
                List<Song> songsToAdd = new(0);
                var addSongForPlaylistIds = _songManagerService.GetSongIds(playlistId);
                foreach (var addSongId in addSongForPlaylistIds)
                {
                    DataAccess.Models.Song? song = _songManagerService.GetSongFromDatabase(addSongId);
                    if (song != null)
                    {
                        songsToAdd.Add(new Song(song.SongId, song.SongName, song.ArtistName, song.Duration, File.ReadAllBytes(song.ImageFileName)));
                    }
                }

                if(songsToAdd.Count > 0)
                {
                    addSongs.Add((playlistId, songsToAdd));
                }
            }

            SyncDiff diff = new SyncDiff();

            if(deletePlaylists.Count > 0)
            {
                diff.DeletePlaylists = deletePlaylists;
            }

            if(addPlaylists.Count > 0)
            {
                diff.AddPlaylists = addPlaylists;
            }

            if(renamePlaylists.Count > 0)
            {
                diff.RenamePlaylists = renamePlaylists;
            }

            if(deleteSongs.Count > 0)
            {
                diff.DeleteSongs = deleteSongs;
            }

            if(addSongs.Count > 0)
            {
                diff.AddSongs = addSongs;
            }

            return diff;
        }
    }
}
