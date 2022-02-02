using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.Text.Tests.DynamicModels;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class DictionaryTests
    {
        public class EdgeCaseProperties : Dictionary<string, string>
        {
            private const string Id = "id";

            [DataMember]
            public int id
            {
                get
                {
                    int value;
                    return (ContainsKey(Id) && int.TryParse(this[Id], out value))
                               ? value
                               : 0;
                }
                set { this[Id] = value.ToString(CultureInfo.InvariantCulture); }
            }

            public static EdgeCaseProperties Create(int i)
            {
                var value = new EdgeCaseProperties { id = i };
                value[i.ToString()] = i.ToString();
                return value;
            }
        }

        [Test]
        public void Can_Serialize()
        {
            var model = EdgeCaseProperties.Create(1);
            var s = JsonSerializer.SerializeToString(model);

            Console.WriteLine(s);
        }

        [Test]
        public void Can_Serialize_list()
        {
            var model = new List<EdgeCaseProperties>
               {
                EdgeCaseProperties.Create(1),
                EdgeCaseProperties.Create(2)
               };
            var s = JsonSerializer.SerializeToString(model);

            Console.WriteLine(s);
        }

        [Test]
        public void Can_Serialize_map()
        {
            var model = new Dictionary<string, EdgeCaseProperties>
               {
                {"A", EdgeCaseProperties.Create(1)},
                {"B", EdgeCaseProperties.Create(2)},
               };
            var s = JsonSerializer.SerializeToString(model);

            Console.WriteLine(s);
        }

        [Test]
        public void Can_Deserialize()
        {
            const string json = "{\"id\":\"1\",\"1\":\"1\"}";

            var model = EdgeCaseProperties.Create(1);

            var fromJson = JsonSerializer.DeserializeFromString<EdgeCaseProperties>(json);

            Assert.That(fromJson, Is.EqualTo(model));
        }

        [DataContract]
        public class Tree
        {
            [DataMember]
            public string Value { get; set; }

            [DataMember]
            public List<Tree> Nodes { get; set; }
        }

        [Test]
        public void CanSerializeAndDeserializeTree()
        {
            var original = new Tree
            {
                Value = "root",
                Nodes = new List<Tree>
                                       {
                                           new Tree {Value = "foo"},
                                           new Tree {Value = "bar"},
                                           new Tree {Value = "baz"}
                                       }
            };
            var json = original.ToJson();
            Console.WriteLine(json);
            var result = JsonSerializer.DeserializeFromString<Tree>(json);
            var resultJson = result.ToJson();
            Assert.AreEqual(json, resultJson);
        }
    }
}