using System;

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
        public object[] Parameters { get; }
        public InternalRequestType Type { get; }
        public InternalRequest(params object[] parameters)
        {
            Type = (InternalRequestType)parameters[0];
            Parameters = new object[parameters.Length - 1];
            Array.Copy(parameters, 1, Parameters, 0, parameters.Length - 1);
        }
    }
}
