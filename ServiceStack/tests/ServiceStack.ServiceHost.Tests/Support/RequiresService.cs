using System;
using System.Runtime.Serialization;
using ServiceStack.Web;

namespace ServiceStack.ServiceHost.Tests.Support
{
    [DataContract]
    public class RequiresContext { }

    [DataContract]
    public class RequiresContextResponse { }

    public class RequiresService
        : IService, IRequiresRequest
    {
        public IRequest Request { get; set; }

        public object Any(RequiresContext requires)
        {
            if (Request == null)
                throw new ArgumentNullException("RequestContext");

            return new RequiresContextResponse();
        }
    }
}