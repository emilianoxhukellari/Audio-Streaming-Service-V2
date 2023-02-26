using Client_Application.Client;
using System.Collections.Generic;

namespace Client_Application
{
    public enum EventType
    {
        DisplaySongs,
        DisplayQueue,
        NetworkRequest,
        InternalRequest,
        ChangePlayState,
        DisplayCurrentSong,
        UpdateProgress,
        UpdateProgressBarState,
        MoveSongUpQueue,
        MoveSongDownQueue,
        RemoveSongQueue,
        DeleteQueue,
        ChangeVolume,
        CreateNewPlaylist,
        PlaylistExists,
        DisplayPlaylistLinks,
        DisplayPlaylistSongs,
        UpdatePlaylistCanvas,
        ShowPlaylistCanvas,
        UpdatePlaylist,
        AddSongToPlaylist,
        RemoveSongFromPlaylist,
        PlayCurrentPlaylist,
        AddPlaylistToQueue,
        DeletePlaylist,
        RenamePlaylist,
        UpdateRepeatState,
        SearchPlaylist,
        LogInStateUpdate,
        WindowReady,
        UpdateRememberMe,
        LogOut,
        LogIn,
        ResetWindow,
        UpdateConnectionState,
        SearchSongOrArtist,
        LogInStartEnd,
    }

    public enum LogInState
    {
        LogInValid,
        LogInInvalid,
        LogOut
    }

    public enum PlaybackState
    {
        Playing,
        Stopped,
        Paused
    }

    public enum DisplayPlaylistLinksMode
    {
        New,
        Rename,
        Delete,
        None,
    }

    public enum ShuffleState
    {
        Shuffled,
        Unshuffled
    }

    public enum ProgressBarState
    {
        Busy,
        Free,
    }
    public enum RepeatState
    {
        RepeatOn,
        RepeatOff,
        OnRepeat,
        None
    }

    public enum PlaylistLinkMode
    {
        Rename,
        New
    }

    public enum PlayButtonState
    {
        Play,
        Pause
    }

    public delegate void BufferFillEventHandler(byte[] data, int size);
    public delegate void CallbackDelegate(int id);
    public delegate void CallbackOptimize(int start, int end);
    public delegate void ClientEventCallback(params object[] args);
    public delegate void CallbackEndOfStream();
    public delegate void CallbackSendProgressInfo(double progress);
    public delegate void CallbackTerminateSongDataReceive();
    public delegate void CallbackSendCurrentSongInfo(Song song);
    public delegate void CallbackSendQueueInfo(List<(Song, int)> songs);
    public delegate void CallbackExtraInformation(params object[] parameters);
    public delegate void CallbackUpdateRepeatState(RepeatState repeatState);
    public delegate void CallbackRecoverSession();
    public delegate void CallbackConnectionStateUpdate(bool connected);
}
