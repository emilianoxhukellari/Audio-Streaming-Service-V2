using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client_Application.Client
{
    /// <summary>
    /// This class represents a listener. Call Listen() to subscribe for an event.
    /// </summary>
    public sealed class ClientListener
    {
        public static List<ClientListener> Listeners = new List<ClientListener>();
        public Dictionary<EventType, ClientEventCallback> ListeningFor;

        public ClientListener()
        {
            Listeners.Add(this);
            ListeningFor = new Dictionary<EventType, ClientEventCallback>();
        }
        public void Listen(EventType eventType, ClientEventCallback callback)
        {
            ListeningFor.Add(eventType, callback);
        }
    }

    /// <summary>
    /// When you create an instance of this class, and set selfFire to true, it will immediately call the callback from the class that 
    /// is listening for this specific event.
    /// </summary>
    public static class ClientEvent
    {
        public static void Fire(EventType eventType, params object[] parameters)
        {
            foreach (var listener in ClientListener.Listeners)
            {
                if (listener.ListeningFor.ContainsKey(eventType)) // Check if there are any subscribers
                {
                    listener.ListeningFor[eventType](parameters); // Call method
                }
            }
        }
    }
}
