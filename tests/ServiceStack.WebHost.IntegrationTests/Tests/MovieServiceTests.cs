using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.Common.Web;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class MovieServiceTests
		: RestsTestBase
	{
		public Movie NewMovie = new Movie
		{
			ImdbId = "tt0111161",
			Title = "The Shawshank Redemption",
			Rating = 9.2m,
			Director = "Frank Darabont",
			ReleaseDate = new DateTime(1995, 2, 17),
			TagLine = "Fear can hold you prisoner. Hope can set you free.",
			Genres = new List<string> { "Crime", "Drama" },
		};

		IDbConnectionFactory DbFactory { get; set; }

		[SetUp]
		public override void OnBeforeEachTest()
		{
			base.OnBeforeEachTest();

			this.Container.Register<IDbConnectionFactory>(c =>
				new OrmLiteConnectionFactory(
					":memory:", false,
					SqliteOrmLiteDialectProvider.Instance));

			this.DbFactory = this.Container.Resolve<IDbConnectionFactory>();
			this.DbFactory.Exec(dbCmd => dbCmd.CreateTable<Movie>(true));
		}

		[Test]
		public void Can_create_new_Movie_from_dto()
		{
			var response = ExecutePath(HttpMethods.Put, "/movies", null, null, NewMovie);

			this.DbFactory.Exec(dbCmd =>
			{
				var lastInsertId = dbCmd.GetLastInsertId();
				var createdMovie = dbCmd.GetById<Movie>(lastInsertId);
				Assert.That(createdMovie, Is.Not.Null);
				Assert.That(createdMovie, Is.EqualTo(NewMovie));
			});
		}

		[Test]
		public void Can_create_new_Movie_from_FormData()
		{
			var formData = NewMovie.ToStringDictionary();

			var response = ExecutePath(HttpMethods.Put, "/movies", null, formData, null);

			this.DbFactory.Exec(dbCmd =>
			{
				var lastInsertId = dbCmd.GetLastInsertId();
				var createdMovie = dbCmd.GetById<Movie>(lastInsertId);
				Assert.That(createdMovie, Is.Not.Null);
				Assert.That(createdMovie, Is.EqualTo(NewMovie));
			});
		}

		[Test]
		public void Can_POST_to_resetmovies()
		{
			var response = ExecutePath(HttpMethods.Post, "/reset-movies");

			this.DbFactory.Exec(dbCmd =>
			{
				var movies = dbCmd.Select<Movie>();
				Assert.That(movies.Count, Is.EqualTo(ResetMoviesService.Top5Movies.Count));
			});
		}

		[Test]
		public void Error_calling_GET_on_resetmovies()
		{
			var response = (ResetMoviesResponse)ExecutePath(HttpMethods.Get, "/reset-movies");
			Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo(typeof(NotImplementedException).Name));
		}

	}

}