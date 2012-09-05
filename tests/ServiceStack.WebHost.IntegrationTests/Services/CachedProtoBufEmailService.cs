using System.Runtime.Serialization;
using ServiceStack.CacheAccess;
using ServiceStack.Common;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
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

    class UncachedProtoBufEmailService : ServiceBase<UncachedProtoBufEmail>
    {
        public IDbConnectionFactory DbFactory { get; set; }

        public ICacheClient CacheClient { get; set; }

        protected override object Run(UncachedProtoBufEmail request)
        {
            return new ProtoBufEmail() { FromAddress = request.FromAddress ?? "none" };
        }
    }

    class CachedProtoBufEmailService : ServiceBase<CachedProtoBufEmail>
    {
        public IDbConnectionFactory DbFactory { get; set; }

        public ICacheClient CacheClient { get; set; }

        protected override object Run(CachedProtoBufEmail request)
        {
            return base.RequestContext.ToOptimizedResultUsingCache(
                    this.CacheClient,
                    UrnId.Create<ProtoBufEmail>(request.FromAddress ?? "none"),
                    () => new ProtoBufEmail { FromAddress = request.FromAddress ?? "none" });
        }
    }
}