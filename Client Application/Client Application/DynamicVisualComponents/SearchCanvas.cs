using Client_Application.Client;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client_Application.DynamicVisualComponents
{
    public class SearchCanvas : Canvas
    {
        SearchBox _searchBox;
        ScrollableSearchStackPanel _scrollableSearchStackPanel;
        List<string> _playlistLinks = new List<string>();
        ImageBrush _playButtonImageBrush;
        ImageBrush _moreButtonImageBrush;

        public SearchCanvas(ImageBrush playButtonImageBrush, ImageBrush moreButtonImageBrush) : base()
        {
            _playButtonImageBrush = playButtonImageBrush;
            _moreButtonImageBrush = moreButtonImageBrush;
            Width = 440;
            Height = 420;
            _searchBox = new SearchBox();
            _scrollableSearchStackPanel = new ScrollableSearchStackPanel();
            Children.Add(_searchBox);
            Children.Add(_scrollableSearchStackPanel);
        }

        public void Reset()
        {
            _searchBox.Reset();
            RemoveAllSongs();
        }

        public void SetPlaylistLinks(List<string> playlistLinks)
        {
            _playlistLinks.Clear();
            foreach (var element in playlistLinks)
            {
                _playlistLinks.Add(element);
            }
            foreach (var child in _scrollableSearchStackPanel.Children)
            {
                if (child.GetType() == typeof(SearchSongContainer))
                    ((SearchSongContainer)child).ResetMenus(playlistLinks.ToArray());
            }
        }

        public void DisplaySongs(List<Song> songs)
        {
            foreach (Song song in songs)
            {

                _scrollableSearchStackPanel.Children.Add(new SearchSongContainer(song, _playlistLinks.ToArray(), _playButtonImageBrush, _moreButtonImageBrush));
            }
        }

        public void RemoveAllSongs()
        {
            _scrollableSearchStackPanel.Children.Clear();
        }
    }

    public class SearchBox : TextBox
    {
        public SearchBox() : base()
        {
            Canvas.SetLeft(this, 10);
            Canvas.SetTop(this, 40);
            TextWrapping = TextWrapping.Wrap;
            Width = 420;
            Height = 30;
            FontSize = 18;
            TextChanged += SearchBox_TextChanged;
            Text = "";
        }

        public void Reset()
        {
            Text = "";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            new ClientEvent(EventType.SearchSongOrArtist, true, Text);
        }
    }

    public class ScrollableSearchStackPanel : ScrollViewer
    {
        StackPanel _searchStackPanel;
        public UIElementCollection Children { get { return _searchStackPanel.Children; } }
        public ScrollableSearchStackPanel() : base()
        {
            _searchStackPanel = new StackPanel();
            Content = _searchStackPanel;
            Width = 440;
            Height = 340;
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            Canvas.SetTop(this, 80);
        }
    }
    public class SearchSongContainer : Canvas
    {
        public Song Song { get; set; }
        public Image Image { get; set; }
        public Label SongNameInner { get; set; }
        public Label ArtistNameInner { get; set; }
        public Label DurationLabel { get; set; }
        public Button PlayThisButton { get; set; }
        public Button MoreButton { get; set; }
        public ImageBrush PlayButtonImageBrush { get; set; }
        public ImageBrush MoreButtonImageBrush { get; set; }

        public SearchSongContainer(Song song, string[] playlistLinks, ImageBrush playButtonImageBrush, ImageBrush moreButtonImageBrush) : base()
        {
            Song = song;
            Height = 60;
            Width = 420;
            Background = new SolidColorBrush(Color.FromRgb(35, 35, 35));
            Margin = new Thickness(0, 6, 0, 0);

            Image = new Image();
            SongNameInner = new Label();
            ArtistNameInner = new Label();
            DurationLabel = new Label();
            PlayThisButton = new Button();
            MoreButton = new Button();
            PlayButtonImageBrush = playButtonImageBrush;
            MoreButtonImageBrush = moreButtonImageBrush;

            InitializeSearchSongContainerImage(Image);
            InitializeSongNameInnerLabel(SongNameInner);
            InitializeSearchSongContainerDurationLabel(DurationLabel);
            InitializeArtistNameInnerLabel(ArtistNameInner);
            InitializePlayThisButton(PlayThisButton);
            InitializeMoreButton(MoreButton, playlistLinks);

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

        private void InitializeSearchSongContainerImage(Image image)
        {
            image.Height = 60;
            image.Width = 60;
            Canvas.SetTop(image, 0);
            Canvas.SetLeft(image, 0);
            ImageSource? source = (ImageSource?)new ImageSourceConverter().ConvertFrom(Song.ImageBinary);
            image.Source = source;
        }

        private void InitializeSongNameInnerLabel(Label songNameInnerLabel)
        {
            songNameInnerLabel.Width = 180;
            songNameInnerLabel.Height = 26;
            Canvas.SetTop(songNameInnerLabel, 3);
            Canvas.SetLeft(songNameInnerLabel, 62);
            songNameInnerLabel.Foreground = new SolidColorBrush(Colors.White);
            songNameInnerLabel.Content = Song.SongName;
        }

        private void InitializeArtistNameInnerLabel(Label artistNameInnerLabel)
        {
            artistNameInnerLabel.Width = 180;
            artistNameInnerLabel.Height = 26;
            Canvas.SetBottom(artistNameInnerLabel, 3);
            Canvas.SetLeft(artistNameInnerLabel, 62);
            artistNameInnerLabel.Foreground = new SolidColorBrush(Colors.White);
            artistNameInnerLabel.Content = Song.ArtistName;
        }

        private void InitializePlayThisButton(Button playThisButton)
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
            new ClientEvent(EventType.InternalRequest, true, InternalRequestType.PlayThis, Song);
        }

        private void InitializeMoreButton(Button moreButton, string[] playlistLinks)
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

            foreach (string playlistLink in playlistLinks)
            {
                CustomMenuItem item = new CustomMenuItem(Song, playlistLink);
                moreButton.ContextMenu.Items.Add(item);
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

            foreach (string playlistLink in playlistLinks)
            {
                CustomMenuItem item = new CustomMenuItem(Song, playlistLink);
                MoreButton.ContextMenu.Items.Add(item);
            }
        }

        private void ShowMoreButtonMenu(object sender, RoutedEventArgs e)
        {
            MoreButton.ContextMenu.IsOpen = true;
        }

        private void AddToQueue_Click(object sender, RoutedEventArgs e)
        {
            new ClientEvent(EventType.InternalRequest, true, InternalRequestType.AddSongToQueue, Song);
        }
    }

    public class CustomMenuItem : MenuItem
    {
        private string _playlistLink;
        private Song _song;
        public CustomMenuItem(Song song, string playlistLink) : base()
        {
            _playlistLink = playlistLink;
            _song = song;
            Header = $"Add to Playlist: {_playlistLink}";
            Click += CustomMenuItem_Click;
        }

        private void CustomMenuItem_Click(object sender, RoutedEventArgs e)
        {
            new ClientEvent(EventType.AddSongToPlaylist, true, _song, _playlistLink);
        }
    }
}
