using DataAccess.Models;

namespace DataAccess.Services
{
    public interface ISongManagerService
    {
        Task<int> GetNumberOfSongsAsync();
        Task<bool> AddSongAsync(Song song);
        Task<bool> DeleteSongAsync(int songId);
        Song? GetSongFromDatabase(int songId);
        Task<Song?> GetSongFromDatabaseAsync(int songId);
        List<int> GetSongIds(int playlistId);
        List<Song> GetSongsForDesktopApp(string search);
        Task<List<Song>> GetSongsForWebAppAsync();
        Task<List<Song>> GetSongsForWebAppAsync(string pattern);
    }
}