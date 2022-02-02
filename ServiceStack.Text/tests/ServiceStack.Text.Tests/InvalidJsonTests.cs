using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    public class InvalidJsonTests
    {
        public class StoreContact 
        {
            public string Name { get; set; }
            public string Company { get; set; }
            public int Age { get; set; }
        }

        [Test]
        public void Does_not_throw_in_empty_integer()
        {
            var json = "{\"Name\":\"\",\"Age\":\"\",\"Company\":\"\"}";

            var dto = json.FromJson<StoreContact>();
            
            Assert.That(dto.Name, Is.EqualTo(""));
            Assert.That(dto.Company, Is.EqualTo(""));
            Assert.That(dto.Age, Is.EqualTo(0));
        }
    }
}