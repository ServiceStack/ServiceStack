using System.Runtime.Serialization;
using ServiceStack.CacheAccess;
using ServiceStack.Common;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace ServiceStack.WebHost.IntegrationTests.Services
{

	[DataContract]
	[Route("/cached/movies", "GET")]
	[Route("/cached/movies/genres/{Genre}")]
	public class CachedMovies
	{
		[DataMember]
		public string Genre { get; set; }
	}

	public class CachedMoviesService : RestServiceBase<CachedMovies>
	{
		public IDbConnectionFactory DbFactory { get; set; }

		public ICacheClient CacheClient { get; set; }

		public override object OnGet(CachedMovies request)
		{
			var service = base.ResolveService<MoviesService>();

			return base.RequestContext.ToOptimizedResultUsingCache(
				this.CacheClient, UrnId.Create<Movies>(request.Genre ?? "all"), () =>
				{
					return (MoviesResponse)service.Get(new Movies { Genre = request.Genre });
				});
		}
	}

}