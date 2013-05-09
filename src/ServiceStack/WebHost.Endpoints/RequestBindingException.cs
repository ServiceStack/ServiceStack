using System;
using System.Runtime.Serialization;

namespace ServiceStack.WebHost.Endpoints
{
    public class RequestBindingException : SerializationException
    {
        public RequestBindingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
