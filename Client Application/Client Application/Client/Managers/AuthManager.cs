using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Client_Application.Client.Event;
using Client_Application.Client.Network;

namespace Client_Application.Client.Managers
{

    public sealed class AuthManager
    {
        private static volatile AuthManager? _instance;
        private static readonly object _syncLock = new object();
        private static readonly ManualResetEvent _isInitialized = new ManualResetEvent(false);
        private readonly CommunicationManager _communicationManager;
        private Session? _session;

        private AuthManager()
        {
            _communicationManager = CommunicationManager.GetInstance();
        }

        public static void WaitForInstance()
        {
            _isInitialized.WaitOne();
        }

        public static AuthManager InitializeSingleton()
        {
            if (_instance == null)
            {
                lock (_syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new AuthManager();
                        _isInitialized.Set();
                    }
                }
            }
            return _instance;
        }

        public static AuthManager GetInstance()
        {
            if (_instance != null)
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

        public enum RegisterResult
        {
            RegistrationError,
            LogInError,
            LogInSuccess
        }

        /// <summary>
        /// Returns a list of registration errors. Automatic Log In if successful.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool Register(string email, string password, out List<string>? errors)
        {
            errors = _communicationManager.RegisterToServer(email, password);

            if(errors is null)
            {
                return true;
            }
            return false;
        }

        public bool LogIn(string email, string password, bool rememberMe)
        {
            bool success = _communicationManager.AuthenticateToServer(email, password);

            if (success)
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
        public bool IsRememberMe()
        {
            IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
            if (isoStore.FileExists("login.dat"))
            {
                return true;
            }
            return false;
        }

        public bool IsRememberMe(out string? email, out string? password)
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

    public static class AES
    {
        private static readonly byte[] _key = { 0x2F, 0xC1, 0x10, 0x44, 0xD1, 0x74, 0xA9, 0x3E, 0x86, 0x33, 0xF5, 0x20, 0x4F, 0xA5, 0x8B, 0x5E };
        private static readonly byte[] _iv = { 0x9C, 0x86, 0x31, 0xD2, 0xF8, 0xA5, 0x98, 0x7E, 0x52, 0x10, 0x7F, 0xE3, 0x50, 0x84, 0x06, 0x31 };

        public static string Encrypt(string clearText)
        {
            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            using MemoryStream output = new();
            using CryptoStream cryptoStream = new(output, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(Encoding.Unicode.GetBytes(clearText));
            cryptoStream.FlushFinalBlock();
            return Convert.ToBase64String(output.ToArray());
        }

        public static string Decrypt(string encrypted)
        {
            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            using MemoryStream input = new(Convert.FromBase64String(encrypted));
            using CryptoStream cryptoStream = new(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using MemoryStream output = new();
            cryptoStream.CopyTo(output);
            return Encoding.Unicode.GetString(output.ToArray());
        }
    }
}
