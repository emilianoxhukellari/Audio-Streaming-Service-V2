using System;
using System.Collections.Generic;
using System.Linq;

namespace Client_Application.Client.Core
{
    /// <summary>
    /// This class represents a queue with internal memory. You can shuffle and unshuffle it.
    /// </summary>
    public sealed class SongQueue
    {
        private List<Song> _queue; // Stores Songs
        private List<Song> _orderedQueue; // Stores ordered song_ids
        public int Count { get => _queue.Count; }
        public SongQueue()
        {
            _queue = new List<Song>();
            _orderedQueue = new List<Song>();
        }

        public bool Any()
        {
            return _queue.Any();
        }

        public Song this[int key]
        {
            get => _queue[key];
            set => _queue[key] = value;
        }

        /// <summary>
        /// Tries to get a song at the specified index.
        /// </summary>
        /// <param name="song"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool TryGet(out Song song, int index)
        {
            Song current;
            bool success = false;
            if (_queue.Count > 0)
            {
                try
                {
                    current = _queue[index];
                    song = current;
                    success = true;
                }
                catch (Exception)
                {
                    song = new Song();
                    success = false;
                }
            }
            else
            {
                song = new Song();
            }
            return success;
        }

        public bool Contains(Song song)
        {
            return _queue.Contains(song);
        }

        public void ClearQueue()
        {
            _queue.Clear();
            _orderedQueue.Clear();
        }

        public void InsertToQueue(int index, Song song)
        {
            _queue.Insert(index, song);
            _orderedQueue.Insert(index, song);
        }

        public void RemoveFromQueue(int index)
        {
            _orderedQueue.Remove(_queue[index]);
            _queue.RemoveAt(index);
        }

        public void AppendToQueue(Song song)
        {
            _queue.Add(song);
            _orderedQueue.Add(song);
        }

        /// <summary>
        /// Call this method to shuffle the queue. The current song element is not changed.
        /// </summary>
        /// <param name="currentSongIndex"></param>
        public void Shuffle(int currentSongIndex)
        {

            if (_queue.Any())
            {
                for (int i = 0; i < Count; i++)
                {
                    Random random = new Random();
                    if (i != currentSongIndex)
                    {
                        int r = random.Next(0, Count);
                        if (r == currentSongIndex && currentSongIndex < Count - 2)
                        {
                            r++;
                        }
                        else if (r == currentSongIndex && currentSongIndex > 0)
                        {
                            r--;
                        }
                        (_queue[i], _queue[r]) = (_queue[r], _queue[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Call this method to unshuffle the queue. Information from ordered queue is used to do this operation.
        /// Current song and the respective ordered index of the current songs are exceptions.
        /// Thus, two songs might not be changed.
        /// </summary>
        /// <param name="currentSongIndex"></param>
        public void UnShuffle(int currentSongIndex)
        {
            if (_queue.Any())
            {
                Song[] _orderedQueueCopy = _orderedQueue.ToArray();
                for (int i = 0; i < Count; i++)
                {
                    if (i != currentSongIndex)
                    {
                        _queue[i] = _orderedQueueCopy[i];
                    }
                }
            }
        }
    }
}
