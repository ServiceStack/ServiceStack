using System.Runtime.Serialization;
using RazorRockstars;
using ServiceStack;
using ServiceStack.OrmLite;

namespace Mvc.Core.Tests
{
    [DataContract]
    [Route("/cached/rockstars/gateway")]
    public class CachedRockstarsGateway : IGet, IReturn<RockstarsResponse> { }

    [DataContract]
    [Route("/cached/rockstars")]
    public class CachedRockstars : IGet, IReturn<RockstarsResponse> { }

    [CacheResponse(Duration = 60 * 60, MaxAge = 30 * 60)]
    public class CachedServices : Service
    {
        public object Get(CachedRockstarsGateway request) =>
            Gateway.Send(new SearchRockstars());

        public object Get(CachedRockstars request) =>
            new RockstarsResponse
            {
                Total = Db.Scalar<int>("select count(*) from Rockstar"),
                Results = Db.Select<Rockstar>()
            };
    }
}