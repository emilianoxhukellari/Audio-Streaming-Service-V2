using Client_Application.Client.Core;
using System;
using System.Collections.Generic;

namespace Client_Application.Client.Event
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
        PlayPlaylistNext,
        AddSongToQueue,
        PlaySongNext,
        DeletePlaylist,
        RenamePlaylist,
        UpdateRepeatState,
        SearchPlaylist,
        LogInStateUpdate,
        RegisterErrorsUpdate,
        WindowReady,
        UpdateRememberMe,
        LogOut,
        LogIn,
        Register,
        ResetWindow,
        UpdateConnectionState,
        SearchSongsServer,
        LongNetworkRequest,
        UpdatePage,
        ResetPage
    }

    public enum PageType
    {
        Register,
        LogIn,
        Main
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
    public delegate void ClientEventCallback<T>(T args) where T : EventArgs;
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
