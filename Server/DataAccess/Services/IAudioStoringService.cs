using DataAccess.Models;
using Microsoft.AspNetCore.Http;

namespace DataAccess.Services
{
    public interface IAudioStoringService
    {
        string RelativeImagesPath { get; }
        string RelativeSongsPath { get; }

        Task StoreSongAsync(SongInput songInput);
        Task<bool> UpdateSongFileAsync(int id, IFormFile songFile);
    }
}