using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server_Application.Server
{
    /// <summary>
    /// This class represents a song and all its essential information. 
    /// You have the ability to serialize the song into bytes and create a new instance from these bytes.
    /// </summary>
    public sealed class Song
    {
        public int SongId { get; set; }
        public string SongName { get; set; }
        public string ArtistName { get; set; }
        public double Duration { get; set; }
        public string DurationString { get; set; }
        public byte[] ImageBinary { get; set; }


        public Song(int songId, string songName, string artistName, double durationSeconds, byte[] imageBinary)
        {
            SongId = songId;
            SongName = songName;
            ArtistName = artistName;
            Duration = durationSeconds;
            DurationString = SecondsToString(durationSeconds);
            ImageBinary = imageBinary;
        }

        /// <summary>
        /// Create instance from serialized song.
        /// </summary>
        /// <param name="serlialized"></param>
        public Song(byte[] serlialized)
        {
            byte[] indexesBytes = serlialized.Take(40).ToArray();
            int[] indexes = new int[10];

            for (int i = 0; i < 10; i++)
            {
                indexes[i] = BitConverter.ToInt32(indexesBytes.Take(new Range(i * 4, (i + 1) * 4)).ToArray());
            }

            SongId = BitConverter.ToInt32(serlialized.Take(new Range(40, indexes[1])).ToArray());
            SongName = Encoding.UTF8.GetString(serlialized.Take(new Range(indexes[2], indexes[3])).ToArray());
            ArtistName = Encoding.UTF8.GetString(serlialized.Take(new Range(indexes[4], indexes[5])).ToArray());
            Duration = BitConverter.ToDouble(serlialized.Take(new Range(indexes[6], indexes[7])).ToArray());
            ImageBinary = serlialized.Take(new Range(indexes[8], indexes[9])).ToArray();
            DurationString = SecondsToString(Duration);
        }

        public Song()
        {
            SongId = -1;
            SongName = "Default Song Name";
            ArtistName = "Default Artist Name";
            Duration = 0;
            DurationString = "00:00";
            ImageBinary = new byte[0];
        }

        /// <summary>
        /// Serialize a song.
        /// </summary>
        /// <returns></returns>
        public byte[] GetSerialized()
        {
            byte[] songIdBytes = BitConverter.GetBytes(SongId);
            byte[] songNameBytes = Encoding.UTF8.GetBytes(SongName);
            byte[] artistNameBytes = Encoding.UTF8.GetBytes(ArtistName);
            byte[] durationBytes = BitConverter.GetBytes(Duration);

            int[] lengths = new int[5];
            lengths[0] = songIdBytes.Length;
            lengths[1] = songNameBytes.Length;
            lengths[2] = artistNameBytes.Length;
            lengths[3] = durationBytes.Length;
            lengths[4] = ImageBinary.Length;

            List<int> indexes = new List<int>(10);
            int currentIndex = 40; // 40 bytes in the beginning are reserved for indexes

            for (int i = 0; i < lengths.Length; i++)
            {
                indexes.Add(currentIndex);
                currentIndex += lengths[i];
                indexes.Add(currentIndex);
            }

            byte[] serialized = new byte[currentIndex]; // currentIndex is the sum of all lengths

            byte[] indexesByes = new byte[40];

            for (int i = 0; i < indexes.Count; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(indexes[i]), 0, indexesByes, i * 4, 4);
            }

            Buffer.BlockCopy(indexesByes, 0, serialized, 0, 40);
            Buffer.BlockCopy(songIdBytes, 0, serialized, indexes[0], songIdBytes.Length);
            Buffer.BlockCopy(songNameBytes, 0, serialized, indexes[2], songNameBytes.Length);
            Buffer.BlockCopy(artistNameBytes, 0, serialized, indexes[4], artistNameBytes.Length);
            Buffer.BlockCopy(durationBytes, 0, serialized, indexes[6], durationBytes.Length);
            Buffer.BlockCopy(ImageBinary, 0, serialized, indexes[8], ImageBinary.Length);

            return serialized;
        }

        public static string SecondsToString(double seconds)
        {
            int minutes = (int)(seconds / 60);
            int leftSeconds = (int)(seconds % 60);
            return $"{minutes:D2}:{leftSeconds:D2}";
        }
    }
}
