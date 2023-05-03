using Client_Application.Client.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using System.Windows;

namespace Client_Application.Wpf.DynamicComponents
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

            if (active)
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
            ClientEvent.Fire(EventType.UpdatePlaylistCanvas, new UpdatePlaylistCanvasArgs { PlaylistLink = PlaylistLink });
            ClientEvent.Fire(EventType.ShowPlaylistCanvas, EventArgs.Empty);
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
