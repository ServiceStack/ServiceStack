using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;

namespace ServiceStack.Text.Tests.JsonTests
{
    public class ContainsIDictionary
    {
        public IDictionary Container { get; set; }
    }
    public class ContainsGenericStringDictionary
    {
        public Dictionary<string, string> Container { get; set; }
    }

    public class SeveralTypesOfDictionary
    {
        public IDictionary GuidToInt { get; set; }
        public IDictionary DateTimeTo_DictStrStr { get; set; }
    }

    [TestFixture]
    public class BasicPropertiesTests
    {
        [Test]
        public void Generic_dictionary_backed_IDictionary_round_trips_ok()
        {
            var original = new ContainsIDictionary
            {
                Container = new Dictionary<string, string>
                {
                    {"one", "header one"},
                    {"two", "header two"}
                }
            };

            var str = JsonSerializer.SerializeToString(original);
            var obj = JsonSerializer.DeserializeFromString<ContainsIDictionary>(str);

            Console.WriteLine(DictStr(obj.Container));
            Assert.That(DictStr(obj.Container), Is.EqualTo(DictStr(original.Container)));
        }

        [Test]
        public void Generic_dictionary_backed_IDictionary_deserialises_to_generic_dictionary()
        {
            var original = new ContainsIDictionary // Using IDictionary backing
            {
                Container = new Dictionary<string, string>
                {
                    {"one", "header one"},
                    {"two", "header two"}
                }
            };

            var str = JsonSerializer.SerializeToString(original);
            var obj = JsonSerializer.DeserializeFromString<ContainsGenericStringDictionary>(str); // decoding to Dictionary<,>

            Console.WriteLine(DictStr(obj.Container));
            Assert.That(DictStr(obj.Container), Is.EqualTo(DictStr(original.Container)));
        }

        [Test]
        public void Generic_dictionary_deserialises_to_IDictionary()
        {
            var original = new ContainsGenericStringDictionary // Using Dictionary<,> backing
            {
                Container = new Dictionary<string, string>
                {
                    {"one", "header one"},
                    {"two", "header two"}
                }
            };

            var str = JsonSerializer.SerializeToString(original);
            var obj = JsonSerializer.DeserializeFromString<ContainsIDictionary>(str); // decoding to IDictionary

            Console.WriteLine(DictStr(obj.Container));
            Assert.That(DictStr(obj.Container), Is.EqualTo(DictStr(original.Container)));
        }

        [Test]
        public void Generic_dictionary_round_trips_ok()
        {
            var original = new ContainsGenericStringDictionary
            {
                Container = new Dictionary<string, string>
                {
                    {"one", "header one"},
                    {"two", "header two"}
                }
            };

            var str = JsonSerializer.SerializeToString(original);
            var obj = JsonSerializer.DeserializeFromString<ContainsGenericStringDictionary>(str);

            Console.WriteLine(DictStr(obj.Container));
            Assert.That(DictStr(obj.Container), Is.EqualTo(DictStr(original.Container)));
        }

        [Test]
        public void Generic_dictionary_and_IDictionary_serialise_the_same()
        {
            JsConfig.PreferInterfaces = true;
            JsConfig.ExcludeTypeInfo = false;
            JsConfig.ConvertObjectTypesIntoStringDictionary = false;

            var genericStringDictionary = new ContainsGenericStringDictionary
            {
                Container = new Dictionary<string, string>
                {
                    {"one", "header one"},
                    {"two", "header two"}
                }
            };
            var iDictionary = new ContainsIDictionary
            {
                Container = new Dictionary<string, string>
                {
                    {"one", "header one"},
                    {"two", "header two"}
                }
            };

            var genDict = genericStringDictionary.ToJson();
            var iDict = iDictionary.ToJson();

            Console.WriteLine("Dictionary<string,string> --> " + genDict);
            Console.WriteLine();
            Console.WriteLine("IDictionary               --> " + iDict);

            Assert.That(genDict, Is.EqualTo(iDict));
        }

        [Test]
        [Ignore("Very complex mappings, not needed for most tasks.")]
        public void Complex_dictionaries_round_trip()
        {
            var original = new SeveralTypesOfDictionary
            {
                GuidToInt = new Dictionary<Guid, int>
                {
                    {Guid.Empty, 10},
                    {Guid.NewGuid(), 25}
                },
                DateTimeTo_DictStrStr = new Dictionary<DateTime, Dictionary<string, string>> {
                    {DateTime.Today, new Dictionary<string, string> {{"a","b"},{"c","d"}}},
                    {DateTime.Now, new Dictionary<string, string> {{"a","b"},{"c","d"}}}
                }
            };
            // see WriteDictionary.cs line 105
            // Problems:
            //   - Int is turning into String on Deserialise
            //   - Dictionary of dictionaries is totally failing on Deserialise
            var string_a = original.ToJson();
            var copy_a = string_a.FromJson<SeveralTypesOfDictionary>();
            var string_b = copy_a.ToJson();
            var copy_b = string_b.FromJson<SeveralTypesOfDictionary>();

            Console.WriteLine(string_a);
            Console.WriteLine(string_b);
            Assert.That(copy_a.GuidToInt[Guid.Empty], Is.EqualTo(10), "First copy was incorrect");
            Assert.That(copy_b.GuidToInt[Guid.Empty], Is.EqualTo(10), "Second copy was incorrect");
            Assert.That(string_a, Is.EqualTo(string_b), "Serialised forms not same");
        }

        static string DictStr(IDictionary d)
        {
            var sb = StringBuilderCache.Allocate();
            foreach (var key in d.Keys)
            {
                sb.AppendLine(key + " = " + d[key]);
            }
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public class ModelWithHashSet
        {
            public HashSet<string> Set { get; set; }
        }

        [Test]
        public void Can_deserialize_null_Nested_HashSet()
        {
            JsConfig.ThrowOnError = true;
            string json = @"{""set"":null}";
            var o = json.FromJson<ModelWithHashSet>();
            Assert.That(o.Set, Is.Null);

            JsConfig.Reset();
        }
    }
}
