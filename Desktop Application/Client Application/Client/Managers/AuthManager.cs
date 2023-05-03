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
    /// <summary>
    /// This class represent an authentication manager that deals with log in, log out, register, remember me.
    /// </summary>
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

        /// <summary>
        /// Wait until the singleton is created.
        /// </summary>
        public static void WaitForInstance()
        {
            _isInitialized.WaitOne();
        }

        /// <summary>
        /// Initialize the singleton. This method is thread-safe.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Get the singleton instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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

        /// <summary>
        /// Create a new empty session for this client.
        /// </summary>
        public void NewSession()
        {
            _session = new Session();
        }

        /// <summary>
        /// Reauthenticate to server if the client disconnected.
        /// </summary>
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

        /// <summary>
        /// Perform log in with the server. 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="rememberMe"></param>
        /// <returns></returns>
        public AuthenticationResult LogIn(string email, string password, bool rememberMe)
        {
            AuthenticationResult result = _communicationManager.AuthenticateToServer(email, password);

            if (result == AuthenticationResult.Valid)
            {
                _session = new Session(email, password);
                ExecuteValidLogIn(email, password, rememberMe);
                return AuthenticationResult.Valid;
            }
            else if (result == AuthenticationResult.Invalid)
            {
                _session = new Session();
                ExecuteInvalidLogIn(email, password, rememberMe);
                return AuthenticationResult.Invalid;
            }
            else if (result == AuthenticationResult.Error)
            {
                _session = new Session();
                ExecuteInvalidLogIn(email, password, rememberMe);
                return AuthenticationResult.Error;
            }
            return result;
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

        /// <summary>
        /// Remove credentials of this client for next time they log in.
        /// </summary>
        public void RemoveRememberMe()
        {
            IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
            if (isoStore.FileExists("login.dat"))
            {
                isoStore.DeleteFile("login.dat");
            }
        }

        /// <summary>
        /// Change the credentials stored in this computer for this client.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
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

        /// <summary>
        /// Check whether the client had checked "Remember Me" when they logged in last.
        /// </summary>
        /// <returns></returns>
        public bool IsRememberMe()
        {
            IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
            if (isoStore.FileExists("login.dat"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check whether the client had checked "Remember Me" when they logged in last. Also, provide the email and password
        /// of that user.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Encrypt the clearText using AES encryption.
        /// </summary>
        /// <param name="clearText"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Decrypt the encrypted message using AES encryption.
        /// </summary>
        /// <param name="encrypted"></param>
        /// <returns></returns>
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
