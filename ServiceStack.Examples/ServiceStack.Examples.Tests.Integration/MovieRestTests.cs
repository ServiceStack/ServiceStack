using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.Examples.ServiceInterface.Support;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.Text;

namespace ServiceStack.Examples.Tests.Integration
{
	[TestFixture]
	public class MovieRestTests
		: IntegrationTestBase
	{
		[Test]
		public void Can_list_all_movies()
		{
			SendToEachEndpoint<MoviesResponse>(new Movies(), HttpMethods.Get, response =>
				Assert.That(response.Movies, Has.Count.EqualTo(ConfigureDatabase.Top5Movies.Count))
			);
		}

		[Test]
		public void Can_get_single_movie()
		{
			var topMovie = ConfigureDatabase.Top5Movies[0];
			SendToEachEndpoint<MoviesResponse>(new Movies { Id = topMovie.Id }, HttpMethods.Get, response =>
				Assert.That(topMovie.Equals(response.Movies[0]), Is.True)
			);
		}

		[Test]
		public void Can_update_movie()
		{
			var topMovie = ConfigureDatabase.Top5Movies[0];
			var updatedMovie = TypeSerializer.Clone(topMovie);
			updatedMovie.Title = "Updated Movie";

			SendToEachEndpoint<MoviesResponse>(new Movies { Movie = updatedMovie }, HttpMethods.Post, null);

			SendToEachEndpoint<MoviesResponse>(new Movies { Id = topMovie.Id }, HttpMethods.Get, response =>
				Assert.That(updatedMovie.Equals(response.Movies[0]), Is.True)
			);
		}

		[Test]
		public void Can_add_movie()
		{
			var newMovie = new Movie
               	{
               		Id = "tt0110912",
               		Title = "Pulp Fiction",
               		Rating = 8.9m,
               		Director = "Quentin Tarantino",
               		ReleaseDate = new DateTime(1994, 10, 24),
               		TagLine = "Girls like me don't make invitations like this to just anyone!",
               		Genres = new List<string> { "Crime", "Drama", "Thriller" },
               	};

			SendToEachEndpoint<MoviesResponse>(new Movies { Movie = newMovie }, HttpMethods.Put, null);

			SendToEachEndpoint<MoviesResponse>(new Movies { Id = newMovie.Id }, HttpMethods.Get, response =>
				Assert.That(newMovie.Equals(response.Movies[0]), Is.True)
			);
		}

		[Test]
		public void Can_delete_movie()
		{
			var topMovie = ConfigureDatabase.Top5Movies[0];

			SendToEachEndpoint<MoviesResponse>(new Movies { Id = topMovie.Id }, HttpMethods.Delete, null);

			SendToEachEndpoint<MoviesResponse>(new Movies { Id = topMovie.Id }, HttpMethods.Get, response =>
				Assert.That(response.Movies, Has.Count.EqualTo(0))
			);
		}
	}
}