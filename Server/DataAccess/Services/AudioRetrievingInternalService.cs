using DataAccess.Contexts;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public class AudioRetrievingInternalService : IDisposable
    {
        private readonly StreamingDbContext _streamingDbContext;
        public AudioRetrievingInternalService(StreamingDbContext streamingDbContext)
        {
            _streamingDbContext = streamingDbContext;
        }

        ~AudioRetrievingInternalService() 
        {
            _streamingDbContext?.Dispose();
        }

        public void Dispose()
        {
            _streamingDbContext?.Dispose();
        }

        public void DeletePlaylist(int playlistId)
        {
            var playlist = _streamingDbContext.Playlists.Find(playlistId);
            if (playlist != null)
            {
                playlist.IsDeleted = true;
                _streamingDbContext.SaveChanges();
            }
        }

        public int AddPlaylist(string playlistName, string userId)
        {
            var playlist = new Playlist
            {
                PlaylistName = playlistName,
                UserId = userId
            };

            _streamingDbContext.Playlists.Add(playlist);
            _streamingDbContext.SaveChanges();

            return playlist.PlaylistId;
        }

        public void RenamePlaylist(int playlistId, string newName)
        {
            var playlist = _streamingDbContext.Playlists.Find(playlistId);

            if (playlist != null)
            {
                playlist.PlaylistName = newName;
                _streamingDbContext.SaveChanges();
            }
        }

        public void AddSongToPlaylist(int playlistId, int songId)
        {
            var playlist = _streamingDbContext.Playlists.Find(playlistId);
            var song = _streamingDbContext.Songs.Find(songId);

            if (playlist != null && song != null)
            {
                var playlistSong = new PlaylistSong
                {
                    PlaylistId = playlist.PlaylistId,
                    SongId = song.SongId
                };

                _streamingDbContext.PlaylistSongs.Add(playlistSong);
                _streamingDbContext.SaveChanges();
            }
        }

        public void DeleteSongFromPlaylist(int playlistId, int songId)
        {
            var playlistSong = _streamingDbContext.PlaylistSongs
                .SingleOrDefault(ps => ps.PlaylistId == playlistId && ps.SongId == songId);

            if (playlistSong != null)
            {
                _streamingDbContext.PlaylistSongs.Remove(playlistSong);
                _streamingDbContext.SaveChanges();
            }
        }

        public List<int> GetPlaylistIds(string userId)
        {
            return _streamingDbContext.Playlists
                .Where(p => p.UserId == userId && p.IsDeleted != true)
                .Select(p => p.PlaylistId)
                .ToList();
        }

        public string? GetPlaylistName(int playlistId)
        {
            return _streamingDbContext.Playlists
                .Where(p => p.PlaylistId == playlistId)
                .Select(p => p.PlaylistName)
                .FirstOrDefault();
        }

        public DataAccess.Models.Song? GetSongFromDatabase(int songId)
        {
            DataAccess.Models.Song? song;
            song = (from s in _streamingDbContext.Songs
                    where s.SongId == songId
                    select new Song
                    {
                        SongId = s.SongId,
                        SongName = s.SongName,
                        ArtistName = s.ArtistName,
                        NormalizedSongName = s.NormalizedSongName,
                        NormalizedArtistname = s.NormalizedArtistname,
                        Duration = s.Duration,
                        ImageFileName = s.ImageFileName,
                        SongFileName = s.SongFileName
                    }).FirstOrDefault();
            return song;
        }

        public List<int> GetSongIds(int playlistId)
        {
            return _streamingDbContext.PlaylistSongs
                .Where(ps => ps.PlaylistId == playlistId)
                .Select(ps => ps.SongId)
                .ToList();
        }

        public IQueryable<DataAccess.Models.Song> GetSongsForSearch(string search)
        {
            IQueryable<DataAccess.Models.Song> songs;

            songs = from song in _streamingDbContext.Songs
                    where song.NormalizedSongName.Contains(search) || song.NormalizedArtistname.Contains(search)
                    select new DataAccess.Models.Song
                    {
                        SongId = song.SongId,
                        SongName = song.SongName,
                        ArtistName = song.ArtistName,
                        NormalizedSongName = song.NormalizedSongName,
                        NormalizedArtistname = song.NormalizedArtistname,
                        Duration = song.Duration,
                        ImageFileName = song.ImageFileName
                    };
            return songs;
        }
    }
}
