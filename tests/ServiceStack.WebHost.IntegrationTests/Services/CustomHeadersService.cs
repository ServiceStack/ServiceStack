using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Route("/customHeaders")]
    [DataContract]
    public class CustomHeaders : IReturn<CustomHeadersResponse>
    { }

    [DataContract]
    public class CustomHeadersResponse
    {
        [DataMember(Order = 1)]
        public string Foo { get; set; }
        [DataMember(Order = 2)]
        public string Bar { get; set; }
    }

    public class CustomHeadersService : ServiceInterface.Service
    {
        public CustomHeadersResponse Any(CustomHeaders c)
        {
            var response = new CustomHeadersResponse
                {
                    Foo = Request.Headers["Foo"], 
                    Bar = Request.Headers["Bar"]
                };
            return response;
        }

    }
}