using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client_Application.Client.Network
{
    public class Session
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool IsLoggedIn { get; set; }

        public Session(string email, string password)
        {
            Email = email;
            Password = password;
            IsLoggedIn = true;
        }

        public Session()
        {
            Email = string.Empty;
            Password = string.Empty;
            IsLoggedIn = false;
        }
    }
}
