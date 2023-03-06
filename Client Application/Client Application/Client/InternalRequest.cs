﻿using System;

namespace Client_Application.Client
{
    public enum InternalRequestType
    {
        PlayPauseStateChange,
        RepeatStateChange,
        ShuffleStateChange,
        NextSong,
        PreviousSong,
        PlayThis,
        PlayCurrentPlaylist,
        AddSongToQueue,
        LogIn
    }

    /// <summary>
    /// Represents an internal request that might take a long time to complete. 
    /// </summary>
    public sealed class InternalRequest
    {
        public InternalRequestArgs Args { get; }
        public InternalRequestType Type { get; }
        public InternalRequest(InternalRequestType type, InternalRequestArgs args)
        {
            Type = type;
            Args = args;
        }
    }
}
