using Client_Application.Client;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Client_Application.DynamicVisualComponents
{
    public class PlaylistLinkContainer : Button
    {
        public string PlaylistLink { get; set; }
        public bool IsActive { get; set; }
        public PlaylistLinkContainer(string playlistLink, bool active)
        {
            Style = (Style)FindResource("ButtonSimple");
            PlaylistLink = playlistLink;
            Height = 30;
            Margin = new Thickness(0, 6, 0, 0);
            Background = new SolidColorBrush(Color.FromRgb(35, 35, 35));
            Foreground = new SolidColorBrush(Colors.White);
            FontSize = 14;
            Content = playlistLink;
            Cursor = Cursors.Hand;
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
            ClientEvent.Fire(EventType.UpdatePlaylistCanvas, PlaylistLink);
            ClientEvent.Fire(EventType.ShowPlaylistCanvas, PlaylistLink);
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
