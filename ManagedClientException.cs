using System;

namespace K8090.ManagedClient
{
    public class ManagedClientException : Exception
    {
        public ManagedClientException(string message) : base(message)
        {
        }

        public ManagedClientException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
