using System;
using System.Linq;
using System.Text;

namespace DataAccess.Services.DataLogic
{
    /// <summary>
    /// This class makes sure that any extra information in a wave file is removed.
    /// After serializing the wave file, this file can be played by any wave player.
    /// </summary>
    public sealed class StandardWaveBuilder
    {
        private byte[]? _data;
        private byte[] _waveBytes;
        private int _dataLength;

        public StandardWaveBuilder(byte[] waveFile)
        {
            _waveBytes = waveFile;
            _data = ReadAllData();
            _dataLength = GetDataLength();
        }

        private bool IsWaveValid()
        {
            bool checkRiff = ReadRiff();
            bool checkWave = ReadWave();
            bool checkFmt = ReadFmt();
            int dataIndex = GetDataStartIndex();
            int formatDataLength = GetFormatDataLength();
            return checkRiff && checkWave && checkFmt && dataIndex > 0 && formatDataLength == 16;
        }

        private byte[]? GetWaveHeader()
        {
            // Read 0 - 36 [bytes] from the original wav header
            // File Size = 36 + dataLength
            byte[] waveHeader = new byte[44];
            if (IsWaveValid())
            {
                Array.Copy(_waveBytes, waveHeader, 36);

                int fileSize = 36 + _dataLength;

                Array.Copy(BitConverter.GetBytes(fileSize).ToArray(), 0, waveHeader, 4, 4);
                Array.Copy(Encoding.UTF8.GetBytes("data").ToArray(), 0, waveHeader, 36, 4);
                Array.Copy(BitConverter.GetBytes(_dataLength).ToArray(), 0, waveHeader, 40, 4);
                return waveHeader;
            }
            return null;
        }

        /// <summary>
        /// This method will remove all extra information from wave file.
        /// </summary>
        /// <returns></returns>
        public byte[] GetStandardWave()
        {
            byte[]? waveHeader = GetWaveHeader();
            if (waveHeader != null && _data != null)
            {
                return waveHeader.Concat(_data).ToArray();
            }
            return Array.Empty<byte>();
        }

        private byte[]? ReadAllData()
        {
            int dataStartIndex = GetDataStartIndex();
            if (dataStartIndex != -1)
            {
                return _waveBytes.Take(new Range(dataStartIndex, _waveBytes.Length)).ToArray();
            }
            return null;
        }
        private int GetFormatDataLength()
        {
            return BitConverter.ToInt32(_waveBytes.Take(new Range(16, 20)).ToArray());
        }

        /// <summary>
        /// Finds where data starts.
        /// </summary>
        /// <returns></returns>
        private int GetDataStartIndex()
        {
            for (int i = 0; i < _waveBytes.Length - 12; i++)
            {
                if (_waveBytes[i] == 0x64 // d
                    && _waveBytes[i + 1] == 0x61 // a
                    && _waveBytes[i + 2] == 0x74 // t
                    && _waveBytes[i + 3] == 0x61) // a
                {
                    return i + 8; // After "data" and dataLength
                }
            }
            return -1;
        }
        private int GetDataLength()
        {
            if (_data != null)
            {
                return _data.Length;
            }
            else
            {
                return -1;
            }
        }
        private bool ReadRiff()
        {
            if (Encoding.UTF8.GetString(_waveBytes.Take(4).ToArray()) == "RIFF")
            {
                return true;
            }
            return false;
        }
        private bool ReadWave()
        {
            if (Encoding.UTF8.GetString(_waveBytes.Take(new Range(8, 12)).ToArray()) == "WAVE")
            {
                return true;
            }
            return false;
        }
        private bool ReadFmt()
        {
            if (Encoding.UTF8.GetString(_waveBytes.Take(new Range(12, 16)).ToArray()) == "fmt ")
            {
                return true;
            }
            return false;
        }
    }
}
