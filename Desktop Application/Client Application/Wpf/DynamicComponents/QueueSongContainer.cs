using Client_Application.Client.Core;
using Client_Application.Client.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows;

namespace Client_Application.Wpf.DynamicComponents
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
            Height = 40;
            Background = new SolidColorBrush(Color.FromRgb(35, 35, 35));
            Margin = new Thickness(0, 10, 0, 0);
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
            songNameQueueLabel.Width = 180;
            songNameQueueLabel.Background = null;
            songNameQueueLabel.FontSize = 13;
            songNameQueueLabel.Foreground = new SolidColorBrush(Colors.White);
            SetTop(songNameQueueLabel, 2);
            SetLeft(songNameQueueLabel, 4);
            songNameQueueLabel.Content = _song.SongName;
        }

        private void InitializeArtistNameQueueLabel(Label artistNameQueueLabel)
        {
            artistNameQueueLabel.Width = 180;
            artistNameQueueLabel.Background = null;
            artistNameQueueLabel.FontSize = 11;
            artistNameQueueLabel.Foreground = new SolidColorBrush(Color.FromRgb(85, 255, 255));
            SetTop(artistNameQueueLabel, 22);
            SetLeft(artistNameQueueLabel, 4);
            artistNameQueueLabel.Content = _song.ArtistName;
        }

        private void InitializeMoveUpButton(Button moveUpButton)
        {
            SetRight(moveUpButton, 80);
            SetTop(moveUpButton, 14);
            moveUpButton.Style = (Style)FindResource("MyButton");
            moveUpButton.Width = 15;
            moveUpButton.Height = 15;
            moveUpButton.Background = _moveUpImageBrush;
            moveUpButton.Click += MoveUpButton_Click;
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.MoveSongUpQueue, new MoveSongArgs { Index = _currentIndex });
        }

        private void InitializeMoveDownButton(Button moveDownButton)
        {
            SetRight(moveDownButton, 50);
            SetTop(moveDownButton, 14);
            moveDownButton.Style = (Style)FindResource("MyButton");
            moveDownButton.Width = 15;
            moveDownButton.Height = 15;
            moveDownButton.Background = _moveDownImageBrush;
            moveDownButton.Click += MoveDownButton_Click;
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.MoveSongDownQueue, new MoveSongArgs { Index = _currentIndex });
        }

        private void InitializeRemoveFromQueueButton(Button removeFromQueueButton)
        {
            SetRight(removeFromQueueButton, 20);
            SetTop(removeFromQueueButton, 14);
            removeFromQueueButton.Style = (Style)FindResource("MyButton");
            removeFromQueueButton.Width = 15;
            removeFromQueueButton.Height = 15;
            removeFromQueueButton.Foreground = new SolidColorBrush(Colors.Black);
            removeFromQueueButton.Background = _removeImageBrush;
            removeFromQueueButton.Click += RemoveFromQueueButton_Click;
        }

        private void RemoveFromQueueButton_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.RemoveSongQueue, new RemoveSongQueueArgs { Index = _currentIndex });
        }
    }
}
