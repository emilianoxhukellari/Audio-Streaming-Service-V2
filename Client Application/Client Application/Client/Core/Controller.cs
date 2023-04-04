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
using Client_Application.Client.Network;
using Client_Application.Client.Managers;
using Client_Application.Client.Event;
using static Client_Application.Client.Managers.AuthManager;

namespace Client_Application.Client.Core
{

    public sealed class Controller
    {
        // ATTRIBUTES
        private MediaPlayer _mediaPlayer;
        private CommunicationManager _communicationManager;
        private AuthManager _authManager;
        private PlaylistManager _playlistManager;

        private readonly ClientListener _clientListener;
        private ProgressBarState _progressBarState;
        private readonly Queue<InternalRequest> _internalRequestQueue;
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
            _authManager = AuthManager.InitializeSingleton();
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
            Listen(EventType.InternalRequest, new ClientEventCallback<InternalRequestArgs>(OnAddInternalRequest));
            Listen(EventType.UpdateProgressBarState, new ClientEventCallback<UpdateProgressBarStateArgs>(OnUpdateProgressBarState));
            Listen(EventType.MoveSongUpQueue, new ClientEventCallback<MoveSongArgs>(OnMoveSongUpQueue));
            Listen(EventType.MoveSongDownQueue, new ClientEventCallback<MoveSongArgs>(OnMoveSongDownQueue));
            Listen(EventType.RemoveSongQueue, new ClientEventCallback<RemoveSongQueueArgs>(OnRemoveSongQueue));
            Listen(EventType.DeleteQueue, new ClientEventCallback<EventArgs>(OnDeleteQueue));
            Listen(EventType.ChangeVolume, new ClientEventCallback<ChangeVolumeArgs>(OnChangeVolume));
            Listen(EventType.CreateNewPlaylist, new ClientEventCallback<CreateNewPlaylistArgs>(OnCreateNewPlaylist));
            Listen(EventType.WindowReady, new ClientEventCallback<EventArgs>(OnWindowReady));
            Listen(EventType.UpdatePlaylist, new ClientEventCallback<UpdatePlaylistArgs>(OnUpdatePlaylist));
            Listen(EventType.AddSongToPlaylist, new ClientEventCallback<AddSongToPlaylistArgs>(OnAddSongToPlaylist));
            Listen(EventType.RemoveSongFromPlaylist, new ClientEventCallback<RemoveSongFromPlaylistArgs>(OnRemoveSongFromPlaylist));
            Listen(EventType.PlayCurrentPlaylist, new ClientEventCallback<EventArgs>(OnPlayCurrentPlaylist));
            Listen(EventType.RenamePlaylist, new ClientEventCallback<RenamePlaylistArgs>(OnRenamePlaylist));
            Listen(EventType.AddPlaylistToQueue, new ClientEventCallback<EventArgs>(OnAddPlaylistToQueue));
            Listen(EventType.DeletePlaylist, new ClientEventCallback<EventArgs>(OnDeletePlaylist));
            Listen(EventType.SearchPlaylist, new ClientEventCallback<SearchSongOrArtistArgs>(OnSearchPlaylist));
            Listen(EventType.SearchSongsServer, new ClientEventCallback<SearchSongOrArtistArgs>(OnSearchSongsServer));
            Listen(EventType.LogOut, new ClientEventCallback<EventArgs>(OnLogOut));
            Listen(EventType.LogIn, new ClientEventCallback<LogInArgs>(OnLogIn));
            Listen(EventType.Register, new ClientEventCallback<RegisterArgs>(OnRegister));
            Listen(EventType.AddSongToQueue, new ClientEventCallback<AddSongToQueueArgs>(OnAddSongToQueue));
            Listen(EventType.PlaySongNext, new ClientEventCallback<PlaySongNextArgs>(OnPlaySongNext));
            Listen(EventType.PlayPlaylistNext, new ClientEventCallback<EventArgs>(OnPlayPlaylistNext));
        }

        private void OnSearchSongsServer(SearchSongOrArtistArgs args)
        {
            Task.Run(() =>
            {
                string search = args.Search;
                List<Song> foundSongs = _communicationManager.SearchSongsServer(search);
                ClientEvent.Fire(EventType.DisplaySongs, new DisplaySongsArgs { Songs = foundSongs });
            });
        }

        private void ConnectionStateUpdate(bool connected)
        {
            ClientEvent.Fire(EventType.UpdateConnectionState, new UpdateConnectionStateArgs { Connected = connected });
        }

        private void RecoverSession()
        {
            AuthManager.WaitForInstance();
            _authManager.RecoverSession();
        }

        private void OnRegister(RegisterArgs args)
        {
            Task.Run(() =>
            {
                string email = args.Email;
                string password = args.Password;

                ClientEvent.Fire(EventType.LongNetworkRequest, new LongNetworkRequestArgs { Started = true });
                _dualSocket.Connect();

                List<string>? errors;
                bool success = _authManager.Register(email, password, out errors);
                
                if (!success)
                {
                    ClientEvent.Fire(EventType.RegisterErrorsUpdate,
                        new RegisterErrorsUpdateArgs
                        {
                            Errors = errors! 
                        });
                }
                else
                {
                    ClientEvent.Fire(EventType.UpdateRememberMe, new UpdateRememberMeArgs { Email = email, Password = password });
                    ClientEvent.Fire(EventType.ResetPage, new ResetPageArgs { PageType = PageType.Register });
                    ClientEvent.Fire(EventType.UpdatePage, new UpdatePageArgs { PageType = PageType.LogIn });
                }
                ClientEvent.Fire(EventType.LongNetworkRequest, new LongNetworkRequestArgs { Started = false });
            });
        }
        
        private void OnLogIn(LogInArgs args)
        {
            Task.Run(() =>
            {
                ClientEvent.Fire(EventType.LongNetworkRequest, new LongNetworkRequestArgs { Started = true });
                _dualSocket.Connect();
                bool success = _authManager.LogIn(args.Email, args.Password, args.RememberMe);
                if (success)
                {
                    ClientEvent.Fire(EventType.LogInStateUpdate, new LogInStateUpdateArgs { LogInState = LogInState.LogInValid, Email = args.Email });
                    PlaylistResult result = _playlistManager.Sync();
                    if (result == PlaylistResult.Success)
                    {
                        DisplayPlaylistLinks(DisplayPlaylistLinksMode.None, _playlistManager.GetPlaylistLinks());
                    }
                    else
                    {
                        MessageBox.Show($"Could not sync playlists.", "Sync Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    ClientEvent.Fire(EventType.LogInStateUpdate,
                        new LogInStateUpdateArgs
                        {
                            LogInState = LogInState.LogInInvalid,
                        });
                }

                ClientEvent.Fire(EventType.LongNetworkRequest, new LongNetworkRequestArgs { Started = false });
            });
        }

        private void OnLogOut(EventArgs args)
        {
            Task.Run(() =>
            {
                _communicationManager.DisconnectFromServer();
                _authManager.NewSession();
                ResetApp();
                FillRememberMe();
                ClientEvent.Fire(EventType.LogInStateUpdate, new LogInStateUpdateArgs { LogInState = LogInState.LogOut });
                if (!_authManager.IsRememberMe())
                {
                    PlaylistResult result = _playlistManager.DeleteAllPlaylistsLocal();
                }
            });
        }

        private void ResetApp()
        {
            _mediaPlayer.ResetMediaPlayer();
            ClientEvent.Fire(EventType.ResetWindow, EventArgs.Empty);
        }

        /// <summary>
        /// Expects (int)index. It will ask the media player to move the song at index up the queue.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnMoveSongUpQueue(MoveSongArgs args)
        {
            _mediaPlayer.MoveSongUp(args.Index);
        }

        /// <summary>
        /// Expects (int)index. It will ask the media player to move the song at index down the queue.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnMoveSongDownQueue(MoveSongArgs args)
        {
            _mediaPlayer.MoveSongDown(args.Index);
        }

        /// <summary>
        /// Expects no parameters. It will ask the media player to delete the entire queue.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnDeleteQueue(EventArgs args)
        {
            _mediaPlayer.DeleteQueue();
        }

        /// <summary>
        /// Expects (int)index. It will ask the media player to remove song at index from queue.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnRemoveSongQueue(RemoveSongQueueArgs args)
        {
            _mediaPlayer.RemoveSongFromQueue(args.Index);
        }

        /// <summary>
        /// Expects (string)search. It will Seach for a song in the current playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnSearchPlaylist(SearchSongOrArtistArgs args)
        {
            Task.Run(() =>
            {
                List<Song> songs = _playlistManager.SearchPlaylistSongs(args.Search);
                ClientEvent.Fire(EventType.DisplayPlaylistSongs,
                    new DisplayPlaylistSongsArgs
                    {
                        Songs = songs,
                        CurrentPlaylist = _playlistManager.CurrentPlaylist
                    });
            });
        }

        /// <summary>
        /// Expects (float)volume from 0 to 100. It will ask the media player to change the volume from 0 to 1.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnChangeVolume(ChangeVolumeArgs args)
        {
            float volume = args.Volume100 / 100;
            _mediaPlayer.SetVolume(volume);
        }

        /// <summary>
        /// Expects no parameters. It delets the current playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnDeletePlaylist(EventArgs args)
        {
            PlaylistResult result = _playlistManager.DeleteCurrentPlaylist();
            if (result == PlaylistResult.Success)
            {
                DisplayPlaylistLinks(DisplayPlaylistLinksMode.Delete, _playlistManager.GetPlaylistLinks());
            }
        }

        /// <summary>
        /// Expects (Song)song and (string) playlist name. It will add the specified song to the specified playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnAddSongToPlaylist(AddSongToPlaylistArgs args)
        {
            Song song = args.Song;
            string playlistLink = args.PlaylistLink;

            PlaylistResult result = _playlistManager.AddSongToPlaylist(song, playlistLink);

            if (result == PlaylistResult.SongAlreadyInPlaylist)
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
        private void OnRenamePlaylist(RenamePlaylistArgs args)
        {
            string newName = args.NewName;

            PlaylistResult result = _playlistManager.RenameCurrentPlaylist(newName);

            if (result == PlaylistResult.AlreadyExists)
            {
                ClientEvent.Fire(EventType.PlaylistExists, new PlaylistExistsArgs { PlaylistName = newName });
            }

            else if (result == PlaylistResult.Success)
            {
                DisplayPlaylistLinks(DisplayPlaylistLinksMode.Rename, _playlistManager.GetPlaylistLinks(), newName);
            }
        }

        /// <summary>
        /// Expects (string)name. It will create an empty playlist with the name and display empty playlist window.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnCreateNewPlaylist(CreateNewPlaylistArgs args)
        {
            string playlistName = args.PlaylistName;

            PlaylistResult result = _playlistManager.CreateNewPlaylist(playlistName);

            if (result == PlaylistResult.Success)
            {
                DisplayPlaylistLinks(DisplayPlaylistLinksMode.New, _playlistManager.GetPlaylistLinks(), playlistName);
            }
            else if (result == PlaylistResult.AlreadyExists)
            {
                ClientEvent.Fire(EventType.PlaylistExists, new PlaylistExistsArgs { PlaylistName = playlistName });
            }
        }

        /// <summary>
        /// Expects no parameters. It will add the current playlist to queue.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnAddPlaylistToQueue(EventArgs args)
        {
            List<Song> playlistSongs = _playlistManager.GetCurrentPlaylistSongs();
            _mediaPlayer.AddPlaylistSongsToQueue(playlistSongs);
        }

        private void OnPlayPlaylistNext(EventArgs args)
        {
            List<Song> playlistSongs = _playlistManager.GetCurrentPlaylistSongs();
            _mediaPlayer.PlayPlaylistNext(playlistSongs);
        }

        /// <summary>
        /// Expects no parameters. It will play the current playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnPlayCurrentPlaylist(EventArgs args)
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
        private void OnUpdatePlaylist(UpdatePlaylistArgs args)
        {
            UpdatePlaylist(args.PlaylistLink);
        }

        /// <summary>
        /// Expects (Song)song and (string)playlist name. It will remove that song from that playlist.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnRemoveSongFromPlaylist(RemoveSongFromPlaylistArgs args)
        {
            Song song = args.Song;
            string playlistLink = args.PlaylistLink;
            PlaylistResult result = _playlistManager.RemoveSongFromPlaylist(song, playlistLink);
            if (result == PlaylistResult.Success)
            {
                UpdatePlaylist(playlistLink);
            }
        }

        /// <summary>
        /// Expects no parameters. It will execute methods after Main Window is ready.
        /// </summary>
        /// <param name="parameters"></param>
        private void OnWindowReady(EventArgs args)
        {
            AuthManager.WaitForInstance();
            FillRememberMe();
        }

        private void FillRememberMe()
        {
            string? email;
            string? password;
            bool rememberMe = _authManager.IsRememberMe(out email, out password);

            if (rememberMe && email != null && password != null)
            {
                ClientEvent.Fire(EventType.UpdateRememberMe,
                    new UpdateRememberMeArgs
                    {
                        Email = email,
                        Password = password
                    });
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
                Song song = internalRequest.Args.Song!;
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
            else if (internalRequest.Type == InternalRequestType.RepeatStateChange)
            {
                RepeatState repeatState = internalRequest.Args.RepeatState;
                ExecuteRepeatStateChange(repeatState);
            }
            else if (internalRequest.Type == InternalRequestType.ShuffleStateChange)
            {
                ShuffleState shuffleState = internalRequest.Args.ShuffleState;
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
            ClientEvent.Fire(EventType.UpdateRepeatState, new UpdateRepeatStateArgs { RepeatState = repeatState });
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
                ClientEvent.Fire(EventType.DisplayPlaylistLinks,
                    new DisplayPlaylistLinksArgs
                    {
                        PlaylistLinks = playlistLinks,
                        DisplayPlaylistLinksMode = displayPlaylistLinksMode
                    });
            }
            else if (displayPlaylistLinksMode == DisplayPlaylistLinksMode.New || displayPlaylistLinksMode == DisplayPlaylistLinksMode.Rename)
            {
                ClientEvent.Fire(EventType.DisplayPlaylistLinks,
                    new DisplayPlaylistLinksArgs
                    {
                        PlaylistLinks = playlistLinks,
                        DisplayPlaylistLinksMode = displayPlaylistLinksMode,
                        Active = active
                    });
            }
        }

        /// <summary>
        /// This method updates the current playlist.
        /// </summary>
        /// <param name="playlistLink"></param>
        private void UpdatePlaylist(string playlistLink)
        {
            Task.Run(() =>
            {
                _playlistManager.CurrentPlaylist = playlistLink;
                ClientEvent.Fire(EventType.DisplayPlaylistSongs,
                    new DisplayPlaylistSongsArgs
                    {
                        Songs = _playlistManager.GetPlaylistSongs(playlistLink),
                        CurrentPlaylist = _playlistManager.CurrentPlaylist
                    });
            });
        }

        /// <summary>
        /// Callback method for mediaplayer.
        /// </summary>
        /// <param name="songs"></param>
        private void DisplayQueue(List<(Song, int)> songs)
        {
            Task.Run(() =>
            {
                ClientEvent.Fire(EventType.DisplayQueue,
                    new DisplayQueueArgs
                    {
                        Songs = songs
                    }); ;
            });
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
                    ClientEvent.Fire(EventType.UpdateProgress,
                        new UpdateProgressArgs
                        {
                            Progress = progress.Progress,
                            CurrentTime = progress.CurrentTime
                        });
                }
            }
        }

        private void OnUpdateProgressBarState(UpdateProgressBarStateArgs args)
        {
            ProgressBarState state = args.ProgressBarState;
            if (state == ProgressBarState.Free)
            {
                double progress = args.Progress;
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
            ClientEvent.Fire(EventType.DisplayCurrentSong,
                new DisplayCurrentSongArgs
                {
                    SongName = song.SongName,
                    ArtistName = song.ArtistName,
                    DurationString = song.DurationString,
                    ImageBytes = song.ImageBinary,
                });
        }

        private void TerminateSongDataReceiveRequest()
        {
            _communicationManager.TerminateSongDataReceive();
        }

        /// <summary>
        /// Adds the specified song to mediaplayer queue.
        /// </summary>
        /// <param name="song"></param>
        private void OnAddSongToQueue(AddSongToQueueArgs args)
        {
            if (args.Song is not null)
            {
                _mediaPlayer.AddToQueue(args.Song);
            }
        }

        private void OnPlaySongNext(PlaySongNextArgs args)
        {
            if (args.Song is not null)
            {
                _mediaPlayer.PlaySongNext(args.Song);
            }
        }

        private void SendPlayState(PlayButtonState playButtonState)
        {
            ClientEvent.Fire(EventType.ChangePlayState, new ChangePlayStateArgs { PlayButtonState = playButtonState });
        }

        private void OnAddInternalRequest(InternalRequestArgs args)
        {
            _internalRequestQueue.Enqueue(new InternalRequest(args.InternalRequestType, args));
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
        private void Listen<T>(EventType eventType, ClientEventCallback<T> callback) where T : EventArgs
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
