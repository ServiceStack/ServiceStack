using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Common.Extensions;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace ServiceStack.WebHost.IntegrationTests.Services
{

	[DataContract]
	[Route("/movies", "GET, OPTIONS")]
	[Route("/movies/genres/{Genre}")]
	public class Movies
	{
		[DataMember]
		public string Genre { get; set; }

		[DataMember]
		public Movie Movie { get; set; }
	}

	[DataContract]
	public class MoviesResponse
	{
		[DataMember(Order = 1)]
		public List<Movie> Movies { get; set; }
	}

	public class MoviesService : RestServiceBase<Movies>
	{
		public IDbConnectionFactory DbFactory { get; set; }

		/// <summary>
		/// GET /movies 
		/// GET /movies/genres/{Genre}
		/// </summary>
		public override object OnGet(Movies request)
		{
			return new MoviesResponse
			{
				Movies = request.Genre.IsNullOrEmpty()
					? DbFactory.Run(db => db.Select<Movie>())
					: DbFactory.Run(db => db.Select<Movie>("Genres LIKE {0}", "%" + request.Genre + "%"))
			};
		}
	}

}