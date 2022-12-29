using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    [TestFixture]
    public class EnumTests
    {
        [SetUp]
        public void SetUp()
        {
            JsConfig.Reset();
        }

        public enum EnumWithoutFlags
        {
            Zero = 0,
            One = 1,
            Two = 2
        }

        [Flags]
        public enum EnumWithFlags
        {
            Zero = 0,
            One = 1,
            Two = 2
        }

        public class ClassWithEnums
        {
            public EnumWithFlags FlagsEnum { get; set; }
            public EnumWithoutFlags NoFlagsEnum { get; set; }
            public EnumWithFlags? NullableFlagsEnum { get; set; }
            public EnumWithoutFlags? NullableNoFlagsEnum { get; set; }
        }
        
        [Test]
        public void Can_correctly_serialize_enums()
        {
            var item = new ClassWithEnums
            {
                FlagsEnum = EnumWithFlags.One,
                NoFlagsEnum = EnumWithoutFlags.One,
                NullableFlagsEnum = EnumWithFlags.Two,
                NullableNoFlagsEnum = EnumWithoutFlags.Two
            };

            const string expected = "{\"FlagsEnum\":1,\"NoFlagsEnum\":\"One\",\"NullableFlagsEnum\":2,\"NullableNoFlagsEnum\":\"Two\"}";
            var text = JsonSerializer.SerializeToString(item);

            Assert.AreEqual(expected, text);
        }

        [Test]
        public void Can_exclude_default_enums()
        {
            var item = new ClassWithEnums
            {
                FlagsEnum = EnumWithFlags.Zero,
                NoFlagsEnum = EnumWithoutFlags.One,
            };

            Assert.That(item.ToJson(), Is.EqualTo("{\"FlagsEnum\":0,\"NoFlagsEnum\":\"One\"}"));

            JsConfig.IncludeDefaultEnums = false;

            Assert.That(item.ToJson(), Is.EqualTo("{\"NoFlagsEnum\":\"One\"}"));

            JsConfig.Reset();
        }

        public void Should_deserialize_enum()
        {
            Assert.That(JsonSerializer.DeserializeFromString<EnumWithoutFlags>("\"Two\""), Is.EqualTo(EnumWithoutFlags.Two));
        }

        public void Should_handle_empty_enum()
        {
            Assert.That(JsonSerializer.DeserializeFromString<EnumWithoutFlags>(""), Is.EqualTo((EnumWithoutFlags)0));
        }

        [Test]
        public void CanSerializeIntFlag()
        {
            JsConfig.TreatEnumAsInteger = true;
            var val = JsonSerializer.SerializeToString(FlagEnum.A);

            Assert.AreEqual("0", val);
        }

        [Test]
        public void CanSerializeSbyteFlag()
        {
            JsConfig.TryToParsePrimitiveTypeValues = true;
            JsConfig.TreatEnumAsInteger = true;
            JsConfig.IncludeNullValues = true;
            var val = JsonSerializer.SerializeToString(SbyteFlagEnum.A);

            Assert.AreEqual("0", val);
        }

        [Flags]
        public enum FlagEnum
        {
            A,
            B
        }

        [Flags]
        public enum SbyteFlagEnum : sbyte
        {
            A,
            B
        }

        [Flags]
        public enum AnEnum
        {
            This,
            Is,
            An,
            Enum
        }

        [Test]
        public void Can_use_enum_as_key_in_map()
        {
            var dto = new Dictionary<AnEnum, int> { { AnEnum.This, 1 } };
            var json = dto.ToJson();
            json.Print();
            
            var map = json.FromJson<Dictionary<AnEnum, int>>();
            Assert.That(map[AnEnum.This], Is.EqualTo(1));
        }

        public enum EnumStyles
        {
            None=0,
            Word,
            DoubleWord,
            lowerWord,
            Underscore_Words,
        }

        [Test]
        public void Can_serialize_different_enum_styles()
        {
            Assert.That("Word".FromJson<EnumStyles>(), Is.EqualTo(EnumStyles.Word));
            Assert.That("DoubleWord".FromJson<EnumStyles>(), Is.EqualTo(EnumStyles.DoubleWord));
            Assert.That("Underscore_Words".FromJson<EnumStyles>(), Is.EqualTo(EnumStyles.Underscore_Words));

            using (JsConfig.With(new Config { TextCase = TextCase.SnakeCase }))
            {
                Assert.That("Double_Word".FromJson<EnumStyles>(), Is.EqualTo(EnumStyles.DoubleWord));
                Assert.That("Underscore_Words".FromJson<EnumStyles>(), Is.EqualTo(EnumStyles.Underscore_Words));
            }
        }

        [DataContract]
        public class NullableEnum
        {
            [DataMember(Name = "myEnum")]
            public EnumWithoutFlags? MyEnum { get; set; }
        }

        [Test]
        public void Can_deserialize_null_Nullable_Enum()
        {
            JsConfig.ThrowOnError = true;
            string json = @"{""myEnum"":null}";
            var o = json.FromJson<NullableEnum>();
            Assert.That(o.MyEnum, Is.Null);

            JsConfig.Reset();
        }

        [Test]
        public void Does_write_EnumValues_when_ExcludeDefaultValues()
        {
            using (JsConfig.With(new Config { ExcludeDefaultValues = true }))
            {
                Assert.That(new ClassWithEnums
                {
                    NoFlagsEnum = EnumWithoutFlags.One
                }.ToJson(), Is.EqualTo("{\"FlagsEnum\":0,\"NoFlagsEnum\":\"One\"}"));

                Assert.That(new ClassWithEnums
                {
                    NoFlagsEnum = EnumWithoutFlags.Zero
                }.ToJson(), Is.EqualTo("{\"FlagsEnum\":0,\"NoFlagsEnum\":\"Zero\"}"));
            }

            using (JsConfig.With(new Config { ExcludeDefaultValues = true, IncludeDefaultEnums = false }))
            {
                Assert.That(new ClassWithEnums
                {
                    NoFlagsEnum = EnumWithoutFlags.One
                }.ToJson(), Is.EqualTo("{\"NoFlagsEnum\":\"One\"}"));

                Assert.That(new ClassWithEnums
                {
                    NoFlagsEnum = EnumWithoutFlags.Zero
                }.ToJson(), Is.EqualTo("{}"));
            }
        }
        
        [DataContract]
        public enum Day
        {
            [EnumMember(Value = "MON")]
            Monday,
            [EnumMember(Value = "TUE")]
            Tuesday,
            [EnumMember(Value = "WED")]
            Wednesday,
            [EnumMember(Value = "THU")]
            Thursday,
            [EnumMember(Value = "FRI")]
            Friday,
            [EnumMember(Value = "SAT")]
            Saturday,
            [EnumMember(Value = "SUN")]
            Sunday,            
        }

        class EnumMemberDto
        {
            public Day Day { get; set; }

            public Day? NDay { get; set; }
        }

        [Test]
        public void Does_serialize_EnumMember_Value()
        {
            var dto = new EnumMemberDto { Day = Day.Sunday };

            var json = dto.ToJson();
            Assert.That(json, Is.EqualTo("{\"Day\":\"SUN\"}"));
            var fromDto = json.FromJson<EnumMemberDto>();
            Assert.That(fromDto.Day, Is.EqualTo(Day.Sunday));

            var jsv = dto.ToJsv();
            Assert.That(jsv, Is.EqualTo("{Day:SUN}"));
            fromDto = jsv.FromJsv<EnumMemberDto>();
            Assert.That(fromDto.Day, Is.EqualTo(Day.Sunday));

            var csv = dto.ToCsv();
            Assert.That(csv.NormalizeNewLines(), Is.EqualTo("Day,NDay\nSUN,\n".NormalizeNewLines()));
        }

        [Test]
        public void Does_serialize_EnumMember_enum()
        {
            Assert.That(Day.Sunday.ToJson(), Is.EqualTo("\"SUN\""));
            Assert.That(Day.Sunday.ToJsv(), Is.EqualTo("SUN"));
            Assert.That(Day.Sunday.ToCsv(), Is.EqualTo("SUN"));
            
            Assert.That(((Day?)Day.Sunday).ToJson(), Is.EqualTo("\"SUN\""));
            Assert.That(((Day?)Day.Sunday).ToJsv(), Is.EqualTo("SUN"));
            Assert.That(((Day?)Day.Sunday).ToCsv(), Is.EqualTo("SUN"));
        }

        [Test]
        public void Can_deserialize_EnumMember_with_int_value()
        {
            var fromDto = "{\"Day\":1}".FromJson<EnumMemberDto>();
            Assert.That(fromDto.Day, Is.EqualTo(Day.Tuesday));
        }

        public class GetDayOfWeekAsInt
        {
            public DayOfWeek DayOfWeek { get; set; }
        }

        [Test]
        public void Can_override_TreatEnumAsInteger()
        {
            JsConfig.Init(new Config
            {
                TreatEnumAsInteger = false,
            });

            using (JsConfig.With(new Config
            {
                TreatEnumAsInteger = true
            }))
            {
                Assert.That(new GetDayOfWeekAsInt { DayOfWeek = DayOfWeek.Tuesday }.ToJson(), Is.EqualTo("{\"DayOfWeek\":2}"));
                Assert.That("{\"DayOfWeek\":2}".FromJson<GetDayOfWeekAsInt>().DayOfWeek, Is.EqualTo(DayOfWeek.Tuesday));
            }

            Assert.That(new GetDayOfWeekAsInt { DayOfWeek = DayOfWeek.Tuesday }.ToJson(), Is.EqualTo("{\"DayOfWeek\":\"Tuesday\"}"));
            Assert.That("{\"DayOfWeek\":\"Tuesday\"}".FromJson<GetDayOfWeekAsInt>().DayOfWeek, Is.EqualTo(DayOfWeek.Tuesday));
            
            JsConfig.Reset();
        }

        public class FeatureDto
        {
            public LicenseFeature Feature { get; set; }
        }
        
        [Test]
        public void Can_deserialize_Flag_Enum_with_multiple_same_values()
        {
            var key = "{\"Feature\":\"Premium\"}".FromJson<FeatureDto>();
            Assert.That(key.Feature, Is.EqualTo(LicenseFeature.Premium));
        }

    }
}

