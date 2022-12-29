using System.Runtime.Serialization;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class InvalidJsonTests
    {
        public class Invalid1
        {
            public string ExtId { get; set; }
            public string Name { get; set; }
            public string[] Mobiles { get; set; }
        }

        [Test]
        public void Does_parse_invalid_JSON()
        {
            var json = "[{\"ExtId\":\"2\",\"Name\":\"VIP sj�lland\",\"Mobiles\":[\"4533333333\",\"4544444444\"]";

            var dto = json.FromJson<Invalid1[]>();

            Assert.That(dto[0].ExtId, Is.EqualTo("2"));
            Assert.That(dto[0].Name, Is.EqualTo("VIP sj�lland"));
            Assert.That(dto[0].Mobiles, Is.Null);
        }

        [Test]
        public void Does_throw_on_invalid_JSON()
        {
            JsConfig.ThrowOnError = true;

            var json = "[{\"ExtId\":\"2\",\"Name\":\"VIP sj�lland\",\"Mobiles\":[\"4533333333\",\"4544444444\"]";

            Assert.Throws<SerializationException>(() => 
                json.FromJson<Invalid1[]>());

            JsConfig.Reset();
        }
    }
}