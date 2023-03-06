using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client_Application.Client
{
    public class SearchSongOrArtistArgs : EventArgs
    {
        public string Search { get; set; } = string.Empty;
    }

    public class DisplaySongsArgs : EventArgs
    {
        public List<Song> Songs { get; set; } = new List<Song>();
    }

    public class UpdateConnectionStateArgs : EventArgs
    {
        public bool Connected { get; set; }
    }

    public class LogInArgs : EventArgs
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class LogInStartEndArgs : EventArgs
    {
        public bool Started { get; set; } // True when started, False when ended.
    }

    public class LogInStateUpdateArgs : EventArgs
    {
        public LogInState LogInState { get; set; } = LogInState.LogOut;
        public string Email { get; set; } = string.Empty;
    }

    public class MoveSongArgs : EventArgs
    {
        public int Index { get; set; }
    }

    public class RemoveSongQueueArgs : EventArgs
    {
        public int Index { get; set; }
    }

    public class DisplayPlaylistSongsArgs : EventArgs
    {
        public List<Song> Songs { get; set; } = new List<Song>();
        public string CurrentPlaylist { get; set; } = string.Empty;
    }

    public class ChangeVolumeArgs : EventArgs
    {
        public float Volume100 { get; set; }
    }

    public class AddSongToPlaylistArgs : EventArgs
    {
        public Song Song { get; set; } = new Song();
        public string PlaylistLink { get; set; } = string.Empty;
    }

    public class RenamePlaylistArgs : EventArgs
    {
        public string NewName { get; set; } = string.Empty;
    }

    public class PlaylistExistsArgs : EventArgs
    {
        public string PlaylistName { get; set; } = string.Empty;
    }

    public class CreateNewPlaylistArgs : EventArgs
    {
        public string PlaylistName { get; set; } = string.Empty;
    }

    public class UpdatePlaylistArgs : EventArgs
    {
        public string PlaylistLink { get; set; } = string.Empty;
    }

    public class RemoveSongFromPlaylistArgs : EventArgs
    {
        public Song Song = new Song();
        public string PlaylistLink { get; set; } = string.Empty;
    }

    public class UpdateRememberMeArgs : EventArgs
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateRepeatStateArgs : EventArgs
    {
        public RepeatState RepeatState { get; set; } = RepeatState.RepeatOff;
    }

    public class DisplayPlaylistLinksArgs : EventArgs
    {
        public string[] PlaylistLinks { get; set; } = Array.Empty<string>();
        public DisplayPlaylistLinksMode DisplayPlaylistLinksMode { get; set; } = DisplayPlaylistLinksMode.None;
        public string Active { get; set; } = string.Empty;
    }

    public class DisplayQueueArgs : EventArgs
    {
        public List<(Song, int)> Songs { get; set; } = new List<(Song, int)>();
    }

    public class UpdateProgressArgs : EventArgs
    {
        public double Progress { get; set; }
        public string CurrentTime { get; set; } = string.Empty;
    }

    public class UpdateProgressBarStateArgs : EventArgs
    {
        public ProgressBarState ProgressBarState { get; set; } = ProgressBarState.Free;
        public double Progress { get; set; }
    }

    public class DisplayCurrentSongArgs : EventArgs
    {
        public string SongName { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public string DurationString { get; set; } = string.Empty;
        public byte[] ImageBytes { get; set; } = Array.Empty<byte>();
    }

    public class ChangePlayStateArgs : EventArgs
    {
        public PlayButtonState PlayButtonState { get; set; } = PlayButtonState.Play;
    }

    public class UpdatePlaylistCanvasArgs : EventArgs
    {
        public string PlaylistLink { get; set; } = string.Empty;
    }

    public class InternalRequestArgs : EventArgs
    {
        public InternalRequestType InternalRequestType { get; set; }
        public ShuffleState ShuffleState { get; set; }
        public RepeatState RepeatState { get; set; }
        public Song? Song { get; set; } 
    }
}
