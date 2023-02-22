using Client_Application.WaveOut;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Client_Application.Streaming
{
    /// <summary>
    /// Represents a range that has song data.
    /// </summary>
    public sealed class PacketRange
    {
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public PacketRange(int startIndex, int endIndex)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        /// <summary>
        /// It checks whether the range contains the index.
        /// </summary>
        /// <param name="currentPacketIndex"></param>
        /// <returns></returns>
        public bool Contains(int index)
        {
            return (index >= StartIndex && index <= EndIndex);
        }
    }

    /// <summary>
    /// This class represents an intelligent stream. It is thread-safe, where mutiple threads can write and read
    /// at the same time.
    /// Prepare() must be called before using it.
    /// </summary>
    public sealed class AudioStream
    {
        private byte[] _data;
        private int _dataSize;
        private int _lastIndex;
        private WaveFormat _waveFormat;
        public int ReadIndex { get; private set; }
        public int WriteIndex { get; private set; }
        private bool IsActive { get; set; }
        private int _maxPosition;
        private List<PacketRange> _ranges = new List<PacketRange>(0);
        private readonly int _playerBufferSize;
        private readonly int _packetSize;
        private readonly object _lock = new object();
        private readonly object _readLock = new object();
        private CallbackOptimize Optimize;
        private CallbackEndOfStream EndOfStream;

        public AudioStream(int playerBufferSize, int packetSize, CallbackOptimize callbackOptimize, CallbackEndOfStream callbackEndOfStream) // packet size without index (32 bit)
        {
            _data = new byte[0];
            Optimize = new CallbackOptimize(callbackOptimize);
            EndOfStream = new CallbackEndOfStream(callbackEndOfStream);
            _playerBufferSize = playerBufferSize;
            _packetSize = packetSize;
            _waveFormat = new WaveFormat(0, 0, 0);
            IsActive = false;
        }

        /// <summary>
        /// Prepares the AudioStream for the specified WaveHeader.
        /// </summary>
        /// <param name="header"></param>
        public void Prepare(byte[] header)
        {
            lock (_lock)
            {
                _ranges.Clear();
                ReadHeader(header, out _waveFormat, out _dataSize);
                _data = new byte[_dataSize];
                _lastIndex = _dataSize - 1;
                ReadIndex = 0;
                WriteIndex = 0;
                _maxPosition = _dataSize / _playerBufferSize;
                IsActive = true;
                GC.Collect();
            }
        }

        public WaveFormat Format
        {
            get { return _waveFormat; }
        }

        /// <summary>
        /// Reads the header and gets the format and dataSize from the header.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="waveFormat"></param>
        /// <param name="dataSize"></param>
        private void ReadHeader(byte[] header, out WaveFormat waveFormat, out int dataSize)
        {
            waveFormat = new WaveFormat(0, 0, 0);
            waveFormat.Format = BitConverter.ToInt16(header.Take(new Range(20, 22)).ToArray());
            waveFormat.NumberOfChannels = BitConverter.ToInt16(header.Take(new Range(22, 24)).ToArray());
            waveFormat.SampleRate = BitConverter.ToInt32(header.Take(new Range(24, 28)).ToArray());
            waveFormat.BytesPerSecond = BitConverter.ToInt32(header.Take(new Range(28, 32)).ToArray());
            waveFormat.BlockAlign = BitConverter.ToInt16(header.Take(new Range(32, 34)).ToArray());
            waveFormat.BitsPerSample = BitConverter.ToInt16(header.Take(new Range(34, 36)).ToArray());
            dataSize = BitConverter.ToInt32(header.Take(new Range(40, 44)).ToArray());
        }

        public enum RangeState
        {
            RangeAdded,
            RangeRemoved,
            RangeNoChange
        }

        /// <summary>
        /// This method will update the ranges. If the currentPacketIndex is the same as the end index
        /// of one of the ranges, it will concatinate them into one.
        /// If the currentPacketIndex is not the same it will create a new range.
        /// If the end index of a range is the same as the start index of the next range, it will concatinate
        /// them and remove the range on the right.
        /// </summary>
        /// <param name="currentPacketIndex"></param>
        /// <returns></returns>
        private RangeState UpdateRange(int currentPacketIndex)
        {
            for (int i = 0; i < _ranges.Count; i++)
            {
                if (AddToRange(_ranges[i], currentPacketIndex))
                {
                    if (i != _ranges.Count - 1) // Not last range
                    {
                        if (_ranges[i].EndIndex == _ranges[i + 1].StartIndex)
                        {
                            _ranges[i].EndIndex = _ranges[i + 1].EndIndex;
                            _ranges.RemoveAt(i + 1);
                            return RangeState.RangeRemoved;
                        }
                    }
                    return RangeState.RangeNoChange;
                }
            }
            _ranges.Add(new PacketRange(currentPacketIndex, currentPacketIndex + _packetSize));
            _ranges.Sort((x, y) => x.StartIndex.CompareTo(y.StartIndex));
            return RangeState.RangeAdded;
        }


        private int GetValidEndIndex(int index)
        {
            if (index < _dataSize)
            {
                return index;
            }
            else
            {
                return _dataSize;
            }
        }

        /// <summary>
        /// Adds the currentPacketIndex to the range.
        /// </summary>
        /// <param name="range"></param>
        /// <param name="currentPacketIndex"></param>
        /// <returns></returns>
        private bool AddToRange(PacketRange range, int currentPacketIndex)
        {
            if (range.EndIndex == currentPacketIndex)
            {
                range.EndIndex = GetValidEndIndex(currentPacketIndex + _packetSize);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Optimize for dynamic streaming.
        /// </summary>
        /// <param name="fromPacket"></param>
        /// <param name="toPacket"></param>
        private void OptimizeDynamicStreaming(int fromPacket, int toPacket)
        {
            Optimize(fromPacket, toPacket);
        }

        /// <summary>
        /// Call this method to auto fill the patches in the audio stream only when a dynamic stream request has been done.
        /// </summary>
        public void AutomaticFilling()
        {
            int currentReadIndex = ReadIndex;
            if (!HasPacket(0, _dataSize)) // If it does not have all Data
            {
                if (_ranges.Count > 1) // There is at least one empty space
                {
                    var result = HasEmptySpaceRight(GetValidIndexFloor(currentReadIndex));

                    if (result.HasRange) // If there is an empty space on the right of ReadIndex, start there
                    {
                        bool hasEndRange = false;
                        int endRangeIndex = -1;
                        // Is there a range that ends after read index and starts before read index?
                        for (int i = 0; i < _ranges.Count; i++)
                        {
                            if (_ranges[i].StartIndex < currentReadIndex && _ranges[i].EndIndex >= currentReadIndex)
                            {
                                hasEndRange = true;
                                endRangeIndex = _ranges[i].EndIndex;
                                break; // Only one at a time
                            }
                        }
                        if (hasEndRange)
                        {
                            Task.Run(() => { OptimizeDynamicStreaming(endRangeIndex, result.RangeIndex); });
                        }
                        else
                        {
                            // There is no packet that starts before ReadIndex and ends after ReadIndex
                            // Thus, we can start from ReadIndex
                            Task.Run(() => { OptimizeDynamicStreaming(GetValidIndexFloor(currentReadIndex), result.RangeIndex); });
                        }
                    }

                    else // Start filling from the beginning
                    {
                        Task.Run(() => { OptimizeDynamicStreaming(_ranges[0].EndIndex, _ranges[1].StartIndex - 4092); });
                    }
                }
                else if (_ranges.Count != 0) // There is only one range, it starts at 0 but does not end at _dataSize
                {
                    FillNextRangeRight(_ranges[0].EndIndex);
                }
            }
        }

        /// <summary>
        /// The index must be a multiplier of 4092.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private int GetValidIndexFloor(int index)
        {
            while (index % 4092 != 0)
            {
                index--;
            }
            return index;
        }

        /// <summary>
        /// Call this method add data to the stream at the specified index.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="currentPacketIndex"></param>
        public void ReceivePacket(byte[] data, int currentPacketIndex)
        {
            lock (_lock)
            {
                if (!HasPacket(currentPacketIndex, currentPacketIndex + data.Length)) // If it already has the packet, don't add -> unlikely to hapen
                {
                    WriteIndex = currentPacketIndex;
                    for (int i = 0; i < data.Length; i++)
                    {
                        _data[WriteIndex] = data[i];
                        if (WriteIndex == _dataSize - 1)
                        {
                            WriteIndex = 0;
                        }
                        else
                        {
                            WriteIndex++;
                        }
                    }
                    UpdateRange(currentPacketIndex);
                }
            }
        }

        private bool IsLastPacket(int size)
        {
            if (ReadIndex >= _dataSize - size)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Call this method to read data from the audio stream. Returns true if all data was read or if end of stream.
        /// Returns false if could not read all data and not end of stream.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public bool ReadPacket(byte[] buffer, int size)
        {
            lock (_readLock)
            {
                if (HasPacket(ReadIndex, ReadIndex + size))
                {
                    for (int i = 0; i < size; i++)
                    {
                        buffer[i] = _data[ReadIndex++];
                    }
                    return true;
                }
                else if (IsLastPacket(size))
                {
                    int index = 0;
                    int bytesLeft = _dataSize - ReadIndex;
                    while (index < bytesLeft)
                    {
                        buffer[index] = _data[ReadIndex++];
                        index++;
                    }
                    while (index < size - 1)
                    {
                        buffer[index] = 0;
                        index++;
                    }
                    ReadIndex = 0;
                    EndOfStream();
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Get the progress of where data is being read from.
        /// </summary>
        /// <returns></returns>
        public double GetProgress()
        {
            if (_lastIndex == 0)
            {
                return 0;
            }
            else
            {
                return ((double)ReadIndex / _lastIndex);
            }
        }


        /// <summary>
        /// Position from 0 to 1.
        /// </summary>
        /// <param name="position"></param>
        public void SetProgress(double progress)
        {
            lock(_readLock)
            {
                if (progress < 0)
                {
                    ReadIndex = 0;
                }
                else if (progress > 1)
                {
                    ReadIndex = _dataSize - 1;
                }
                else
                {
                    int position = (int)(progress * _maxPosition);
                    ReadIndex = position * _playerBufferSize;
                    if (IsActive)
                    {
                        if (!HasPacket(0, _dataSize))
                        {
                            ProcessReadIndexChange();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if there is empty space on the right of index and returns the index where it ends.
        /// </summary>
        /// <param name="fromIndex"></param>
        /// <returns></returns>
        private (bool HasRange, int RangeIndex) HasEmptySpaceRight(int fromIndex)
        {
            for (int i = 0; i < _ranges.Count; i++)
            {
                if (_ranges[i].StartIndex > fromIndex)
                {
                    return (true, _ranges[i].StartIndex);
                }
            }
            return (false, -1);
        }

        /// <summary>
        /// Fill the next empty space on the right.
        /// </summary>
        /// <param name="fromIndex"></param>
        private void FillNextRangeRight(int fromIndex)
        {
            var result = HasEmptySpaceRight(fromIndex);

            if (result.HasRange) // Fill data until the end of the empty space.
            {
                Task.Run(() => { OptimizeDynamicStreaming(fromIndex, result.RangeIndex); });
            }

            else // Fill until the end
            {
                Task.Run(() => { OptimizeDynamicStreaming(fromIndex, GetValidIndexFloor(_dataSize - 1)); });
            }
        }

        /// <summary>
        /// User changed index. Dynamic streaming optimization will start.
        /// </summary>
        private void ProcessReadIndexChange()
        {
            if(!HasPacket(0, _dataSize))
            {
                int currentReadIndex = ReadIndex;
                int currentWriteIndex = WriteIndex;
                if (WriteIndex < ReadIndex + 300 * _playerBufferSize)
                {
                    if (!HasPacket(currentReadIndex, _dataSize)) // Write Index is too far on the left -> start filling on the right immediately
                    {
                        FillNextRangeRight(GetValidIndexFloor(ReadIndex));
                    }
                }
                else if (WriteIndex >= ReadIndex + 300 * _playerBufferSize && WriteIndex < ReadIndex) // Write index is closer than 300 buffers away on the left
                {
                    if (!HasPacket(currentReadIndex, _dataSize)) // However, if it does not has all the data on the right of Read Index -> start on the right
                    {
                        FillNextRangeRight(GetValidIndexFloor(ReadIndex));
                    }
                    // Else wait for a very short time
                }
                else if (WriteIndex > ReadIndex)
                {
                    if (!HasPacket(currentReadIndex, currentWriteIndex)) // There is a space between Read Index and Write Index -> Fill it
                    {
                        FillNextRangeRight(GetValidIndexFloor(currentReadIndex));
                    }
                }
            }
        }

        /// <summary>
        /// Excludes "end".
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private bool HasPacket(int start, int end)
        {
            lock(_lock)
            {
                for (int i = 0; i < _ranges.Count; i++)
                {
                    if (_ranges[i].Contains(start) && _ranges[i].Contains(end))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
