using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.Text.Tests.Support;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class CamelCaseTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            JsConfig.TextCase = TextCase.CamelCase;
        }

        [TearDown]
        public void TearDown()
        {
            JsConfig.Reset();
        }

        [Test]
        public void Does_serialize_To_CamelCase()
        {
            var dto = new Movie
            {
                Id = 1,
                ImdbId = "tt0111161",
                Title = "The Shawshank Redemption",
                Rating = 9.2m,
                Director = "Frank Darabont",
                ReleaseDate = new DateTime(1995, 2, 17, 0, 0, 0, DateTimeKind.Utc),
                TagLine = "Fear can hold you prisoner. Hope can set you free.",
                Genres = new List<string> { "Crime", "Drama" },
            };

            var json = dto.ToJson();

            Assert.That(json, Is.EqualTo(
                "{\"id\":1,\"title\":\"The Shawshank Redemption\",\"imdbId\":\"tt0111161\",\"rating\":9.2,\"director\":\"Frank Darabont\",\"releaseDate\":\"\\/Date(792979200000)\\/\",\"tagLine\":\"Fear can hold you prisoner. Hope can set you free.\",\"genres\":[\"Crime\",\"Drama\"]}"));

            Serialize(dto);
        }

        [DataContract]
        class Person
        {
            [DataMember(Name = "MyID")]
            public int Id { get; set; }
            [DataMember]
            public string Name { get; set; }
        }

        [Test]
        public void Can_override_name()
        {
            var person = new Person
            {
                Id = 123,
                Name = "Abc"
            };

            Assert.That(TypeSerializer.SerializeToString(person), Is.EqualTo("{MyID:123,name:Abc}"));
            Assert.That(JsonSerializer.SerializeToString(person), Is.EqualTo("{\"MyID\":123,\"name\":\"Abc\"}"));
        }

    }
}