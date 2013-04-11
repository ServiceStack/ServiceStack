using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.ServiceHost.Tests.Support
{
    public class GetRequest
    {
    }

    public class GetRequestResponse
    {
    }

    public class GetMarkerService : ServiceInterface.Service, IGet<GetRequest>
    {
        public object Get(GetRequest request)
        {
            return new GetRequestResponse();
        }
    }
}
