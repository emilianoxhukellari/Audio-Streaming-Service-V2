using System.Security.Cryptography.X509Certificates;

namespace AudioEngine.Services
{
    public interface IAudioEngineConfigurationService
    {
        string Host { get; }
        int PortCommunication { get; }
        int PortStreaming { get; }
        X509Certificate X509Certificate { get; }
    }
}