using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Client_Application.WaveOut
{
    public sealed class WaveOutBuffer : IDisposable
    {
        private readonly IntPtr _waveOutHandle;
        private WindowsNative.WaveHeader _waveHeader;
        private readonly int _uSize;
        private readonly byte[] _bufferData;
        private readonly GCHandle _bufferHandle;
        private readonly GCHandle _headerHandle;
        private readonly GCHandle _thisHandle;
        private readonly BufferFillEventHandler _FillProc;
        private readonly object _lock = new object();
        public int Id { get; private set; }

        /// <summary>
        /// This class represents a buffer that can be used by the WaveOutPlayer.
        /// Use ReFill() to fill this buffer with data and to play it immediately after.
        /// </summary>
        /// <param name="waveOutHandle"></param>
        /// <param name="bufferSize"></param>
        /// <param name="userData"></param>
        /// <param name="fillProc"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public WaveOutBuffer(IntPtr waveOutHandle, int bufferSize, IntPtr userData, BufferFillEventHandler fillProc)
        {

            _FillProc = fillProc;

            if (waveOutHandle == IntPtr.Zero)
                throw new ArgumentNullException("waveOutHandle");
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize");

            _waveOutHandle = waveOutHandle;
            _bufferData = new byte[bufferSize];
            _bufferHandle = GCHandle.Alloc(_bufferData, GCHandleType.Pinned);
            
            _waveHeader = new WindowsNative.WaveHeader()
            {
                dwBufferSize = bufferSize,
                lpData = _bufferHandle.AddrOfPinnedObject(),
                dwLoops = 1,
                dwInstance = userData,
            };
            _headerHandle = GCHandle.Alloc(_waveHeader, GCHandleType.Pinned);
            _thisHandle = GCHandle.Alloc(this);
            _uSize = Marshal.SizeOf(_waveHeader);

            WindowsNative.waveOutPrepareHeader(_waveOutHandle, _waveHeader, Marshal.SizeOf(_waveHeader));
            Id = (int)_waveHeader.dwInstance;
        }

        /// <summary>
        /// Call this method to refill the buffer with data and play it using waveOutWrite().
        /// </summary>
        public void Refill()
        {
            _FillProc(_bufferData, _bufferData.Length);
            WindowsNative.waveOutWrite(_waveOutHandle, _waveHeader, Marshal.SizeOf(_waveHeader));
            GC.KeepAlive(this);
        }

        public HeaderFlags GetHeaderFlag()
        {
            return _waveHeader.dwFlags;
        }

        public bool IsQueued
        {
            get
            {
                return (_waveHeader.dwFlags & HeaderFlags.InQueue) == HeaderFlags.InQueue;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {

            if (_bufferHandle.IsAllocated)
            {
                _bufferHandle.Free();
            }
            if(_thisHandle.IsAllocated)
            {
                _thisHandle.Free();
            }
            if(_headerHandle.IsAllocated)
            {
                _headerHandle.Free();
            }
            WindowsNative.waveOutUnprepareHeader(_waveOutHandle, _waveHeader, _uSize);
        }


        ~WaveOutBuffer()
        {
            Dispose(false);
        }
    }
}
