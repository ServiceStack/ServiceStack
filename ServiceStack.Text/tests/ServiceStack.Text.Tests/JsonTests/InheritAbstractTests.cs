using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.Text.Tests.Support;

namespace ServiceStack.Text.Tests.JsonTests
{
    [DataContract]
    public class MovieChild : Movie
    {
        public MovieChild()
        {
            this.Oscar = new List<string>();
        }

        [DataMember]
        public List<string> Oscar { get; set; }
    }

    class BlockBuster
    {
        public BlockBuster(string address)
        {
            this.Address = address;
            this.Movies = new List<Movie>();
        }

        public string Address { get; set; }
        public List<Movie> Movies { get; set; }
    }
    
    [TestFixture]
    public class InheritAbstractTests
    {
        [Test]
        public void Can_serialize_class_with_list_that_classes_inherited_from_non_abstract_class()
        {
            var child = new MovieChild { ImdbId = "tt0068646", Title = "The Godfather", Rating = 9.2m, Director = "Francis Ford Coppola", ReleaseDate = new DateTime(1972, 3, 24), TagLine = "An offer you can't refuse.", Genres = new List<string> { "Crime", "Drama", "Thriller" }, };
            child.Oscar.Add("Best Picture - 1972");
            child.Oscar.Add("Best Actor - 1972");
            child.Oscar.Add("Best Adapted Screenplay - 1972");

            var blockBuster = new BlockBuster("Av. República do Líbano, 2175 - Indinópolis, São Paulo - SP, 04502-300");
            blockBuster.Movies.Add(MoviesData.Movies[0]);
            blockBuster.Movies.Add(child);

            // serialize to JSON using ServiceStack
            string jsonString = JsonSerializer.SerializeToString(blockBuster);

            var expected = "{\"Address\":\"Av. República do Líbano, 2175 - Indinópolis, São Paulo - SP, 04502-300\",\"Movies\":[{\"Title\":\"The Shawshank Redemption\",\"ImdbId\":\"tt0111161\",\"Rating\":9.2,\"Director\":\"Frank Darabont\",\"ReleaseDate\":"
                + JsonSerializer.SerializeToString(MoviesData.Movies[0].ReleaseDate) + ",\"TagLine\":\"Fear can hold you prisoner. Hope can set you free.\",\"Genres\":[\"Crime\",\"Drama\"]},{\"__type\":\"ServiceStack.Text.Tests.JsonTests.MovieChild, ServiceStack.Text.Tests\",\"Oscar\":[\"Best Picture - 1972\",\"Best Actor - 1972\",\"Best Adapted Screenplay - 1972\"],\"Title\":\"The Godfather\",\"ImdbId\":\"tt0068646\",\"Rating\":9.2,\"Director\":\"Francis Ford Coppola\",\"ReleaseDate\":"
                + JsonSerializer.SerializeToString(child.ReleaseDate) + ",\"TagLine\":\"An offer you can't refuse.\",\"Genres\":[\"Crime\",\"Drama\",\"Thriller\"]}]}";

            Assert.That(jsonString, Is.EqualTo(expected));
        }
    }
}