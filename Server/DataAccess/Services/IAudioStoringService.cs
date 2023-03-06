using DataAccess.Models;

namespace DataAccess.Services
{
    public interface IAudioStoringService
    {
        string RelativeImagesPath { get; }
        string RelativeSongsPath { get; }

        Task StoreSongAsync(SongInput songInput);
    }
}