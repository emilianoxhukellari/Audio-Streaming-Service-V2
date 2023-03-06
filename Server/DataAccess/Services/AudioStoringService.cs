using DataAccess.Contexts;
using DataAccess.Models;
using DataAccess.Services.DataLogic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Server_Application.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public class AudioStoringService : IAudioStoringService
    {
        private readonly StreamingDbContext _streamingDbContext;
        private readonly IDataAccessConfigurationService _dataAccessConfigurationService;
        public string RelativeSongsPath { get; private set; }
        public string RelativeImagesPath { get; private set; }

        public AudioStoringService(StreamingDbContext streamingDbContext, IDataAccessConfigurationService dataAccessConfigurationService)
        {
            _streamingDbContext = streamingDbContext;
            _dataAccessConfigurationService = dataAccessConfigurationService;

            RelativeSongsPath = _dataAccessConfigurationService.AudioFilesRelativePath;
            RelativeImagesPath = _dataAccessConfigurationService.ImageFilesRelativePath;
        }

        public async Task StoreSongAsync(SongInput songInput)
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
                ImageFileName = relativeImageFilePath
            };

            _streamingDbContext.Add<Models.Song>(song);
            _streamingDbContext.SaveChanges();
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


        private string GetSerializedForDatabase(string input)
        {
            List<char> result = new List<char>();

            foreach (char c in input)
            {
                if (c.Equals('\''))
                {
                    result.Add('\'');
                    result.Add(c);
                }
                else
                {
                    result.Add(c);
                }
            }

            return new string(result.ToArray());
        }

        private string GetNormalized(string input)
        {
            return new string(input.ToCharArray().Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpper();
        }
    }
}
