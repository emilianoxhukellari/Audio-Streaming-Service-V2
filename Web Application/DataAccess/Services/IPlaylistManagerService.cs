using DataAccess.Models;
using System.Security.Claims;

namespace DataAccess.Services
{
    public interface IPlaylistManagerService
    {
        /// <summary>
        /// Creates a new playlist with playlistName that belongs to user with userId.
        /// </summary>
        /// <param name="playlistName"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        int AddPlaylist(string playlistName, string userId);

        /// <summary>
        /// Adds a song with id to the playlist with id.
        /// </summary>
        /// <param name="playlistId"></param>
        /// <param name="songId"></param>
        void AddSongToPlaylist(int playlistId, int songId);

        /// <summary>
        /// Deletes a song with id from the playlist with id.
        /// </summary>
        /// <param name="playlistId"></param>
        /// <param name="songId"></param>
        void DeleteSongFromPlaylist(int playlistId, int songId);

        /// <summary>
        /// Returns the playlist with id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Playlist?> GetPlaylistAsync(int id);

        /// <summary>
        /// Get the ids of playlists that belong to the user with id.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        List<int> GetPlaylistIds(string userId);

        /// <summary>
        /// Get the name of the playlist.
        /// </summary>
        /// <param name="playlistId"></param>
        /// <returns></returns>
        string? GetPlaylistName(int playlistId);

        /// <summary>
        /// Returns the number of playlists. Only playlists that have state "IsDeleted = false" are considered.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<int> GetNumberOfPlaylistsAsync(ClaimsPrincipal user);

        /// <summary>
        /// Returns the number of deleted playlists.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<int> GetNumberOfDeletedPlaylistsAsync(ClaimsPrincipal user);

        /// <summary>
        /// Returns a list of playlists for the user. Only playlists that have state "IsDeleted = false" are returned.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<List<Playlist>> GetPlaylistsAsync(ClaimsPrincipal user);

        /// <summary>
        /// Returns a list of playlists that were soft deleted for the user.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<List<Playlist>> GetRemovedPlaylistAsync(ClaimsPrincipal user);

        /// <summary>
        /// Get all songs that are in the playlist as a list.
        /// </summary>
        /// <param name="playlistId"></param>
        /// <returns></returns>
        Task<List<Song>> GetSongsAsync(int playlistId);

        /// <summary>
        /// Delete the playlist from the database.
        /// </summary>
        /// <param name="playlistId"></param>
        /// <returns></returns>
        Task<bool> PermanentlyDeletePlaylistAsync(int playlistId);

        /// <summary>
        /// Change the state of "IsDeleted = true" for the playlist.
        /// </summary>
        /// <param name="playlistId"></param>
        /// <returns></returns>
        Task<bool> RecoverPlaylist(int playlistId);

        /// <summary>
        /// Give the playlist a new name and update the database.
        /// </summary>
        /// <param name="playlistId"></param>
        /// <param name="newName"></param>
        void RenamePlaylist(int playlistId, string newName);

        /// <summary>
        /// Change the state of "IsDeleted = true". This does not delete the playlist from database.
        /// </summary>
        /// <param name="playlistId"></param>
        /// <returns></returns>
        bool SoftDeletePlaylist(int playlistId);

        /// <summary>
        /// Change the state of "IsDeleted = true". This does not delete the playlist from database.
        /// </summary>
        /// <param name="playlistId"></param>
        /// <returns></returns>
        Task<bool> SoftDeletePlaylistAsync(int playlistId);

        /// <summary>
        /// Checks whether the logged in user owns the playlist.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="playlistId"></param>
        /// <returns></returns>
        Task<bool> UserHasPlaylist(ClaimsPrincipal user, int playlistId);
    }
}