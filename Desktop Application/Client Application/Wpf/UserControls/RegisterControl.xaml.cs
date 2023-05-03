using Client_Application.Client.Event;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
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
    public partial class RegisterControl : UserControl
    {
        ClientListener _clientListener;
        public RegisterControl()
        {
            InitializeComponent();
            _clientListener = new ClientListener(); 

            Listen(EventType.RegisterErrorsUpdate, new ClientEventCallback<RegisterErrorsUpdateArgs>(ExecuteRegisterErrorUpdate));
            Listen(EventType.LongNetworkRequest, new ClientEventCallback<LongNetworkRequestArgs>(ExecuteLongNetworkRequest));
            Listen(EventType.ResetPage, new ClientEventCallback<ResetPageArgs>(ExecuteResetPage));
        }

        #region Methods
        private void Listen<T>(EventType eventType, ClientEventCallback<T> callback) where T : EventArgs
        {
            _clientListener.Listen(eventType, callback);
        }

        public void Reset()
        {
            progressRing.IsActive = false;
            progressRing.Visibility = Visibility.Collapsed;
            registerErrorTextBox.Text = string.Empty;
            errorConfirmPasswordLabel.Content = string.Empty;
            errorPasswordLabel.Content = string.Empty;
            emailErrorLabel.Content = string.Empty;
            emailTextBox.Text = string.Empty;
            confirmPasswordBox.Password = string.Empty;
            passwordBox.Password = string.Empty;
        }

        #endregion

        #region ClientEventHandlers
        private void ExecuteResetPage(ResetPageArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                if (args.PageType == PageType.Register)
                {
                    Reset();
                }
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

        private void ExecuteRegisterErrorUpdate(RegisterErrorsUpdateArgs args)
        {
            List<string> errors = args.Errors;
            if (errors.Count > 0)
            {
                string errorList = string.Empty;

                foreach (string error in errors)
                {
                    errorList += $"\u2022 {error}\n";
                }

                Dispatcher.Invoke(() =>
                {
                    registerErrorTextBox.Text = errorList;
                });
            }
        }
        #endregion

        #region EventHandlers
        private void registerButton_Click(object sender, RoutedEventArgs e)
        {
            string email = emailTextBox.Text;
            string password = passwordBox.Password;
            string confirmPassword = confirmPasswordBox.Password;
            List<string> errors = new List<string>();
            bool isValid = true;

            if(email == string.Empty)
            {
                emailErrorLabel.Content = "Email must not be empty";
                isValid = false;
            }

            if(password == string.Empty)
            {
                errorPasswordLabel.Content = "Password must not be empty";
                isValid = false;
            }

            if(confirmPassword == string.Empty)
            {
                errorConfirmPasswordLabel.Content = "Confirm Password must not be empty";
                isValid = false;
            }

            if(password != string.Empty && confirmPassword != string.Empty) 
            {
                if(password != confirmPassword)
                {
                    errors.Add("Passwords must match");
                    isValid = false;
                }
            }

            if(email != string.Empty)
            {
                var check = new EmailAddressAttribute();
                if(!check.IsValid(email))
                {
                    errors.Add("Invalid email address");
                    isValid = false;
                }
            }

            if(errors.Count > 0)
            {
                string errorList = string.Empty;

                foreach(string error in errors)
                {
                    errorList += $"\u2022 {error}\n";
                }
                registerErrorTextBox.Text = errorList;
            }

            if(isValid)
            {
                ClientEvent.Fire(EventType.Register, new RegisterArgs { Email = email, Password = password });
            }
        }

        private void emailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            emailErrorLabel.Content = string.Empty;
            registerErrorTextBox.Text = string.Empty;
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            errorPasswordLabel.Content = string.Empty;
            registerErrorTextBox.Text = string.Empty;
        }

        private void confirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            errorConfirmPasswordLabel.Content = string.Empty;
            registerErrorTextBox.Text = string.Empty;
        }

        private void logInLink_Click(object sender, RoutedEventArgs e)
        {
            ClientEvent.Fire(EventType.UpdatePage, new UpdatePageArgs { PageType = PageType.LogIn });
        }
        #endregion
    }
}
