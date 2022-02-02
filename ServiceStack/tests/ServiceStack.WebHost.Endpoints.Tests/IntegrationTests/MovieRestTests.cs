using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	[TestFixture]
	public class MovieRestTests
		: IntegrationTestBase
	{
		[SetUp]
		public void OnBeforeEachTest()
		{
			var jsonClient = new JsonServiceClient(BaseUrl);
            jsonClient.Post<ResetMoviesResponse>("reset-movies", new ResetMovies());
		}

		[Test]
		public void Can_list_all_movies()
		{
			SendToEachEndpoint<RestMoviesResponse>(new RestMovies(), HttpMethods.Get, response =>
				Assert.That(response.Movies, Has.Count.EqualTo(ConfigureDatabase.Top5Movies.Count))
			);
		}

		[Test]
		public void Can_add_movie()
		{
			var newMovie = new RestMovie
			{
				Id = "tt0110912",
				Title = "Pulp Fiction",
				Rating = 8.9m,
				Director = "Quentin Tarantino",
				ReleaseDate = new DateTime(1994, 10, 24),
				TagLine = "Girls like me don't make invitations like this to just anyone!",
				Genres = new List<string> { "Crime", "Drama", "Thriller" },
			};

			SendToEachEndpoint<RestMoviesResponse>(new RestMovies { Movie = newMovie }, HttpMethods.Put, null);

			SendToEachEndpoint<RestMoviesResponse>(new RestMovies { Id = newMovie.Id }, HttpMethods.Get, response =>
				Assert.That(newMovie.Equals(response.Movies[0]), Is.True)
			);

			//Test if possible to get single movie
			var topMovie = ConfigureDatabase.Top5Movies[0];
			SendToEachEndpoint<RestMoviesResponse>(new RestMovies { Id = topMovie.Id }, HttpMethods.Get, response =>
				Assert.That(topMovie.Equals(response.Movies[0]), Is.True)
			);

			//Test if possible to update movie
			var topMovie2 = ConfigureDatabase.Top5Movies[0];
			var updatedMovie = TypeSerializer.Clone(topMovie2);
			updatedMovie.Title = "Updated Movie";

			SendToEachEndpoint<RestMoviesResponse>(new RestMovies { Movie = updatedMovie }, HttpMethods.Post, null);

			SendToEachEndpoint<RestMoviesResponse>(new RestMovies { Id = topMovie2.Id }, HttpMethods.Get, response =>
				Assert.That(updatedMovie.Equals(response.Movies[0]), Is.True)
			);
		}

		[Test]
		public void Can_ResetMovieDatabase()
		{
			SendToEachEndpoint<ResetMovieDatabaseResponse>(new ResetMovieDatabase(), HttpMethods.Post, response =>
				Assert.That(response.ResponseStatus.ErrorCode, Is.Null)
			);
		}

		[Test]
		public void Can_delete_movie()
		{
			var topMovie = ConfigureDatabase.Top5Movies[0];

			DeleteOnEachEndpoint<RestMoviesResponse>("restmovies/" + topMovie.Id, null);

			SendToEachEndpoint<RestMoviesResponse>(new RestMovies { Id = topMovie.Id }, HttpMethods.Get, response =>
				Assert.That(response.Movies, Has.Count.EqualTo(0))
			);
		}
	}
}