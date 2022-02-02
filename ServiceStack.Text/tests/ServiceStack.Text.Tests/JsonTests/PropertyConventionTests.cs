using NUnit.Framework;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class PropertyConventionTests : TestBase
    {
        [Test]
        public void Does_require_exact_match_by_default()
        {
            Assert.That(JsConfig.PropertyConvention, Is.EqualTo(PropertyConvention.Strict));
            const string bad = "{ \"total_count\":45, \"was_published\":true }";
            const string good = "{ \"TotalCount\":45, \"WasPublished\":true }";
            
            var actual = JsonSerializer.DeserializeFromString<Example>(bad);
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.TotalCount, Is.EqualTo(0));
            Assert.That(actual.WasPublished, Is.EqualTo(false));

            actual = JsonSerializer.DeserializeFromString<Example>(good);
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.TotalCount, Is.EqualTo(45));
            Assert.That(actual.WasPublished, Is.EqualTo(true));
        }
        
        [Test]
        public void Does_deserialize_from_inexact_source_when_lenient_convention_is_used()
        {
            JsConfig.PropertyConvention = PropertyConvention.Lenient;
            const string bad = "{ \"total_count\":45, \"was_published\":true }";
            
            var actual = JsonSerializer.DeserializeFromString<Example>(bad);
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.TotalCount, Is.EqualTo(45));
            Assert.That(actual.WasPublished, Is.EqualTo(true));
            
            JsConfig.Reset();
        }

        public class Example
        {
            public int TotalCount { get; set; }
            public bool WasPublished { get; set; }
        }

        public class Hyphens
        {
            public string SnippetFormat { get; set; }
            public int Total { get; set; }
            public int Start { get; set; }
            public int PageLength { get; set; }
        }

        [Test]
        public void Can_deserialize_hyphens()
        {
            var json = @"{
                ""snippet-format"":""raw"",
                ""total"":1,
                ""start"":1,
                ""page-length"":200
             }";

            var map = JsonObject.Parse(json);
            Assert.That(map["snippet-format"], Is.EqualTo("raw"));
            Assert.That(map["total"], Is.EqualTo("1"));
            Assert.That(map["start"], Is.EqualTo("1"));
            Assert.That(map["page-length"], Is.EqualTo("200"));

            JsConfig.PropertyConvention = PropertyConvention.Lenient;

            var dto = json.FromJson<Hyphens>();

            Assert.That(dto.SnippetFormat, Is.EqualTo("raw"));
            Assert.That(dto.Total, Is.EqualTo(1));
            Assert.That(dto.Start, Is.EqualTo(1));
            Assert.That(dto.PageLength, Is.EqualTo(200));

            JsConfig.Reset();
        }
    }
}