using NUnit.Framework;
using ServiceStack.Text.Tests.DynamicModels;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class ModelWithComplexTypesTest
    {
        [Test]
        public void Can_Serialize()
        {
            var m1 = ModelWithComplexTypes.Create(1);
            var s = JsonSerializer.SerializeToString(m1);
            var m2 = JsonSerializer.DeserializeFromString<ModelWithComplexTypes>(s);

            Assert.AreEqual(m1.ListValue[0], m2.ListValue[0]);
            Assert.AreEqual(m1.DictionaryValue["a"], m2.DictionaryValue["a"]);
            Assert.AreEqual(m1.ByteArrayValue[0], m2.ByteArrayValue[0]);
        }

        [Test]
        public void Can_Serialize_WhenNull()
        {
            var m1 = new ModelWithComplexTypes();

            JsConfig.IncludeNullValues = false;
            var s = JsonSerializer.SerializeToString(m1);
            var m2 = JsonSerializer.DeserializeFromString<ModelWithComplexTypes>(s);
            JsConfig.Reset();

            Assert.IsNull(m2.DictionaryValue);
            Assert.IsNull(m2.ListValue);
            Assert.IsNull(m2.ArrayValue);
            Assert.IsNull(m2.NestedTypeValue);
            Assert.IsNull(m2.ByteArrayValue);
        }

        [Test]
        public void Can_Serialize_NullsWhenNull()
        {
            var m1 = new ModelWithComplexTypes();

            JsConfig.IncludeNullValues = true;
            var s = JsonSerializer.SerializeToString(m1);
            var m2 = JsonSerializer.DeserializeFromString<ModelWithComplexTypes>(s);
            JsConfig.Reset();

            Assert.IsNull(m2.DictionaryValue);
            Assert.IsNull(m2.ListValue);
            Assert.IsNull(m2.ArrayValue);
            Assert.IsNull(m2.NestedTypeValue);
            Assert.IsNull(m2.ByteArrayValue);
        }
    }
}