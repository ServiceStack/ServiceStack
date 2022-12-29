using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ServiceStack.Redis.Tests.Support;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture]
    public class SerializationTests
    {
        public class Tuple
        {
            public Tuple()
            {
            }

            public Tuple(Type type, object value)
            {
                Type = type;
                Value = value;
            }

            public Type Type { get; set; }
            public object Value { get; set; }
        }

        [Test]
        public void Can_Serialize_type_with_object()
        {
            var obj = new CustomType { CustomId = 1, CustomName = "Name" };
            var typeWithObject = new Tuple(obj.GetType(), obj);
            byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.SerializeToString(typeWithObject));

            var bytesStr = Encoding.UTF8.GetString(bytes);
            var fromTypeWithObject = JsonSerializer.DeserializeFromString<Tuple>(bytesStr);
            var newObj = fromTypeWithObject.Value as CustomType;

            Assert.That(newObj.CustomId, Is.EqualTo(obj.CustomId));
            Assert.That(newObj.CustomName, Is.EqualTo(obj.CustomName));
        }
    }
}