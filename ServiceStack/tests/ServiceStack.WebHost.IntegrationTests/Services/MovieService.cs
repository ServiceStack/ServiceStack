using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Route("/movies/query", "GET")]
    public class QueryMovies : QueryDb<Movie>
    {
        public string TitleContains { get; set; }
    }

	[Route("/movies", "POST,PUT,PATCH")]
	[Route("/movies/{Id}")]
	[DataContract]
	public class Movie
	{
		public Movie()
		{
			this.Genres = new List<string>();
		}

        [DataMember(Order = 1)]
		[AutoIncrement]
		public int Id { get; set; }

        [DataMember(Order = 2)]
		public string ImdbId { get; set; }

        [DataMember(Order = 3)]
		public string Title { get; set; }

        [DataMember(Order = 4)]
		public decimal Rating { get; set; }

        [DataMember(Order = 5)]
		public string Director { get; set; }

        [DataMember(Order = 6)]
		public DateTime ReleaseDate { get; set; }

        [DataMember(Order = 7)]
		public string TagLine { get; set; }

        [DataMember(Order = 8)]
		public List<string> Genres { get; set; }

		#region AutoGen ReSharper code, only required by tests
		public bool Equals(Movie other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(other.ImdbId, ImdbId)
				&& Equals(other.Title, Title)
				&& other.Rating == Rating
				&& Equals(other.Director, Director)
				&& other.ReleaseDate.Equals(ReleaseDate)
				&& Equals(other.TagLine, TagLine)
				&& Genres.EquivalentTo(other.Genres);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof(Movie)) return false;
			return Equals((Movie)obj);
		}

		public override int GetHashCode()
		{
			return ImdbId != null ? ImdbId.GetHashCode() : 0;
		}
		#endregion
	}

	[DataContract]
	public class MovieResponse
	{
		[DataMember]
		public Movie Movie { get; set; }
	}

	public class MovieService : Service
	{
		/// <summary>
		/// GET /movies/{Id} 
		/// </summary>
		public object Get(Movie movie)
		{
			return new MovieResponse {
				Movie = Db.SingleById<Movie>(movie.Id)
			};
		}

		/// <summary>
		/// POST /movies
		/// </summary>
		public object Post(Movie movie)
		{
            Db.Save(movie);

			var newMovie = new MovieResponse {
				Movie = Db.SingleById<Movie>(movie.Id)
			};
			return new HttpResult(newMovie) {
				StatusCode = HttpStatusCode.Created,
				Headers = {
					{ HttpHeaders.Location, this.Request.AbsoluteUri.WithTrailingSlash() + movie.Id }
				}
			};
		}

		/// <summary>
		/// PUT /movies
		/// </summary>
		public object Put(Movie movie)
		{
		    Db.Update(movie);
			return new MovieResponse();
		}

		/// <summary>
		/// DELETE /movies/{Id}
		/// </summary>
		public object Delete(Movie request)
		{
			Db.DeleteById<Movie>(request.Id);
			return new MovieResponse();
		}

		/// <summary>
		/// PATCH /movies
		/// </summary>
		public object Patch(Movie movie)
		{
            var existingMovie = Db.SingleById<Movie>(movie.Id);
            if (movie.Title != null)
                existingMovie.Title = movie.Title;
            Db.Save(existingMovie);
            
            return new MovieResponse();
		}
	}
}