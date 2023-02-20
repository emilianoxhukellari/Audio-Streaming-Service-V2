using Client_Application.Client;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client_Application.DynamicVisualComponents
{
    public class QueueSongContainer : Canvas
    {
        private Song _song;
        private int _currentIndex;
        private Label _songNameQueueLabel;
        private Label _artistNameQueueLabel;
        private Button _moveUpButton;
        private Button _moveDownButton;
        private Button _removeFromQueueButton;
        private ImageBrush _moveUpImageBrush;
        private ImageBrush _moveDownImageBrush;
        private ImageBrush _removeImageBrush;
        public QueueSongContainer(Song song, int currentIndex, ImageBrush moveUpImageBrush, ImageBrush moveDownImageBrush, ImageBrush removeImageBrush) : base()
        {
            Height = 35;
            Background = new SolidColorBrush(Color.FromRgb(35, 35, 35));
            Margin = new Thickness(0, 2, 0, 0);
            _song = song;
            _currentIndex = currentIndex;
            _songNameQueueLabel = new Label();
            _artistNameQueueLabel = new Label();
            _moveUpButton = new Button();
            _moveDownButton = new Button();
            _removeFromQueueButton = new Button();
            _moveUpImageBrush = moveUpImageBrush;
            _moveDownImageBrush = moveDownImageBrush;
            _removeImageBrush = removeImageBrush;

            InitializeSongNameQueueLabel(_songNameQueueLabel);
            InitializeArtistNameQueueLabel(_artistNameQueueLabel);
            InitializeMoveUpButton(_moveUpButton);
            InitializeMoveDownButton(_moveDownButton);
            InitializeRemoveFromQueueButton(_removeFromQueueButton);

            Children.Add(_songNameQueueLabel);
            Children.Add(_artistNameQueueLabel);
            Children.Add(_moveUpButton);
            Children.Add(_moveDownButton);
            Children.Add(_removeFromQueueButton);
        }

        private void InitializeSongNameQueueLabel(Label songNameQueueLabel)
        {
            songNameQueueLabel.Width = 114;
            songNameQueueLabel.Height = 22;
            songNameQueueLabel.Background = null;
            songNameQueueLabel.FontSize = 10;
            songNameQueueLabel.Foreground = new SolidColorBrush(Colors.White);
            Canvas.SetTop(songNameQueueLabel, 0);
            Canvas.SetLeft(songNameQueueLabel, 0);
            songNameQueueLabel.Content = _song.SongName;
        }

        private void InitializeArtistNameQueueLabel(Label artistNameQueueLabel)
        {
            artistNameQueueLabel.Width = 114;
            artistNameQueueLabel.Height = 22;
            artistNameQueueLabel.Background = null;
            artistNameQueueLabel.FontSize = 9;
            artistNameQueueLabel.Foreground = new SolidColorBrush(Color.FromRgb(85, 255, 255));
            Canvas.SetTop(artistNameQueueLabel, 13);
            Canvas.SetLeft(artistNameQueueLabel, 0);
            artistNameQueueLabel.Content = _song.ArtistName;
        }

        private void InitializeMoveUpButton(Button moveUpButton)
        {
            moveUpButton.Style = (Style)FindResource("MyButton");
            Canvas.SetLeft(moveUpButton, 125);
            Canvas.SetTop(moveUpButton, 12);
            moveUpButton.Width = 14;
            moveUpButton.Height = 14;
            moveUpButton.Background = _moveUpImageBrush;
            moveUpButton.Click += MoveUpButton_Click;
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            new ClientEvent(EventType.MoveSongUpQueue, true, _currentIndex);
        }

        private void InitializeMoveDownButton(Button moveDownButton)
        {
            moveDownButton.Style = (Style)FindResource("MyButton");
            Canvas.SetLeft(moveDownButton, 150);
            Canvas.SetTop(moveDownButton, 12);
            moveDownButton.Width = 14;
            moveDownButton.Height = 14;
            moveDownButton.Background = _moveDownImageBrush;
            moveDownButton.Click += MoveDownButton_Click;
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            new ClientEvent(EventType.MoveSongDownQueue, true, _currentIndex);
        }

        private void InitializeRemoveFromQueueButton(Button removeFromQueueButton)
        {
            removeFromQueueButton.Style = (Style)FindResource("MyButton");
            Canvas.SetLeft(removeFromQueueButton, 175);
            Canvas.SetTop(removeFromQueueButton, 12);
            removeFromQueueButton.Width = 14;
            removeFromQueueButton.Height = 14;
            removeFromQueueButton.Foreground = new SolidColorBrush(Colors.Black);
            removeFromQueueButton.Background = _removeImageBrush;
            removeFromQueueButton.Click += RemoveFromQueueButton_Click;
        }

        private void RemoveFromQueueButton_Click(object sender, RoutedEventArgs e)
        {
            new ClientEvent(EventType.RemoveSongQueue, true, _currentIndex);
        }
    }
}
