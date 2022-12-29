using NUnit.Framework;
using ServiceStack.Redis.Support;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
#if NETCORE
        [Ignore(".NET Core does not implement BinaryFormatter required for these tests")]
#endif
    public class ObjectSerializerTests
    {
        [Test]
        public void Can_serialize_object_with_default_serializer()
        {
            var ser = new ObjectSerializer();
            string test = "test";
            var serialized = ser.Serialize(test);
            Assert.AreEqual(test, ser.Deserialize(serialized));
        }
        [Test]
        public void Can_serialize_object_with_optimized_serializer()
        {
            var ser = new OptimizedObjectSerializer();
            string test = "test";
            var serialized = ser.Serialize(test);
            Assert.AreEqual(test, ser.Deserialize(serialized));

            float testFloat = 320.0f;
            serialized = ser.Serialize(testFloat);
            Assert.AreEqual(testFloat, ser.Deserialize(serialized));
        }
    }

}