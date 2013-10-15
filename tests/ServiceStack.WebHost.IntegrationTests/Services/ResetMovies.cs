using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[DataContract]
	[Description("Resets the database back to the original Top 5 movies.")]
	[Route("/reset-movies")]
    public class ResetMovies : IReturn<ResetMoviesResponse> { }

	[DataContract]
	public class ResetMoviesResponse : IHasResponseStatus
	{
		public ResetMoviesResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class ResetMoviesService : Service
	{
		public static List<Movie> Top5Movies = new List<Movie>
		{
			new Movie { ImdbId = "tt0111161", Title = "The Shawshank Redemption", Rating = 9.2m, Director = "Frank Darabont", ReleaseDate = new DateTime(1995,2,17), TagLine = "Fear can hold you prisoner. Hope can set you free.", Genres = new List<string>{"Crime","Drama"}, },
			new Movie { ImdbId = "tt0068646", Title = "The Godfather", Rating = 9.2m, Director = "Francis Ford Coppola", ReleaseDate = new DateTime(1972,3,24), TagLine = "An offer you can't refuse.", Genres = new List<string> {"Crime","Drama", "Thriller"}, },
			new Movie { ImdbId = "tt1375666", Title = "Inception", Rating = 9.2m, Director = "Christopher Nolan", ReleaseDate = new DateTime(2010,7,16), TagLine = "Your mind is the scene of the crime", Genres = new List<string>{"Action", "Mystery", "Sci-Fi", "Thriller"}, },
			new Movie { ImdbId = "tt0071562", Title = "The Godfather: Part II", Rating = 9.0m, Director = "Francis Ford Coppola", ReleaseDate = new DateTime(1974,12,20), Genres = new List<string> {"Crime","Drama", "Thriller"}, },
			new Movie { ImdbId = "tt0060196", Title = "The Good, the Bad and the Ugly", Rating = 9.0m, Director = "Sergio Leone", ReleaseDate = new DateTime(1967,12,29), TagLine = "They formed an alliance of hate to steal a fortune in dead man's gold", Genres = new List<string>{"Adventure","Western"}, },
		};

		public IDbConnectionFactory DbFactory { get; set; }

		public object Post(ResetMovies request)
		{
            const bool overwriteTable = true;
            Db.CreateTable<Movie>(overwriteTable);
            Db.SaveAll(Top5Movies);

			return new ResetMoviesResponse();
		}
	}

    [Route("/factorial/{ForNumber}")]
    [DataContract]
    public class GetFactorial
    {
        [DataMember]
        public long ForNumber { get; set; }
    }

    [DataContract]
    public class GetFactorialResponse
    {
        [DataMember]
        public long Result { get; set; }
    }

    public class GetFactorialService : IService
    {
        public object Any(GetFactorial request)
        {
            return new GetFactorialResponse { Result = GetFactorial(request.ForNumber) };
        }

        public static long GetFactorial(long n)
        {
            return n > 1 ? n * GetFactorial(n - 1) : 1;
        }
    }
}