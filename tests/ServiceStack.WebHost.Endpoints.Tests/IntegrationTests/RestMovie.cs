using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Common.Extensions;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

/*
 * Examples of preliminery REST method support in ServiceStack
 */
namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	[Route("/restmovies/{Id}")]
	public class RestMovies
	{
		[DataMember(EmitDefaultValue = false)]
		public string Id { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public RestMovie Movie { get; set; }
	}

	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class RestMoviesResponse
	{
		public RestMoviesResponse()
		{
			this.ResponseStatus = new ResponseStatus();
			this.Movies = new List<RestMovie>();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public List<RestMovie> Movies { get; set; }
	}

	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class RestMovie 
	{
		public RestMovie()
		{
			this.Genres = new List<string>();
		}

		[DataMember]
		public string Id { get; set; }

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

		public bool Equals(RestMovie other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(other.Id, Id) && Equals(other.Title, Title) && other.Rating == Rating && Equals(other.Director, Director) && other.ReleaseDate.Equals(ReleaseDate) && Equals(other.TagLine, TagLine) && Genres.EquivalentTo(other.Genres);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (RestMovie)) return false;
			return Equals((RestMovie) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = (Id != null ? Id.GetHashCode() : 0);
				result = (result*397) ^ (Title != null ? Title.GetHashCode() : 0);
				result = (result*397) ^ Rating.GetHashCode();
				result = (result*397) ^ (Director != null ? Director.GetHashCode() : 0);
				result = (result*397) ^ ReleaseDate.GetHashCode();
				result = (result*397) ^ (TagLine != null ? TagLine.GetHashCode() : 0);
				result = (result*397) ^ (Genres != null ? Genres.GetHashCode() : 0);
				return result;
			}
		}
	}
}