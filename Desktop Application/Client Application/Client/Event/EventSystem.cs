using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client_Application.Client.Event
{
    /// <summary>
    /// This class represents a listener. Call Listen() to subscribe for an event.
    /// </summary>
    public sealed class ClientListener
    {
        public static List<ClientListener> Listeners = new List<ClientListener>();
        public Dictionary<EventType, Delegate> ListeningFor;

        public ClientListener()
        {
            Listeners.Add(this);
            ListeningFor = new Dictionary<EventType, Delegate>();
        }
        public void Listen<T>(EventType eventType, ClientEventCallback<T> callback) where T : EventArgs
        {
            ListeningFor.Add(eventType, callback);
        }
    }

    /// <summary>
    /// Call Fire() to execute on the current thread a method that is listening fo
    /// </summary>
    public static class ClientEvent
    {
        /// <summary>
        /// Executes on the current thread.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="parameters"></param>
        public static void Fire<T>(EventType eventType, T args) where T : EventArgs
        {
            foreach (var listener in ClientListener.Listeners)
            {
                if (listener.ListeningFor.ContainsKey(eventType)) // Check if there are any subscribers
                {
                    Delegate callback = listener.ListeningFor[eventType];

                    if (callback is ClientEventCallback<T>)
                    {
                        ((ClientEventCallback<T>)callback)(args);
                    }
                    else
                    {
                        throw new InvalidCastException("Invalid Event Args for Event Delegate");
                    }
                }
            }
        }

        /// <summary>
        /// Executes on the current thread async.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static async Task FireAsync<T>(EventType eventType, T args) where T : EventArgs
        {
            await Task.Run(() =>
            {
                foreach (var listener in ClientListener.Listeners)
                {
                    if (listener.ListeningFor.ContainsKey(eventType)) // Check if there are any subscribers
                    {
                        Delegate callback = listener.ListeningFor[eventType];

                        if (callback is ClientEventCallback<T>)
                        {
                            ((ClientEventCallback<T>)callback)(args);
                        }
                        else
                        {
                            throw new InvalidCastException("Invalid Event Args for Event Delegate");
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Adds the event to ThreadPool.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="parameters"></param>
        public static void FireForget<T>(EventType eventType, T args) where T : EventArgs
        {
            Task.Run(() =>
            {
                foreach (var listener in ClientListener.Listeners)
                {
                    if (listener.ListeningFor.ContainsKey(eventType)) // Check if there are any subscribers
                    {
                        Delegate callback = listener.ListeningFor[eventType];

                        if (callback is ClientEventCallback<T>)
                        {
                            ((ClientEventCallback<T>)callback)(args);
                        }
                        else
                        {
                            throw new InvalidCastException("Invalid Event Args for Event Delegate");
                        }
                    }
                }
            });
        }
    }
}
