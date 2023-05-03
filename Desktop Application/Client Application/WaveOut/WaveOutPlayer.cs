using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using Client_Application.Client.Event;

namespace Client_Application.WaveOut
{
    public sealed class WaveOutHelper
    {
        public static void Try(int err)
        {
            if (err != WindowsNative.MMSYSERR_NOERROR)
                throw new Exception(err.ToString());
        }
    }

    public sealed class WaveOutPlayer : IDisposable
    {
        // ATTRIBUTES
        private IntPtr _waveOutHandle;
        private WaveOutBuffer[] _buffers;
        private WindowsNative.WaveDelegate BufferProc; 
        private int _bufferCount;
        private readonly object _lock = new object();
        private readonly object _playLoopLock = new object();
        private int _bufferSize;
        private int _device;
        private volatile int _activeBuffers;
        public volatile PlaybackState _playbackState;
        private bool _disposed;
        private volatile bool _isPrepared;
        private volatile bool _stopPlayLoop = false;
        private readonly Queue<int> _bufferQueue = new Queue<int>();
        private WaveFormat _lastWaveFormat;
        // ATTRIBUTES

        // CALLBACKS
        private BufferFillEventHandler _FillProc;
        // CALLBACKS

        // THREADS
        private readonly Thread _bufferThread;
        // THREADS

        // THREADING RESET EVENTS 
        private readonly AutoResetEvent _buffersQueuedFlag = new AutoResetEvent(false);
        private readonly ManualResetEvent _playLoopDoneFlag = new ManualResetEvent(true);
        private static readonly AutoResetEvent _bufferDoneFlag = new AutoResetEvent(false);
        // THREADING RESET EVENTS

        public static int DeviceCount
        {
            get { return WindowsNative.waveOutGetNumDevs(); }
        }

        /// <summary>
        /// This class represents a player that can play wav data.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="bufferSize"></param>
        /// <param name="bufferCount"></param>
        /// <param name="fillProc"></param>
        public WaveOutPlayer(int device, int bufferSize, int bufferCount, BufferFillEventHandler fillProc)
        {
            _device = device;
            _FillProc = fillProc;
            BufferProc = new WindowsNative.WaveDelegate(Callback);
            byte[] bufferData = new byte[bufferSize];
            _bufferSize = bufferSize;
            _bufferCount = bufferCount;
            _buffers = new WaveOutBuffer[bufferCount];
            _playbackState = PlaybackState.Stopped;
            _isPrepared = false;
            _lastWaveFormat = new WaveFormat(0, 0, 0);
            _bufferThread = new Thread(PlayLoop);
            _bufferThread.Priority = ThreadPriority.Highest;
            _bufferThread.IsBackground = true;
        }

        /// <summary>
        /// Prepares the player for a Song.
        /// </summary>
        /// <param name="format"></param>
        public void Prepare(WaveFormat format)
        {
            Stop();
            if(_isPrepared)
            {
                WindowsNative.waveOutClose(_waveOutHandle);
            }
            WindowsNative.waveOutOpen(out _waveOutHandle, _device, format, BufferProc, 0, WindowsNative.CALLBACK_FUNCTION);
            DeallocateBuffers();
            AllocateBuffers();
            _lastWaveFormat = format;
            _isPrepared = true;
        }

        /// <summary>
        /// Start playing sound. Only call this method if the player is prepared using Prepare().
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Play()
        {
            lock (_lock)
            {
                if (!_isPrepared)
                    throw new Exception("Player is not prepared.");

                if (_playbackState == PlaybackState.Stopped)
                {
                    _playbackState = PlaybackState.Playing;
                    lock(_playLoopLock)
                    {
                        foreach (WaveOutBuffer buffer in _buffers.Where(x => !x.IsQueued))
                        {
                            try
                            {
                                _bufferQueue.Enqueue(buffer.Id);
                            }
                            catch { }
                        }

                        _buffersQueuedFlag.Set();
                    }
                }
                else if (_playbackState == PlaybackState.Paused)
                {
                    Resume();
                }
            }
        }

        /// <summary>
        /// Resume playback after calling Pause().
        /// </summary>
        public void Resume()
        {
            lock (_lock)
            {
                if (_playbackState == PlaybackState.Paused)
                {
                    WindowsNative.waveOutRestart(_waveOutHandle);
                    _playbackState = PlaybackState.Playing;
                }
            }
        }

        /// <summary>
        /// Pause playing sound.
        /// </summary>
        public void Pause()
        {
            lock (_lock)
            {
                if (_playbackState == PlaybackState.Playing)
                {
                    WindowsNative.waveOutPause(_waveOutHandle);
                    _playbackState = PlaybackState.Paused;
                }
            }
        }

        /// <summary>
        /// Check if player is not stopped.
        /// </summary>
        /// <returns></returns>
        public bool IsPlayerActive()
        {
            return _playbackState != PlaybackState.Stopped;
        }

        /// <summary>
        /// Stops playback and clears the internal buffers.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (_playbackState == PlaybackState.Playing)
                {
                    lock(_playLoopLock)
                    {
                        _stopPlayLoop = true;
                        _bufferDoneFlag.Set(); // In case Play Loop is waiting for buffers
                    }

                    WindowsNative.waveOutReset(_waveOutHandle);
                    _playbackState = PlaybackState.Stopped;
                    WaitForBuffersToFinish();
                    _bufferQueue.Clear();
                }

                else if (_playbackState == PlaybackState.Paused)
                {
                    _stopPlayLoop = true;
                    _bufferDoneFlag.Set();
                    WindowsNative.waveOutReset(_waveOutHandle);
                    _playbackState = PlaybackState.Stopped;
                    WaitForBuffersToFinish();
                    _bufferQueue.Clear();
                }

                else if (_playbackState == PlaybackState.Stopped && Interlocked.CompareExchange(ref _activeBuffers, 0, 0) != 0)
                {
                    WaitForBuffersToFinish();
                }
            }
        }

        /// <summary>
        /// Waits for active buffers to finish. Must not call waveOutClose before all the buffers have been returned.
        /// </summary>
        private void WaitForBuffersToFinish()
        {
            while (Interlocked.CompareExchange(ref _activeBuffers, 0, 0) > 0)
            {
                Thread.Sleep(10);
            }
        }

        private void PlayBuffer(WaveOutBuffer buffer)
        {
            buffer.Refill();
            Interlocked.Increment(ref _activeBuffers);
        }

        public void SetVolume(float volume)
        {
            int stereoVolume = (int)(volume * 0xFFFF) + ((int)(volume * 0xFFFF) << 16);
            WindowsNative.waveOutSetVolume(_waveOutHandle, stereoVolume);
        }

        private void AllocateBuffers()
        {
            lock (_lock)
            {
                for (int i = 0; i < _bufferCount; i++)
                {
                    _buffers[i] = new WaveOutBuffer(_waveOutHandle, _bufferSize, (IntPtr)i, _FillProc);
                }
            }
        }

        private void DeallocateBuffers()
        {
            lock (_lock)
            {
                Array.Clear(_buffers);
            }
        }

        /// <summary>
        /// Call this method to start the player. 
        /// </summary>
        public void Run()
        {
            _playLoopDoneFlag.Set();
            _bufferThread.Start();
        }

        /// <summary>
        /// This is the main playback loop.
        /// </summary>
        private void PlayLoop()
        {
            int currentBufferId;
            while (true)
            {
                _buffersQueuedFlag.WaitOne();
                Thread.Sleep(200); // Give time to properly open the waveform-audio output device for playback before sending buffers

                while (true)
                {
                    lock (_playLoopLock)
                    {
                        if (_stopPlayLoop)
                        {
                            _stopPlayLoop = false;
                            break;
                        }

                        if (_bufferQueue.Count == 0)
                        {
                            _bufferDoneFlag.WaitOne();
                        }

                        if (_bufferQueue.TryDequeue(out currentBufferId))
                        {
                            PlayBuffer(_buffers[currentBufferId]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This method is called by Windows when a buffer is played and it is returned.
        /// </summary>
        /// <param name="hwo"></param>
        /// <param name="uMsg"></param>
        /// <param name="dwInstance"></param>
        /// <param name="waveHeader"></param>
        /// <param name="dwParam2"></param>
        private void Callback(
            IntPtr hwo,
            uint uMsg,
            IntPtr dwInstance,
            WindowsNative.WaveHeader waveHeader,
            IntPtr dwParam2
            )
        {
            try
            {
                if (uMsg == WindowsNative.MM_WOM_DONE)
                {
                    int id = (int)waveHeader.dwInstance;
                    _bufferQueue.Enqueue(id);
                    _bufferDoneFlag.Set();
                    Interlocked.Decrement(ref _activeBuffers);
                }
            }
            catch (Exception)
            {
                Recover();
            }
        }

        private void Recover()
        {
            Task.Run(() =>
            {
                _bufferDoneFlag.Set();
            });
            Task.Run(() =>
            {
                Prepare(_lastWaveFormat);
                Play();
            });
        }

        private void DisposeBuffers()
        {
            for (int i = 0; i < _bufferCount; i++)
            {
                if (_buffers[i] != null)
                {
                    _buffers[i].Dispose();
                }
            }
        }

        public void Dispose()
        {
            DisposeBuffers();
        }

        ~WaveOutPlayer()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    if (_waveOutHandle != IntPtr.Zero)
                        return;
                    Stop();

                    DisposeBuffers();
                    WindowsNative.waveOutClose(_waveOutHandle);
                    _waveOutHandle = IntPtr.Zero;
                    _isPrepared = false;
                    _disposed = true;
                }
            }
        }
    }
}
