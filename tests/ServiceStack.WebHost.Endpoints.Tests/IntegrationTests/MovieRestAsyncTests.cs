using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	[TestFixture]
	public class MovieRestAsyncTests
		: IntegrationTestBase
	{
		[SetUp]
		public void OnBeforeEachTest()
		{
			var jsonClient = new JsonServiceClient(BaseUrl);
			jsonClient.Post<ResetMoviesResponse>("reset-movies", new ResetMovies());
		}

		[Test]
		public void Performance()
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			for (int i = 0; i < 100; i++)
				SendToEachEndpoint<RestAsyncMoviesResponse>(new RestAsyncMovies(), HttpMethods.Get, null);
			sw.Stop();
			Console.WriteLine("Async time: {0} ms", sw.ElapsedMilliseconds);

			sw.Start();
			for (int i = 0; i < 100; i++)
				SendToEachEndpoint<RestMoviesResponse>(new RestMovies(), HttpMethods.Get, null);
			sw.Stop();
			Console.WriteLine("Sync time: {0} ms", sw.ElapsedMilliseconds);
		}

		[Test]
		public void Can_list_all_movies()
		{
			SendToEachEndpoint<RestAsyncMoviesResponse>(new RestAsyncMovies(), HttpMethods.Get, response =>
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

			SendToEachEndpoint<RestAsyncMoviesResponse>(new RestAsyncMovies { Movie = newMovie }, HttpMethods.Put, null);

			SendToEachEndpoint<RestAsyncMoviesResponse>(new RestAsyncMovies { Id = newMovie.Id }, HttpMethods.Get, response =>
				Assert.That(newMovie.Equals(response.Movies[0]), Is.True)
			);

			//Test if possible to get single movie
			var topMovie = ConfigureDatabase.Top5Movies[0];
			SendToEachEndpoint<RestAsyncMoviesResponse>(new RestAsyncMovies { Id = topMovie.Id }, HttpMethods.Get, response =>
				Assert.That(topMovie.Equals(response.Movies[0]), Is.True)
			);

			//Test if possible to update movie
			var topMovie2 = ConfigureDatabase.Top5Movies[0];
			var updatedMovie = TypeSerializer.Clone(topMovie2);
			updatedMovie.Title = "Updated Movie";

			SendToEachEndpoint<RestAsyncMoviesResponse>(new RestAsyncMovies { Movie = updatedMovie }, HttpMethods.Post, null);

			SendToEachEndpoint<RestAsyncMoviesResponse>(new RestAsyncMovies { Id = topMovie2.Id }, HttpMethods.Get, response =>
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

			DeleteOnEachEndpoint<RestAsyncMoviesResponse>("restasyncmovies/" + topMovie.Id, null);

			SendToEachEndpoint<RestAsyncMoviesResponse>(new RestAsyncMovies { Id = topMovie.Id }, HttpMethods.Get, response =>
				Assert.That(response.Movies, Has.Count.EqualTo(0))
			);
		}
	}
}