using System;

namespace ServiceStack.ServiceHost
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class DefaultRequestAttribute : Attribute
    {
        public Type RequestType { get; set; }

        public DefaultRequestAttribute(Type requestType)
        {
            RequestType = requestType;
        }
    }
}