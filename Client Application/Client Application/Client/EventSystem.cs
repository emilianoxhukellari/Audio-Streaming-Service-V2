using System.Collections.Generic;

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
    public sealed class ClientEvent
    {
        private readonly EventType _eventType;
        private readonly object[] _parameters;

        public ClientEvent(EventType eventType, bool selfFire, params object[] parameters)
        {
            _eventType = eventType;
            _parameters = parameters;
            if (selfFire)
            {
                Fire();
            }
        }
        public void Fire()
        {
            foreach (var listener in ClientListener.Listeners)
            {
                if (listener.ListeningFor.ContainsKey(_eventType)) // Check if there are any subscribers
                {
                    listener.ListeningFor[_eventType](_parameters); // Call method
                }
            }
        }
    }
}
