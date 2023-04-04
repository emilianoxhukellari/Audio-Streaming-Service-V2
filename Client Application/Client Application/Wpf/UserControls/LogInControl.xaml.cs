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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client_Application.Wpf.UserControls
{
    /// <summary>
    /// Interaction logic for RegisterControl.xaml
    /// </summary>
    public partial class LogInControl : UserControl
    {
        private ClientListener _clientListener;
        public LogInControl()
        {
            InitializeComponent();
            _clientListener = new ClientListener();

            Listen(EventType.UpdateRememberMe, new ClientEventCallback<UpdateRememberMeArgs>(ExecuteUpdateRememberMe));
            Listen(EventType.LongNetworkRequest, new ClientEventCallback<LongNetworkRequestArgs>(ExecuteLongNetworkRequest));
            Listen(EventType.ResetPage, new ClientEventCallback<ResetPageArgs>(ExecuteResetPage));
        }

        #region Methods
        private void Listen<T>(EventType eventType, ClientEventCallback<T> callback) where T : EventArgs
        {
            _clientListener.Listen(eventType, callback);
        }

        public void InvalidLoginNotify()
        {
            loginErrorLabel.Content = "Username or Password Incorrect";
        }

        public void Reset()
        {
            loginErrorLabel.Content = string.Empty;
            emailErrorLabel.Content = string.Empty;
            errorPasswordLabel.Content = string.Empty;
            emailTextBox.Text = string.Empty;
            passwordBox.Password = string.Empty;
            progressRing.Visibility = Visibility.Collapsed;
            progressRing.IsActive = false;
            rememeberMeCheckBox.IsChecked = false;
        }

        #endregion

        #region ClientEventHandlers

        private void ExecuteResetPage(ResetPageArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                if (args.PageType == PageType.LogIn)
                {
                    Reset();
                }
            });
        }

        private void ExecuteUpdateRememberMe(UpdateRememberMeArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                rememeberMeCheckBox.IsChecked = true;
                emailTextBox.Text = args.Email;
                passwordBox.Password = args.Password;
            });
        }

        private void ExecuteLongNetworkRequest(LongNetworkRequestArgs args)
        {
            bool started = args.Started;
            Dispatcher.Invoke(() =>
            {
                if (started)
                {
                    progressRing.IsActive = true;
                    progressRing.Visibility = Visibility.Visible;
                }
                else
                {
                    progressRing.IsActive = false;
                    progressRing.Visibility = Visibility.Collapsed;
                }
            });
        }

        #endregion

        #region EventHandlers

        private void registerLink_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.UpdatePage, new UpdatePageArgs { PageType = PageType.Register });
        }

        private void loginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = emailTextBox.Text;
            string password = passwordBox.Password;

            if (email == string.Empty)
            {
                emailErrorLabel.Content = "You must supply an Email";
            }

            if (password == string.Empty)
            {
                errorPasswordLabel.Content = "You must supply a Password";
            }

            if (email != string.Empty && password != string.Empty)
            {
                bool rememberMe;
                if (rememeberMeCheckBox.IsChecked == true)
                {
                    rememberMe = true;
                }
                else
                {
                    rememberMe = false;
                }

                ClientEvent.Fire(EventType.LogIn,
                    new LogInArgs
                    {
                        Email = email,
                        Password = password,
                        RememberMe = rememberMe
                    });
            }
        }

        private void emailTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            emailErrorLabel.Content = string.Empty;
            loginErrorLabel.Content = string.Empty;
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            errorPasswordLabel.Content = string.Empty;
            loginErrorLabel.Content = string.Empty;
        }

        #endregion
    }
}
