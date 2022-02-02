using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Data;
using ServiceStack.OrmLite;
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
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            this.DbFactory = this.Container.Resolve<IDbConnectionFactory>();

            using (var db = DbFactory.Open())
                db.DropAndCreateTable<Movie>();
        }

        [Test]
        public void Can_PATCH_Movie_from_dto()
        {
            ExecutePath(HttpMethods.Post, "/movies", null, null, NewMovie);

            using (var db = DbFactory.Open())
            {
                var lastInsertId = (int)db.LastInsertId();

                var patchMovie = new Movie { Id = lastInsertId, Title = "PATCHED " + NewMovie.Title };
                ExecutePath(HttpMethods.Patch, "/movies", null, null, patchMovie);

                var movie = db.SingleById<Movie>(lastInsertId);
                Assert.That(movie, Is.Not.Null);
                Assert.That(movie.Title, Is.EqualTo(patchMovie.Title));
            }
        }

        [Test]
        public void Can_create_new_Movie_from_FormData()
        {
            var formData = NewMovie.ToStringDictionary();

            var response = ExecutePath(HttpMethods.Post, "/movies", null, formData, null);
            using (var db = DbFactory.Open())
            {
                var lastInsertId = db.LastInsertId();
                var createdMovie = db.SingleById<Movie>(lastInsertId);
                Assert.That(createdMovie, Is.Not.Null);
                Assert.That(createdMovie, Is.EqualTo(NewMovie));
            };
        }

        [Test]
        public void Can_create_new_Movie_from_dto()
        {
            var response = ExecutePath(HttpMethods.Post, "/movies", null, null, NewMovie);
            using (var db = DbFactory.Open())
            {
                var lastInsertId = db.LastInsertId();
                var createdMovie = db.SingleById<Movie>(lastInsertId);
                Assert.That(createdMovie, Is.Not.Null);
                Assert.That(createdMovie, Is.EqualTo(NewMovie));
            };
        }

        [Test]
        public void Can_POST_to_resetmovies()
        {
            var response = ExecutePath(HttpMethods.Post, "/reset-movies");
            using (var db = DbFactory.Open())
            {
                var movies = db.Select<Movie>();
                Assert.That(movies.Count, Is.EqualTo(ResetMoviesService.Top5Movies.Count));
            };
        }

        [Test]
        public void Error_calling_GET_on_resetmovies()
        {
            try
            {
                var response = (ResetMoviesResponse)ExecutePath(HttpMethods.Get, "/reset-movies");
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