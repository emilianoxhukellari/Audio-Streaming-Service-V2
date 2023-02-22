using Client_Application.Client;
using Client_Application.DynamicVisualComponents;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Client_Application
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
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

        public MainWindow()
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

            Listen(EventType.DisplaySongs, new ClientEventCallback(DisplaySongs));
            Listen(EventType.ChangePlayState, new ClientEventCallback(ChangePlayPauseButton));
            Listen(EventType.DisplayCurrentSong, new ClientEventCallback(DisplayCurrentSong));
            Listen(EventType.UpdateProgress, new ClientEventCallback(DisplayCurrentProgress));
            Listen(EventType.DisplayQueue, new ClientEventCallback(DisplayQueue));
            Listen(EventType.PlaylistExists, new ClientEventCallback(DisplayPlaylistExsitsError));
            Listen(EventType.DisplayPlaylistLinks, new ClientEventCallback(DisplayPlaylistLinks));
            Listen(EventType.UpdatePlaylistCanvas, new ClientEventCallback(ExecuteUpdatePlaylistCanvas));
            Listen(EventType.ShowPlaylistCanvas, new ClientEventCallback(ExecuteShowPlaylistCanvas));
            Listen(EventType.DisplayPlaylistSongs, new ClientEventCallback(ExecuteDisplayPlaylistSongs));
            Listen(EventType.UpdateRepeatState, new ClientEventCallback(ExecuteUpdateRepeatState));
            Listen(EventType.LogInStateUpdate, new ClientEventCallback(ExecuteLogInStateUpdate));
            Listen(EventType.UpdateRememberMe, new ClientEventCallback(ExecuteUpdateRememberMe));
            Listen(EventType.ResetWindow, new ClientEventCallback(ExecuteResetWindow));
            Listen(EventType.UpdateConnectionState, new ClientEventCallback(ExecuteUpdateConnectionState));

            new ClientEvent(EventType.WindowReady, true);

            SetButtonIconsInitialState();
        }


        private void ExecuteUpdateConnectionState(params object[] parameters)
        {
            bool connected = (bool)parameters[0];

            Dispatcher.Invoke(() =>
            {
                if (connected)
                {
                    connectionIndicator.Fill = Brushes.Green;
                }
                else
                {
                    connectionIndicator.Fill = Brushes.Red;
                }
            });
        }

        private void ExecuteResetWindow(params object[] parameters)
        {
            Dispatcher.Invoke(() =>
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
                currentUserLabel.Content = "";
            });
        }

        private void ExecuteUpdateRememberMe(params object[] parameters)
        {
            string email = (string)parameters[0];
            string password = (string)parameters[1];

            Dispatcher.Invoke(() =>
            {
                rememeberMeCheckBox.IsChecked = true;
                emailTextBox.Text = email;
                passwordBox.Password = password;
            });
        }

        /// <summary>
        /// The user email and password did not match.
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteLogInStateUpdate(params object[] parameters)
        {
            LogInState logInState = (LogInState)parameters[0];
            string email = (string)parameters[1];

            if(logInState == LogInState.LogInValid)
            {
                Dispatcher.Invoke(() =>
                {
                    currentUserLabel.Content = email;
                    DisplayLoggedInPage();
                });
            }
            else if(logInState == LogInState.LogInInvalid)
            {
                Dispatcher.Invoke(() =>
                {
                    InvalidLoginNotify();
                });
            }
            else if(logInState == LogInState.LogOut)
            {
                Dispatcher.Invoke(() =>
                {
                    DisplayLogInPage();
                });
            }
        }

        private void ExecuteUpdateRepeatState(params object[] parameters)
        {
            RepeatState repeatState = (RepeatState)parameters[0];
            Dispatcher.Invoke(() =>
            {
                SetRepeatState(repeatState);
            });
        }

        private ImageSource? GetImageSource(string iconName)
        {
            return (ImageSource?)new ImageSourceConverter().ConvertFrom(File.ReadAllBytes($"{_iconsRelativePath}{iconName}"));
        }

        private ImageBrush GetImageBrush(string iconName)
        {
            ImageBrush imageBrush = new ImageBrush(GetImageSource(iconName));
            imageBrush.Stretch = Stretch.Uniform;
            return imageBrush;
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

        private void ExecuteDisplayPlaylistSongs(params object[] parameters)
        {
            Dispatcher.Invoke(() =>
            {
                _playlistCanvas.RemoveAllSongs();
                _playlistCanvas.DisplaySongs((List<Song>)parameters[0]);
            });
        }

        private void ExecuteUpdatePlaylistCanvas(params object[] parameters)
        {
            string playlistLink = (string)parameters[0];
            _playlistCanvas.CurrentPlaylistName = playlistLink;
            SetSearchButtonInactive();
            SetActivePlaylistLink(playlistLink);
            new ClientEvent(EventType.UpdatePlaylist, true, playlistLink);
        }

        private void ExecuteShowPlaylistCanvas(params object[] parameters)
        {
            contentControl.Content = _playlistCanvas;
        }

        private void DisplayPlaylistLinks(params object[] parameters)
        {
            string[] playlistLinks = (string[])parameters[0];
            DisplayPlaylistLinksMode displayPlaylistLinksMode = (DisplayPlaylistLinksMode)parameters[1];
            if (playlistLinks.Any())
            {
                if (displayPlaylistLinksMode == DisplayPlaylistLinksMode.Rename)
                {
                    ActiveLink = (string)parameters[2];
                    _playlistCanvas.CurrentPlaylistName = ActiveLink;
                }
                else if (displayPlaylistLinksMode == DisplayPlaylistLinksMode.New)
                {
                    ActiveLink = (string)parameters[2];
                    contentControl.Content = _playlistCanvas;
                    _playlistCanvas.CurrentPlaylistName = ActiveLink;
                    Dispatcher.Invoke(() =>
                    {
                        _playlistCanvas.RemoveAllSongs();
                    });
                }
                else if (displayPlaylistLinksMode == DisplayPlaylistLinksMode.Delete)
                {
                    contentControl.Content = _searchCanvas;
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
                contentControl.Content = _searchCanvas;
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

        private void DisplayPlaylistExsitsError(params object[] parameters)
        {
            string playlistName = (string)parameters[0];
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"Playlist \"{playlistName}\" Already Exists", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void SetActivePlaylistLink(string playlistLink)
        {
            foreach (PlaylistLinkContainer playlistLinkContainer in playlistStackPanel.Children)
            {
                if(playlistLink == playlistLinkContainer.PlaylistLink)
                {
                    playlistLinkContainer.SetStyleActive();
                }
                else
                {
                    playlistLinkContainer.SetStyleInactive();
                }
            }
        }

        private string GetActivePlaylistLink()
        {
            foreach (PlaylistLinkContainer playlistLinkContainer in playlistStackPanel.Children)
            {
                if (playlistLinkContainer.IsActive)
                {
                    return playlistLinkContainer.PlaylistLink;
                }
            }
            return "";
        }

        private void DisplayQueue(params object[] parameters)
        {
            List<(Song, int)> songs = (List<(Song, int)>)parameters[0];

            Dispatcher.Invoke(() =>
            {
                queueStackPanel.Children.Clear();
                foreach (var song in songs)
                {
                    queueStackPanel.Children.Add(new QueueSongContainer(song.Item1, song.Item2, _arrowUpDefaultImageBrush, _arrowDownDefaultImageBrush, _removeButtonDefaultImageBrush));
                }
            });
        }

        private void DisplayCurrentProgress(params object[] parameters)
        {
            double progress = (double)parameters[0];
            string currentTime = (string)parameters[1];

            Dispatcher.Invoke(() =>
            {
                progressBar.Value = 100 * progress;
                timePassedLabel.Content = currentTime;
            });
        }

        private void DisplayCurrentSong(params object[] parameters)
        {
            string songName = (string)parameters[0];
            string artistName = (string)parameters[1];
            string durationString = (string)parameters[2];
            byte[] imageBinary = (byte[])parameters[3];
            Dispatcher.Invoke(() =>
            {
                songNameLabel.Content = songName;
                artistNameLabel.Content = artistName;
                timeMaxLabel.Content = durationString;
                imageContainer.Source = BinaryToImageSource(imageBinary);
            });
        }

        private ImageSource? BinaryToImageSource(byte[] imageBinary)
        {
            return (ImageSource?)new ImageSourceConverter().ConvertFrom(imageBinary);
        }

        private void ChangePlayPauseButton(params object[] parameters)
        {
            PlayButtonState playButtonState = (PlayButtonState)parameters[0];
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

        private void DisplaySongs(params object[] parameters)
        {
            List<Song> results = (List<Song>)parameters[0];

            Dispatcher.Invoke(() =>
            {
                _searchCanvas.RemoveAllSongs();

                if (results.Count > 0)
                {
                    _searchCanvas.DisplaySongs(results);
                }
            });
        }

        private void Listen(EventType eventType, ClientEventCallback serverEventCallback)
        {
            _clientListener.Listen(eventType, serverEventCallback);
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            new ClientEvent(EventType.InternalRequest, true, InternalRequestType.PlayPauseStateChange);
        }

        private void shuffleButton_Click(object sender, RoutedEventArgs e)
        {
            SetShuffleState();
        }

        private void SetShuffleState()
        {
            if (_shuffleState == ShuffleState.Unshuffled)
            {
                _shuffleState = ShuffleState.Shuffled;
                new ClientEvent(EventType.InternalRequest, true, InternalRequestType.ShuffleStateChange, _shuffleState);
                shuffleButton.Background = _shuffleButtonOnImageBrush;
            }
            else if (_shuffleState == ShuffleState.Shuffled)
            {
                _shuffleState = ShuffleState.Unshuffled;
                new ClientEvent(EventType.InternalRequest, true, InternalRequestType.ShuffleStateChange, _shuffleState);
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
                new ClientEvent(EventType.InternalRequest, true, InternalRequestType.RepeatStateChange, RepeatState.RepeatOff);
                repeatButton.Background = _repeatButtonOffImageBrush;
            }
            else if (_repeatState == RepeatState.RepeatOn)
            {
                new ClientEvent(EventType.InternalRequest, true, InternalRequestType.RepeatStateChange, RepeatState.RepeatOn);
                repeatButton.Background = _repeatButtonOnImageBrush;
            }
            else if (_repeatState == RepeatState.OnRepeat)
            {
                new ClientEvent(EventType.InternalRequest, true, InternalRequestType.RepeatStateChange, RepeatState.OnRepeat);
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

        private void repeatButton_Click(object sender, RoutedEventArgs e)
        {
            SetRepeatState();
        }

        private void nextSongButton_Click(object sender, RoutedEventArgs e)
        {
            new ClientEvent(EventType.InternalRequest, true, InternalRequestType.NextSong);
        }

        private void previousSongButton_Click(object sender, RoutedEventArgs e)
        {
            new ClientEvent(EventType.InternalRequest, true, InternalRequestType.PreviousSong);
        }

        private void progressBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            new ClientEvent(EventType.UpdateProgressBarState, true, ProgressBarState.Busy);
        }

        private void progressBar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            new ClientEvent(EventType.UpdateProgressBarState, true, ProgressBarState.Free, progressBar.Value);
        }

        private void volumeBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            volumeLabel.Content = (int)volumeBar.Value;
            new ClientEvent(EventType.ChangeVolume, true, (float)volumeBar.Value);
        }

        private void addPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            AddPlaylistWindow addPlaylistWindow = new AddPlaylistWindow();
            addPlaylistWindow.Show();
        }

        private void removeQueueButton_Click(object sender, RoutedEventArgs e)
        {
            new ClientEvent(EventType.DeleteQueue, true);
        }

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            contentControl.Content = _searchCanvas;
            SetSearchButtonActive();
        }

        private void SetSearchButtonActive()
        {
            searchButton.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 170, 127));
            SetActivePlaylistLink("");
        }

        private void SetSearchButtonInactive()
        {
            searchButton.BorderBrush = null;
        }

        private void searchButton_MouseEnter(object sender, MouseEventArgs e)
        {
            searchButton.Foreground = new SolidColorBrush(Colors.Black);
        }

        private void searchButton_MouseLeave(object sender, MouseEventArgs e)
        {
            searchButton.Foreground = new SolidColorBrush(Colors.White);
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
            MessageBoxResult result = MessageBox.Show("Are you sure you want to log out?", "Confirmation", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                new ClientEvent(EventType.LogOut, true);
            }
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

        private void loginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = emailTextBox.Text;
            string password = passwordBox.Password;

            if(email == string.Empty)
            {
                emailErrorLabel.Content = "You must supply an Email";
            }    

            if(password == string.Empty)
            {
                errorPasswordLabel.Content = "You must supply a Password";
            }

            if(email != string.Empty && password != string.Empty)
            {
                bool rememberMe;
                if(rememeberMeCheckBox.IsChecked == true)
                {
                    rememberMe = true;
                }
                else
                {
                    rememberMe = false;
                }

                new ClientEvent(EventType.LogIn, true, email, password, rememberMe);
            }
        }

        private void InvalidLoginNotify()
        {
            loginErrorLabel.Content = "Username or Password Incorrect";
        }

        private void DisplayLogInPage()
        {
            grid.Visibility = Visibility.Collapsed;
            loginGrid.Visibility = Visibility.Visible;
        }

        private void DisplayLoggedInPage()
        {
            grid.Visibility = Visibility.Visible;
            loginGrid.Visibility = Visibility.Collapsed;
            emailErrorLabel.Content = string.Empty;
            errorPasswordLabel.Content = string.Empty;
            loginErrorLabel.Content = string.Empty;
            emailTextBox.Text = string.Empty;
            passwordBox.Password = string.Empty;
        }


        private void emailTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            emailErrorLabel.Content = string.Empty;
            loginErrorLabel.Content = string.Empty;
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            errorPasswordLabel.Content = string.Empty;
            loginErrorLabel.Content = string.Empty;
        }

        
    }
}
