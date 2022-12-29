using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class IDictionarySerializationTests
    {
        [Test]
        public void CanSerializeHashtable()
        {
            var hash = new Hashtable();

            hash["a"] = "b";
            hash[1] = 1;
            hash[2.0m] = 2.0m;

            var serialized = JsonSerializer.SerializeToString(hash);

            var deserialized = JsonSerializer.DeserializeFromString<Dictionary<string, object>>(serialized);

            Assert.AreEqual("b", deserialized["a"]);
            Assert.AreEqual("1", deserialized["1"]);
            Assert.AreEqual("2.0", deserialized["2.0"]);
        }
    }
}