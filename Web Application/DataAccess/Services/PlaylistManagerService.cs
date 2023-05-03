using DataAccess.Contexts;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DataAccess.Services
{
    public class PlaylistManagerService : IPlaylistManagerService
    {
        private readonly StreamingDbContext _streamingDbContext;
        private readonly UserManager<IdentityUser> _userManager;
        public PlaylistManagerService(StreamingDbContext streamingDbContext, UserManager<IdentityUser> userManager)
        {
            _streamingDbContext = streamingDbContext;
            _userManager = userManager;
        }

        /// <inheritdoc/>
        public async Task<int> GetNumberOfPlaylistsAsync(ClaimsPrincipal user)
        {
            var identityUser = await _userManager.GetUserAsync(user);
            if (identityUser is not null)
            {
                return await _streamingDbContext
                    .Playlists
                    .AsNoTracking()
                    .Where(p => p.UserId == identityUser.Id && p.IsDeleted == false)
                    .CountAsync();
            }
            return 0;
        }

        /// <inheritdoc/>
        public async Task<int> GetNumberOfDeletedPlaylistsAsync(ClaimsPrincipal user)
        {
            var identityUser = await _userManager.GetUserAsync(user);
            if (identityUser is not null)
            {
                return await _streamingDbContext
                    .Playlists
                    .AsNoTracking()
                    .Where(p => p.UserId == identityUser.Id && p.IsDeleted == true)
                    .CountAsync();
            }
            return 0;
        }

        /// <inheritdoc/>
        public async Task<List<Playlist>> GetRemovedPlaylistAsync(ClaimsPrincipal user)
        {
            var identityUser = await _userManager.GetUserAsync(user);
            if (identityUser is not null)
            {
                return await _streamingDbContext
                    .Playlists
                    .AsNoTracking()
                    .Where(p => p.UserId == identityUser.Id && p.IsDeleted == true)
                    .Include(p => p.PlaylistSongs)
                    .ToListAsync();
            }
            return new List<Playlist>(0);
        }

        /// <inheritdoc/>
        public async Task<List<Playlist>> GetPlaylistsAsync(ClaimsPrincipal user)
        {
            var identityUser = await _userManager.GetUserAsync(user);
            if (identityUser is not null)
            {
                return await _streamingDbContext
                    .Playlists
                    .AsNoTracking()
                    .Where(p => p.UserId == identityUser.Id && p.IsDeleted == false)
                    .Include(p => p.PlaylistSongs)
                    .ToListAsync();
            }
            return new List<Playlist>(0);
        }

        /// <inheritdoc/>
        public async Task<bool> UserHasPlaylist(ClaimsPrincipal user, int playlistId)
        {
            var identityUser = await _userManager.GetUserAsync(user);

            if (identityUser is not null)
            {
                return await _streamingDbContext
                    .Playlists
                    .AsNoTracking()
                    .AnyAsync(p => p.UserId == identityUser.Id && p.PlaylistId == playlistId);
            }
            return false;
        }

        /// <inheritdoc/>
        public async Task<Playlist?> GetPlaylistAsync(int id)
        {
            return await _streamingDbContext.Playlists.AsNoTracking().FirstOrDefaultAsync(p => p.PlaylistId == id);
        }

        /// <inheritdoc/>
        public async Task<bool> RecoverPlaylist(int playlistId)
        {
            var playlist = await _streamingDbContext.Playlists.FindAsync(playlistId);
            if (playlist is not null)
            {
                playlist.IsDeleted = false;
                await _streamingDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public async Task<bool> PermanentlyDeletePlaylistAsync(int playlistId)
        {
            var playlist = await _streamingDbContext.Playlists.FindAsync(playlistId);

            if (playlist is null)
            {
                return false;
            }

            _streamingDbContext.Playlists.Remove(playlist);
            await _streamingDbContext.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task<bool> SoftDeletePlaylistAsync(int playlistId)
        {
            var playlist = await _streamingDbContext.Playlists.FindAsync(playlistId);

            if (playlist is null)
            {
                return false;
            }

            playlist.IsDeleted = true;
            await _streamingDbContext.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public bool SoftDeletePlaylist(int playlistId)
        {
            var playlist = _streamingDbContext.Playlists.Find(playlistId);

            if (playlist is null)
            {
                return false;
            }

            playlist.IsDeleted = true;
            _streamingDbContext.SaveChanges();
            return true;
        }

        /// <inheritdoc/>
        public async Task<List<Song>> GetSongsAsync(int playlistId)
        {
            var songs = await _streamingDbContext.PlaylistSongs
                .AsNoTracking()
                .Where(ps => ps.PlaylistId == playlistId)
                .Select(ps => ps.Song)
                .ToListAsync();
            return songs;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void RenamePlaylist(int playlistId, string newName)
        {
            var playlist = _streamingDbContext.Playlists.Find(playlistId);

            if (playlist != null)
            {
                playlist.PlaylistName = newName;
                _streamingDbContext.SaveChanges();
            }
        }

        /// <inheritdoc/>
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
                playlist.Duration += song.Duration;
                _streamingDbContext.SaveChanges();
            }
        }

        /// <inheritdoc/>
        public void DeleteSongFromPlaylist(int playlistId, int songId)
        {
            var playlist = _streamingDbContext.Playlists.Find(playlistId);
            var song = _streamingDbContext.Songs.Find(songId);
            var playlistSong = _streamingDbContext.PlaylistSongs
                .SingleOrDefault(ps => ps.PlaylistId == playlistId && ps.SongId == songId);

            if (playlist != null && song != null && playlistSong != null)
            {
                _streamingDbContext.PlaylistSongs.Remove(playlistSong);
                playlist.Duration -= song.Duration;
                _streamingDbContext.SaveChanges();
            }
        }

        /// <inheritdoc/>
        public List<int> GetPlaylistIds(string userId)
        {
            return _streamingDbContext.Playlists
                .AsNoTracking()
                .Where(p => p.UserId == userId && p.IsDeleted != true)
                .Select(p => p.PlaylistId)
                .ToList();
        }

        /// <inheritdoc/>
        public string? GetPlaylistName(int playlistId)
        {
            return _streamingDbContext.Playlists
                .AsNoTracking()
                .Where(p => p.PlaylistId == playlistId)
                .Select(p => p.PlaylistName)
                .FirstOrDefault();
        }
    }
}
