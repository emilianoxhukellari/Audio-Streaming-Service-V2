using Client_Application.Client;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client_Application.DynamicVisualComponents
{
    public class PlaylistLinkContainer : Button
    {
        public string PlaylistLink { get; set; }
        public bool IsActive { get; set; }
        public PlaylistLinkContainer(string playlistLink, bool active)
        {
            PlaylistLink = playlistLink;
            Height = 25;
            Margin = new Thickness(0, 4, 0, 0);
            Background = new SolidColorBrush(Color.FromRgb(35, 35, 35));
            Foreground = new SolidColorBrush(Colors.White);
            Content = playlistLink;
            MouseEnter += PlaylistLinkContainer_MouseEnter;
            MouseLeave += PlaylistLinkContainer_MouseLeave;
            Click += PlaylistLinkContainer_Click;

            if(active)
            {
                SetStyleActive();
            }
            else
            {
                SetStyleInactive();
            }
        }

        private void PlaylistLinkContainer_Click(object sender, RoutedEventArgs e)
        {
            new ClientEvent(EventType.UpdatePlaylistCanvas, true, PlaylistLink);
            new ClientEvent(EventType.ShowPlaylistCanvas, true, PlaylistLink);
        }

        private void PlaylistLinkContainer_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Foreground = new SolidColorBrush(Colors.White);
        }

        private void PlaylistLinkContainer_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Foreground = new SolidColorBrush(Colors.Black);
        }

        public void SetStyleActive()
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 170, 127));
            IsActive = true;
        }

        public void SetStyleInactive()
        {
            BorderBrush = null;
            IsActive = false;
        }
    }
}
