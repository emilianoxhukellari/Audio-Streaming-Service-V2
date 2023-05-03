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
    /// Interaction logic for RenamePlaylistWindow.xaml
    /// </summary>
    public partial class RenamePlaylistWindow : Window
    {
        public RenamePlaylistWindow()
        {
            InitializeComponent();
        }
        private void renamePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            string playlistName = newPlaylistNameTextBox.Text;
            if (playlistName != null && playlistName != string.Empty)
            {
                ClientEvent.Fire(EventType.RenamePlaylist,
                    new RenamePlaylistArgs
                    {
                        NewName = playlistName,
                    });
            }
            Close();
        }

        private void cancelRenamePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
