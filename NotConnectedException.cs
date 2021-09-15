using System;

namespace K8090.ManagedClient
{
    public class NotConnectedException : ManagedClientException
    {
        public NotConnectedException(string message) : base(message)
        {
        }

        public NotConnectedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
