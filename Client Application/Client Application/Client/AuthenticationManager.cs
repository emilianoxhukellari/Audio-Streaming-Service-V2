using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client_Application.Client
{
    public sealed class AuthenticationManager
    {
        private static volatile AuthenticationManager? _instance;
        private static readonly object syncLock = new object();
        private static readonly ManualResetEvent _isInitialized = new ManualResetEvent(false);
        private readonly CommunicationManager _communicationManager;
        private Session? _session;

        private AuthenticationManager()
        {
            _communicationManager = CommunicationManager.GetInstance();
        }
        
        public static void WaitForInstance()
        {
            _isInitialized.WaitOne();
        }

        public static AuthenticationManager InitializeSingleton()
        {
            if (_instance == null)
            {
                lock (syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new AuthenticationManager();
                        _isInitialized.Set();
                    }
                }
            }
            return _instance;
        }

        public static AuthenticationManager GetInstance()
        {
            if(_instance != null)
            {
                return _instance;
            }
            else
            {
                throw new InvalidOperationException("Authentication Manager is not Initialized."); 
            }
        }

        public void NewSession()
        {
            _session = new Session();
        }

        public void RecoverSession()
        {
            if (_session != null)
            {
                if (_session.IsLoggedIn)
                {
                    _communicationManager.AuthenticateToServer(_session.Email, _session.Password);
                }
            }
        }

        public bool LogIn(string email, string password, bool rememberMe)
        {
            bool result = _communicationManager.AuthenticateToServer(email, password);

            Trace.WriteLine(result);

            if (result)
            {
                _session = new Session(email, password);
                ExecuteValidLogIn(email, password, rememberMe);
                return true;
            }
            else
            {
                _session = new Session();
                ExecuteInvalidLogIn(email, password, rememberMe);
                return false;
            }
        }

        private void ExecuteValidLogIn(string email, string password, bool rememberMe)
        {
            new ClientEvent(EventType.LogInStateUpdate, true, LogInState.LogInValid, email);

            if (rememberMe)
            {
                RememberMeUpdate(email, password);
            }
            else
            {
                RemoveRememberMe();
            }
        }

        private void ExecuteInvalidLogIn(string email, string password, bool rememberMe)
        {
            new ClientEvent(EventType.LogInStateUpdate, true, LogInState.LogInInvalid);

            if (!rememberMe)
            {
                RemoveRememberMe();
            }
        }

        public void RemoveRememberMe()
        {
            IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
            if (isoStore.FileExists("login.dat"))
            {
                isoStore.DeleteFile("login.dat");
            }
        }

        public void RememberMeUpdate(string email, string password)
        {
            IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

            using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream("login.dat", FileMode.Create, isoStore))
            {
                using (StreamWriter writer = new StreamWriter(isoStream))
                {
                    writer.WriteLine(AES.Encrypt(email));
                    writer.WriteLine(AES.Encrypt(password));
                }
            }
        }

        public bool IsRememeberMe(out string? email, out string? password)
        {
            IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

            if (isoStore.FileExists("login.dat"))
            {
                using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream("login.dat", FileMode.Open, isoStore))
                {
                    using (StreamReader reader = new StreamReader(isoStream))
                    {
                        string? encryptedEmail = reader.ReadLine();
                        string? encryptedPassword = reader.ReadLine();

                        if (encryptedEmail != null && encryptedPassword != null)
                        {
                            email = AES.Decrypt(encryptedEmail);
                            password = AES.Decrypt(encryptedPassword);
                        }
                        else
                        {
                            email = null;
                            password = null;
                        }
                    }
                }
                return true;
            }
            email = null;
            password = null;
            return false;
        }
    }
}
