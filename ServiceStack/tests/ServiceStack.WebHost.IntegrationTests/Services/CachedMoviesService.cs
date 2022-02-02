using System.Runtime.Serialization;

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

	public class CachedMoviesService : Service
	{
		public object Get(CachedMovies request)
		{
			var service = base.ResolveService<MoviesService>();

			return base.Request.ToOptimizedResultUsingCache(
				this.Cache, UrnId.Create<Movies>(request.Genre ?? "all"), () => {
					return (MoviesResponse)service.Get(new Movies { Genre = request.Genre });
				});
		}
	}
}