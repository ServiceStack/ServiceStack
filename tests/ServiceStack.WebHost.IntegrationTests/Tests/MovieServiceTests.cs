using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class MovieServiceTests
		: RestsTestBase
	{
		public Movie NewMovie = new Movie {
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
			this.DbFactory.Run(db => db.CreateTable<Movie>(true));
		}

		[Test]
		public void Can_PATCH_Movie_from_dto()
		{
			ExecutePath(HttpMethods.Post, "/movies", null, null, NewMovie);

			var lastInsertId = (int)this.DbFactory.Run(db => db.GetLastInsertId());

			var patchMovie = new Movie { Id = lastInsertId, Title = "PATCHED " + NewMovie.Title };
			ExecutePath(HttpMethods.Patch, "/movies", null, null, patchMovie);

			this.DbFactory.Run(db => {
				var movie = db.GetById<Movie>(lastInsertId);
				Assert.That(movie, Is.Not.Null);
				Assert.That(movie.Title, Is.EqualTo(patchMovie.Title));
			});
		}

		[Test]
		public void Can_create_new_Movie_from_FormData()
		{
			var formData = NewMovie.ToStringDictionary();

			var response = ExecutePath(HttpMethods.Post, "/movies", null, formData, null);
			response.PrintDump();
			this.DbFactory.Run(db => {
				var lastInsertId = db.GetLastInsertId();
				var createdMovie = db.GetById<Movie>(lastInsertId);
				Assert.That(createdMovie, Is.Not.Null);
				Assert.That(createdMovie, Is.EqualTo(NewMovie));
			});
		}

		[Test]
		public void Can_create_new_Movie_from_dto()
		{
			var response = ExecutePath(HttpMethods.Post, "/movies", null, null, NewMovie);
			response.PrintDump();
			this.DbFactory.Run(db => {
				var lastInsertId = db.GetLastInsertId();
				var createdMovie = db.GetById<Movie>(lastInsertId);
				Assert.That(createdMovie, Is.Not.Null);
				Assert.That(createdMovie, Is.EqualTo(NewMovie));
			});
		}

		[Test]
		public void Can_POST_to_resetmovies()
		{
			var response = ExecutePath(HttpMethods.Post, "/reset-movies");
			response.PrintDump();
			this.DbFactory.Run(db => {
				var movies = db.Select<Movie>();
				Assert.That(movies.Count, Is.EqualTo(ResetMoviesService.Top5Movies.Count));
			});
		}

		[Test]
		public void Error_calling_GET_on_resetmovies()
		{
			try
			{
				var response = (ResetMoviesResponse)ExecutePath(HttpMethods.Get, "/reset-movies");
				response.PrintDump();
				Assert.Fail("Should throw HTTP errors");
			}
			catch (WebServiceException webEx)
			{
				var response = (ResetMoviesResponse)webEx.ResponseDto;
				Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo(typeof(NotImplementedException).Name));
			}
		}

	}

}