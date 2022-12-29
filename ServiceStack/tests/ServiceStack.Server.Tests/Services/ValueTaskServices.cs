using System.Threading.Tasks;

namespace ServiceStack.Server.Tests.Services
{
    [Route("/async/redis")]
    [Route("/async/redis/{Incr}")]
    public class AsyncRedis : IReturn<IdResponse>
    {
        public uint Incr { get; set; }
    }

    public class SGAsyncRedis1 : IReturn<IdResponse>
    {
        public uint Incr { get; set; }
    }

    public class SGAsyncRedis2 : IReturn<IdResponse>
    {
        public uint Incr { get; set; }
    }

    public class SGAsyncRedisSync : IReturn<IdResponse>
    {
        public uint Incr { get; set; }
    }

    public class ValueTaskServices : Service
    {
        public async ValueTask<object> Any(AsyncRedis request)
        {
            await using var redis = await GetRedisAsync();
            await redis.IncrementAsync(nameof(AsyncRedis), request.Incr);
            
            var response = new IdResponse {
                Id = (await redis.GetAsync<int>(nameof(AsyncRedis))).ToString()
            };
            return response;
        }

        public async ValueTask<object> Any(SGAsyncRedis1 request)
        {
            return await Gateway.SendAsync(new AsyncRedis { Incr = request.Incr });
        }

        public ValueTask<object> Any(SGAsyncRedis2 request)
        {
            return new ValueTask<object>(Gateway.SendAsync(new AsyncRedis { Incr = request.Incr }));
        }

        public object Any(SGAsyncRedisSync request)
        {
            return Gateway.Send(new AsyncRedis { Incr = request.Incr });
        }

    }
}