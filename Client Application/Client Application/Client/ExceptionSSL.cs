using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Client_Application.Client
{
    public class ExceptionSSL : Exception
    {
        public ExceptionSSL() : base() 
        { 
        }

        public ExceptionSSL(string message) : base(message)
        {
        }
    }
}
