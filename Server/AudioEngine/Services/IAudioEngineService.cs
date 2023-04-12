namespace AudioEngine.Services
{
    public interface IAudioEngineService
    {
        bool IsRunning { get; set; }

        event EventHandler<int>? ServerDesktopClientCountLimitChanged;
        event EventHandler<int>? ServerDesktopSongLimitChanged;
        event EventHandler<AudioEngineService.ServerLoadArgs>? ServerLoadUpdate;
        event EventHandler<bool>? ServerStartStopChanged;
        event EventHandler<int>? ServerWebSongLimitChanged;

        int GetDesktopAppSongSearchLimit();
        int GetDesktopClientCountLimit();
        AudioEngineService.ServerLoadArgs GetServerLoadInitialState();
        int GetWebAppSongSearchLimit();
        void SetDesktopAppSongSearchLimit(int limit);
        void SetDesktopClientCountLimit(int limit);
        void SetWebAppSongSearchLimit(int limit);
        Task StartEngineAsync();
        Task StopEngineAsync();
    }
}