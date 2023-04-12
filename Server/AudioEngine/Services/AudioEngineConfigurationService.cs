using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace AudioEngine.Services
{
    public class AudioEngineConfigurationService : IAudioEngineConfigurationService
    {
        private readonly IConfiguration _configuration;
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
