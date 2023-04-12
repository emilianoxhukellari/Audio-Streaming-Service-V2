﻿using DataAccess.Contexts;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public class SongManagerService : ISongManagerService
    {
        private readonly StreamingDbContext _context;
        private readonly IDataAccessConfigurationService _dataAccessConfigurationService;
        public SongManagerService(StreamingDbContext context, IDataAccessConfigurationService dataAccessConfigurationService)
        {
            _context = context;
            _dataAccessConfigurationService = dataAccessConfigurationService;
        }

        public async Task<int> GetNumberOfSongsAsync()
        {
            return await _context.Songs.CountAsync();
        }

        public async Task<bool> AddSongAsync(Song song)
        {
            try
            {
                await _context.AddAsync(song);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteSongAsync(int songId)
        {
            var song = await _context.Songs.FindAsync(songId);

            if (song is null)
            {
                return false;
            }

            try
            {
                File.Delete(song.ImageFileName);
                File.Delete(song.SongFileName);
                _context.Songs.Remove(song);
                await _context.SaveChangesAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<List<Song>> GetSongsForWebAppAsync(string pattern)
        {
            IQueryable<DataAccess.Models.Song> songs;

            songs = from song in _context.Songs
                    where song.NormalizedSongName.Contains(GetNormalized(pattern)) || song.NormalizedArtistname.Contains(GetNormalized(pattern))
                    select new DataAccess.Models.Song
                    {
                        SongId = song.SongId,
                        SongName = song.SongName,
                        ArtistName = song.ArtistName,
                        NormalizedSongName = song.NormalizedSongName,
                        NormalizedArtistname = song.NormalizedArtistname,
                        Duration = song.Duration,
                        ImageFileName = song.ImageFileName,
                        SongFileName = song.SongFileName,
                    };
            if (songs.Count() > _dataAccessConfigurationService.WebAppSongSearchLimit)
            {
                return await songs.Take(_dataAccessConfigurationService.WebAppSongSearchLimit).ToListAsync();
            }
            return await songs.ToListAsync();
        }

        public async Task<List<Song>> GetSongsForWebAppAsync()
        {
            IQueryable<DataAccess.Models.Song> songs;

            songs = from song in _context.Songs
                    select new DataAccess.Models.Song
                    {
                        SongId = song.SongId,
                        SongName = song.SongName,
                        ArtistName = song.ArtistName,
                        NormalizedSongName = song.NormalizedSongName,
                        NormalizedArtistname = song.NormalizedArtistname,
                        Duration = song.Duration,
                        ImageFileName = song.ImageFileName,
                        SongFileName = song.SongFileName,
                    };
            if (songs.Count() > _dataAccessConfigurationService.WebAppSongSearchLimit)
            {
                return await songs.Take(_dataAccessConfigurationService.WebAppSongSearchLimit).ToListAsync();
            }
            return await songs.ToListAsync();
        }

        public List<DataAccess.Models.Song> GetSongsForDesktopApp(string search)
        {
            IQueryable<DataAccess.Models.Song> songs;

            songs = from song in _context.Songs
                    where song.NormalizedSongName.Contains(GetNormalized(search)) || song.NormalizedArtistname.Contains(GetNormalized(search))
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

            if (songs.Count() > _dataAccessConfigurationService.DesktopAppSongSearchLimit)
            {
                return songs.Take(_dataAccessConfigurationService.DesktopAppSongSearchLimit).ToList();
            }
            return songs.ToList();
        }

        public DataAccess.Models.Song? GetSongFromDatabase(int songId)
        {
            return _context.Songs.Find(songId);
        }

        public async Task<Song?> GetSongFromDatabaseAsync(int songId)
        {
            return await _context.Songs.FindAsync(songId);
        }

        public List<int> GetSongIds(int playlistId)
        {
            return _context.PlaylistSongs
                .Where(ps => ps.PlaylistId == playlistId)
                .Select(ps => ps.SongId)
                .ToList();
        }

        private string GetNormalized(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }
            return new string(input.ToCharArray().Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpper();
        }
    }
}
