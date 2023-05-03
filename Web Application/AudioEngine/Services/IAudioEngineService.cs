namespace AudioEngine.Services
{
    public interface IAudioEngineService
    {
        bool IsRunning { get; }

        event EventHandler<int>? ServerDesktopClientCountLimitChanged;
        event EventHandler<int>? ServerDesktopSongLimitChanged;
        event EventHandler<ServerLoadArgs>? ServerLoadUpdate;
        event EventHandler<bool>? ServerStartStopChanged;
        event EventHandler<int>? ServerWebSongLimitChanged;

        int GetDesktopAppSongSearchLimit();
        int GetDesktopClientCountLimit();
        ServerLoadArgs GetServerLoadInitialState();
        int GetWebAppSongSearchLimit();
        void SetDesktopAppSongSearchLimit(int limit);
        void SetDesktopClientCountLimit(int limit);
        void SetWebAppSongSearchLimit(int limit);
        Task StartEngineAsync();
        Task StopEngineAsync();
    }
}