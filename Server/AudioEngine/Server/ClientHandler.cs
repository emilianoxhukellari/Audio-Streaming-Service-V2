using DataAccess.Contexts;
using DataAccess.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;


namespace Server_Application.Server
{
    /// <summary>
    /// This class represents a handler for a single client. It contains the necessary logic and 
    /// attributes to communicate with a client.
    /// </summary>
    public sealed class ClientHandler
    {
        private readonly AudioRetrievingInternalService _audioRetrievingInternalService;
        private readonly IServiceProvider _serviceProvider;
        private SslStream? _streamingSSL;
        private SslStream? _communcationSSL;
        public string ClientId { get; }
        public string UserId { get; private set; }
        private Thread _streamingThread;
        private Thread _communicationThread;
        private readonly object _lock = new object();

        private bool _stopSend;

        private readonly byte[] DATA = Encoding.UTF8.GetBytes("data");
        private readonly byte[] EXIT = Encoding.UTF8.GetBytes("exit");
        private readonly byte[] MOD1 = Encoding.UTF8.GetBytes("mod1"); // Regular streaming
        private readonly byte[] MOD2 = Encoding.UTF8.GetBytes("mod2"); // Optimization streaming
        private byte[] _lastSongData;

        private volatile bool _exitStreamingLoop;
        private volatile bool _exitCommunicationLoop;
        private volatile bool _isLoggedIn;

        public ClientHandler(string clientId, SslStream streamingSSL, SslStream communicationSSL, StreamingDbContext  streamingDbContext, IServiceProvider serviceProvider)
        {
            _audioRetrievingInternalService = new AudioRetrievingInternalService(streamingDbContext);
            _serviceProvider = serviceProvider;
            ClientId = clientId;
            _lastSongData = new byte[0];
            _streamingSSL = streamingSSL;
            _communcationSSL = communicationSSL;
            _streamingThread = new Thread(StreamingLoop);
            _streamingThread.IsBackground = true;
            _communicationThread = new Thread(CommuncationLoop);
            _communicationThread.IsBackground = true;
            _stopSend = false;
            Run();
        }

        ~ClientHandler() 
        {
            _streamingSSL?.Dispose();
            _communcationSSL?.Dispose();
        }

        /// <summary>
        /// This method will send a range of packets to the client. It can be interrupted if
        /// _stopSend is set to false.
        /// </summary>
        /// <param name="fromPacket"></param>
        /// <param name="toPacket"></param>
        private void SendSongData(int fromPacket, int toPacket)
        {
            byte[] songData = _lastSongData;

            SendSSL(MOD2, 4, _streamingSSL);
            int fromPacketOffset = (fromPacket / 4092) * 4 + 48;
            int toPacketOffset = (toPacket / 4092) * 4 + 48;

            int firstPacket = fromPacket + fromPacketOffset;
            int secondPacket = toPacket + toPacketOffset;

            int count = toPacket >= fromPacket ? ((secondPacket - firstPacket) / 4096) + 1 : 0;
            byte[] countBytes = BitConverter.GetBytes(count);
            SendSSL(countBytes, 4, _streamingSSL);
            byte[] currentPacketIndexBytes = new byte[4];
            byte[] currentPacketDataBytes = new byte[4092];

            int index = firstPacket;

            for (int i = 0; i < count; i++)
            {
                if (_stopSend)
                {
                    _stopSend = false;
                    SendSSL(EXIT, 4, _streamingSSL);

                    break;
                }
                SendSSL(DATA, 4, _streamingSSL);
                Buffer.BlockCopy(songData, index, currentPacketIndexBytes, 0, 4);
                index += 4;
                Buffer.BlockCopy(songData, index, currentPacketDataBytes, 0, 4092);
                index += 4092;
                SendSSL(currentPacketIndexBytes, 4, _streamingSSL);
                SendSSL(currentPacketDataBytes, 4092, _streamingSSL);
            }
        }


        public void TerminateClientHandler()
        {
            ExitStreamingLoop();
            ExitCommunicationLoop();
        }

        private void ExitStreamingLoop()
        {
            try
            {
                _exitStreamingLoop = true;
                _streamingSSL?.Close();
                _streamingSSL?.Dispose();
                _streamingSSL = null;
            }
            catch { }
        }

        private void ExitCommunicationLoop()
        {
            try
            {
                _exitCommunicationLoop = true;
                _communcationSSL?.Close();
                _communcationSSL?.Dispose();
                _communcationSSL = null;
            }
            catch { }
        }



        /// <summary>
        /// This method will send the entire wave file of the speciifed songId to the client. It
        /// can be interrupted if _stopSend is set to false.
        /// </summary>
        /// <param name="songId"></param>
        private void SendSongData(int songId)
        {
            DataAccess.Models.Song? song = _audioRetrievingInternalService.GetSongFromDatabase(songId);

            if (song != null)
            {

                byte[] songData = File.ReadAllBytes(song.SongFileName);
                byte[] header = new byte[44];
                Buffer.BlockCopy(songData, 0, header, 0, 44);
                _lastSongData = songData;
                SendSSL(MOD1, 4, _streamingSSL);
                SendSSL(header, 44, _streamingSSL);
                byte[] packetCountBytes = new byte[4];
                Buffer.BlockCopy(songData, 44, packetCountBytes, 0, 4);
                SendSSL(packetCountBytes, 4, _streamingSSL);
                int packetCount = BitConverter.ToInt32(packetCountBytes);
                int index = 48;
                byte[] currentPacketIndexBytes = new byte[4];
                byte[] currentPacketDataBytes = new byte[4092];

                for (int i = 0; i < packetCount; i++)
                {
                    if (_stopSend)
                    {
                        _stopSend = false;
                        SendSSL(EXIT, 4, _streamingSSL);
                        break;
                    }

                    SendSSL(DATA, 4, _streamingSSL);
                    Buffer.BlockCopy(songData, index, currentPacketIndexBytes, 0, 4);
                    index += 4;
                    Buffer.BlockCopy(songData, index, currentPacketDataBytes, 0, 4092);
                    index += 4092;
                    SendSSL(currentPacketIndexBytes, 4, _streamingSSL);
                    SendSSL(currentPacketDataBytes, 4092, _streamingSSL);
                }
            }
            GC.Collect();
        }

        /// <summary>
        /// This method must be run in a separate thread. It waits for the client to send the streaming mode.
        /// </summary>
        private void StreamingLoop()
        {
            try
            {
                while (!_exitStreamingLoop)
                {
                    byte[] mode = ReceiveSSL(4, _streamingSSL);
                    if (Enumerable.SequenceEqual(mode, MOD1))
                    {
                        byte[] songIdBytes = ReceiveSSL(4, _streamingSSL);
                        int songId = BitConverter.ToInt32(songIdBytes);
                        SendSongData(songId);
                    }
                    else if (Enumerable.SequenceEqual(mode, MOD2))
                    {
                        byte[] fromPacketBytes = ReceiveSSL(4, _streamingSSL);
                        byte[] toPacketBytes = ReceiveSSL(4, _streamingSSL);

                        int startIndex = BitConverter.ToInt32(fromPacketBytes);
                        int endIndex = BitConverter.ToInt32(toPacketBytes);
                        SendSongData(startIndex, endIndex);
                    }
                }
            }
            catch { }

            finally
            {
                TerminateSelf();
            }
        }

        /// <summary>
        /// If the client disconnects, ask the controller to terminate this clientHandler.
        /// </summary>
        private void TerminateSelf()
        {
            try
            {
                if(Monitor.TryEnter(_lock, 0))
                {
                    new ServerEvent(EventType.InternalRequest, true, InternalRequestType.RemoveClientHandler, this);
                }
            }
            catch { }
        }

        /// <summary>
        /// This method must be run in a separate thread. Here the clientHandler will handle the communication 
        /// with the client.
        /// </summary>
        private void CommuncationLoop()
        {
            try
            {
                while (!_exitCommunicationLoop)
                {
                    byte[] lengthBytes = ReceiveSSL(4, _communcationSSL);
                    int length = BitConverter.ToInt32(lengthBytes);

                    byte[] requestBytes = ReceiveSSL(length, _communcationSSL);
                    string request = Encoding.UTF8.GetString(requestBytes);

                    ExecuteCommunicationRequest(request);
                }
            }
            catch { }

            finally
            {
                TerminateSelf();
            }
        }


        private void ExecuteCommunicationRequest(string request)
        {
            string[] translate = request.Split('@');

            if(_isLoggedIn)
            {
                if (translate[0] == "TERMINATE_SONG_DATA_RECEIVE")
                {
                    TerminateSongDataReceive();
                }

                else if (translate[0] == "SEARCH")
                {
                    SearchForSong(translate[1]);
                }

                else if (translate[0] == "DISCONNECT")
                {
                    DisconnectClient();
                }
            }

            else
            {
                if (translate[0] == "LOGIN")
                {
                    LogIn().Wait();
                }
            }
        }

        private void DisconnectClient()
        {
            _communcationSSL!.WriteTimeout = 1000;
            _communcationSSL!.ReadTimeout = 1000;
            string ack = "";
            try
            {
                string reply = "OK";
                byte[] replyBytes = Encoding.UTF8.GetBytes(reply);

                int replyLength = replyBytes.Length;
                byte[] replyLengthBytes = BitConverter.GetBytes(replyLength);

                SendSSL(replyLengthBytes, 4, _communcationSSL);
                SendSSL(replyBytes, replyLength, _communcationSSL);

                byte[] ackLengthBytes = ReceiveSSL(4, _communcationSSL);
                int ackLength = BitConverter.ToInt32(ackLengthBytes);
                byte[] ackBytes = ReceiveSSL(ackLength, _communcationSSL);
                ack = Encoding.UTF8.GetString(ackLengthBytes);
            }
            
            catch { }

            finally
            {
                _communcationSSL!.WriteTimeout = Timeout.Infinite;
                _communcationSSL!.ReadTimeout = Timeout.Infinite;

                if(ack == "ACK")
                {
                    Thread.Sleep(50);
                    new Task(() =>
                    {
                        TerminateClientHandler();
                    }).Start();
                }
                else
                {
                    TerminateClientHandler();
                }
            }
        }

        private async Task LogIn()
        {
            IdentityUser? identityUser = null;

            byte[] emailLengthBytes = ReceiveSSL(4, _communcationSSL);
            int emailLength = BitConverter.ToInt32(emailLengthBytes);
            byte[] emailBytes = ReceiveSSL(emailLength, _communcationSSL);
            string email = Encoding.UTF8.GetString(emailBytes);

            byte[] passwordLengthBytes = ReceiveSSL(4, _communcationSSL);
            int passwordLength = BitConverter.ToInt32(passwordLengthBytes);
            byte[] passwordBytes = ReceiveSSL(passwordLength, _communcationSSL);
            string password = Encoding.UTF8.GetString(passwordBytes);

            bool validRequest = false;
            using (var scope = _serviceProvider.CreateScope())
            {
                UserManager<IdentityUser>? userManager = scope.ServiceProvider.GetService<UserManager<IdentityUser>>();
                if(userManager != null)
                {
                    identityUser = await userManager.FindByEmailAsync(email);

                    if (identityUser != null && await userManager.IsInRoleAsync(identityUser, "User"))
                    {
                        validRequest = await userManager.CheckPasswordAsync(identityUser, password);
                    }
                    else
                    {
                        validRequest = false;
                    }
                }
            }
            

            byte[] replyBytes;

            if (validRequest)
            {
                UserId = identityUser!.Id;
                LoggedIn(true);
                replyBytes = Encoding.UTF8.GetBytes("VALID");
            }
            else
            {
                LoggedIn(false);
                replyBytes = Encoding.UTF8.GetBytes("INVALID");
            }

            int replyLength = replyBytes.Length;
            byte[] replyLengthBytes = BitConverter.GetBytes(replyLength);

            SendSSL(replyLengthBytes, 4, _communcationSSL);
            SendSSL(replyBytes, replyLength, _communcationSSL);
        }

        private void LoggedIn(bool state)
        {
            if(state)
            {
                _isLoggedIn = true;
                _streamingThread.Start();
            }
            else
            {
                _isLoggedIn = false;
            }
        }

        /// <summary>
        /// Takes a pattern and searches the database for song names or artist names that match the pattern.
        /// If it finds songs that match the pattern, it will create Song object and serialize them into bytes.
        /// It will finally send the songs to the client. These songs do not contain song wave data.
        /// </summary>
        /// <param name="searchString"></param>
        private void SearchForSong(string searchString)
        {
            if (searchString != "")
            {
                var query = _audioRetrievingInternalService.GetSongsForSearch(searchString);

                List<Song> results = new List<Song>();

                foreach (var song in query)
                {
                    byte[] imageBytes = File.ReadAllBytes(song.ImageFileName);
                    results.Add(new Song(song.SongId, song.SongName, song.ArtistName, song.Duration, imageBytes));
                }

                int numberOfSongs = results.Count;
                byte[] numberOfSongsBytes = BitConverter.GetBytes(numberOfSongs);
                SendSSL(numberOfSongsBytes, 4, _communcationSSL);

                foreach (Song song in results)
                {
                    byte[] serializedSong = song.GetSerialized();
                    List<byte[]> packets = GetPackets(serializedSong, 1024);

                    int packetCount = packets.Count;
                    byte[] packetCountBytes = BitConverter.GetBytes(packetCount);
                    SendSSL(packetCountBytes, 4, _communcationSSL);


                    byte[] lastPacket = packets.Last();
                    int lastPacketLength = lastPacket.Length;
                    byte[] lastPacketLengthBytes = BitConverter.GetBytes(lastPacketLength);
                    SendSSL(lastPacketLengthBytes, 4, _communcationSSL);
                    SendSSL(lastPacket, lastPacketLength, _communcationSSL);

                    for (int i = 0; i < packets.Count - 1; i++)
                    {
                        SendSSL(packets[i], 1024, _communcationSSL);
                    }
                }
            }
            else
            {
                int numberOfSongs = 0;
                byte[] numberOfSongsBytes = BitConverter.GetBytes(numberOfSongs);
                SendSSL(numberOfSongsBytes, 4, _communcationSSL);
            }
        }

        /// <summary>
        /// This method will split the serializedSong into packets of packetSize.
        /// </summary>
        /// <param name="serializedSong"></param>
        /// <param name="packetSize"></param>
        /// <returns></returns>
        private List<byte[]> GetPackets(byte[] serializedSong, int packetSize)
        {
            int packetCount = serializedSong.Length / packetSize; // Possibly without last packet
            List<byte[]> packets = new List<byte[]>();

            int index = 0;
            byte[] currentPacket;

            for (int i = 0; i < packetCount; i++)
            {
                currentPacket = new byte[packetSize];
                Array.Copy(serializedSong, index, currentPacket, 0, packetSize);
                packets.Add(currentPacket);
                index += packetSize;
            }

            int lastPacketSize = serializedSong.Length - index;

            if (lastPacketSize > 0)
            {
                byte[] lastPacket = new byte[lastPacketSize];
                Array.Copy(serializedSong, index, lastPacket, 0, lastPacketSize);
                packets.Add(lastPacket);
            }

            return packets;
        }

        private void TerminateSongDataReceive()
        {
            _stopSend = true;
        }

        private void Run()
        {
            _communicationThread.Start();
        }

        /// <summary>
        /// Makes sure ReceiveTCP() receives exactly size amount of bytes.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="socket"></param>
        /// <returns></returns>
        /// <exception cref="SocketException"></exception>
        public static byte[] ReceiveSSL(int size, SslStream? sslStream)
        {
            if (sslStream == null)
            {
                throw new SocketException();
            }
            byte[] packet = new byte[size];
            int bytesReceived = 0;
            int x;
            while (bytesReceived < size)
            {
                byte[] buffer = new byte[size - bytesReceived];
                x = sslStream.Read(buffer);
                Buffer.BlockCopy(buffer, 0, packet, bytesReceived, x);
                bytesReceived += x;
            }
            return packet;
        }

        /// <summary>
        /// Makes sure SendTCP() sends exactly size abount of data.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="size"></param>
        /// <param name="socket"></param>
        /// <exception cref="SocketException"></exception>
        public static void SendSSL(byte[] data, int size, SslStream? sslStream)
        {
            if (sslStream == null)
            {
                throw new SocketException();
            }
            sslStream.Write(data, 0, size);
        }
    }
}
