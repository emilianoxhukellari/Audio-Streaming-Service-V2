﻿using Client_Application.Client;
using System.Windows;

namespace Client_Application
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
