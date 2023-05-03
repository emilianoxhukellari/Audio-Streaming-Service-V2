using Client_Application.Client.Core;
using Client_Application.Client.Event;
using Client_Application.Wpf.DynamicComponents;
using Client_Application.Wpf.UserWindows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client_Application.Wpf.UserControls
{
    /// <summary>
    /// Interaction logic for MainControl.xaml
    /// </summary>
    public partial class MainControl : UserControl
    {
        private ClientListener _clientListener;
        private SearchCanvas _searchCanvas;
        private PlaylistCanvas _playlistCanvas;
        private RepeatState _repeatState;
        private ShuffleState _shuffleState;
        private string ActiveLink { get; set; }
        private List<string> _playlistLinks;
        private readonly string _iconsRelativePath;

        ImageBrush _playButtonGreenImageBrush;
        ImageBrush _pauseButtonGreenImageBrush;
        ImageBrush _nextSongButtonDefaultImageBrush;
        ImageBrush _nextSongButtonHoverImageBrush;
        ImageBrush _previousSongButtonDefaultImageBrush;
        ImageBrush _previousSongButtonHoverImageBrush;
        ImageBrush _addPlaylistButtonDefaultImageBrush;
        ImageBrush _addPlaylistButtonHoverImageBrush;
        ImageBrush _removeQueueButtonDefaultImageBrush;
        ImageBrush _removeQueueButtonHoverImageBrush;
        ImageBrush _repeatButtonOffImageBrush;
        ImageBrush _repeatButtonOnImageBrush;
        ImageBrush _repeatButtonOneImageBrush;
        ImageBrush _shuffleButtonOffImageBrush;
        ImageBrush _shuffleButtonOnImageBrush;
        ImageBrush _removeButtonDefaultImageBrush;
        ImageBrush _removeButtonHoverImageBrush;
        ImageBrush _arrowUpDefaultImageBrush;
        ImageBrush _arrowDownDefaultImageBrush;
        ImageBrush _arrowUpHoverImageBrush;
        ImageBrush _arrowDownHoverImageBrush;
        ImageBrush _moreButtonImageBrush;
        ImageBrush _logOutButtonDeafaultImageBrush;
        ImageBrush _logOutButtonHoverImageBrush;

        public MainControl()
        {
            InitializeComponent();
            _iconsRelativePath = Config.Config.GetIconsRelativePath();
            InitializeIcons();
            _searchCanvas = new SearchCanvas(_playButtonGreenImageBrush, _moreButtonImageBrush);
            _playlistCanvas = new PlaylistCanvas(_playButtonGreenImageBrush, _moreButtonImageBrush);
            contentControl.Content = _searchCanvas;
            progressBar.Value = 1;
            _clientListener = new ClientListener();
            ActiveLink = "";
            _playlistLinks = new List<string>(0);
            _repeatState = RepeatState.RepeatOff;
            _shuffleState = ShuffleState.Unshuffled;
            SetButtonIconsInitialState();

            Listen(EventType.DisplaySongs, new ClientEventCallback<DisplaySongsArgs>(DisplaySongs));
            Listen(EventType.ChangePlayState, new ClientEventCallback<ChangePlayStateArgs>(ChangePlayPauseButton));
            Listen(EventType.DisplayCurrentSong, new ClientEventCallback<DisplayCurrentSongArgs>(DisplayCurrentSong));
            Listen(EventType.UpdateProgress, new ClientEventCallback<UpdateProgressArgs>(DisplayCurrentProgress));
            Listen(EventType.DisplayQueue, new ClientEventCallback<DisplayQueueArgs>(DisplayQueue));
            Listen(EventType.PlaylistExists, new ClientEventCallback<PlaylistExistsArgs>(DisplayPlaylistExsitsError));
            Listen(EventType.DisplayPlaylistLinks, new ClientEventCallback<DisplayPlaylistLinksArgs>(DisplayPlaylistLinks));
            Listen(EventType.UpdatePlaylistCanvas, new ClientEventCallback<UpdatePlaylistCanvasArgs>(ExecuteUpdatePlaylistCanvas));
            Listen(EventType.ShowPlaylistCanvas, new ClientEventCallback<EventArgs>(ExecuteShowPlaylistCanvas));
            Listen(EventType.DisplayPlaylistSongs, new ClientEventCallback<DisplayPlaylistSongsArgs>(ExecuteDisplayPlaylistSongs));
            Listen(EventType.UpdateRepeatState, new ClientEventCallback<UpdateRepeatStateArgs>(ExecuteUpdateRepeatState));
            Listen(EventType.UpdateConnectionState, new ClientEventCallback<UpdateConnectionStateArgs>(ExecuteUpdateConnectionState));
            Listen(EventType.ResetPage, new ClientEventCallback<ResetPageArgs>(ExecuteResetPage));
        }

        #region Methods

        private void SetSearchButtonActive()
        {
            searchButton.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 170, 127));
            SetActivePlaylistLink("");
        }

        private void SetSearchButtonInactive()
        {
            searchButton.BorderBrush = null;
        }

        public void Reset()
        {
            _repeatState = RepeatState.RepeatOff;
            repeatButton.Background = _repeatButtonOffImageBrush;
            _shuffleState = ShuffleState.Unshuffled;
            shuffleButton.Background = _shuffleButtonOffImageBrush;
            playButton.Background = _playButtonGreenImageBrush;
            imageContainer.Source = null;
            songNameLabel.Content = null;
            artistNameLabel.Content = null;
            timeMaxLabel.Content = "00:00";
            RemoveAllPlaylistLinks();
            _searchCanvas.Reset();
            _playlistCanvas.Reset();
            SetSearchButtonActive();
            contentControl.Content = _searchCanvas;
            volumeBar.Value = 100;
            volumeLabel.Content = "100";
            currentUserLabel.Content = string.Empty;
            ActiveLink = string.Empty;
        }

        public void SetCurrentUser(string username)
        {
            currentUserLabel.Content = username;
        }

        private void DisplayPlaylistLinks(DisplayPlaylistLinksArgs args)
        {
            string[] playlistLinks = args.PlaylistLinks;
            DisplayPlaylistLinksMode displayPlaylistLinksMode = args.DisplayPlaylistLinksMode;
            if (playlistLinks.Any())
            {
                if (displayPlaylistLinksMode == DisplayPlaylistLinksMode.Rename)
                {
                    ActiveLink = args.Active;
                    _playlistCanvas.CurrentPlaylistName = ActiveLink;
                }
                else if (displayPlaylistLinksMode == DisplayPlaylistLinksMode.New)
                {
                    ActiveLink = args.Active;
                    _playlistCanvas.CurrentPlaylistName = ActiveLink;
                    Dispatcher.Invoke(() =>
                    {
                        contentControl.Content = _playlistCanvas;
                        _playlistCanvas.RemoveAllSongs();
                        SetSearchButtonInactive();
                    });
                }
                else if (displayPlaylistLinksMode == DisplayPlaylistLinksMode.Delete)
                {
                    contentControl.Content = _searchCanvas;
                    SetSearchButtonActive();
                }
                RemoveAllPlaylistLinks();

                foreach (string playlistLink in playlistLinks)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (playlistLink == ActiveLink)
                        {
                            playlistStackPanel.Children.Add(new PlaylistLinkContainer(playlistLink, true));
                        }
                        else
                        {
                            playlistStackPanel.Children.Add(new PlaylistLinkContainer(playlistLink, false));
                        }
                        AddToPlaylistLinks(playlistLink);
                    });
                }
            }
            else
            {
                RemoveAllPlaylistLinks();
                Dispatcher.Invoke(() =>
                {
                    contentControl.Content = _searchCanvas;
                    SetSearchButtonActive();
                });
            }
        }

        private void AddToPlaylistLinks(string playlistLink)
        {
            _playlistLinks.Add(playlistLink);
            NotifyPlaylistLinksChanged();
        }

        private void NotifyPlaylistLinksChanged()
        {
            _searchCanvas.SetPlaylistLinks(_playlistLinks);
            _playlistCanvas.SetPlaylistLinks(_playlistLinks);
        }

        private void RemoveAllPlaylistLinks()
        {
            Dispatcher.Invoke(() =>
            {
                playlistStackPanel.Children.Clear();
                _playlistLinks.Clear();
                NotifyPlaylistLinksChanged();
            });
        }

        private void DisplayPlaylistExsitsError(PlaylistExistsArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Playlist \"{args.PlaylistName}\" Already Exists", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void SetActivePlaylistLink(string playlistLink)
        {
            foreach (PlaylistLinkContainer playlistLinkContainer in playlistStackPanel.Children)
            {
                if (playlistLink == playlistLinkContainer.PlaylistLink)
                {
                    playlistLinkContainer.SetStyleActive();
                }
                else
                {
                    playlistLinkContainer.SetStyleInactive();
                }
            }
        }

        private void DisplayQueue(DisplayQueueArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                queueStackPanel.Children.Clear();
            });

            foreach (var song in args.Songs)
            {
                Dispatcher.Invoke(() =>
                {
                    queueStackPanel.Children.Add(new QueueSongContainer(song.Item1, song.Item2, _arrowUpDefaultImageBrush, _arrowDownDefaultImageBrush, _removeButtonDefaultImageBrush));
                });
            }
        }

        private void DisplayCurrentProgress(UpdateProgressArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = 100 * args.Progress;
                timePassedLabel.Content = args.CurrentTime;
            });
        }

        private void DisplayCurrentSong(DisplayCurrentSongArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                songNameLabel.Content = args.SongName;
                artistNameLabel.Content = args.ArtistName;
                timeMaxLabel.Content = args.DurationString;
                imageContainer.Source = BinaryToImageSource(args.ImageBytes);
            });
        }

        private ImageSource? BinaryToImageSource(byte[] imageBinary)
        {
            return (ImageSource?)new ImageSourceConverter().ConvertFrom(imageBinary);
        }

        private void ChangePlayPauseButton(ChangePlayStateArgs args)
        {
            PlayButtonState playButtonState = args.PlayButtonState;
            if (playButtonState == PlayButtonState.Play)
            {
                Dispatcher.Invoke(() =>
                {
                    playButton.Background = _pauseButtonGreenImageBrush;
                });
            }
            else if (playButtonState == PlayButtonState.Pause)
            {
                Dispatcher.Invoke(() =>
                {
                    playButton.Background = _playButtonGreenImageBrush;
                });
            }
        }

        private void DisplaySongs(DisplaySongsArgs args)
        {
            List<Song> results = args.Songs;

            Dispatcher.Invoke(() =>
            {
                _searchCanvas.RemoveAllSongs();
            });
            _searchCanvas.DisplaySongsByOne(results);
        }

        private void Listen<T>(EventType eventType, ClientEventCallback<T> callback) where T : EventArgs
        {
            _clientListener.Listen(eventType, callback);
        }

        private void SetShuffleState()
        {
            if (_shuffleState == ShuffleState.Unshuffled)
            {
                _shuffleState = ShuffleState.Shuffled;
                ClientEvent.Fire(EventType.InternalRequest,
                    new InternalRequestArgs
                    {
                        InternalRequestType = InternalRequestType.ShuffleStateChange,
                        ShuffleState = _shuffleState
                    });

                shuffleButton.Background = _shuffleButtonOnImageBrush;
            }
            else if (_shuffleState == ShuffleState.Shuffled)
            {
                _shuffleState = ShuffleState.Unshuffled;
                ClientEvent.Fire(EventType.InternalRequest,
                    new InternalRequestArgs
                    {
                        InternalRequestType = InternalRequestType.ShuffleStateChange,
                        ShuffleState = _shuffleState
                    });
                shuffleButton.Background = _shuffleButtonOffImageBrush;
            }
        }

        private void SetRepeatState(RepeatState repeatState = RepeatState.None)
        {
            if (repeatState != RepeatState.None)
            {
                _repeatState = repeatState;
            }
            else
            {
                ChangeRepeatState();
            }

            if (_repeatState == RepeatState.RepeatOff)
            {
                ClientEvent.Fire(EventType.InternalRequest,
                    new InternalRequestArgs
                    {
                        InternalRequestType = InternalRequestType.RepeatStateChange,
                        RepeatState = RepeatState.RepeatOff
                    });
                repeatButton.Background = _repeatButtonOffImageBrush;
            }
            else if (_repeatState == RepeatState.RepeatOn)
            {
                ClientEvent.Fire(EventType.InternalRequest,
                    new InternalRequestArgs
                    {
                        InternalRequestType = InternalRequestType.RepeatStateChange,
                        RepeatState = RepeatState.RepeatOn
                    });
                repeatButton.Background = _repeatButtonOnImageBrush;
            }
            else if (_repeatState == RepeatState.OnRepeat)
            {
                ClientEvent.Fire(EventType.InternalRequest,
                    new InternalRequestArgs
                    {
                        InternalRequestType = InternalRequestType.RepeatStateChange,
                        RepeatState = RepeatState.OnRepeat
                    });
                repeatButton.Background = _repeatButtonOneImageBrush;
            }
        }

        private void ChangeRepeatState()
        {
            if (_repeatState == RepeatState.RepeatOff)
            {
                _repeatState = RepeatState.RepeatOn;
            }
            else if (_repeatState == RepeatState.RepeatOn)
            {
                _repeatState = RepeatState.OnRepeat;
            }
            else if (_repeatState == RepeatState.OnRepeat)
            {
                _repeatState = RepeatState.RepeatOff;
            }
        }

        private void InitializeIcons()
        {
            _playButtonGreenImageBrush = GetImageBrush("play_green.png");
            _pauseButtonGreenImageBrush = GetImageBrush("pause.png");
            _nextSongButtonDefaultImageBrush = GetImageBrush("next_default.png");
            _nextSongButtonHoverImageBrush = GetImageBrush("next_hover.png");
            _previousSongButtonDefaultImageBrush = GetImageBrush("previous_default.png");
            _previousSongButtonHoverImageBrush = GetImageBrush("previous_hover.png");
            _addPlaylistButtonDefaultImageBrush = GetImageBrush("add_default.png");
            _addPlaylistButtonHoverImageBrush = GetImageBrush("add_hover.png");
            _removeQueueButtonDefaultImageBrush = GetImageBrush("delete_default.png"); //remove_default.png
            _removeQueueButtonHoverImageBrush = GetImageBrush("delete_hover.png"); //remove_hover.png
            _repeatButtonOffImageBrush = GetImageBrush("repeat_many_off.png");
            _repeatButtonOnImageBrush = GetImageBrush("repeat_many_on.png");
            _repeatButtonOneImageBrush = GetImageBrush("repeat_one.png");
            _shuffleButtonOffImageBrush = GetImageBrush("shuffle_off.png");
            _shuffleButtonOnImageBrush = GetImageBrush("shuffle_on.png");
            _removeButtonDefaultImageBrush = GetImageBrush("remove_default.png"); //delete_default
            _removeButtonHoverImageBrush = GetImageBrush("remove_hover.png"); //delete_default
            _arrowUpDefaultImageBrush = GetImageBrush("arrow_up_default.png");
            _arrowDownDefaultImageBrush = GetImageBrush("arrow_down_default.png");
            _arrowUpHoverImageBrush = GetImageBrush("arrow_up_hover.png");
            _arrowDownHoverImageBrush = GetImageBrush("arrow_down_hover.png");
            _moreButtonImageBrush = GetImageBrush("more_white.png");
            _logOutButtonDeafaultImageBrush = GetImageBrush("log_out_default.png");
            _logOutButtonHoverImageBrush = GetImageBrush("log_out_hover.png");
        }

        private void SetButtonIconsInitialState()
        {
            playButton.Background = _playButtonGreenImageBrush;
            nextSongButton.Background = _nextSongButtonDefaultImageBrush;
            previousSongButton.Background = _previousSongButtonDefaultImageBrush;
            shuffleButton.Background = _shuffleButtonOffImageBrush;
            repeatButton.Background = _repeatButtonOffImageBrush;
            removeQueueButton.Background = _removeQueueButtonDefaultImageBrush;
            addPlaylistButton.Background = _addPlaylistButtonDefaultImageBrush;
            logOutButton.Background = _logOutButtonDeafaultImageBrush;
        }
        private ImageBrush GetImageBrush(string iconName)
        {
            ImageBrush imageBrush = new ImageBrush(GetImageSource(iconName));
            imageBrush.Stretch = Stretch.Uniform;
            return imageBrush;
        }

        private ImageSource? GetImageSource(string iconName)
        {
            return (ImageSource?)new ImageSourceConverter().ConvertFrom(File.ReadAllBytes($"{_iconsRelativePath}{iconName}"));
        }

        #endregion

        #region ClientEventHandlers

        private void ExecuteResetPage(ResetPageArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                if (args.PageType == PageType.Main)
                {
                    Reset();
                }
            });
        }

        private void ExecuteUpdateRepeatState(UpdateRepeatStateArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                SetRepeatState(args.RepeatState);
            });
        }

        private void ExecuteUpdateConnectionState(UpdateConnectionStateArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                if (args.Connected)
                {
                    connectionIndicator.Fill = Brushes.Green;
                }
                else
                {
                    connectionIndicator.Fill = Brushes.Red;
                }
            });
        }

        private void ExecuteDisplayPlaylistSongs(DisplayPlaylistSongsArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                _playlistCanvas.RemoveAllSongs();
            });
            _playlistCanvas.DisplaySongsByOne(args.Songs);
        }

        private void ExecuteUpdatePlaylistCanvas(UpdatePlaylistCanvasArgs args)
        {
            string playlistLink = args.PlaylistLink;
            _playlistCanvas.CurrentPlaylistName = playlistLink;
            SetSearchButtonInactive();
            SetActivePlaylistLink(playlistLink);
            ClientEvent.Fire(EventType.UpdatePlaylist,
                new UpdatePlaylistArgs
                {
                    PlaylistLink = playlistLink
                });
        }

        private void ExecuteShowPlaylistCanvas(EventArgs args)
        {
            contentControl.Content = _playlistCanvas;
        }

        #endregion

        #region EventHandlers

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.InternalRequest,
                    new InternalRequestArgs
                    {
                        InternalRequestType = InternalRequestType.PlayPauseStateChange,
                    });
        }

        private void shuffleButton_Click(object sender, RoutedEventArgs e)
        {
            SetShuffleState();
        }


        private void repeatButton_Click(object sender, RoutedEventArgs e)
        {
            SetRepeatState();
        }

        private void nextSongButton_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.InternalRequest, new InternalRequestArgs { InternalRequestType = InternalRequestType.NextSong });
        }

        private void previousSongButton_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.InternalRequest, new InternalRequestArgs { InternalRequestType = InternalRequestType.PreviousSong });
        }

        private void progressBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ClientEvent.Fire(EventType.UpdateProgressBarState, new UpdateProgressBarStateArgs { ProgressBarState = ProgressBarState.Busy });
        }

        private void progressBar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ClientEvent.Fire(EventType.UpdateProgressBarState,
                new UpdateProgressBarStateArgs
                {
                    ProgressBarState = ProgressBarState.Free,
                    Progress = progressBar.Value
                });
        }

        private void volumeBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            volumeLabel.Content = (int)volumeBar.Value;
            ClientEvent.Fire(EventType.ChangeVolume, new ChangeVolumeArgs { Volume100 = (float)volumeBar.Value });
        }

        private void addPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            AddPlaylistWindow addPlaylistWindow = new AddPlaylistWindow();
            addPlaylistWindow.ShowDialog();
        }

        private void removeQueueButton_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.DeleteQueue, EventArgs.Empty);
        }

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            contentControl.Content = _searchCanvas;
            SetSearchButtonActive();
        }

        

        private void addPlaylistButton_MouseEnter(object sender, MouseEventArgs e)
        {
            addPlaylistButton.Background = _addPlaylistButtonHoverImageBrush;
        }

        private void addPlaylistButton_MouseLeave(object sender, MouseEventArgs e)
        {
            addPlaylistButton.Background = _addPlaylistButtonDefaultImageBrush;
        }

        private void removeQueueButton_MouseEnter(object sender, MouseEventArgs e)
        {
            removeQueueButton.Background = _removeQueueButtonHoverImageBrush;
        }

        private void removeQueueButton_MouseLeave(object sender, MouseEventArgs e)
        {
            removeQueueButton.Background = _removeQueueButtonDefaultImageBrush;
        }

        private void logOutButton_Click(object sender, RoutedEventArgs e)
        {
            LogOut window = new LogOut();
            window.ShowDialog();
        }

        private void logOutButton_MouseEnter(object sender, MouseEventArgs e)
        {
            logOutButton.Background = _logOutButtonHoverImageBrush;
        }

        private void logOutButton_MouseLeave(object sender, MouseEventArgs e)
        {
            logOutButton.Background = _logOutButtonDeafaultImageBrush;
        }


        private void nextSongButton_MouseEnter(object sender, MouseEventArgs e)
        {
            nextSongButton.Background = _nextSongButtonHoverImageBrush;
        }

        private void nextSongButton_MouseLeave(object sender, MouseEventArgs e)
        {
            nextSongButton.Background = _nextSongButtonDefaultImageBrush;
        }

        private void previousSongButton_MouseEnter(object sender, MouseEventArgs e)
        {
            previousSongButton.Background = _previousSongButtonHoverImageBrush;
        }

        private void previousSongButton_MouseLeave(object sender, MouseEventArgs e)
        {
            previousSongButton.Background = _previousSongButtonDefaultImageBrush;
        }
        #endregion
    }
}
