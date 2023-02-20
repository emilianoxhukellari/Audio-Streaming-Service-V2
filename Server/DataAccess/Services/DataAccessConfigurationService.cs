using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Services
{
    public class DataAccessConfigurationService
    {
        private readonly IConfiguration _configuration;
        public string AudioFilesRelativePath { get; }
        public string ImageFilesRelativePath { get; }

        public DataAccessConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;

            AudioFilesRelativePath = _configuration["DataAccessConfiguration:AudioFilesRelativePath"]!;

            if (AudioFilesRelativePath == null)
            {
                throw new InvalidDataException("Cannot find relative audio files path.");
            }

            ImageFilesRelativePath = _configuration["DataAccessConfiguration:ImageFilesRelativePath"]!;

            if (ImageFilesRelativePath == null)
            {
                throw new InvalidDataException("Cannot find relative images files path.");
            }
        }
    }
}
