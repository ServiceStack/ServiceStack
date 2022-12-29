using System.Collections.Generic;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    [Route("/echorequestinfo")]
    public class EchoRequestInfo : IReturn<EchoRequestInfoResponse> { }

    public class EchoRequestInfoResponse
    {
        public Dictionary<string, string> Headers { get; set; }
    }

    public class EchoService : Service
    {
        public object Any(EchoRequestInfo request)
        {
            return new EchoRequestInfoResponse
            {
                Headers = base.Request.Headers.ToDictionary(),
            };
        }
    }
}