namespace Server_Application.Server
{
    public enum EventType
    {
        InternalRequest,
        DisplayConnectedClients
    }
    public delegate void ServerEventCallback(params object[] args);

    /// <summary>
    /// This class represents a listener. Call Listen() to subscribe for an event.
    /// </summary>
    public sealed class ServerListener
    {
        public static List<ServerListener> Listeners = new List<ServerListener>();
        public Dictionary<EventType, ServerEventCallback> ListeningFor;

        public ServerListener()
        {
            Listeners.Add(this);
            ListeningFor = new Dictionary<EventType, ServerEventCallback>();
        }
        public void Listen(EventType eventType, ServerEventCallback callback)
        {
            ListeningFor.Add(eventType, callback);
        }
    }

    /// <summary>
    /// When you create an instance of this class, and set selfFire to true, it will immediately call the callback from the class that 
    /// is listening for this specific event.
    /// </summary>
    public sealed class ServerEvent
    {
        private readonly EventType _eventType;
        private readonly object[] _parameters;

        public ServerEvent(EventType eventType, bool selfFire, params object[] parameters)
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
            foreach (var listener in ServerListener.Listeners)
            {
                if (listener.ListeningFor.ContainsKey(_eventType)) // Check if there are any subscribers
                {
                    listener.ListeningFor[_eventType](_parameters); // Call method
                }
            }
        }
    }
}
