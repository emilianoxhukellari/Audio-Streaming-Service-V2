using Client_Application.Client.Core;
using Client_Application.Client.Event;
using Client_Application.Wpf.DynamicComponents;
using Client_Application.Wpf.UserControls;
using Client_Application.Wpf.UserWindows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Client_Application
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private ClientListener _clientListener;

        private MainControl _mainControl;
        private LogInControl _logInControl;
        private RegisterControl _registerControl;
        public MainWindow()
        {
            InitializeComponent();
            _clientListener = new ClientListener();
            _mainControl = new MainControl();
            _logInControl = new LogInControl();
            _registerControl = new RegisterControl();

            Listen(EventType.LogInStateUpdate, new ClientEventCallback<LogInStateUpdateArgs>(ExecuteLogInStateUpdate));
            Listen(EventType.ResetWindow, new ClientEventCallback<EventArgs>(ExecuteResetWindow));
            Listen(EventType.UpdatePage, new ClientEventCallback<UpdatePageArgs>(ExecuteUpdatePage));

            ClientEvent.Fire(EventType.WindowReady, EventArgs.Empty);
            currentUserControl.Content = _logInControl;
        }

        #region ClientEventHandlers

        private void ExecuteUpdatePage(UpdatePageArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                if(args.PageType == PageType.Main)
                {
                    currentUserControl.Content = _mainControl;
                }
                else if(args.PageType == PageType.Register)
                {
                    currentUserControl.Content = _registerControl;
                }
                else if(args.PageType == PageType.LogIn)
                {
                    currentUserControl.Content = _logInControl;
                }
            });
        }

        private void ExecuteResetWindow(EventArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                _mainControl.Reset();
                _registerControl.Reset();
                _logInControl.Reset();
            });
        }

        private void ExecuteLogInStateUpdate(LogInStateUpdateArgs args)
        {
            LogInState logInState = args.LogInState;

            if(logInState == LogInState.LogInValid)
            {
                string email = args.Email;
                Dispatcher.Invoke(() =>
                {
                    _mainControl.SetCurrentUser(email);
                    currentUserControl.Content = _mainControl;
                });
            }
            else if(logInState == LogInState.LogInInvalid)
            {
                Dispatcher.Invoke(() =>
                {
                    _logInControl.InvalidLoginNotify();
                });
            }
            else if(logInState == LogInState.LogOut)
            {
                Dispatcher.Invoke(() =>
                {
                    currentUserControl.Content = _logInControl;
                });
            }
        }

        #endregion

        #region Methods

        private void Listen<T>(EventType eventType, ClientEventCallback<T> callback) where T : EventArgs
        {
            _clientListener.Listen(eventType, callback);
        }

        #endregion
    }
}
