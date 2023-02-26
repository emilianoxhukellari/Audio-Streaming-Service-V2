using Client_Application.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client_Application.DynamicVisualComponents
{
    public class PlaylistCanvas : Canvas
    {
        PlaylistSearchBox _playlistSearchBox;
        Button _playThisPlaylistButton;
        ImageBrush _playButtonImageBrush;
        ImageBrush _moreButtonImageBrush;
        Button _playlistMoreButton;
        Label _currentPlaylistLabel;
        ScrollablePlaylistStackPanel _scrollablePlaylistStackPanel;
        List<string> _playlistLinks = new List<string>();
        private string myVar = "";

        public string CurrentPlaylistName
        {
            get { return myVar; }
            set { myVar = value; _currentPlaylistLabel.Content = value; }
        }

        public PlaylistCanvas(ImageBrush playButtonImageBrush, ImageBrush moreButtonImageBrush) : base()
        {
            Width = 440;
            _playButtonImageBrush = playButtonImageBrush;
            _moreButtonImageBrush = moreButtonImageBrush;
            _playlistSearchBox = new PlaylistSearchBox();
            _scrollablePlaylistStackPanel = new ScrollablePlaylistStackPanel();
            _playThisPlaylistButton = new Button();
            _playlistMoreButton = new Button();
            _currentPlaylistLabel = new Label();
            InitializePlayThisPlaylistButton(_playThisPlaylistButton);
            InitializePlaylistMoreButton(_playlistMoreButton);
            InitializeCurrentPlaylistLabel(_currentPlaylistLabel);
            Children.Add(_playlistSearchBox);
            Children.Add(_scrollablePlaylistStackPanel);
            Children.Add(_playThisPlaylistButton);
            Children.Add(_playlistMoreButton);
            Children.Add(_currentPlaylistLabel);
            CurrentPlaylistName = "";
        }

        public void Reset()
        {
            _playlistSearchBox.Reset();
            RemoveAllSongs();
        }

        private void InitializePlaylistMoreButton(Button playlistMoreButton)
        {
            playlistMoreButton.Style = (Style)FindResource("MyButton");
            playlistMoreButton.Width = 25;
            playlistMoreButton.Height = 25;
            Canvas.SetTop(playlistMoreButton, 43);
            Canvas.SetRight(playlistMoreButton, 14);
            playlistMoreButton.Background = _moreButtonImageBrush;
            playlistMoreButton.ContextMenu = new ContextMenu();

            MenuItem addToQueue = new MenuItem();
            addToQueue.Header = "Add to queue";
            addToQueue.Click += AddToQueue_Click;
            playlistMoreButton.ContextMenu.Items.Add(addToQueue);

            MenuItem deletePlaylist = new MenuItem();
            deletePlaylist.Header = "Delete playlist";
            deletePlaylist.Click += DeletePlaylist_Click;
            playlistMoreButton.ContextMenu.Items.Add(deletePlaylist);

            MenuItem renamePlaylist = new MenuItem();
            renamePlaylist.Header = "Rename Playlist";
            renamePlaylist.Click += RenamePlaylist_Click;
            playlistMoreButton.ContextMenu.Items.Add(renamePlaylist);

            playlistMoreButton.Click += PlaylistMoreButton_Click;
        }

        private void PlaylistMoreButton_Click(object sender, RoutedEventArgs e)
        {
            _playlistMoreButton.ContextMenu.IsOpen = true;
        }

        private void RenamePlaylist_Click(object sender, RoutedEventArgs e)
        {
            RenamePlaylistWindow renamePlaylistWindow = new RenamePlaylistWindow();
            renamePlaylistWindow.Show();
        }

        private void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.DeletePlaylist);
        }

        private void AddToQueue_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.AddPlaylistToQueue);
        }


        private void InitializeCurrentPlaylistLabel(Label currentPlaylistLabel)
        {
            currentPlaylistLabel.Content = CurrentPlaylistName;
            Canvas.SetTop(currentPlaylistLabel, 1);
            Canvas.SetLeft(currentPlaylistLabel, 32);
            currentPlaylistLabel.Foreground = new SolidColorBrush(Colors.White);
            currentPlaylistLabel.FontSize = 26;
        }

        private void InitializePlayThisPlaylistButton(Button playThisPlaylistButton)
        {
            playThisPlaylistButton.Style = (Style)FindResource("MyButton");
            playThisPlaylistButton.Width = 22;
            playThisPlaylistButton.Height = 22;
            Canvas.SetTop(playThisPlaylistButton, 9);
            Canvas.SetLeft(playThisPlaylistButton, 00);
            playThisPlaylistButton.Background = _playButtonImageBrush;
            playThisPlaylistButton.Click += PlayThisPlaylistButton_Click;
        }

        private void PlayThisPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.PlayCurrentPlaylist);
        }

        public void SetPlaylistLinks(List<string> playlistLinks)
        {
            _playlistLinks.Clear();
            foreach (var element in playlistLinks)
            {
                _playlistLinks.Add(element);
            }

            foreach (var child in _scrollablePlaylistStackPanel.PlaylistSongContainers)
            {
                if (child.GetType() == typeof(PlaylistSongContainer))
                    ((PlaylistSongContainer)child).ResetMenus(playlistLinks.ToArray());
            }
        }

        public void DisplaySongsByOne(List<Song> songs)
        {
            foreach(Song song in songs)
            {
                Dispatcher.Invoke(() =>
                {
                    _scrollablePlaylistStackPanel.Add(new PlaylistSongContainer(song, CurrentPlaylistName, _playlistLinks.ToArray(), _playButtonImageBrush, _moreButtonImageBrush));
                });
            }
        }


        public void RemoveAllSongs()
        {
            _scrollablePlaylistStackPanel.Clear();
        }
    }

    public class PlaylistSearchBox : TextBox
    {
        public PlaylistSearchBox() : base()
        {
            Canvas.SetLeft(this, 0);
            Canvas.SetTop(this, 40);
            TextWrapping = TextWrapping.Wrap;
            Width = 390;
            Height = 30;
            FontSize = 18;
            TextChanged += PlaylistSearchBox_TextChanged;
            Text = "";
        }

        public void Reset()
        {
            Text = "";
        }

        private void PlaylistSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClientEvent.Fire(EventType.SearchPlaylist, Text);
        }
    }

    public class ScrollablePlaylistStackPanel : ScrollViewer
    {
        StackPanel _playlistStackPanel;

        public List<PlaylistSongContainer> PlaylistSongContainers;
        public UIElementCollection Children { get { return _playlistStackPanel.Children; } }
        public ScrollablePlaylistStackPanel() : base()
        {
            _playlistStackPanel = new StackPanel();
            Content = _playlistStackPanel;
            Width = 440;
            Height = 420;
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            Canvas.SetTop(this, 80);
            PlaylistSongContainers = new List<PlaylistSongContainer>(0);
        }

        public void Add(PlaylistSongContainer playlistSongContainer)
        {
            PlaylistSongContainers.Add(playlistSongContainer);
            Children.Add(playlistSongContainer);
        }

        public void Clear()
        {
            PlaylistSongContainers.Clear();
            Children.Clear();
            GC.Collect();
        }
    }

    public class PlaylistSongContainer : Canvas
    {
        public Song Song { get; set; }
        public Image Image { get; set; }
        public Label SongNameInner { get; set; }
        public Label ArtistNameInner { get; set; }
        public Label DurationLabel { get; set; }
        public Button PlayThisButton { get; set; }
        public Button MoreButton { get; set; }
        public string ThisPlaylist { get; set; }
        public ImageBrush PlayButtonImageBrush { get; set; }
        public ImageBrush MoreButtonImageBrush { get; set; }
        public PlaylistSongContainer(Song song, string thisPlaylist, string[] playlistLinks, ImageBrush playButtonImageBrush, ImageBrush moreButtonImageBrush) : base()
        {
            Song = song;
            Height = 60;
            Width = 440;
            Background = new SolidColorBrush(Color.FromRgb(35, 35, 35));
            Margin = new Thickness(0, 6, 0, 0);

            Image = new Image();
            SongNameInner = new Label();
            ArtistNameInner = new Label();
            DurationLabel = new Label();
            PlayThisButton = new Button();
            MoreButton = new Button();
            ThisPlaylist = thisPlaylist;
            PlayButtonImageBrush = playButtonImageBrush;
            MoreButtonImageBrush = moreButtonImageBrush;

            InitializePlaylistSongContainerImage(Image);
            InitializePlaylistSongNameInnerLabel(SongNameInner);
            InitializePlaylistArtistNameInnerLabel(ArtistNameInner);
            InitializeSearchSongContainerDurationLabel(DurationLabel);
            InitializePlaylistPlayThisButton(PlayThisButton);
            InitializePlaylistMoreButton(MoreButton, playlistLinks);

            Children.Add(Image);
            Children.Add(SongNameInner);
            Children.Add(ArtistNameInner);
            Children.Add(DurationLabel);
            Children.Add(PlayThisButton);
            Children.Add(MoreButton);
        }
        private void InitializeSearchSongContainerDurationLabel(Label label)
        {
            label.Width = 45;
            label.Height = 26;
            Canvas.SetLeft(label, 280);
            Canvas.SetTop(label, 20);
            label.Foreground = new SolidColorBrush(Colors.White);
            label.Content = Song.DurationString;
        }
        private void InitializePlaylistSongContainerImage(Image image)
        {
            image.Height = 60;
            image.Width = 60;
            Canvas.SetTop(image, 0);
            Canvas.SetLeft(image, 0);
            ImageSource? source = (ImageSource?)new ImageSourceConverter().ConvertFrom(Song.ImageBinary);
            image.Source = source;
        }

        private void InitializePlaylistSongNameInnerLabel(Label songNameInnerLabel)
        {
            songNameInnerLabel.Width = 180;
            songNameInnerLabel.Height = 24;
            Canvas.SetTop(songNameInnerLabel, 6);
            Canvas.SetLeft(songNameInnerLabel, 66);
            songNameInnerLabel.Foreground = new SolidColorBrush(Colors.White);
            songNameInnerLabel.FontSize = 15;
            songNameInnerLabel.Content = Song.SongName;
        }

        private void InitializePlaylistArtistNameInnerLabel(Label artistNameInnerLabel)
        {
            artistNameInnerLabel.Width = 180;
            artistNameInnerLabel.Height = 20;
            Canvas.SetBottom(artistNameInnerLabel, 5);
            Canvas.SetLeft(artistNameInnerLabel, 66);
            artistNameInnerLabel.Foreground = new SolidColorBrush(Colors.White);
            artistNameInnerLabel.FontSize = 14;
            artistNameInnerLabel.Content = Song.ArtistName;
        }


        private void InitializePlaylistPlayThisButton(Button playThisButton)
        {
            playThisButton.Style = (Style)FindResource("MyButton");
            playThisButton.Width = 20;
            playThisButton.Height = 20;
            Canvas.SetTop(playThisButton, 22);
            Canvas.SetLeft(playThisButton, 346);
            playThisButton.Background = PlayButtonImageBrush;
            playThisButton.Click += PlayThisButton_Click;
        }

        private void PlayThisButton_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.InternalRequest, InternalRequestType.PlayThis, Song);
        }

        private void InitializePlaylistMoreButton(Button moreButton, string[] playlistLinks)
        {
            moreButton.Style = (Style)FindResource("MyButton");
            moreButton.Width = 20;
            moreButton.Height = 20;
            Canvas.SetTop(moreButton, 22);
            Canvas.SetLeft(moreButton, 380);
            moreButton.Background = MoreButtonImageBrush;
            moreButton.ContextMenu = new ContextMenu();

            MenuItem addToQueue = new MenuItem();
            addToQueue.Header = "Add to queue";
            addToQueue.Click += AddToQueue_Click;
            moreButton.ContextMenu.Items.Add(addToQueue);

            MenuItem removeFromPlaylist = new MenuItem();
            removeFromPlaylist.Header = "Remove from playlist";
            removeFromPlaylist.Click += RemoveFromPlaylist_Click;
            moreButton.ContextMenu.Items.Add(removeFromPlaylist);


            foreach (string playlistLink in playlistLinks)
            {
                if (playlistLink != ThisPlaylist)
                {
                    CustomMenuItem item = new CustomMenuItem(Song, playlistLink);
                    moreButton.ContextMenu.Items.Add(item.MenuItem);
                }
            }

            moreButton.Click += ShowMoreButtonMenu;
        }

        public void ResetMenus(string[] playlistLinks)
        {
            MoreButton.ContextMenu = new ContextMenu();

            MenuItem addToQueue = new MenuItem();
            addToQueue.Header = "Add to queue";
            addToQueue.Click += AddToQueue_Click;
            MoreButton.ContextMenu.Items.Add(addToQueue);

            MenuItem removeFromPlaylist = new MenuItem();
            removeFromPlaylist.Header = "Remove from playlist";
            removeFromPlaylist.Click += RemoveFromPlaylist_Click;
            MoreButton.ContextMenu.Items.Add(removeFromPlaylist);

            foreach (string playlistLink in playlistLinks)
            {
                CustomMenuItem item = new CustomMenuItem(Song, playlistLink);
                MoreButton.ContextMenu.Items.Add(item.MenuItem);
            }
        }
        private void ShowMoreButtonMenu(object sender, RoutedEventArgs e)
        {
            MoreButton.ContextMenu.IsOpen = true;
        }

        private void RemoveFromPlaylist_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.RemoveSongFromPlaylist, Song, ThisPlaylist);
        }

        private void AddToQueue_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.InternalRequest, InternalRequestType.AddSongToQueue, Song);
        }
    }
}
