using DataAccess.Models;
using DataAccess.Services.DataLogic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Server_Application.Server;
using System.Security.Claims;

namespace DataAccess.Services
{
    public class AudioStoringService : IAudioStoringService
    {
        private readonly ISongManagerService _songManagerService;
        private readonly IDataAccessConfigurationService _dataAccessConfigurationService;
        private readonly UserManager<IdentityUser> _userManager;
        public string RelativeSongsPath { get; private set; }
        public string RelativeImagesPath { get; private set; }

        public AudioStoringService(ISongManagerService songManagerService, IDataAccessConfigurationService dataAccessConfigurationService, UserManager<IdentityUser> userManager)
        {
            _songManagerService = songManagerService;
            _dataAccessConfigurationService = dataAccessConfigurationService;
            _userManager = userManager;

            RelativeSongsPath = _dataAccessConfigurationService.AudioFilesRelativePath;
            RelativeImagesPath = _dataAccessConfigurationService.ImageFilesRelativePath;
        }

        /// <inheritdoc/>
        public async Task StoreSongAsync(SongInput songInput, ClaimsPrincipal uploader)
        {
            string songName = songInput.SongName;
            string artistName = songInput.ArtistName;
            string songNameNormalized = GetNormalized(songName);
            string artistNameNormalized = GetNormalized(artistName);
            string relativeSongFilePath = GetRelativeSongPath(songName, artistName, RelativeSongsPath);
            string relativeImageFilePath = GetRelativeImagePath(songName, artistName, RelativeImagesPath);
            double durationSeconds = await CreateAndStoreSongFileAsync(songInput.SongFile, songName, artistName, RelativeSongsPath);

            await StoreImageAsync(songInput.ImageFile, songName, artistName, RelativeImagesPath);


            var song = new Models.Song
            {
                SongName = songName,
                ArtistName = artistName,
                NormalizedSongName = songNameNormalized,
                NormalizedArtistname = artistNameNormalized,
                Duration = durationSeconds,
                SongFileName = relativeSongFilePath,
                ImageFileName = relativeImageFilePath,
                UploaderId = _userManager.GetUserId(uploader)!
            };

            await _songManagerService.AddSongAsync(song);
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateSongFileAsync(int songId, IFormFile formFile)
        {
            var song = await _songManagerService.GetSongFromDatabaseAsync(songId);

            if (song == null)
            {
                return false;
            }

            byte[] newSongFileHeader = await GetHeaderAsync(formFile);
            double newSongFileDuration = GetDurationSeconds(newSongFileHeader);
            double oldSongFileDuration = song.Duration;
            if (oldSongFileDuration != newSongFileDuration)
            {
                return false;
            }
            await CreateAndStoreSongFileAsync(formFile, song.SongName, song.ArtistName, RelativeSongsPath);
            return true;
        }

        private string GetRelativeImagePath(string songName, string artistName, string relativeImagesPath)
        {
            return $"{relativeImagesPath}{songName} by {artistName}.png";
        }

        private string GetRelativeSongPath(string songName, string artistName, string relativeSongsPath)
        {
            return $"{relativeSongsPath}{songName} by {artistName}.bytes";
        }

        /// <summary>
        /// Returns Duration Seconds
        /// </summary>
        /// <param name="audioFile"></param>
        /// <param name="songName"></param>
        /// <param name="artistName"></param>
        /// <param name="relativeSongsPath"></param>
        /// <returns></returns>
        private async Task<double> CreateAndStoreSongFileAsync(IFormFile audioFile, string songName, string artistName, string relativeSongsPath)
        {
            string relativeFile = GetRelativeSongPath(songName, artistName, relativeSongsPath);
            byte[] header = await WriteAudioBytesAsync(audioFile, relativeFile);
            double durationSeconds = GetDurationSeconds(header);
            return durationSeconds;
        }


        /// <summary>
        /// Returns header.
        /// </summary>
        /// <param name="audioFile"></param>
        /// <param name="relativeFilePath"></param>
        /// <returns></returns>
        private async Task<byte[]> WriteAudioBytesAsync(IFormFile audioFile, string relativeFilePath)
        {
            StandardWaveBuilder standardWaveBuilder;
            using (var stream = new MemoryStream()) // This can be optimized with FileStream
            {
                await audioFile.CopyToAsync(stream);
                standardWaveBuilder = new StandardWaveBuilder(stream.ToArray());
            }

            GC.Collect();

            byte[] audioFileBytes = AudioFile.GetAudioBytes(standardWaveBuilder.GetStandardWave());
            byte[] header = audioFileBytes.Take(44).ToArray();
            await File.WriteAllBytesAsync(relativeFilePath, audioFileBytes);
            return header;
        }

        private async Task<byte[]> GetHeaderAsync(IFormFile audioFile)
        {
            StandardWaveBuilder standardWaveBuilder;
            using (var stream = new MemoryStream()) // This can be optimized with FileStream
            {
                await audioFile.CopyToAsync(stream);
                standardWaveBuilder = new StandardWaveBuilder(stream.ToArray());
            }

            GC.Collect();

            byte[] audioFileBytes = AudioFile.GetAudioBytes(standardWaveBuilder.GetStandardWave());
            byte[] header = audioFileBytes.Take(44).ToArray();
            return header;
        }

        /// <summary>
        /// Returns Relative File Path.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="songName"></param>
        /// <param name="artistName"></param>
        /// <returns></returns>
        private async Task StoreImageAsync(IFormFile imageFile, string songName, string artistName, string relativeImagesPath)
        {
            string relativeFile = GetRelativeImagePath(songName, artistName, relativeImagesPath);
            using (var stream = File.Create(relativeFile))
            {
                await imageFile.CopyToAsync(stream);
            }
        }

        private double GetDurationSeconds(byte[] header)
        {
            int bytesPerSecond = BitConverter.ToInt32(header.Take(new Range(28, 32)).ToArray());
            int dataSize = BitConverter.ToInt32(header.Take(new Range(40, 44)).ToArray());

            return (double)dataSize / bytesPerSecond;
        }

        private string GetNormalized(string input)
        {
            return new string(input.ToCharArray().Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpper();
        }
    }
}
