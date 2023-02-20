using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Client_Application.Client
{
    public static class AES
    {
        private static readonly string _encryptionPassword = "JJS2j2@49cnnskl[]qwkj@112";
        private static readonly byte[] _IV =
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16
        };

        public static string Encrypt(string clearText)
        {
            using Aes aes = Aes.Create();
            aes.Key = Key();
            aes.IV = _IV;
            using MemoryStream output = new ();
            using CryptoStream cryptoStream = new(output, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(Encoding.Unicode.GetBytes(clearText));
            cryptoStream.FlushFinalBlock();
            return Encoding.Unicode.GetString(output.ToArray());
        }

        public static string Decrypt(string encrypted)
        {
            using Aes aes = Aes.Create();
            aes.Key = Key();
            aes.IV = _IV;
            using MemoryStream input = new(Encoding.Unicode.GetBytes(encrypted));
            using CryptoStream cryptoStream = new(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using MemoryStream output = new();
            cryptoStream.CopyTo(output);
            return Encoding.Unicode.GetString(output.ToArray());
        }

        private static byte[] Key()
        {
            var emptySalt = Array.Empty<byte>();
            var iterations = 1000;
            var desiredKeyLength = 16; 
            var hashMethod = HashAlgorithmName.SHA384;
            return Rfc2898DeriveBytes.Pbkdf2(Encoding.Unicode.GetBytes(_encryptionPassword), emptySalt, iterations, hashMethod, desiredKeyLength);
        }
    }
}
