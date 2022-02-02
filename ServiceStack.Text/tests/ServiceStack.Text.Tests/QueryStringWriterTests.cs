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
            Movie newMovie = new Movie
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

            queryString.Print();

            Assert.That(queryString,
                Is.EqualTo("Id=tt0110912&Title=Pulp+Fiction&Rating=8.9&Director=Quentin+Tarantino&ReleaseDate=1994-10-24&TagLine=Girls+like+me+don%27t+make+invitations+like+this+to+just+anyone%21&Genres=Crime,Drama,Thriller"));
        }

        [Test]
        public void Can_write_dictionary_to_QueryString()
        {
            var map = new Dictionary<string, string>
                {
                    {"Id","tt0110912"},
                    {"Title","Pulp Fiction"},
                    {"Rating","8.9"},
                    {"Director","Quentin Tarantino"},
                    {"ReleaseDate","1994-10-24"},
                    {"TagLine","Girls like me don't make invitations like this to just anyone!"},
                    {"Genres","Crime,Drama,Thriller"},
                };

            var queryString = QueryStringSerializer.SerializeToString(map);

            queryString.Print();

            Assert.That(queryString,
                Is.EqualTo("Id=tt0110912&Title=Pulp+Fiction&Rating=8.9&Director=Quentin+Tarantino&ReleaseDate=1994-10-24&TagLine=Girls+like+me+don%27t+make+invitations+like+this+to+just+anyone%21&Genres=Crime,Drama,Thriller"));
        }

        [Test]
        public void Can_write_AnonymousType_to_QueryString()
        {
            var anonType = new
            {
                Id = "tt0110912",
                Title = "Pulp Fiction",
                Rating = 8.9m,
                Director = "Quentin Tarantino",
                ReleaseDate = new DateTime(1994, 10, 24),
                TagLine = "Girls like me don't make invitations like this to just anyone!",
                Genres = new List<string> { "Crime", "Drama", "Thriller" },
            };

            var queryString = QueryStringSerializer.SerializeToString(anonType);
            queryString.Print();

            Assert.That(queryString,
                Is.EqualTo("Id=tt0110912&Title=Pulp+Fiction&Rating=8.9&Director=Quentin+Tarantino&ReleaseDate=1994-10-24&TagLine=Girls+like+me+don%27t+make+invitations+like+this+to+just+anyone%21&Genres=Crime,Drama,Thriller"));
        }

        [Test]
        public void Can_write_string_to_QueryString()
        {
            var str = "title=Pulp Fiction";

            var queryString = QueryStringSerializer.SerializeToString(str);

            queryString.Print();
        }
    }
}