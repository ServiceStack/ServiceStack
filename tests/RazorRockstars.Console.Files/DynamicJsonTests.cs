using NUnit.Framework;
using ServiceStack.Razor2;
using ServiceStack.Razor2.Json;
using ServiceStack.Text;

namespace RazorRockstars.Console.Files
{
    [TestFixture]
    public class DynamicJsonTests
    {
        [Test]
        public void Can_serialize_dynamic_instance()
        {
            var dog = new { Name = "Spot" };
            var json = DynamicJson.Serialize(dog);

            Assert.IsNotNull(json);
            json.Print();
        }

        [Test]
        public void Can_deserialize_dynamic_instance()
        {
            var dog = new { Name = "Spot" };
            var json = DynamicJson.Serialize(dog);
            var deserialized = DynamicJson.Deserialize(json);

            Assert.IsNotNull(deserialized);
            Assert.AreEqual(dog.Name, deserialized.Name);
        }         
    }
}
