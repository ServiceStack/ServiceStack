using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Web;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class SpecialTypesTests
        : TestBase
    {
#if NETFRAMEWORK
        [Test]
        public void Can_Serialize_Version()
        {
            Serialize(new Version());
            Serialize(Environment.Version);
        }
#endif

        public class JsonEntityWithPrivateGetter
        {
            public string Name { private get; set; }
        }

        public class JsonEntityWithNoProperties
        {
        }

        [Test]
        public void Can_Serialize_Type_with_no_public_getters()
        {
            Serialize(new JsonEntityWithPrivateGetter { Name = "Daniel" });
        }

        [Test]
        public void Can_Serialize_Type_with_no_public_properties()
        {
            Serialize(new JsonEntityWithNoProperties());
        }

        [Test]
        public void Can_Serialize_Type_with_ByteArray()
        {
            var test = new { Name = "Test", Data = new byte[] { 1, 2, 3, 4, 5 } };
            var json = JsonSerializer.SerializeToString(test);
            Assert.That(json, Is.EquivalentTo("{\"Name\":\"Test\",\"Data\":\"AQIDBAU=\"}"));
        }

        class PocoWithBytes
        {
            public string Name { get; set; }
            public byte[] Data { get; set; }
        }

        [Test]
        public void Can_Serialize_Type_with_ByteArray_as_Int_Array()
        {
            var test = "{\"Name\":\"Test\",\"Data\":[1,2,3,4,5]}".FromJson<PocoWithBytes>();
            Assert.That(test.Data, Is.EquivalentTo(new byte[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public void Can_Serialize_ByteArray()
        {
            var test = new byte[] { 1, 2, 3, 4, 5 };
            var json = JsonSerializer.SerializeToString(test);
            var fromJson = JsonSerializer.DeserializeFromString<byte[]>(json);

            Assert.That(test, Is.EquivalentTo(fromJson));
        }

        [Test]
        public void Can_Serialize_HashTable()
        {
            var h = new Hashtable { { "A", 1 }, { "B", 2 } };
            var fromJson = h.ToJson().FromJson<Hashtable>();
            Assert.That(fromJson.Count, Is.EqualTo(h.Count));
            Assert.That(fromJson["A"].ToString(), Is.EqualTo(h["A"].ToString()));
            Assert.That(fromJson["B"].ToString(), Is.EqualTo(h["B"].ToString()));
        }

        [Test]
        public void Can_serialize_delegate()
        {
            Action x = () => { };

            Assert.That(x.ToJson(), Is.Null);
            Assert.That(x.ToJsv(), Is.Null);
            Assert.That(x.Dump(), Is.Not.Null);
        }

        string MethodWithArgs(int id, string name)
        {
            return null;
        }

        [Test]
        public void Does_dump_delegate_info()
        {
            Action d = Can_Serialize_ByteArray;
            Assert.That(d.Dump(), Is.EqualTo("Void Can_Serialize_ByteArray()"));

            Func<int, string, string> methodWithArgs = MethodWithArgs;
            Assert.That(methodWithArgs.Dump(), Is.EqualTo("String MethodWithArgs(Int32 arg1, String arg2)"));

            Action x = () => { };
            Assert.That(x.Dump(), Does.StartWith("Void <Does_dump_delegate_info>"));
        }


        public class RawRequest : IRequiresRequestStream
        {
            public int Id { get; set; }
            public Stream RequestStream { get; set; }
        }

        [Test]
        public void Does_not_serialize_Streams()
        {
            var dto = new RawRequest { Id = 1, RequestStream = new MemoryStream() };
            Serialize(dto, includeXml: false);
        }

        [Test]
        public void Serializing_Tasks_throws_NotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Task.FromResult(1).ToJson());
            Assert.Throws<NotSupportedException>(() => Task.FromResult(1).ToJsv());
        }

    }
}