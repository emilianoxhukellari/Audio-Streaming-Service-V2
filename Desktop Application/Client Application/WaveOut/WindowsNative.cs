using System;
using System.Runtime.InteropServices;

namespace Client_Application.WaveOut
{
    public enum WaveFormats
    {
        Pcm = 1,
        Float = 3
    }

    [Flags]
    public enum HeaderFlags
    {
        None = 0x0,
        BeginLoop = 0x00000004,
        Done = 0x00000001,
        EndLoop = 0x00000008,
        InQueue = 0x00000010,
        Prepared = 0x00000002
    }

    [StructLayout(LayoutKind.Sequential)]
    public sealed class WaveFormat
    {
        public short Format;
        public short NumberOfChannels;
        public int SampleRate;
        public int BytesPerSecond;
        public short BlockAlign;
        public short BitsPerSample;
        public short cbSize;

        public WaveFormat(int rate, int bits, int channels)
        {
            Format = (short)WaveFormats.Pcm;
            NumberOfChannels = (short)channels;
            SampleRate = rate;
            BitsPerSample = (short)bits;
            cbSize = 0;
            BlockAlign = (short)(channels * (bits / 8));
            BytesPerSecond = SampleRate * BlockAlign;
        }
    }

    /// <summary>
    /// This class contains Native Windows Calls.
    /// </summary>
  
    // https://learn.microsoft.com/en-us/windows/win32/multimedia/multimedia-functions
    public sealed class WindowsNative
    {
        public const int MMSYSERR_NOERROR = 0;
        public const int MM_WOM_OPEN = 0x3BB;
        public const int MM_WOM_CLOSE = 0x3BC;
        public const int MM_WOM_DONE = 0x3BD;
        public const int CALLBACK_FUNCTION = 0x30000;
        public delegate void WaveDelegate(IntPtr hwo, uint uMsg, IntPtr dwInstance, WaveHeader wavhdr, IntPtr dwParam2);

        [StructLayout(LayoutKind.Sequential)]
        public class WaveHeader
        {
            public IntPtr lpData; // pointer to data buffer
            public int dwBufferSize; // length of data buffer
            public int dwBytesRecorded; // used for input only
            public IntPtr dwInstance; // for client's use
            public HeaderFlags dwFlags; // flags
            public int dwLoops; // loop control counter
            public IntPtr lpNext; // PWaveHdr, reserved for driver
            public IntPtr reserved; // reserved for driver
        }

        // Windows Native Calls
        [DllImport("winmm.dll")]
        public static extern int waveOutGetNumDevs();
        [DllImport("winmm.dll")]
        public static extern int waveOutPrepareHeader(IntPtr hWaveOut, WaveHeader lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern int waveOutUnprepareHeader(IntPtr hWaveOut, WaveHeader lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern int waveOutWrite(IntPtr hWaveOut, WaveHeader lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, WaveFormat lpFormat, WaveDelegate dwCallback, int dwInstance, int dwFlags);
        [DllImport("winmm.dll")]
        public static extern int waveOutReset(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern int waveOutClose(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern int waveOutPause(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern int waveOutRestart(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern int waveOutGetPosition(IntPtr hWaveOut, out int lpInfo, int uSize);
        [DllImport("winmm.dll")]
        public static extern int waveOutSetVolume(IntPtr hWaveOut, int dwVolume);
        [DllImport("winmm.dll")]
        public static extern int waveOutGetVolume(IntPtr hWaveOut, out int dwVolume);
    }
}
