using System;
using System.Runtime.Serialization;

namespace ServiceStack.Host
{
    public class RequestBindingException : SerializationException
    {
        public RequestBindingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
