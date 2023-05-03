using DataAccess.Contexts;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;

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

        /// <inheritdoc/>
        public async Task<int> GetNumberOfSongsAsync()
        {
            return await _context.Songs.CountAsync();
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public async Task<List<Song>> GetSongsForWebAppAsync(string search)
        {
            List<Song> songs;

            songs = await _context.Songs.Where(
                s => s.NormalizedSongName.Contains(GetNormalized(search))
                || s.NormalizedArtistname.Contains(GetNormalized(search))).ToListAsync();

            if (songs.Count() > _dataAccessConfigurationService.DesktopAppSongSearchLimit)
            {
                return songs.Take(_dataAccessConfigurationService.WebAppSongSearchLimit).ToList();
            }

            return songs;
        }

        /// <inheritdoc/>
        public async Task<List<Song>> GetSongsForWebAppAsync()
        {
            var songs = await _context.Songs.ToListAsync();
          
            if (songs.Count() > _dataAccessConfigurationService.WebAppSongSearchLimit)
            {
                return songs.Take(_dataAccessConfigurationService.WebAppSongSearchLimit).ToList();
            }

            return songs;
        }

        /// <inheritdoc/>
        public List<Song> GetSongsForDesktopApp(string search)
        {
            List<Song> songs;

            songs = _context.Songs.Where(
                s => s.NormalizedSongName.Contains(GetNormalized(search)) 
                || s.NormalizedArtistname.Contains(GetNormalized(search))).ToList();

            if (songs.Count() > _dataAccessConfigurationService.DesktopAppSongSearchLimit)
            {
                return songs.Take(_dataAccessConfigurationService.DesktopAppSongSearchLimit).ToList();
            }
            return songs;
        }

        /// <inheritdoc/>
        public Song? GetSongFromDatabase(int songId)
        {
            return _context.Songs.Find(songId);
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
