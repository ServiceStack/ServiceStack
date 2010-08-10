using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Examples.ServiceInterface.Support;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.Examples.Tests
{
	[TestFixture]
	public class MovieRestTests
		: TestHostBase
	{
		[Test]
		public void Can_list_all_movies()
		{
			var response = base.Send<MoviesResponse>(new Movies());
			Assert.That(response.Movies, Has.Count.EqualTo(ConfigureDatabase.Top5Movies.Count));
		}

		[Test]
		public void Can_get_single_movie()
		{
			var topMovie = ConfigureDatabase.Top5Movies[0];
			var response = base.Send<MoviesResponse>(new Movies { Id = topMovie.Id });
			Assert.That(topMovie.Equals(response.Movies[0]), Is.True);
		}

		[Test]
		public void Can_update_movie()
		{
			var topMovie = ConfigureDatabase.Top5Movies[0];
			var updatedMovie = TypeSerializer.Clone(topMovie);
			updatedMovie.Title = "Updated Movie";

			base.Send<MoviesResponse>(new Movies { Movie = updatedMovie },
				EndpointAttributes.HttpPost);

			var response = base.Send<MoviesResponse>(new Movies { Id = topMovie.Id });
			Assert.That(updatedMovie.Equals(response.Movies[0]), Is.True);
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

			base.Send<MoviesResponse>(new Movies { Movie = newMovie },
				EndpointAttributes.HttpPut);

			var response = base.Send<MoviesResponse>(new Movies { Id = newMovie.Id });
			Assert.That(newMovie.Equals(response.Movies[0]), Is.True);
		}

		[Test]
		public void Can_delete_movie()
		{
			var topMovie = ConfigureDatabase.Top5Movies[0];

			base.Send<MoviesResponse>(new Movies { Id = topMovie.Id },
				EndpointAttributes.HttpDelete);

			var response = base.Send<MoviesResponse>(new Movies { Id = topMovie.Id });
			Assert.That(response.Movies, Has.Count.EqualTo(0));
		}
	}

}