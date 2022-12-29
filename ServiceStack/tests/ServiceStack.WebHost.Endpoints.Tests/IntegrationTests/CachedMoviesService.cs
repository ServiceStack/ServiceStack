using System;
using System.Runtime.Serialization;
using ServiceStack.Data;
using ServiceStack.Redis;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	[DataContract]
	[Route("/cached/movies", "GET")]
    [Route("/cached/movies/genres/{Genre}")]
    public class CachedMovies : IReturn<MoviesResponse>
    {
        [DataMember]
        public string Genre { get; set; }
    }

    [Route("/cached-timeout/movies", "GET")]
    public class CachedMoviesWithTimeout : IReturn<MoviesResponse>
    {
        [DataMember]
        public string Genre { get; set; }
    }

    [Route("/cached-timeout-redis/movies", "GET")]
    public class CachedMoviesWithTimeoutAndRedis : IReturn<MoviesResponse>
    {
        [DataMember]
        public string Genre { get; set; }
    }

    [Route("/cached-string/{Id}")]
    public class CachedString : IReturn<string>
    {
        public string Id { get; set; }
    }

    public class CachedMoviesService : Service
	{
        public object Get(CachedMovies request)
        {
            using (var service = base.ResolveService<MoviesService>())
            {
                return base.Request.ToOptimizedResultUsingCache(
                    this.Cache, UrnId.Create<Movies>(request.Genre ?? "all"), () =>
                    {
                        return (MoviesResponse)service.Get(new Movies { Genre = request.Genre });
                    });
            }
        }

        public object Get(CachedMoviesWithTimeout request)
        {
            using (var service = base.ResolveService<MoviesService>())
            {
                return base.Request.ToOptimizedResultUsingCache(
                    this.Cache, UrnId.Create<Movies>(request.Genre ?? "all"), TimeSpan.FromMinutes(1), () =>
                    {
                        return (MoviesResponse)service.Get(new Movies { Genre = request.Genre });
                    });
            }
        }

        public object Get(CachedMoviesWithTimeoutAndRedis request)
        {
            using (var service = base.ResolveService<MoviesService>())
            {
                return base.Request.ToOptimizedResultUsingCache(
                    new RedisClient(), UrnId.Create<Movies>(request.Genre ?? "all"), TimeSpan.FromMinutes(1), () =>
                    {
                        return (MoviesResponse)service.Get(new Movies { Genre = request.Genre });
                    });
            }
        }

        public object Get(CachedString request)
        {
            return base.Request.ToOptimizedResultUsingCache(
                new RedisClient(), UrnId.Create<CachedString>(request.Id ?? "all"), TimeSpan.FromMinutes(1), () =>
                {
                    return request.Id;
                });
        }
    }
}