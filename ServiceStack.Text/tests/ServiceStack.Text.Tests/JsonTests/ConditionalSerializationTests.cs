using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;
using System;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class ConditionalSerializationTests
    {
        [Test]
        public void TestSerializeRespected()
        {
            var obj = new Foo { X = "abc", Z = "def" }; // don't touch Y...

            string json = JsonSerializer.SerializeToString(obj);
            Assert.That(json, Does.Match("{\"X\":\"abc\",\"Z\":\"def\"}"));   
        }

        [Test]
        public void TestSerializeRespectedWithInheritance()
        {
            var obj = new SuperFoo { X = "abc", Z = "def", A =123, C = 456 }; // don't touch Y or B...

            string json = JsonSerializer.SerializeToString(obj);
            Assert.That(json, Does.Match("{\"A\":123,\"C\":456,\"X\":\"abc\",\"Z\":\"def\"}"));
        }

        [Test]
        public void TestSerializeHasAttributeNull()
        {
            var obj = new ByNameFoo { A = 123, B = 456 };

            string json = JsonSerializer.SerializeToString(obj);
            Assert.That(json, Does.Match("{\"A\":123,\"B\":456}"));
        }

        [Test]
        public void TestSerializeHasAttributeSet()
        {
            var obj = new ByNameFoo { A = 123, B = 456, hasAttribute = new HashSet<string> { "A" } };

            string json = JsonSerializer.SerializeToString(obj);
            Assert.That(json, Does.Match("{\"A\":123"));

            var cpy = JsonSerializer.DeserializeFromString<ByNameFoo>(json);
            Assert.That(cpy.A, Is.EqualTo(obj.A));
            Assert.That(cpy.hasAttribute, Is.EqualTo(obj.hasAttribute));
        }

        [Test]
        public void TestSerializeHasAttributeSetNullValue()
        {
            var obj = new ByNameFoo { A = 123, B = null, hasAttribute = new HashSet<string> { "B" } };

            string json = JsonSerializer.SerializeToString(obj);
            Assert.That(json, Does.Match("{\"B\":null"));

            var cpy = JsonSerializer.DeserializeFromString<ByNameFoo>(json);
            Assert.That(cpy.A, Is.EqualTo(0));
            Assert.That(cpy.B, Is.EqualTo(obj.B));
            Assert.That(cpy.hasAttribute, Is.EqualTo(obj.hasAttribute));
        }

        [Test]
        public void OnDeserializingMemberUnknown()
        {
            var cpy = JsonSerializer.DeserializeFromString<ByNameFoo>("{\"B\":1,\"C\":1");
            Assert.That(cpy.A, Is.EqualTo(0));
            Assert.That(cpy.B, Is.EqualTo(1));
            Assert.That(cpy.hasAttribute, Is.EqualTo(new HashSet<string> { "B", "C" }));
        }
        
        public class Foo
        {
            public string X { get; set; } // not conditional

            public string Y // conditional: never serialized
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public bool ShouldSerializeY()
            {
                return false;
            }

            public string Z { get;set;} // conditional: always serialized
            public bool ShouldSerializeZ()
            {
                return true;
            }
        }

        public class SuperFoo : Foo
        {
            public int A { get; set; } // not conditional

            public int B // conditional: never serialized
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public bool ShouldSerializeB()
            {
                return false;
            }

            public int  C { get; set; } // conditional: always serialized
            public bool ShouldSerializeC()
            {
                return true;
            }
        }

        [DataContract]
        public class ByNameFoo
        {
            [IgnoreDataMember]
            public HashSet<string> hasAttribute;

            [DataMember(EmitDefaultValue = false, IsRequired = false)]
            public int A { get; set; }

            [DataMember(EmitDefaultValue = false, IsRequired = false)]
            public int? B { get; set; }

            public bool? ShouldSerialize(string fieldName)
            {
                if (hasAttribute == null)
                {
                    return null;
                }
                return hasAttribute.Contains(fieldName);
            }

            public object OnDeserializing(string fieldName, object value)
            {
                if (hasAttribute == null)
                {
                    hasAttribute = new HashSet<string>();
                }
                hasAttribute.Add(fieldName);
                return value;
            }
        }

        [DataContract]
        public class HasEmitDefaultValue
        {
            [DataMember(EmitDefaultValue = false)]
            public int DontEmitDefaultValue { get; set; }

            [DataMember]
            public int IntValue { get; set; }
        }

        [Test]
        public void Does_exclude_default_property_with_EmitDefaultValue()
        {
            var dto = new HasEmitDefaultValue();

            Assert.That(dto.ToJson(), Is.EqualTo("{\"IntValue\":0}"));
        }
    }
}
