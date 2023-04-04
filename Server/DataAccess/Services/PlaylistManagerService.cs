using DataAccess.Contexts;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public class PlaylistManagerService
    {
        private readonly StreamingDbContext _streamingDbContext;
        private readonly UserManager<IdentityUser> _userManager;
        public PlaylistManagerService(StreamingDbContext streamingDbContext, UserManager<IdentityUser> userManager)
        {
            _streamingDbContext = streamingDbContext;
            _userManager = userManager;
        }

        public async Task<List<Playlist>> GetRemovedPlaylistAsync(ClaimsPrincipal user)
        {
            var identityUser = await _userManager.GetUserAsync(user);
            if (identityUser is not null)
            {
                return await _streamingDbContext.Playlists.Where(p => p.UserId == identityUser.Id && p.IsDeleted == true).Include(p => p.PlaylistSongs).ToListAsync();
            }
            return new List<Playlist>(0);
        }

        public async Task<List<Playlist>> GetPlaylistsAsync(ClaimsPrincipal user)
        {
            var identityUser = await _userManager.GetUserAsync(user);
            if (identityUser is not null)
            {
                return await _streamingDbContext.Playlists.Where(p => p.UserId == identityUser.Id && p.IsDeleted == false).Include(p => p.PlaylistSongs).ToListAsync();
            }
            return new List<Playlist>(0);
        }

        public async Task<bool> UserHasPlaylist(ClaimsPrincipal user, int playlistId)
        {
            var identityUser = await _userManager.GetUserAsync(user);

            if(identityUser is not null)
            {
                return await _streamingDbContext.Playlists.AnyAsync(p => p.UserId == identityUser.Id && p.PlaylistId == playlistId);
            }
            return false;
        }

        public async Task<Playlist?> GetPlaylistAsync(int id)
        {
            return await _streamingDbContext.Playlists.FindAsync(id);
        }


        public async Task<bool> RecoverPlaylist(int playlistId)
        {
            var playlist = await _streamingDbContext.Playlists.FindAsync(playlistId);
            if(playlist is not null) 
            {
                playlist.IsDeleted = false;
                await _streamingDbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

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

        public async Task<bool> SoftDeletePlaylistAsync(int playlistId)
        {
            var playlist = await _streamingDbContext.Playlists.FindAsync(playlistId);

            if(playlist is null)
            {
                return false;
            }

            playlist.IsDeleted = true;
            await _streamingDbContext.SaveChangesAsync();   
            return true;
        }

        public async Task<List<Song>> GetSongsAsync(int playlistId)
        {
            var songs = await _streamingDbContext.PlaylistSongs
                .Where(ps => ps.PlaylistId == playlistId)
                .Select(ps => ps.Song)
                .ToListAsync();
            return songs;
        }
    }
}
