using DataAccess.Models;
using System.Security.Claims;

namespace DataAccess.Services
{
    public interface IPlaylistManagerService
    {
        
        int AddPlaylist(string playlistName, string userId);
        void AddSongToPlaylist(int playlistId, int songId);
        void DeleteSongFromPlaylist(int playlistId, int songId);
        Task<Playlist?> GetPlaylistAsync(int id);
        List<int> GetPlaylistIds(string userId);
        string? GetPlaylistName(int playlistId);
        Task<int> GetNumberOfPlaylistsAsync(ClaimsPrincipal user);
        Task<int> GetNumberOfDeletedPlaylistsAsync(ClaimsPrincipal user);
        Task<List<Playlist>> GetPlaylistsAsync(ClaimsPrincipal user);
        Task<List<Playlist>> GetRemovedPlaylistAsync(ClaimsPrincipal user);
        Task<List<Song>> GetSongsAsync(int playlistId);
        Task<bool> PermanentlyDeletePlaylistAsync(int playlistId);
        Task<bool> RecoverPlaylist(int playlistId);
        void RenamePlaylist(int playlistId, string newName);
        bool SoftDeletePlaylist(int playlistId);
        Task<bool> SoftDeletePlaylistAsync(int playlistId);
        Task<bool> UserHasPlaylist(ClaimsPrincipal user, int playlistId);
    }
}