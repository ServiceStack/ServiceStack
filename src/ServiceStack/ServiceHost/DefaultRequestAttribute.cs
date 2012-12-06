using System;

namespace ServiceStack.ServiceHost
{

    /// <summary>
    /// Lets you Register new Services and the optional restPaths will be registered against 
    /// this default Request Type
    /// </summary>
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