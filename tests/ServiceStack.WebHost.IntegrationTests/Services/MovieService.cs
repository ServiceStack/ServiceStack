using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Web;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

[RestService("/movies")]
[RestService("/movies/{Id}")]
[DataContract]
public class Movie
{
	public Movie()
	{
		this.Genres = new List<string>();
	}

	[DataMember]
	[AutoIncrement]
	public int Id { get; set; }

	[DataMember]
	public string ImdbId { get; set; }

	[DataMember]
	public string Title { get; set; }

	[DataMember]
	public decimal Rating { get; set; }

	[DataMember]
	public string Director { get; set; }

	[DataMember]
	public DateTime ReleaseDate { get; set; }

	[DataMember]
	public string TagLine { get; set; }

	[DataMember]
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


public class MovieService : RestServiceBase<Movie>
{
	public IDbConnectionFactory DbFactory { get; set; }

	/// <summary>
	/// GET /movies/{Id} 
	/// </summary>
	public override object Get(Movie movie)
	{
		return new MovieResponse
		{
			Movie = DbFactory.Exec(dbCmd => dbCmd.GetById<Movie>(movie.Id))
		};
	}

	/// <summary>
	/// POST /movies
	/// </summary>
	public override object Post(Movie movie)
	{
		var newMovieId = DbFactory.Exec(dbCmd =>
		{
			dbCmd.Insert(movie);
			return dbCmd.GetLastInsertId();
		});

		var newMovie = new MovieResponse
		{
			Movie = DbFactory.Exec(dbCmd => dbCmd.GetById<Movie>(newMovieId))
		};
		return new HttpResult(newMovie)
		{
			StatusCode = HttpStatusCode.Created,
			Headers = {
					{ HttpHeaders.Location, this.RequestContext.RawUrl + "/" + newMovieId }
				}
		};
	}

	/// <summary>
	/// PUT /movies
	/// </summary>
	public override object Put(Movie movie)
	{
		DbFactory.Exec(dbCmd => dbCmd.Save(movie));
		return new MovieResponse();
	}

	/// <summary>
	/// DELETE /movies/{Id}
	/// </summary>
	public override object Delete(Movie request)
	{
		DbFactory.Exec(dbCmd => dbCmd.DeleteById<Movie>(request.Id));
		return new MovieResponse();
	}
}