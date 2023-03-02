using Client_Application.Client;
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

namespace Client_Application.UserWindows
{
    /// <summary>
    /// Interaction logic for LogOut.xaml
    /// </summary>
    public partial class LogOut : Window
    {
        public LogOut()
        {
            InitializeComponent();
        }

        private void yesLogOutButton_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.LogOut);
            Close();
        }

        private void noLogOutButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
