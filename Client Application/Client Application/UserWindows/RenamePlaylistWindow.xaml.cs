using Client_Application.Client;
using System.Windows;

namespace Client_Application
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
            new ClientEvent(EventType.RenamePlaylist, true, playlistName);
            Close();
        }

        private void cancelRenamePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

    }
}
