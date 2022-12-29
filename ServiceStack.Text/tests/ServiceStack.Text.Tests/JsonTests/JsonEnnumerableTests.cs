using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;
using System.Collections.ObjectModel;

namespace ServiceStack.Text.Tests.JsonTests
{
    public class JsonEnnumerableTests
    {
        public class ModelWithReadOnlyCollection
        {
            public ReadOnlyCollection<string> Results { get; set; }
        }

        [Test]
        public void Does_deserialize_Empty_ReadOnlyCollection()
        {
            var dto = "{\"Results\":[]}".FromJson<ModelWithReadOnlyCollection>();
            Assert.That(dto.Results.Count, Is.EqualTo(0));
        }

        public class ModelWithList
        {
            public List<string> Results { get; set; }
        }

        [Test]
        public void Does_deserialize_Empty_List()
        {
            var dto = "{\"Results\":[]}".FromJson<ModelWithList>();
            Assert.That(dto.Results.Count, Is.EqualTo(0));
        }
    }
}