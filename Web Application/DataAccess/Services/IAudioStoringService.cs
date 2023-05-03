using DataAccess.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DataAccess.Services
{
    public interface IAudioStoringService
    {
        string RelativeImagesPath { get; }
        string RelativeSongsPath { get; }

        /// <summary>
        /// Takes the song input, and stores the song in the database and local storage.
        /// </summary>
        /// <param name="songInput"></param>
        /// <returns></returns>
        Task StoreSongAsync(SongInput songInput, ClaimsPrincipal uploader);

        /// <summary>
        /// Updates the audio file of the song with id. The new audio file must have the same duration as the 
        /// old one.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="songFile"></param>
        /// <returns></returns>
        Task<bool> UpdateSongFileAsync(int id, IFormFile songFile);
    }
}