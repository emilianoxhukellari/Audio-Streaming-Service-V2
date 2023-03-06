using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace AudioEngine.Services
{
    public class AudioEngineConfigurationService : IAudioEngineConfigurationService
    {
        private readonly IConfiguration _configuration;
        private int _searchSongLimit; // make it so that it accesses a static method to get the limit each time it is called.
        public int PortCommunication { get; private set; }
        public int PortStreaming { get; private set; }
        public string Host { get; private set; }
        public X509Certificate X509Certificate { get; private set; }

        public AudioEngineConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
            PortCommunication = int.Parse(_configuration["AudioEngineConfiguration:PortCommunication"]!);
            PortStreaming = int.Parse(_configuration["AudioEngineConfiguration:PortStreaming"]!);
            Host = _configuration["AudioEngineConfiguration:Host"]!;
            X509Certificate = new X509Certificate(_configuration["X509Certificate:RelativePath"]!, _configuration["X509Certificate:Password"]!);

            if (Host == null)
            {
                throw new InvalidDataException("Cannot find host path.");
            }
        }
    }
}
