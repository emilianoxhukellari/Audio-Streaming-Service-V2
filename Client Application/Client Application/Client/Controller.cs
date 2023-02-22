using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Printing.IndexedProperties;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.IO.IsolatedStorage;

namespace Client_Application.Client
{

    public sealed class Controller
    {
        // ATTRIBUTES
        private MediaPlayer _mediaPlayer;
        private CommunicationManager _communicationManager;
        private AuthenticationManager _authenticationManager;
        private PlaylistManager _playlistManager;

        private readonly ClientListener _clientListener;
        private ProgressBarState _progressBarState;
        private readonly Queue<InternalRequest> _internalRequestQueue;
        private Session? _session;
        // ATTRIBUTES

        // NETWORKING
        private DualSocket _dualSocket;
        private readonly int _portCommunication;
        private readonly int _portStreaming;
        private readonly string _host;
        private readonly string _clientId;
        private readonly IPAddress _IP;
        private readonly IPEndPoint _controllerIPE;
        private readonly IPEndPoint _mediaPlayerIPE;
        // NETWORKING

        // THREADS AND TASKS
        private readonly Thread _internalRequestThread;
        private readonly Task _progressTask;
        // THREADS AND TASKS

        // THREADING RESET EVENTS
        private readonly AutoResetEvent _newNetworkRequestFlag;
        private readonly AutoResetEvent _newInternalRequestFlag;
        // THREADING RESET EVENTS

        public Controller()
        {
            _portCommunication = Config.Config.GetPortCommunication();
            _portStreaming = Config.Config.GetPortStreaming();
            _host = Config.Config.GetHost();
            _clientId = Config.Config.GetClientId();
            _IP = IPAddress.Parse(_host);
            _controllerIPE = new IPEndPoint(_IP, _portCommunication);
            _mediaPlayerIPE = new IPEndPoint(_IP, _portStreaming);
            _dualSocket = new DualSocket(_controllerIPE,
                _mediaPlayerIPE,
                _clientId,
                new CallbackRecoverSession(RecoverSession),
                new CallbackConnectionStateUpdate(ConnectionStateUpdate));
            _communicationManager = CommunicationManager.InitializeSingleton(_dualSocket);
            _authenticationManager = AuthenticationManager.InitializeSingleton();
            _playlistManager = PlaylistManager.InitializeSingleton();

            _internalRequestThread = new Thread(InternalRequestLoop);
            _internalRequestThread.IsBackground = true;
            _progressTask = new Task(ProgressLoop);
            _newNetworkRequestFlag = new AutoResetEvent(false);
            _newInternalRequestFlag = new AutoResetEvent(false);
            _clientListener = new ClientListener();
            _internalRequestQueue = new Queue<InternalRequest>();
            _progressBarState = ProgressBarState.Free;
            _mediaPlayer = new MediaPlayer(_dualSocket,
                new CallbackTerminateSongDataReceive(TerminateSongDataReceiveRequest),
                new CallbackSendCurrentSongInfo(UpdateCurrentSongInfo),
                new CallbackSendQueueInfo(DisplayQueue),
                new CallbackUpdateRepeatState(UpdateRepeatState));

            // Listen for events from Windows
            Listen(EventType.InternalRequest, new ClientEventCallback(OnAddInternalRequest));
            Listen(EventType.UpdateProgressBarState, new ClientEventCallback(OnSetProgressBarState));
            Listen(EventType.MoveSongUpQueue, new ClientEventCallback(OnMoveSongUpQueue));
            Listen(EventType.MoveSongDownQueue, new ClientEventCallback(OnMoveSongDownQueue));
            Listen(EventType.RemoveSongQueue, new ClientEventCallback(OnRemoveSongQueue));
            Listen(EventType.DeleteQueue, new ClientEventCallback(OnDeleteQueue));
            Listen(EventType.ChangeVolume, new ClientEventCallback(OnChangeVolume));
            Listen(EventType.CreateNewPlaylist, new ClientEventCallback(OnCreateNewPlaylist));
            Listen(EventType.WindowReady, new ClientEventCallback(OnWindowReady));
            Listen(EventType.UpdatePlaylist, new ClientEventCallback(OnUpdatePlaylist));
            Listen(EventType.AddSongToPlaylist, new ClientEventCallback(OnAddSongToPlaylist));
            Listen(EventType.RemoveSongFromPlaylist, new ClientEventCallback(OnRemoveSongFromPlaylist));
            Listen(EventType.PlayCurrentPlaylist, new ClientEventCallback(OnPlayCurrentPlaylist));
            Listen(EventType.RenamePlaylist, new ClientEventCallback(OnRenamePlaylist));
            Listen(EventType.AddPlaylistToQueue, new ClientEventCallback(OnAddPlaylistToQueue));
            Listen(EventType.DeletePlaylist, new ClientEventCallback(OnDeletePlaylist));
            Listen(EventType.SearchPlaylist, new ClientEventCallback(OnSearchPlaylist));
            Listen(EventType.LogOut, new ClientEventCallback(OnLogOut));
            Listen(EventType.SearchSongOrArtist, new ClientEventCallback(OnSearchSongOrArtist));
            Listen(EventType.LogIn, new ClientEventCallback(OnLogIn));
        }

        private void OnSearchSongOrArtist(params object[] parameters)
        {
            Task.Run(() =>
            {
                string search = (string)parameters[0];
                List<Song> foundSongs = _communicationManager.SearchSongOrArtist(search);
                new ClientEvent(EventType.DisplaySongs, true, foundSongs);
            });
        }

        private void ConnectionStateUpdate(bool connected)
        {
            new ClientEvent(EventType.UpdateConnectionState, true, connected);
        }

        private void RecoverSession()
        {
            AuthenticationManager.WaitForInstance();
            _authenticationManager.RecoverSession();
        }

        private void OnLogIn(params object[] parameters)
        {
            Task.Run(() =>
            {
                string email = (string)parameters[0];
                string password = (string)parameters[1];
                bool rememberMe = (bool)parameters[2];
                _dualSocket.Connect();
                bool success = _authenticationManager.LogIn(email, password, rememberMe);
                if (success)
                {
                    DisplayPlaylistLinks(DisplayPlaylistLinksMode.None, _playlistManager.GetPlaylistLinks());
                }
            });
        }

        private void OnLogOut(params object[] parameters)
        {
            Task.Run(() =>
            {
                _communicationManager.DisconnectFromServer();
                _authenticationManager.NewSession();
                new ClientEvent(EventType.LogInStateUpdate, true, LogInState.LogOut, "");
                FillRememberMe();
                ResetApp();
            });
        }

        private void ResetApp()
        {
            _mediaPlayer.ResetMediaPlayer();
            new ClientEvent(EventType.ResetWindow, true);
        }


        /// <summary>
        /// Expects (int)index. It will ask the media player to move the song at index up the queue.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnMoveSongUpQueue(params object[] parameters)
        {
            int index = (int)parameters[0];
            _mediaPlayer.MoveSongUp(index);
        }

        /// <summary>
        /// Expects (int)index. It will ask the media player to move the song at index down the queue.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnMoveSongDownQueue(params object[] parameters)
        {
            int index = (int)parameters[0];
            _mediaPlayer.MoveSongDown(index);
        }

        /// <summary>
        /// Expects no parameters. It will ask the media player to delete the entire queue.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnDeleteQueue(params object[] parameters)
        {
            _mediaPlayer.DeleteQueue();
        }

        /// <summary>
        /// Expects (int)index. It will ask the media player to remove song at index from queue.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnRemoveSongQueue(params object[] parameters)
        {
            int index = (int)parameters[0];
            _mediaPlayer.RemoveSongFromQueue(index);
        }

        /// <summary>
        /// Expects (string)search. It will Seach for a song in the current playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnSearchPlaylist(params object[] parameters)
        {
            string searchString = (string)parameters[0];
            List<Song> songs = _playlistManager.SearchPlaylistSong(searchString);

            new ClientEvent(EventType.DisplayPlaylistSongs, true, songs, _playlistManager.CurrentPlaylist);
        }

        /// <summary>
        /// Expects (float)volume from 0 to 100. It will ask the media player to change the volume from 0 to 1.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnChangeVolume(params object[] parameters)
        {
            float volume100 = (float)parameters[0];
            float volume = volume100 / 100;
            _mediaPlayer.SetVolume(volume);
        }

        /// <summary>
        /// Expects no parameters. It delets the current playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnDeletePlaylist(params object[] parameters)
        {
            PlaylistResult result = _playlistManager.DeleteCurrentPlaylist();
            if(result == PlaylistResult.Success)
            {
                DisplayPlaylistLinks(DisplayPlaylistLinksMode.Delete, _playlistManager.GetPlaylistLinks());
            }
        }

        /// <summary>
        /// Expects (Song)song and (string) playlist name. It will add the specified song to the specified playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnAddSongToPlaylist(params object[] parameters)
        {
            Song song = (Song)parameters[0];
            string playlistLink = (string)parameters[1];

            PlaylistResult result = _playlistManager.AddSongToPlaylist(song, playlistLink);

            if(result == PlaylistResult.SongAlreadyInPlaylist)
            {
                MessageBox.Show($"{song.SongName} by {song.ArtistName} is already in {playlistLink}.",
                    "Cannot add song to playlist",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Expects (string)new name. It renames the current playlist with the new name.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnRenamePlaylist(params object[] parameters)
        {
            string newName = (string)parameters[0];

            PlaylistResult result = _playlistManager.RenameCurrentPlaylist(newName);

            if(result == PlaylistResult.AlreadyExists)
            {
                new ClientEvent(EventType.PlaylistExists, true, newName);
            }

            else if(result == PlaylistResult.Success)
            {
                DisplayPlaylistLinks(DisplayPlaylistLinksMode.Rename, _playlistManager.GetPlaylistLinks(), newName);
            }
        }

        /// <summary>
        /// Expects (string)name. It will create an empty playlist with the name and display empty playlist window.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnCreateNewPlaylist(params object[] parameters)
        {
            string playlistName = (string)parameters[0];

            PlaylistResult result = _playlistManager.CreateNewPlaylist(playlistName);

            if(result == PlaylistResult.Success)
            {
                DisplayPlaylistLinks(DisplayPlaylistLinksMode.New, _playlistManager.GetPlaylistLinks(), playlistName);
            }
            else if(result == PlaylistResult.AlreadyExists)
            {
                new ClientEvent(EventType.PlaylistExists, true, playlistName);
            }
        }

        /// <summary>
        /// Expects no parameters. It will add the current playlist to queue.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnAddPlaylistToQueue(params object[] parameters)
        {
            List<Song> playlistSongs = _playlistManager.GetCurrentPlaylistSongs();
            _mediaPlayer.AddPlaylistSongsToQueue(playlistSongs);
        }

        /// <summary>
        /// Expects no parameters. It will play the current playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnPlayCurrentPlaylist(params object[] parameters)
        {
            List<Song> playlistSongs = _playlistManager.GetCurrentPlaylistSongs();
            if (playlistSongs.Any())
            {
                _mediaPlayer.PlayPlaylistSongs(playlistSongs);
                SendPlayState(PlayButtonState.Play);
            }
        }

        /// <summary>
        /// Expects (string)playlist name. It will update that playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnUpdatePlaylist(params object[] parameters)
        {
            string playlistLink = (string)parameters[0];
            UpdatePlaylist(playlistLink);
        }

        /// <summary>
        /// Expects (Song)song and (string)playlist name. It will remove that song from that playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnRemoveSongFromPlaylist(params object[] parameters)
        {
            Song song = (Song)parameters[0];
            string playlistLink = (string)parameters[1];
            PlaylistResult result = _playlistManager.RemoveSongFromPlaylist(song, playlistLink);
            if(result == PlaylistResult.Success)
            {
                UpdatePlaylist(playlistLink);
            }
        }

        /// <summary>
        /// Expects no parameters. It will execute methods after Main Window is ready.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnWindowReady(params object[] parameters)
        {
            AuthenticationManager.WaitForInstance();
            FillRememberMe();
        }

        private void FillRememberMe()
        {
            string? email;
            string? password;
            bool rememberMe = _authenticationManager.IsRememeberMe(out email, out password);

            if (rememberMe && email != null && password != null)
            {
                new ClientEvent(EventType.UpdateRememberMe, true, email, password);
            }
        }

        /// <summary>
        /// This methods takes an InternalRequest and executes it based on the type.
        /// </summary>
        /// <param name="internalRequest"></param>
        private void ExecuteInternalRequest(InternalRequest internalRequest)
        {
            if (internalRequest.Type == InternalRequestType.PlayPauseStateChange)
            {
                ExecutePlayPauseStateChange();
            }
            else if (internalRequest.Type == InternalRequestType.PlayThis)
            {
                Song song = (Song)internalRequest.Parameters[0];
                ExecutePlayThis(song);
            }
            else if (internalRequest.Type == InternalRequestType.NextSong)
            {
                ExecuteNextSong();
            }
            else if (internalRequest.Type == InternalRequestType.PreviousSong)
            {
                ExecutePreviousSong();
            }
            else if (internalRequest.Type == InternalRequestType.AddSongToQueue)
            {
                Song song = (Song)internalRequest.Parameters[0];
                ExecuteAddToQueue(song);
            }
            else if (internalRequest.Type == InternalRequestType.RepeatStateChange)
            {
                RepeatState repeatState = (RepeatState)internalRequest.Parameters[0];
                ExecuteRepeatStateChange(repeatState);
            }
            else if (internalRequest.Type == InternalRequestType.ShuffleStateChange)
            {
                ShuffleState shuffleState = (ShuffleState)internalRequest.Parameters[0];
                ExecuteShuffleStateChange(shuffleState);
            }
        }

        /// <summary>
        /// This method will change the play state. It will then send the new state to main window.
        /// </summary>
        private void ExecutePlayPauseStateChange()
        {
            PlaybackState playbackState = _mediaPlayer.ChangePlayState();

            switch (playbackState)
            {
                case PlaybackState.Playing:
                    SendPlayState(PlayButtonState.Play);
                    break;
                case PlaybackState.Paused:
                    SendPlayState(PlayButtonState.Pause);
                    break;
            }
        }

        /// <summary>
        /// This method will set the shuffle state of the media player.
        /// </summary>
        /// <param name="shuffleState"></param>
        private void ExecuteShuffleStateChange(ShuffleState shuffleState)
        {
            _mediaPlayer.SetShuffleState(shuffleState);
        }

        /// <summary>
        /// This method will set the repeat state of the media player.
        /// </summary>
        /// <param name="repeatState"></param>
        private void ExecuteRepeatStateChange(RepeatState repeatState)
        {
            _mediaPlayer.SetRepeatState(repeatState);
        }

        /// <summary>
        /// This method will ask the window to update the repeat state with the new one.
        /// </summary>
        /// <param name="repeatState"></param>
        private void UpdateRepeatState(RepeatState repeatState)
        {
            new ClientEvent(EventType.UpdateRepeatState, true, repeatState);
        }

        /// <summary>
        /// This method will ask the window to display the playlist names. 
        /// </summary>
        /// <param name="displayPlaylistLinksMode"></param>
        /// <param name="active"></param>
        private void DisplayPlaylistLinks(DisplayPlaylistLinksMode displayPlaylistLinksMode, string[] playlistLinks, string active = "")
        {
            if (displayPlaylistLinksMode == DisplayPlaylistLinksMode.None || displayPlaylistLinksMode == DisplayPlaylistLinksMode.Delete)
            {
                new ClientEvent(EventType.DisplayPlaylistLinks, true, playlistLinks, displayPlaylistLinksMode);
            }
            else if (displayPlaylistLinksMode == DisplayPlaylistLinksMode.New || displayPlaylistLinksMode == DisplayPlaylistLinksMode.Rename)
            {
                new ClientEvent(EventType.DisplayPlaylistLinks, true, playlistLinks, displayPlaylistLinksMode, active);
            }
        }

        /// <summary>
        /// This method updates the current playlist.
        /// </summary>
        /// <param name="playlistLink"></param>
        private void UpdatePlaylist(string playlistLink)
        {
            _playlistManager.CurrentPlaylist = playlistLink;
            new ClientEvent(EventType.DisplayPlaylistSongs, true, _playlistManager.GetPlaylistSongs(playlistLink), _playlistManager.CurrentPlaylist);
        }

        /// <summary>
        /// Callback method for mediaplayer.
        /// </summary>
        /// <param name="songs"></param>
        private void DisplayQueue(List<(Song, int)> songs)
        {
            new ClientEvent(EventType.DisplayQueue, true, songs);
        }

        /// <summary>
        /// Updates Progress Bar every 500 ms.
        /// </summary>
        private void ProgressLoop()
        {
            while (true)
            {
                Thread.Sleep(500);
                if (_progressBarState == ProgressBarState.Free)
                {
                    (double Progress, string CurrentTime) progress = _mediaPlayer.GetCurrentSongProgress();
                    new ClientEvent(EventType.UpdateProgress, true, progress.Progress, progress.CurrentTime);
                }
            }
        }

        private void OnSetProgressBarState(params object[] parameters)
        {
            ProgressBarState state = (ProgressBarState)parameters[0];
            if (state == ProgressBarState.Free)
            {
                double progress = (double)parameters[1];
                ExecuteChangeProgress(progress);
            }
            _progressBarState = state;
        }

        private void ExecuteChangeProgress(double progress_0To100)
        {
            if (progress_0To100 <= 100 && progress_0To100 >= 0)
            {
                double audioStreamPosition = progress_0To100 / 100;
                _mediaPlayer.SetPosition(audioStreamPosition);
            }
        }

        private void UpdateCurrentSongInfo(Song song)
        {
            new ClientEvent(EventType.DisplayCurrentSong, true, song.SongName, song.ArtistName, song.DurationString, song.ImageBinary);
        }

        private void TerminateSongDataReceiveRequest()
        {
            _communicationManager.TerminateSongDataReceive();
        }

        /// <summary>
        /// Adds the specified song to mediaplayer queue.
        /// </summary>
        /// <param name="song"></param>
        private void ExecuteAddToQueue(Song song)
        {
            _mediaPlayer.AddToQueue(song);
        }

        private void SendPlayState(PlayButtonState playButtonState)
        {
            new ClientEvent(EventType.ChangePlayState, true, playButtonState);
        }

        private void OnAddInternalRequest(params object[] parameters)
        {
            _internalRequestQueue.Enqueue(new InternalRequest(parameters));
            _newInternalRequestFlag.Set();
        }

        /// <summary>
        /// This method should be run in a separate Thread/Task. 
        /// It will execute internal requests. These internal requests are related to playback.
        /// </summary>
        private void InternalRequestLoop()
        {
            while (true)
            {
                if (_internalRequestQueue.Count == 0)
                {
                    _newInternalRequestFlag.WaitOne();
                }

                InternalRequest? internalRequest;

                if (_internalRequestQueue.TryDequeue(out internalRequest))
                {
                    ExecuteInternalRequest(internalRequest);
                }
            }
        }

        /// <summary>
        /// Asks media player to play the specified song.
        /// </summary>
        /// <param name="song"></param>
        public void ExecutePlayThis(Song song)
        {
            SendPlayState(PlayButtonState.Play);
            _mediaPlayer.PlayThis(song);
        }

        /// <summary>
        /// Asks media player to play next song.
        /// </summary>
        public void ExecuteNextSong()
        {
            SendPlayState(PlayButtonState.Play);
            _mediaPlayer.NextSong();
        }

        /// <summary>
        /// Asks media player to play previous song.
        /// </summary>
        public void ExecutePreviousSong()
        {
            SendPlayState(PlayButtonState.Play);
            _mediaPlayer.PreviousSong();
        }

        /// <summary>
        /// Subscribes for an event. The callback will be called when this event happens.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="callback"></param>
        private void Listen(EventType eventType, ClientEventCallback callback)
        {
            _clientListener.Listen(eventType, callback);
        }

        /// <summary>
        /// Call this method to start the controller.
        /// </summary>
        public void Run()
        {
            _mediaPlayer.Run();
            _internalRequestThread.Start();
            _progressTask.Start();
        }
    }
}
