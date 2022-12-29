using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text.Tests.Support;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class JsonDataContractCompatibilityTests
        : TestBase
    {

        [Test]
        public void Can_serialize_a_movie()
        {
            const string clientJson = "{\"Id\":\"0110912\",\"ImdbId\":\"tt0111161\",\"Title\":\"Pulp Fiction\",\"Rating\":\"8.9\",\"Director\":\"Quentin Tarantino\",\"ReleaseDate\":\"/Date(785635200000)/\",\"TagLine\":\"Girls like me don't make invitations like this to just anyone!\",\"Genres\":[\"Crime\",\"Drama\",\"Thriller\"]}";
            var jsonModel = JsonSerializer.DeserializeFromString<Movie>(clientJson);
            var bclJsonModel = BclJsonDataContractDeserializer.Instance.Parse<Movie>(clientJson);

            var ssJson = JsonSerializer.SerializeToString(jsonModel);
            var wcfJson = BclJsonDataContractSerializer.Instance.Parse(jsonModel);

            Console.WriteLine("{0} == {1}", jsonModel.ReleaseDate, bclJsonModel.ReleaseDate);
            Console.WriteLine("CLIENT {0}\nSS {1}\nBCL {2}", clientJson, ssJson, wcfJson);

            Assert.That(jsonModel, Is.EqualTo(bclJsonModel));
            Assert.That(ssJson, Is.EqualTo(wcfJson));
        }

        [Test]
        public void Respects_EmitDefaultValue()
        {
            using (var x = JsConfig.BeginScope())
            {
                x.IncludeNullValues = true;

                var jsonModel = new Movie { Genres = null };

                var ssJson = JsonSerializer.SerializeToString(jsonModel);
                var wcfJson = BclJsonDataContractSerializer.Instance.Parse(jsonModel);

                Assert.That(ssJson, Is.EqualTo(wcfJson));
            }
        }

        [Test]
        public void Can_deserialize_empty_type()
        {
            var ssModel = JsonSerializer.DeserializeFromString<Movie>("{}");
            var ssDynamicModel = JsonSerializer.DeserializeFromString("{}", typeof(Movie));
            var bclModel = BclJsonDataContractDeserializer.Instance.Parse<Movie>("{}");

            var defaultModel = new Movie();
            Assert.That(ssModel, Is.EqualTo(defaultModel));
            Assert.That(ssModel, Is.EqualTo(ssDynamicModel));

            //It's equal except that the BCL resets Lists/Arrays to null which is stupid
            bclModel.Genres = new List<string>();
            Assert.That(ssModel, Is.EqualTo(bclModel));
        }

    }
}