using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.Text.Tests
{
	[TestFixture]
	public class QueryStringWriterTests
	{
		[Test]
		public void Can_Write_QueryString()
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

			var queryString = QueryStringSerializer.SerializeToString(newMovie);

			Console.WriteLine(queryString);
		}
	}
}