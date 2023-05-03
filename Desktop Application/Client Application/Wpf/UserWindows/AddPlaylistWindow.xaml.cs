using Client_Application.Client.Event;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Client_Application.Wpf.UserWindows
{
    /// <summary>
    /// Interaction logic for AddPlaylistWindow.xaml
    /// </summary>
    public partial class AddPlaylistWindow : Window
    {
        public AddPlaylistWindow()
        {
            InitializeComponent();
        }

        private void cancelCreateNewPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void createNewPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            string playlistName = newPlaylistTextbox.Text;
            if (playlistName != null && playlistName != string.Empty)
            {
                ClientEvent.Fire(EventType.CreateNewPlaylist,
                    new CreateNewPlaylistArgs
                    {
                        PlaylistName = playlistName
                    });
            }
            Close();
        }
    }
}
