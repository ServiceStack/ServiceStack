using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Xml;
using NUnit.Framework;
using ServiceStack.Text.Common;
using ServiceStack.Text.Jsv;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class AdhocModelTests
        : TestBase
    {
        public enum FlowPostType
        {
            Content,
            Text,
            Promo,
        }

        public class FlowPostTransient
        {
            public FlowPostTransient()
            {
                this.TrackUrns = new List<string>();
            }

            public long Id { get; set; }

            public string Urn { get; set; }

            public Guid UserId { get; set; }

            public DateTime DateAdded { get; set; }

            public DateTime DateModified { get; set; }

            public Guid? TargetUserId { get; set; }

            public long? ForwardedPostId { get; set; }

            public Guid OriginUserId { get; set; }

            public string OriginUserName { get; set; }

            public Guid SourceUserId { get; set; }

            public string SourceUserName { get; set; }

            public string SubjectUrn { get; set; }

            public string ContentUrn { get; set; }

            public IList<string> TrackUrns { get; set; }

            public string Caption { get; set; }

            public Guid CaptionUserId { get; set; }

            public string CaptionSourceName { get; set; }

            public string ForwardedPostUrn { get; set; }

            public FlowPostType PostType { get; set; }

            public Guid? OnBehalfOfUserId { get; set; }

            public static FlowPostTransient Create()
            {
                return new FlowPostTransient
                {
                    Caption = "Caption",
                    CaptionSourceName = "CaptionSourceName",
                    CaptionUserId = Guid.NewGuid(),
                    ContentUrn = "ContentUrn",
                    DateAdded = DateTime.Now,
                    DateModified = DateTime.Now,
                    ForwardedPostId = 1,
                    ForwardedPostUrn = "ForwardedPostUrn",
                    Id = 1,
                    OnBehalfOfUserId = Guid.NewGuid(),
                    OriginUserId = Guid.NewGuid(),
                    OriginUserName = "OriginUserName",
                    PostType = FlowPostType.Content,
                    SourceUserId = Guid.NewGuid(),
                    SourceUserName = "SourceUserName",
                    SubjectUrn = "SubjectUrn ",
                    TargetUserId = Guid.NewGuid(),
                    TrackUrns = new List<string> { "track1", "track2" },
                    Urn = "Urn ",
                    UserId = Guid.NewGuid(),
                };
            }

            public bool Equals(FlowPostTransient other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return other.Id == Id
                    && Equals(other.Urn, Urn)
                    && other.UserId.Equals(UserId)
                    && other.DateAdded.RoundToMs().Equals(DateAdded.RoundToMs())
                    && other.DateModified.RoundToMs().Equals(DateModified.RoundToMs())
                    && other.TargetUserId.Equals(TargetUserId)
                    && other.ForwardedPostId.Equals(ForwardedPostId)
                    && other.OriginUserId.Equals(OriginUserId)
                    && Equals(other.OriginUserName, OriginUserName)
                    && other.SourceUserId.Equals(SourceUserId)
                    && Equals(other.SourceUserName, SourceUserName)
                    && Equals(other.SubjectUrn, SubjectUrn)
                    && Equals(other.ContentUrn, ContentUrn)
                    && TrackUrns.EquivalentTo(other.TrackUrns)
                    && Equals(other.Caption, Caption)
                    && other.CaptionUserId.Equals(CaptionUserId)
                    && Equals(other.CaptionSourceName, CaptionSourceName)
                    && Equals(other.ForwardedPostUrn, ForwardedPostUrn)
                    && Equals(other.PostType, PostType)
                    && other.OnBehalfOfUserId.Equals(OnBehalfOfUserId);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != typeof(FlowPostTransient)) return false;
                return Equals((FlowPostTransient)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int result = Id.GetHashCode();
                    result = (result * 397) ^ (Urn != null ? Urn.GetHashCode() : 0);
                    result = (result * 397) ^ UserId.GetHashCode();
                    result = (result * 397) ^ DateAdded.GetHashCode();
                    result = (result * 397) ^ DateModified.GetHashCode();
                    result = (result * 397) ^ (TargetUserId.HasValue ? TargetUserId.Value.GetHashCode() : 0);
                    result = (result * 397) ^ (ForwardedPostId.HasValue ? ForwardedPostId.Value.GetHashCode() : 0);
                    result = (result * 397) ^ OriginUserId.GetHashCode();
                    result = (result * 397) ^ (OriginUserName != null ? OriginUserName.GetHashCode() : 0);
                    result = (result * 397) ^ SourceUserId.GetHashCode();
                    result = (result * 397) ^ (SourceUserName != null ? SourceUserName.GetHashCode() : 0);
                    result = (result * 397) ^ (SubjectUrn != null ? SubjectUrn.GetHashCode() : 0);
                    result = (result * 397) ^ (ContentUrn != null ? ContentUrn.GetHashCode() : 0);
                    result = (result * 397) ^ (TrackUrns != null ? TrackUrns.GetHashCode() : 0);
                    result = (result * 397) ^ (Caption != null ? Caption.GetHashCode() : 0);
                    result = (result * 397) ^ CaptionUserId.GetHashCode();
                    result = (result * 397) ^ (CaptionSourceName != null ? CaptionSourceName.GetHashCode() : 0);
                    result = (result * 397) ^ (ForwardedPostUrn != null ? ForwardedPostUrn.GetHashCode() : 0);
                    result = (result * 397) ^ PostType.GetHashCode();
                    result = (result * 397) ^ (OnBehalfOfUserId.HasValue ? OnBehalfOfUserId.Value.GetHashCode() : 0);
                    return result;
                }
            }
        }

        [SetUp]
        public void SetUp()
        {
            JsConfig.Reset();
        }

        [Test]
        public void Can_Deserialize_text()
        {
            var dtoString = "[{Id:1,Urn:urn:post:3a944f18-920c-498a-832d-cf38fed3d0d7/1,UserId:3a944f18920c498a832dcf38fed3d0d7,DateAdded:2010-02-17T12:04:45.2845615Z,DateModified:2010-02-17T12:04:45.2845615Z,OriginUserId:3a944f18920c498a832dcf38fed3d0d7,OriginUserName:testuser1,SourceUserId:3a944f18920c498a832dcf38fed3d0d7,SourceUserName:testuser1,SubjectUrn:urn:track:1,ContentUrn:urn:track:1,TrackUrns:[],CaptionUserId:3a944f18920c498a832dcf38fed3d0d7,CaptionSourceName:testuser1,PostType:Content}]";
            var fromString = TypeSerializer.DeserializeFromString<List<FlowPostTransient>>(dtoString);
        }

        [Test]
        public void Can_Serialize_single_FlowPostTransient()
        {
            var dto = FlowPostTransient.Create();
            SerializeAndCompare(dto);
        }

        [Test]
        public void Can_serialize_jsv_dates()
        {
            var now = DateTime.Now;

            var jsvDate = TypeSerializer.SerializeToString(now);
            var fromJsvDate = TypeSerializer.DeserializeFromString<DateTime>(jsvDate);
            Assert.That(fromJsvDate, Is.EqualTo(now));
        }

        [Test]
        public void Can_serialize_json_dates()
        {
            var now = DateTime.Now;

            var jsonDate = JsonSerializer.SerializeToString(now);
            var fromJsonDate = JsonSerializer.DeserializeFromString<DateTime>(jsonDate);

            Assert.That(fromJsonDate.RoundToMs(), Is.EqualTo(now.RoundToMs()));
        }

        [Test]
        public void Can_Serialize_multiple_FlowPostTransient()
        {
            var dtos = new List<FlowPostTransient> {
				FlowPostTransient.Create(), 
				FlowPostTransient.Create()
			};
            Serialize(dtos);
        }

        [DataContract]
        public class TestObject
        {
            [DataMember]
            public string Value { get; set; }
            public TranslatedString ValueNoMember { get; set; }

            public bool Equals(TestObject other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(other.Value, Value);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != typeof(TestObject)) return false;
                return Equals((TestObject)obj);
            }

            public override int GetHashCode()
            {
                return (Value != null ? Value.GetHashCode() : 0);
            }
        }

        public class Test
        {
            public string Val { get; set; }
        }

        public class TestResponse
        {
            public TestObject Result { get; set; }
        }

        public class TranslatedString : ListDictionary
        {
            public string CurrentLanguage { get; set; }

            public string Value
            {
                get
                {
                    if (this.Contains(CurrentLanguage))
                        return this[CurrentLanguage] as string;
                    return null;
                }
                set
                {
                    if (this.Contains(CurrentLanguage))
                        this[CurrentLanguage] = value;
                    else
                        Add(CurrentLanguage, value);
                }
            }

            public TranslatedString()
            {
                CurrentLanguage = "en";
            }

            public static void SetLanguageOnStrings(string lang, params TranslatedString[] strings)
            {
                foreach (TranslatedString str in strings)
                    str.CurrentLanguage = lang;
            }
        }

        [Test]
        public void Should_ignore_non_DataMember_TranslatedString()
        {
            var dto = new TestObject
            {
                Value = "value",
                ValueNoMember = new TranslatedString
                {
                    {"key1", "val1"},
                    {"key2", "val2"},
                }
            };
            SerializeAndCompare(dto);
        }

        public interface IParent
        {
            int Id { get; set; }
            string ParentName { get; set; }
        }

        public class Parent : IParent
        {
            public int Id { get; set; }
            public string ParentName { get; set; }
            public Child Child { get; set; }
        }

        public class Child
        {
            public int Id { get; set; }
            public string ChildName { get; set; }
            public IParent Parent { get; set; }
        }

        [Test]
        public void Can_Serialize_Cyclical_Dependency_via_interface()
        {
            JsConfig.PreferInterfaces = true;

            var dto = new Parent
            {
                Id = 1,
                ParentName = "Parent",
                Child = new Child { Id = 2, ChildName = "Child" }
            };
            dto.Child.Parent = dto;

            var fromDto = Serialize(dto, includeXml: false);

            var parent = (IParent)fromDto.Child.Parent;
            Assert.That(parent.Id, Is.EqualTo(dto.Id));
            Assert.That(parent.ParentName, Is.EqualTo(dto.ParentName));
        }

        public class Exclude
        {
            public int Id { get; set; }
            public string Key { get; set; }
        }

        [Test]
        public void Can_exclude_properties()
        {
            JsConfig<Exclude>.ExcludePropertyNames = new[] { "Id" };

            var dto = new Exclude { Id = 1, Key = "Value" };

            Assert.That(dto.ToJson(), Is.EqualTo("{\"Key\":\"Value\"}"));
            Assert.That(dto.ToJsv(), Is.EqualTo("{Key:Value}"));
        }

        [Test]
        public void Can_exclude_properties_scoped()
        {
            var dto = new Exclude { Id = 1, Key = "Value" };
            using (var config = JsConfig.BeginScope())
            {
                config.ExcludePropertyReferences = new[] { "Exclude.Id" };
                Assert.That(dto.ToJson(), Is.EqualTo("{\"Key\":\"Value\"}"));
                Assert.That(dto.ToJsv(), Is.EqualTo("{Key:Value}"));
            }

            using (JsConfig.With(new Config { ExcludePropertyReferences = new[] { "Exclude.Id" }}))
            {
                Assert.That(dto.ToJson(), Is.EqualTo("{\"Key\":\"Value\"}"));
                Assert.That(dto.ToJsv(), Is.EqualTo("{Key:Value}"));
            }
        }

        public class IncludeExclude
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Exclude Obj { get; set; }
        }

        [Test]
        public void Can_include_nested_only()
        {
            var dto = new IncludeExclude
            {
                Id = 1234,
                Name = "TEST",
                Obj = new Exclude
                {
                    Id = 1,
                    Key = "Value"
                }
            };

            using (var config = JsConfig.BeginScope())
            {
                config.ExcludePropertyReferences = new[] { "Exclude.Id", "IncludeExclude.Id", "IncludeExclude.Name" };
                Assert.That(dto.ToJson(), Is.EqualTo("{\"Obj\":{\"Key\":\"Value\"}}"));
                Assert.That(dto.ToJsv(), Is.EqualTo("{Obj:{Key:Value}}"));
            }
            Assert.That(JsConfig.ExcludePropertyReferences, Is.EqualTo(null));

        }

        [Test]
        public void Exclude_all_nested()
        {
            var dto = new IncludeExclude
            {
                Id = 1234,
                Name = "TEST",
                Obj = new Exclude
                {
                    Id = 1,
                    Key = "Value"
                }
            };

            using (var config = JsConfig.BeginScope())
            {
                config.ExcludePropertyReferences = new[] { "Exclude.Id", "Exclude.Key" };
                Assert.AreEqual(2, config.ExcludePropertyReferences.Length);

                var actual = dto.ToJson();
                Assert.That(actual, Is.EqualTo("{\"Id\":1234,\"Name\":\"TEST\",\"Obj\":{}}"));
                Assert.That(dto.ToJsv(), Is.EqualTo("{Id:1234,Name:TEST,Obj:{}}"));
            }
        }

        public class ExcludeList
        {
            public int Id { get; set; }
            public List<Exclude> Excludes { get; set; }
        }

        [Test]
        public void Exclude_List_Scope()
        {
            var dto = new ExcludeList
            {
                Id = 1234,
                Excludes = new List<Exclude>() {
                    new Exclude {
                        Id = 2345,
                        Key = "Value"
                    },
                    new Exclude {
                        Id = 3456,
                        Key = "Value"
                    }
                }
            };
            using (var config = JsConfig.BeginScope())
            {
                config.ExcludePropertyReferences = new[] { "ExcludeList.Id", "Exclude.Id" };
                Assert.That(dto.ToJson(), Is.EqualTo("{\"Excludes\":[{\"Key\":\"Value\"},{\"Key\":\"Value\"}]}"));
                Assert.That(dto.ToJsv(), Is.EqualTo("{Excludes:[{Key:Value},{Key:Value}]}"));
            }
        }

        public class HasIndex
        {
            public int Id { get; set; }

            public int this[int id]
            {
                get { return Id; }
                set { Id = value; }
            }
        }

        [Test]
        public void Can_serialize_type_with_indexer()
        {
            var dto = new HasIndex { Id = 1 };
            Serialize(dto);
        }

        public struct Size
        {
            public Size(string value)
            {
                var parts = value.Split(',');
                this.Width = parts[0];
                this.Height = parts[1];
            }

            public Size(string width, string height)
            {
                Width = width;
                Height = height;
            }

            public string Width;
            public string Height;

            public override string ToString()
            {
                return this.Width + "," + this.Height;
            }
        }

        [Test]
        public void Can_serialize_struct_in_list()
        {
            var structs = new[] {
				new Size("10px", "10px"),
				new Size("20px", "20px"),
			};

            Serialize(structs);
        }

        [Test]
        public void Can_serialize_list_of_bools()
        {
            Serialize(new List<bool> { true, false, true });
            Serialize(new[] { true, false, true });
        }

        public class PolarValues
        {
            public int Int { get; set; }
            public long Long { get; set; }
            public float Float { get; set; }
            public double Double { get; set; }
            public decimal Decimal { get; set; }

            public bool Equals(PolarValues other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return other.Int == Int
                    && other.Long == Long
                    && other.Float.Equals(Float)
                    && other.Double.Equals(Double)
                    && other.Decimal == Decimal;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != typeof(PolarValues)) return false;
                return Equals((PolarValues)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int result = Int;
                    result = (result * 397) ^ Long.GetHashCode();
                    result = (result * 397) ^ Float.GetHashCode();
                    result = (result * 397) ^ Double.GetHashCode();
                    result = (result * 397) ^ Decimal.GetHashCode();
                    return result;
                }
            }
        }

        [Test]
        public void Can_serialize_max_values()
        {
            var dto = new PolarValues
            {
                Int = int.MaxValue,
                Long = long.MaxValue,
                Float = float.MaxValue,
                Double = double.MaxValue,
                Decimal = decimal.MaxValue,
            };
            var to = Serialize(dto);
            Assert.That(to, Is.EqualTo(dto));
        }

        [Test]
        public void Can_serialize_max_values_less_1()
        {
            var dto = new PolarValues
            {
                Int = int.MaxValue - 1,
                Long = long.MaxValue - 1,
                Float = float.MaxValue - 1,
                Double = double.MaxValue - 1,
                Decimal = decimal.MaxValue - 1,
            };
            var to = Serialize(dto);
            Assert.That(to, Is.EqualTo(dto));
        }

        [Test]
        public void Can_serialize_min_values()
        {
            var dto = new PolarValues
            {
                Int = int.MinValue,
                Long = long.MinValue,
                Float = float.MinValue,
                Double = double.MinValue,
                Decimal = decimal.MinValue,
            };
            var to = Serialize(dto);
            Assert.That(to, Is.EqualTo(dto));
        }

        public class TestClass
        {
            public string Description { get; set; }
            public TestClass Inner { get; set; }
        }

        [Test]
        public void Can_serialize_1_level_cyclical_dto()
        {
            var dto = new TestClass
            {
                Description = "desc",
                Inner = new TestClass { Description = "inner" }
            };

            var from = Serialize(dto, includeXml: false);

            Assert.That(from.Description, Is.EqualTo(dto.Description));
            Assert.That(from.Inner.Description, Is.EqualTo(dto.Inner.Description));
            Console.WriteLine(from.Dump());
        }

        public enum EnumValues
        {
            Enum1,
            Enum2,
            Enum3,
        }

        [Test]
        public void Can_Deserialize()
        {
            var items = TypeSerializer.DeserializeFromString<List<string>>(
                "/CustomPath35/api,/CustomPath40/api,/RootPath35,/RootPath40,:82,:83,:5001/api,:5002/api,:5003,:5004");

            Console.WriteLine(items.Dump());
        }

        [Test]
        public void Can_Serialize_Array_of_enums()
        {
            var enumArr = new[] { EnumValues.Enum1, EnumValues.Enum2, EnumValues.Enum3, };
            var json = JsonSerializer.SerializeToString(enumArr);
            Assert.That(json, Is.EqualTo("[\"Enum1\",\"Enum2\",\"Enum3\"]"));
        }

        public class DictionaryEnumType
        {
            public Dictionary<EnumValues, Test> DictEnumType { get; set; }
        }

        [Test]
        public void Can_Serialize_Dictionary_With_Enums()
        {
            Dictionary<EnumValues, Test> dictEnumType =
                new Dictionary<EnumValues, Test> 
                {
                    {
                        EnumValues.Enum1, new Test { Val = "A Value" }
                    }
                };

            var item = new DictionaryEnumType
            {
                DictEnumType = dictEnumType
            };
            const string expected = "{\"DictEnumType\":{\"Enum1\":{\"Val\":\"A Value\"}}}";

            var jsonItem = JsonSerializer.SerializeToString(item);
            //Log(jsonItem);
            Assert.That(jsonItem, Is.EqualTo(expected));

            var deserializedItem = JsonSerializer.DeserializeFromString<DictionaryEnumType>(jsonItem);
            Assert.That(deserializedItem, Is.TypeOf<DictionaryEnumType>());
        }

        [Test]
        public void Can_Serialize_Array_of_chars()
        {
            var enumArr = new[] { 'A', 'B', 'C', };
            var json = JsonSerializer.SerializeToString(enumArr);
            Assert.That(json, Is.EqualTo("[\"A\",\"B\",\"C\"]"));
        }

        [Test]
        public void Can_Serialize_Array_with_nulls()
        {
            var t = new
            {
                Name = "MyName",
                Number = (int?)null,
                Data = new object[] { 5, null, "text" }
            };

            ServiceStack.Text.JsConfig.IncludeNullValues = true;
            var json = ServiceStack.Text.JsonSerializer.SerializeToString(t);
            Assert.That(json, Is.EqualTo("{\"Name\":\"MyName\",\"Number\":null,\"Data\":[5,null,\"text\"]}"));
            JsConfig.Reset();
        }

        class A
        {
            public string Value { get; set; }
        }

        [Test]
        public void DumpFail()
        {
            var arrayOfA = new[] { new A { Value = "a" }, null, new A { Value = "b" } };
            Console.WriteLine(arrayOfA.Dump());
        }

        [Test]
        public void Can_deserialize_case_insensitive_names()
        {
            var dto = "{\"vALUE\":\"B\"}".FromJson<A>();
            Assert.That(dto.Value, Is.EqualTo("B"));

            dto = "{vALUE:B}".FromJsv<A>();
            Assert.That(dto.Value, Is.EqualTo("B"));
        }

        [Test]
        public void Deserialize_array_with_null_elements()
        {
            var json = "[{\"Value\": \"a\"},null,{\"Value\": \"b\"}]";
            var o = JsonSerializer.DeserializeFromString<A[]>(json);
        }

        [Test]
        public void Can_serialize_StringCollection()
        {
            var sc = new StringCollection { "one", "two", "three" };
            var from = Serialize(sc, includeXml: false);
            Console.WriteLine(from.Dump());
        }

        public class Breaker
        {
            public IEnumerable Blah { get; set; }
        }

        [Test]
        public void Can_serialize_IEnumerable()
        {
            var dto = new Breaker
            {
                Blah = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 }
            };

            var from = Serialize(dto, includeXml: false);
            Assert.IsNotNull(from.Blah);
            from.PrintDump();
        }

        public class BreakerCollection
        {
            public ICollection Blah { get; set; }
        }

        [Test]
        public void Can_serialize_ICollection()
        {
            var dto = new BreakerCollection
            {
                Blah = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 }
            };

            var from = Serialize(dto, includeXml: false);
            Assert.IsNotNull(from.Blah);
            Assert.AreEqual(dto.Blah.Count, from.Blah.Count);
            from.PrintDump();
        }

        [Test]
        public void Can_parse_different_3_part_date_formats()
        {
            Assert.That("28/06/2015".FromJsv<DateTime>(),
                Is.EqualTo(new DateTime(2015, 6, 28)));

            Assert.That("6/28/2015".FromJsv<DateTime>(),
                Is.EqualTo(new DateTime(2015, 6, 28)));

            DateTimeSerializer.OnParseErrorFn = (s, ex) =>
            {
                var parts = s.Split('/');
                return new DateTime(int.Parse(parts[2]), int.Parse(parts[0]), int.Parse(parts[1]));
            };

            Assert.That("06/28/2015".FromJsv<DateTime>(),
                Is.EqualTo(new DateTime(2015, 6, 28)));

            DateTimeSerializer.OnParseErrorFn = null;
        }
    }
}
