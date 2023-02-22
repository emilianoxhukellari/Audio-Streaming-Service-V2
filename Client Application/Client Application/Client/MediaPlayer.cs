using Client_Application.Streaming;
using Client_Application.WaveOut;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client_Application.Client
{
    public sealed class MediaPlayer
    {
        // ATTRIBUTES
        private WaveOutPlayer _waveOutPlayer;
        private AudioStream _audioStream;
        private SongQueue _queue = new SongQueue();
        private volatile int _currentSongIndex = 0;
        private RepeatState _repeatState;
        private ShuffleState _shuffleState;
        private DualSocket _dualSocket;
        private readonly object _lock = new object();
        // ATTRIBUTES

        // THREADS and TASKS
        private Thread _receiveThread;
        private Task _autoSongTask;
        // THREADS and TASKS

        // THREADING RESET EVENTS
        private readonly ManualResetEvent _startReceiveFlag;
        private readonly ManualResetEvent _currentlyReceivingFlag;
        private readonly AutoResetEvent _preparedFlag;
        private readonly ManualResetEvent _autoSongFlag;
        // THREADING RESET EVENTS

        // NETWORKING COMMANDS
        private readonly byte[] DATA = Encoding.UTF8.GetBytes("data");
        private readonly byte[] EXIT = Encoding.UTF8.GetBytes("exit");
        private readonly byte[] MOD1 = Encoding.UTF8.GetBytes("mod1");
        private readonly byte[] MOD2 = Encoding.UTF8.GetBytes("mod2");
        // NETWORKING COMMANDS

        // CALLBACKS
        private CallbackTerminateSongDataReceive TerminateSongDataReceive;
        private CallbackSendCurrentSongInfo SendCurrentSongInfo;
        private CallbackSendQueueInfo SendQueueInfo;
        private CallbackUpdateRepeatState UpdateRepeatState;
        // CALLBACKS

        public MediaPlayer(DualSocket dualSocket,
            CallbackTerminateSongDataReceive terminateSongDataReceiveCallback,
            CallbackSendCurrentSongInfo callbackSendCurrentSongInfo,
            CallbackSendQueueInfo callbackSendQueueInfo,
            CallbackUpdateRepeatState callbackUpdateRepeatState)
        {
            _dualSocket = dualSocket;
            _audioStream = new AudioStream(8192, 4092, OptimizeStream, EndOfStream);
            _waveOutPlayer = new WaveOutPlayer(-1, 8192, 3, new BufferFillEventHandler(Filler));
            _receiveThread = new Thread(ReceiveFromServer);
            _receiveThread.IsBackground = true;
            _autoSongTask = new Task(AutoSong);
            _startReceiveFlag = new ManualResetEvent(false);
            _currentlyReceivingFlag = new ManualResetEvent(true);
            _preparedFlag = new AutoResetEvent(false);
            _autoSongFlag = new ManualResetEvent(false);
            TerminateSongDataReceive = new CallbackTerminateSongDataReceive(terminateSongDataReceiveCallback);
            SendCurrentSongInfo = new CallbackSendCurrentSongInfo(callbackSendCurrentSongInfo);
            SendQueueInfo = new CallbackSendQueueInfo(callbackSendQueueInfo);
            UpdateRepeatState = new CallbackUpdateRepeatState(callbackUpdateRepeatState);
            _repeatState = RepeatState.RepeatOff;
            _shuffleState = ShuffleState.Unshuffled;
        }

        public void ResetMediaPlayer()
        {
            _waveOutPlayer.Stop();
            TerminateReceive();
            _queue.ClearQueue();
            _currentSongIndex = 0;
            SetRepeatState(RepeatState.RepeatOff);
            SetShuffleState(ShuffleState.Unshuffled);
            SetVolume(1f);
        }

        /// <summary>
        /// Call this method to set the volume of the player. Values from 0 to 1.
        /// </summary>
        /// <param name="volume"></param>
        public void SetVolume(float volume)
        {
            if (volume < 0)
            {
                volume = 0;
            }
            else if (volume > 1)
            {
                volume = 1;
            }
            _waveOutPlayer.SetVolume(volume);
        }

        private void DisplayQueue(bool automatedDisplay = false)
        {
            List<(Song, int)> results = new List<(Song, int)>(0);
            int startIndex;
            if (_waveOutPlayer.IsPlayerActive() || automatedDisplay)
            {
                startIndex = _currentSongIndex + 1;
            }
            else
            {
                startIndex = _currentSongIndex;
            }
            for (int i = startIndex; i < _queue.Count; i++)
            {
                Song song;
                if (_queue.TryGet(out song, i))
                {
                    results.Add((song, i));
                }
            }
            SendQueueInfo(results);
        }


        /// <summary>
        /// This method will return the current progress of the song. Progress is from 0 to 1.
        /// </summary>
        /// <returns></returns>
        public (double progress, string currentTime) GetCurrentSongProgress()
        {
            Song song;
            if (_queue.TryGet(out song, _currentSongIndex))
            {
                double durationDouble = song.Duration;
                double progressDouble = _audioStream.GetProgress();
                double currentTimeDouble = durationDouble * progressDouble;
                string currentTimeString = Song.SecondsToString(currentTimeDouble);
                return (progressDouble, currentTimeString);
            }
            else
            {
                return (0, "00:00");
            }
        }

        // Set position of the playback. Values from 0 to 1.
        public void SetPosition(double position)
        {
            _audioStream.SetProgress(position);
        }

        /// <summary>
        /// Call this method to change the playback state of the player. 
        /// If the player is paused it will resume. 
        /// If the player is playing, it will pause.
        /// </summary>
        /// <returns></returns>
        public PlaybackState ChangePlayState()
        {
            if (_waveOutPlayer._playbackState == PlaybackState.Paused)
            {
                _waveOutPlayer.Resume();
                return PlaybackState.Playing;
            }
            else if (_waveOutPlayer._playbackState == PlaybackState.Playing)
            {
                _waveOutPlayer.Pause();
                return PlaybackState.Paused;
            }
            return PlaybackState.Stopped;
        }

        private void EndOfStream() // Player has reached end of stream
        {
            _autoSongFlag.Set();
        }

        
        private void AutoSong() // Auto next song
        {
            while (true)
            {
                _autoSongFlag.WaitOne();
                _autoSongFlag.Reset();
                if (_repeatState == RepeatState.OnRepeat)
                {
                    NextSong(repeatOne: true);
                }
                else
                {
                    NextSong(repeatOne: false);
                }
            }
        }

        private void StartReceiveData()
        {
            _startReceiveFlag.Set();
            _currentlyReceivingFlag.Reset();
        }

        /// <summary>
        /// Call this method to play the songs given.
        /// The method will clear the queue, add the songs to the queue, and start playing the first song.
        /// </summary>
        /// <param name="songs"></param>
        public void PlayPlaylistSongs(List<Song> songs)
        {
            _waveOutPlayer.Stop();
            TerminateReceive();
            _queue.ClearQueue();
            _currentSongIndex = 0;
            foreach (Song song in songs)
            {
                _queue.AppendToQueue(song);
            }
            if (_shuffleState == ShuffleState.Shuffled)
            {
                _queue.Shuffle(_currentSongIndex);
            }
            if (_repeatState == RepeatState.OnRepeat)
            {
                UpdateRepeatState(RepeatState.RepeatOn);
                _repeatState = RepeatState.RepeatOn;
            }
            StartSong();
        }

        /// <summary>
        /// Call this method to add all the specified songs to the queue.
        /// </summary>
        /// <param name="songs"></param>
        public void AddPlaylistSongsToQueue(List<Song> songs)
        {
            foreach (Song song in songs)
            {
                _queue.AppendToQueue(song);
            }
            DisplayQueue();
        }

        private void StartSong() // Start playing sound.
        {
            if (_queue.Count > 0 && _dualSocket.Connected)
            {
                try
                {
                    _waveOutPlayer.Stop();
                    TerminateReceive();
                    Song currentSong = _queue[_currentSongIndex];
                    int requestSongId = currentSong.SongId;
                    byte[] requestSongIdBytes = BitConverter.GetBytes(requestSongId);
                    lock (_lock)
                    {
                        _dualSocket.StreamingSSL.SendSSL(MOD1, 4);
                        _dualSocket.StreamingSSL.SendSSL(requestSongIdBytes, 4);
                    }
                    StartReceiveData();
                    _preparedFlag.WaitOne();
                    _waveOutPlayer.Play();
                    SendCurrentSongInfo(currentSong);
                    DisplayQueue(true);
                }
                catch (Exception ex) when (ex is IOException or ExceptionSSL or SocketException)
                {
                    _dualSocket.Reconnect();
                }
            }
        }

        /// <summary>
        /// Plays the song.
        /// </summary>
        /// <param name="song"></param>
        public void PlayThis(Song song)
        {
            if (_repeatState == RepeatState.OnRepeat)
            {
                UpdateRepeatState(RepeatState.RepeatOn);
            }
            if (_waveOutPlayer.IsPlayerActive())
            {
                _queue.InsertToQueue(_currentSongIndex + 1, song);
                Interlocked.Increment(ref _currentSongIndex);
            }
            else
            {
                _queue.InsertToQueue(_currentSongIndex, song);
            }
            StartSong();
        }

        /// <summary>
        /// Call this method to delete the Song queue of MediaPlayer.
        /// </summary>
        public void DeleteQueue()
        {
            if (_waveOutPlayer.IsPlayerActive())
            {
                Song currentSong = _queue[_currentSongIndex];
                _queue.ClearQueue();
                _queue.AppendToQueue(currentSong);
                Interlocked.Exchange(ref _currentSongIndex, 0);
            }
            else
            {
                _queue.ClearQueue();
            }
            DisplayQueue();
        }

        /// <summary>
        /// Adds the song to queue.
        /// </summary>
        /// <param name="song"></param>
        public void AddToQueue(Song song)
        {
            _queue.AppendToQueue(song);
            DisplayQueue();
        }

        /// <summary>
        /// Removed the song.
        /// </summary>
        /// <param name="songIndex"></param>
        public void RemoveSongFromQueue(int songIndex)
        {
            try
            {
                _queue.RemoveFromQueue(songIndex);
                DisplayQueue();
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Moves song down the queue one position. 
        /// </summary>
        /// <param name="songIndex"></param>
        public void MoveSongDown(int songIndex)
        {
            if (songIndex < _queue.Count - 1)
            {
                (_queue[songIndex], _queue[songIndex + 1]) = (_queue[songIndex + 1], _queue[songIndex]);
                DisplayQueue();
            }
        }

        /// <summary>
        /// Moves the song up the queue one position.
        /// </summary>
        /// <param name="songIndex"></param>
        public void MoveSongUp(int songIndex)
        {
            int start = _waveOutPlayer.IsPlayerActive() ? _currentSongIndex + 1 : _currentSongIndex;

            if (songIndex > start)
            {
                (_queue[songIndex], _queue[songIndex - 1]) = (_queue[songIndex - 1], _queue[songIndex]);
                DisplayQueue();
            }
        }

        /// <summary>
        /// Sets the repeat state for the media player. 
        /// </summary>
        /// <param name="repeatState"></param>
        public void SetRepeatState(RepeatState repeatState)
        {
            _repeatState = repeatState;
        }

        /// <summary>
        /// Sets the shuffle state for the media player.
        /// </summary>
        /// <param name="shuffleState"></param>
        public void SetShuffleState(ShuffleState shuffleState)
        {
            _shuffleState = shuffleState;
            if (shuffleState == ShuffleState.Shuffled)
            {
                _queue.Shuffle(_currentSongIndex);
                DisplayQueue();
            }
            else if (shuffleState == ShuffleState.Unshuffled)
            {
                _queue.UnShuffle(_currentSongIndex);
                DisplayQueue();
            }
        }

        /// <summary>
        /// Play next song from queue. If repeatOne is true, it will play the same song it was previously playing.
        /// </summary>
        /// <param name="repeatOne"></param>
        public void NextSong(bool repeatOne = false)
        {
            if (_queue.Count != 0)
            {
                if (_repeatState == RepeatState.OnRepeat && !repeatOne)
                {
                    UpdateRepeatState(RepeatState.RepeatOn);
                    _repeatState = RepeatState.RepeatOn;
                }

                if (repeatOne)
                {
                    StartSong();
                }

                else if (_repeatState == RepeatState.RepeatOff)
                {
                    if (_currentSongIndex != _queue.Count - 1)
                    {
                        Interlocked.Increment(ref _currentSongIndex);
                    }
                    StartSong();
                }

                else if (_repeatState == RepeatState.RepeatOn)
                {
                    if (_currentSongIndex == _queue.Count - 1)
                    {
                        Interlocked.Exchange(ref _currentSongIndex, 0);
                    }
                    else
                    {
                        Interlocked.Increment(ref _currentSongIndex);
                    }
                    StartSong();
                }
            }
        }

        /// <summary>
        /// Plays the previous song from queue.
        /// </summary>
        public void PreviousSong()
        {
            if (_repeatState == RepeatState.OnRepeat)
            {
                UpdateRepeatState(RepeatState.RepeatOn);
            }

            if (_queue.Count != 0 && _currentSongIndex != 0)
            {
                Interlocked.Decrement(ref _currentSongIndex);
            }
            StartSong();
        }

        private void OptimizeStream(int start, int end) // Asks the server for the specific packets from start to end
        {
            lock (_lock)
            {
                int fromPacket = start;
                byte[] fromPacketBytes = BitConverter.GetBytes(fromPacket);
                int toPacket = end;
                byte[] toPacketBytes = BitConverter.GetBytes(toPacket);
                TerminateReceive();
                _dualSocket.StreamingSSL.SendSSL(MOD2, 4);
                _dualSocket.StreamingSSL.SendSSL(fromPacketBytes, 4);
                _dualSocket.StreamingSSL.SendSSL(toPacketBytes, 4);
                StartReceiveData();
            }
        }

        private void ReceiveFromServer()
        {
            byte[] packet;
            byte[] data = new byte[4092];
            byte[] indexBytes = new byte[4];
            byte[] currentType;
            byte[] mode = MOD1; // MOD1 for normal receive; MOD2 for optimization receive
            while (true)
            {
                _startReceiveFlag.WaitOne();
                _currentlyReceivingFlag.Reset();
                _startReceiveFlag.Reset();
                bool receivedAll = true;
                try
                {
                    mode = _dualSocket.StreamingSSL.ReceiveSSL(4);

                    if (Enumerable.SequenceEqual(mode, MOD1))
                    {
                        byte[] waveHeader = _dualSocket.StreamingSSL.ReceiveSSL(44);
                        _audioStream.Prepare(waveHeader);
                        _waveOutPlayer.Prepare(_audioStream.Format);
                        _preparedFlag.Set();
                    }

                    byte[] packetCountBytes = _dualSocket.StreamingSSL.ReceiveSSL(4);
                    int packetCount = BitConverter.ToInt32(packetCountBytes);
                    for (int i = 0; i < packetCount; i++) // Receive all packets unless terminated. Termination comes as command from server
                    {
                        currentType = _dualSocket.StreamingSSL.ReceiveSSL(4);
                        if (Enumerable.SequenceEqual(currentType, DATA))
                        {
                            packet = _dualSocket.StreamingSSL.ReceiveSSL(4096);
                            Buffer.BlockCopy(packet, 0, indexBytes, 0, 4);
                            Buffer.BlockCopy(packet, 4, data, 0, 4092);
                            _audioStream.ReceivePacket(data, BitConverter.ToInt32(indexBytes));
                        }
                        else if (Enumerable.SequenceEqual(currentType, EXIT)) // Terminate
                        {
                            receivedAll = false;
                            break;
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException or ExceptionSSL or SocketException)
                {
                    receivedAll = false;
                    _dualSocket.Reconnect();
                }
                finally
                {
                    if (_waveOutPlayer.IsPlayerActive() && Enumerable.SequenceEqual(mode, MOD2) && receivedAll)
                    {
                        _audioStream.AutomaticFilling();
                    }
                    _currentlyReceivingFlag.Set();
                }
            }
        }

        private void TerminateReceive()
        {
            if (!_currentlyReceivingFlag.WaitOne(0, true))
            {
                TerminateSongDataReceive();
            }
            _currentlyReceivingFlag.WaitOne();
        }

        /// <summary>
        /// This method is a callback method used my the waveOutPlayer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        private void Filler(byte[] buffer, int size)
        {

            if (!_audioStream.ReadPacket(buffer, size)) // If not all data was read, fill the buffer with 0s
            {
                for (int i = 0; i < size; i++) 
                    buffer[i] = 0;
            }
        }

        /// <summary>
        /// Call this method to start the MediaPlayer.
        /// </summary>
        public void Run()
        {
            _receiveThread.Start();
            _autoSongTask.Start();
            _waveOutPlayer.Run();
        }
    }
}
