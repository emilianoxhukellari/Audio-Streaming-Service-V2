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
