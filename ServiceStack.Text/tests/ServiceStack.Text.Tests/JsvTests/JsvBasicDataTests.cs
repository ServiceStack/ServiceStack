using NUnit.Framework;
using ServiceStack.Text.Tests.Shared;

namespace ServiceStack.Text.Tests.JsvTests
{
    public class JsvBasicDataTests
    {
        [Test]
        public void Can_serialize_ModelWithFloatTypes()
        {
            var dto = new ModelWithFloatTypes
            {
                Float = 1.1f,
                Double = 2.2d,
                Decimal = 3.3m
            };

            var jsv = dto.ToJsv();
            Assert.That(jsv, Is.EqualTo("{Float:1.1,Double:2.2,Decimal:3.3}"));

            var fromJsv = jsv.FromJsv<ModelWithFloatTypes>();
            Assert.That(fromJsv, Is.EqualTo(dto));

            dto = new ModelWithFloatTypes
            {
                Float = 111111.1f,
                Double = 2222222.22d,
                Decimal = 33333333.333m
            };

            jsv = dto.ToJsv();
            Assert.That(jsv, Is.EqualTo("{Float:111111.1,Double:2222222.22,Decimal:33333333.333}"));

            fromJsv = jsv.FromJsv<ModelWithFloatTypes>();
            Assert.That(fromJsv, Is.EqualTo(dto));
        }

        [Test]
        public void Can_serialize_ModelWithNullableFloatTypes()
        {
            var dto = new ModelWithNullableFloatTypes
            {
                Float = 1.1f,
                Double = 2.2d,
                Decimal = 3.3m
            };

            var jsv = dto.ToJsv();
            Assert.That(jsv, Is.EqualTo("{Float:1.1,Double:2.2,Decimal:3.3}"));

            var fromJsv = jsv.FromJsv<ModelWithNullableFloatTypes>();
            Assert.That(fromJsv, Is.EqualTo(dto));

            dto = new ModelWithNullableFloatTypes
            {
                Float = 111111.1f,
                Double = 2222222.22d,
                Decimal = 33333333.333m
            };

            jsv = dto.ToJsv();
            Assert.That(jsv, Is.EqualTo("{Float:111111.1,Double:2222222.22,Decimal:33333333.333}"));

            fromJsv = jsv.FromJsv<ModelWithNullableFloatTypes>();
            Assert.That(fromJsv, Is.EqualTo(dto));
        }

        [Test]
        public void Can_serialize_ModelWithFloatTypes_From_String()
        {
            var dto = new ModelWithFloatTypes
            {
                Float = 1111.1f,
                Double = 2222.2d,
                Decimal = 3333.3m
            };

            var jsv = "{Float:\"1111.1\",Double:\"2222.2\",Decimal:\"3333.3\"}";
            var fromJsv = jsv.FromJsv<ModelWithFloatTypes>();
            Assert.That(fromJsv, Is.EqualTo(dto));

            jsv = "{Float:\"1,111.1\",Double:\"2,222.2\",Decimal:\"3,333.3\"}";
            fromJsv = jsv.FromJsv<ModelWithFloatTypes>();
            Assert.That(fromJsv, Is.EqualTo(dto));
        }

        [Test]
        public void Can_serialize_ModelWithNullableFloatTypes_From_String()
        {
            var dto = new ModelWithNullableFloatTypes
            {
                Float = 1111.1f,
                Double = 2222.2d,
                Decimal = 3333.3m
            };

            var jsv = "{Float:\"1111.1\",Double:\"2222.2\",Decimal:\"3333.3\"}";
            var fromJsv = jsv.FromJsv<ModelWithNullableFloatTypes>();
            Assert.That(fromJsv, Is.EqualTo(dto));

            jsv = "{Float:\"1,111.1\",Double:\"2,222.2\",Decimal:\"3,333.3\"}";
            fromJsv = jsv.FromJsv<ModelWithNullableFloatTypes>();
            Assert.That(fromJsv, Is.EqualTo(dto));
        }

        [Test]
        public void Does_encode_object_string_values_with_escaped_chars()
        {
            var url = "https://url.com";
            Assert.That(url.ToJsv(), Is.EqualTo("\"https://url.com\""));
            Assert.That(((object)url).ToJsv(), Is.EqualTo("\"https://url.com\""));
        }

    }
}