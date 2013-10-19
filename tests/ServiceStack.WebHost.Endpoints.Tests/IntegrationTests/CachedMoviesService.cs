using System.Runtime.Serialization;
using ServiceStack.Data;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	[DataContract]
	[Route("/cached/movies", "GET")]
    [Route("/cached/movies/genres/{Genre}")]
	public class CachedMovies
	{
		[DataMember]
		public string Genre { get; set; }
	}

	public class CachedMoviesService : Service
	{
		public IDbConnectionFactory DbFactory { get; set; }

		public object Get(CachedMovies request)
		{
			var service = base.ResolveService<MoviesService>();

			return base.Request.ToOptimizedResultUsingCache(
				this.GetCacheClient(), UrnId.Create<Movies>(request.Genre ?? "all"), () =>
				{
					return (MoviesResponse)service.Get(new Movies { Genre = request.Genre });
				});
		}
	}
}