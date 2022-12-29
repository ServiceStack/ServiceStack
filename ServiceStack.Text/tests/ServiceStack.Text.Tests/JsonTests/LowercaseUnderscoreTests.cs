using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.Text.Tests.Support;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class LowercaseUnderscoreTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            JsConfig.TextCase = TextCase.SnakeCase;
        }

        [TearDown]
        public void TearDown()
        {
            JsConfig.Reset();
        }

        [Test]
        public void Does_serialize_To_lowercase_underscore()
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
                "{\"id\":1,\"title\":\"The Shawshank Redemption\",\"imdb_id\":\"tt0111161\",\"rating\":9.2,\"director\":\"Frank Darabont\",\"release_date\":\"\\/Date(792979200000)\\/\",\"tag_line\":\"Fear can hold you prisoner. Hope can set you free.\",\"genres\":[\"Crime\",\"Drama\"]}"));

            Serialize(dto);
        }

        [DataContract]
        class Person
        {
            [DataMember(Name = "MyID")]
            public int Id { get; set; }
            [DataMember]
            public string Name { get; set; }
            
            [DataMember(Name = "sur_name")]
            public string LastName { get; set; }
            
            [DataMember(Name = "current_age")]
            public int? CurrentAge { get; set; }
            
            [DataMember(Name = "birth_day")]
            public DateTime? BirthDay { get; set; }
        }

        class WithUnderscore
        {
            public int? user_id { get; set; }
        }
        class WithUnderscoreDigits
        {
            public int? user_id_0 { get; set; }
        }

        [Test]
        public void Should_not_put_double_underscore()
        {
            var t = new WithUnderscore { user_id = 0 };
            Assert.That(t.ToJson(), Is.EqualTo("{\"user_id\":0}"));

            var u = new WithUnderscoreDigits { user_id_0 = 0 };
            Assert.That(u.ToJson(), Is.EqualTo("{\"user_id_0\":0}"));
        }

        [Test]
        public void Can_override_name()
        {
            var person = new Person
            {
                Id = 123,
                Name = "Abc",
                LastName = "Xyz"
            };
            Assert.That(TypeSerializer.SerializeToString(person), Is.EqualTo("{MyID:123,name:Abc,sur_name:Xyz}"));
            Assert.That(JsonSerializer.SerializeToString(person), Is.EqualTo("{\"MyID\":123,\"name\":\"Abc\",\"sur_name\":\"Xyz\"}"));
        }
        
        [Test]
        public void Can_override_name_and_deserialize_with_lenient_scope()
        {
            var person = new Person
            {
                Id = 123,
                Name = "Abc",
                LastName = "Xyz",
                BirthDay = new DateTime(2000,1,2,12,0,0),
                CurrentAge = 19
            };
            
            using (JsConfig.With(new Config { 
                    TextCase = TextCase.SnakeCase,
                    PropertyConvention = PropertyConvention.Lenient }))
            {
                var test = new List<Person> {person};
                var personSerialized = test.ToJson();
                var personFromString = personSerialized.FromJson<List<Person>>();

                var fromJson = personFromString[0];
                Assert.That(person.Id, Is.EqualTo(fromJson.Id));
                Assert.That(person.Name, Is.EqualTo(fromJson.Name));
                Assert.That(person.LastName, Is.EqualTo(fromJson.LastName));
                Assert.That(person.BirthDay.Value, Is.EqualTo(fromJson.BirthDay.Value));
                Assert.That(person.CurrentAge.Value, Is.EqualTo(fromJson.CurrentAge.Value));
            }
        }
        
        
        class WithUnderscoreSeveralDigits
        {
            public int? user_id_00_11 { get; set; }
        }

        [Test]
        public void Should_not_split_digits()
        {
            var u = new WithUnderscoreSeveralDigits { user_id_00_11 = 0 };
            Assert.That(u.ToJson(), Is.EqualTo("{\"user_id_00_11\":0}"));
        }
    }
}