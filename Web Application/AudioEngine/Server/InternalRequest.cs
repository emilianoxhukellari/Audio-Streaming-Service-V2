namespace Server_Application.Server
{
    public enum InternalRequestType
    {
        AddToDatabase,
        RemoveClientHandler,
    }
    public sealed class InternalRequest
    {
        public object[] Parameters { get; }
        public InternalRequestType Type { get; }

        public InternalRequest(params object[] parameters)
        {
            Type = (InternalRequestType)parameters[0];
            Parameters = new object[parameters.Length - 1];
            Array.Copy(parameters, 1, Parameters, 0, parameters.Length - 1);
        }
    }
}
