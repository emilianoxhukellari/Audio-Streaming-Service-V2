using DataAccess.Models;

namespace DataAccess.Services
{
    public interface ISongManagerService
    {
        /// <summary>
        /// Returns the number of all songs in the database.
        /// </summary>
        /// <returns></returns>
        Task<int> GetNumberOfSongsAsync();

        /// <summary>
        /// Adds a song to database.
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        Task<bool> AddSongAsync(Song song);

        /// <summary>
        /// Deletes the song with songId from the database. This method will also delete the audio and image files 
        /// for this song.
        /// </summary>
        /// <param name="songId"></param>
        /// <returns></returns>
        Task<bool> DeleteSongAsync(int songId);

        /// <summary>
        /// Returns the song with songId from the database.
        /// </summary>
        /// <param name="songId"></param>
        /// <returns></returns>
        Song? GetSongFromDatabase(int songId);

        /// <summary>
        /// Returns the song with songId from the database.
        /// </summary>
        /// <param name="songId"></param>
        /// <returns></returns>
        Task<Song?> GetSongFromDatabaseAsync(int songId);

        /// <summary>
        /// Returns the ids of songs that are in the playlist with playlistId.
        /// </summary>
        /// <param name="playlistId"></param>
        /// <returns></returns>
        List<int> GetSongIds(int playlistId);

        /// <summary>
        /// This method will return a list of songs that are compatible for search for desktop clients. 
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        List<Song> GetSongsForDesktopApp(string search);

        /// <summary>
        /// This method will return a list of songs that are compatible for web clients. 
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        Task<List<Song>> GetSongsForWebAppAsync();

        /// <summary>
        /// This method will return a list of songs that are compatible for web clients based on pattern. 
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        Task<List<Song>> GetSongsForWebAppAsync(string pattern);
    }
}