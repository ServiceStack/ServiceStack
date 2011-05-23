using System.Runtime.Serialization;
using System.Reflection;
using System;

namespace ServiceStack.ServiceHost.Tests.Support
{
    [DataContract]
    public class Generic1 { }

    [DataContract]
    public class Generic1Response { }

    [DataContract]
    public class Generic2 { }

    [DataContract]
    public class Generic2Response { }

	public class GenericService<T> : IService<T>
	{
        public object Execute(T request)
        {
            // Find the rewsponse type within the same namespace and assembly as the request
            var responseType = typeof(T).Assembly.GetType(typeof(T).FullName + "Response");

            return Activator.CreateInstance(responseType);
        }
    }
}
