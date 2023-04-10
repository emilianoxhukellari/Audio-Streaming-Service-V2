namespace AudioEngine.Services
{
    public interface IAudioEngineService
    {
        bool IsRunning { get; set; }

        event EventHandler<bool>? ServerStateChanged;

        Task StartEngineAsync();
        Task StopEngineAsync();

        void SetDesktopSearchLimit(int limit);
    }
}