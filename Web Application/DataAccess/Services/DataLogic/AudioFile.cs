namespace Server_Application.Server
{
    public struct Packet
    {
        public int Index { get; set; }
        public byte[] Data { get; set; }
    }

    /// <summary>
    /// This static class has one public method GetAudioBytes().
    /// This method takes a standardized wave file and does the following:
    /// 1) Adds header in the beginning (44 bytes).
    /// 2) Adds packet count (4 bytes).
    /// 3) Adds packets each of which contains index (4 bytes) and data (4092 bytes).
    /// </summary>
    public static class AudioFile
    {
        public static byte[] GetAudioBytes(byte[] standardWaveAudio)
        {
            byte[] header = standardWaveAudio.Take(44).ToArray();
            byte[] data = standardWaveAudio.Take(new Range(44, standardWaveAudio.Length)).ToArray();
            List<Packet> packets = GetPackets(data);
            return GetBytes(packets, header);
        }

        /// <summary>
        /// Add index and data to the packet.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="position"></param>
        /// <param name="data"></param>
        private static void MakePacket(ref Packet packet, ref int position, byte[] data)
        {
            packet.Index = position;
            for (int i = 0; i < 4092; i++)
            {
                packet.Data[i] = data[position++];
            }
        }

        /// <summary>
        /// Gets all the packets which contain indexes and data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static List<Packet> GetPackets(byte[] data)
        {
            int count = (int)Math.Floor((double)data.Length / 4092);
            List<Packet> packets = new List<Packet>();
            int position = 0;

            for (int i = 0; i < count; i++)
            {
                Packet packet = new Packet();
                packet.Data = new byte[4092];
                MakePacket(ref packet, ref position, data);
                packets.Add(packet);
            }

            Packet lastPacket = new Packet();
            lastPacket.Data = new byte[4092];
            lastPacket.Index = position;

            int j = 0;

            while (position < data.Length)
            {
                lastPacket.Data[j] = data[position++];
                j++;
            }

            for (; j < lastPacket.Data.Length; j++)
            {
                lastPacket.Data[j] = 0;
            }

            packets.Add(lastPacket);
            return packets;
        }

        /// <summary>
        /// Append all the necessary information of the wave to a single byte[].
        /// </summary>
        /// <param name="packets"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        private static byte[] GetBytes(List<Packet> packets, byte[] header)
        {
            int byteArraySize = 44 + 4 + packets.Count * 4096; // Header + Packet Count + (Indexes and Data)
            byte[] result = new byte[byteArraySize];
            Buffer.BlockCopy(header, 0, result, 0, 44); // Add header in the beginning
            Buffer.BlockCopy(BitConverter.GetBytes(packets.Count), 0, result, 44, 4); // Add packet count
            int index = 48; // Index after header and packet count
            foreach (Packet packet in packets)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(packet.Index), 0, result, index, 4); // Add index (bytes)
                index += 4;
                Buffer.BlockCopy(packet.Data, 0, result, index, 4092); // Add data (bytes)
                index += 4092;
            }
            return result;
        }
    }
}