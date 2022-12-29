using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class DynamicJsonTests
    {
        [Test]
        public void Can_Serialize_dynamic_collection()
        {
            dynamic rows = new[] { new { Name = "Foo" }, new { Name = "Bar" } };

            string json = JsonSerializer.SerializeToString(rows);

            Assert.That(json, Is.EqualTo("[{\"Name\":\"Foo\"},{\"Name\":\"Bar\"}]"));

            string csv = CsvSerializer.SerializeToString(rows);
            Assert.That(csv.NormalizeNewLines(), Is.EqualTo("Name\nFoo\nBar"));
        }

        [Test]
        public void Can_deserialize_dynamic_instance()
        {
            var dog = new { Name = "Spot" };
            var json = DynamicJson.Serialize(dog);

            Assert.That(json, Is.EqualTo("{\"Name\":\"Spot\"}"));

            var deserialized = DynamicJson.Deserialize(json);

            Assert.IsNotNull(deserialized);
            Assert.That(deserialized.Name, Is.EqualTo(dog.Name));
        }
    }
}
