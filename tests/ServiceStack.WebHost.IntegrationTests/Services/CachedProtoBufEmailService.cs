using System.Runtime.Serialization;
using ServiceStack.Common;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.IntegrationTests.Tests;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [DataContract]
    [Route("/cached/protobuf")]
    [Route("/cached/protobuf/{FromAddress}")]
    public class CachedProtoBufEmail
    {
        [DataMember(Order = 1)]
        public string FromAddress { get; set; }
    }

    [DataContract]
    [Route("/uncached/protobuf")]
    [Route("/uncached/protobuf/{FromAddress}")]
    public class UncachedProtoBufEmail
    {
        [DataMember(Order = 1)]
        public string FromAddress { get; set; }
    }

    class UncachedProtoBufEmailService : ServiceInterface.Service
    {
        public object Any(UncachedProtoBufEmail request)
        {
            return new ProtoBufEmail { FromAddress = request.FromAddress ?? "none" };
        }
    }

    class CachedProtoBufEmailService : ServiceInterface.Service
    {
        public object Any(CachedProtoBufEmail request)
        {
            return base.RequestContext.ToOptimizedResultUsingCache(this.Cache,
                UrnId.Create<ProtoBufEmail>(request.FromAddress ?? "none"),
                () => new ProtoBufEmail { FromAddress = request.FromAddress ?? "none" });
        }
    }
}