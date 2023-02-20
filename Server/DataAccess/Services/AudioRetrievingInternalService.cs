using DataAccess.Contexts;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public class AudioRetrievingInternalService : IDisposable
    {
        private readonly StreamingDbContext _streamingDbContext;
        public AudioRetrievingInternalService(StreamingDbContext streamingDbContext)
        {
            _streamingDbContext = streamingDbContext;
        }

        ~AudioRetrievingInternalService() 
        {
            _streamingDbContext?.Dispose();
        }

        public void Dispose()
        {
            _streamingDbContext?.Dispose();
        }

        public DataAccess.Models.Song? GetSongFromDatabase(int songId)
        {
            DataAccess.Models.Song? song;
            song = (from s in _streamingDbContext.Songs
                    where s.SongId == songId
                    select new Song
                    {
                        SongId = s.SongId,
                        SongName = s.SongName,
                        ArtistName = s.ArtistName,
                        NormalizedSongName = s.NormalizedSongName,
                        NormalizedArtistname = s.NormalizedArtistname,
                        Duration = s.Duration,
                        ImageFileName = s.ImageFileName,
                        SongFileName = s.SongFileName
                    }).FirstOrDefault();
            return song;
        }

        public IQueryable<DataAccess.Models.Song> GetSongsForSearch(string search)
        {
            IQueryable<DataAccess.Models.Song> songs;

            songs = from song in _streamingDbContext.Songs
                    where song.NormalizedSongName.Contains(search) || song.NormalizedArtistname.Contains(search)
                    select new DataAccess.Models.Song
                    {
                        SongId = song.SongId,
                        SongName = song.SongName,
                        ArtistName = song.ArtistName,
                        NormalizedSongName = song.NormalizedSongName,
                        NormalizedArtistname = song.NormalizedArtistname,
                        Duration = song.Duration,
                        ImageFileName = song.ImageFileName
                    };
            return songs;
        }
    }
}
