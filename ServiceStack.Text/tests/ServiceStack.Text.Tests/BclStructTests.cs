using System;
using System.Drawing;
using System.Runtime.Serialization;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
    public class BclStructTests : TestBase
    {
#if !NETCORE
        static BclStructTests()
        {
            JsConfig<System.Drawing.Color>.SerializeFn = c => c.ToString().Replace("Color ", "").Replace("[", "").Replace("]", "");
            JsConfig<System.Drawing.Color>.DeSerializeFn = System.Drawing.Color.FromName;
        }

        [Test]
        public void Can_serialize_Color()
        {
            var color = Color.Red;

            var fromColor = Serialize(color);

            Assert.That(fromColor, Is.EqualTo(color));
        }
#endif
        public enum MyEnum
        {
            Enum1,
            Enum2,
            Enum3,
        }

        [Test]
        public void Can_serialize_arrays_of_enums()
        {
            var enums = new[] { MyEnum.Enum1, MyEnum.Enum2, MyEnum.Enum3 };
            var fromEnums = Serialize(enums);

            Assert.That(fromEnums[0], Is.EqualTo(MyEnum.Enum1));
            Assert.That(fromEnums[1], Is.EqualTo(MyEnum.Enum2));
            Assert.That(fromEnums[2], Is.EqualTo(MyEnum.Enum3));
        }

        [Flags]
        public enum ExampleEnum
        {
            None = 0,
            One = 1,
            Two = 2,
            Four = 4,
            Eight = 8
        }

        public class ExampleType
        {
            public ExampleEnum Enum { get; set; }
            public string EnumValues { get; set; }
            public string Value { get; set; }
            public int Foo { get; set; }
        }

        [Test]
        public void Can_serialize_dto_with_enum_flags()
        {
            var serialized = TypeSerializer.SerializeToString(new ExampleType
            {
                Value = "test",
                Enum = ExampleEnum.One | ExampleEnum.Four,
                EnumValues = (ExampleEnum.One | ExampleEnum.Four).ToDescription(),
                Foo = 1
            });

            var deserialized = TypeSerializer.DeserializeFromString<ExampleType>(serialized);

            Console.WriteLine(deserialized.ToJsv());

            Assert.That(deserialized.Enum, Is.EqualTo(ExampleEnum.One | ExampleEnum.Four));
        }

        [DataContract]
        public class Item
        {
            [DataMember(Name = "favorite")]
            public bool IsFavorite { get; set; }
        }

        [Test]
        public void Can_customize_bool_deserialization()
        {
            var dto1 = "{\"favorite\":1}".FromJson<Item>();
            Assert.That(dto1.IsFavorite, Is.True);

            var dto0 = "{\"favorite\":0}".FromJson<Item>();
            Assert.That(dto0.IsFavorite, Is.False);

            var dtoTrue = "{\"favorite\":true}".FromJson<Item>();
            Assert.That(dtoTrue.IsFavorite, Is.True);

            var dtoFalse = "{\"favorite\":false}".FromJson<Item>();
            Assert.That(dtoFalse.IsFavorite, Is.False);
        }

        [Test]
        public void GetUnderlyingTypeCode_tests()
        {
            //without explicit putting namespace 'System' before TypeCode test fails on .NET Core
            Assert.That(Type.GetTypeCode(typeof(int)), Is.EqualTo(System.TypeCode.Int32));
            Assert.That(Type.GetTypeCode(typeof(int?)), Is.EqualTo(System.TypeCode.Object));
            Assert.That(Type.GetTypeCode(typeof(string)), Is.EqualTo(System.TypeCode.String));
            Assert.That(Type.GetTypeCode(typeof(TypeCode)), Is.EqualTo(System.TypeCode.Int32)); //enum

            Assert.That(typeof(int).GetUnderlyingTypeCode(), Is.EqualTo(TypeCode.Int32));
            Assert.That(typeof(int?).GetUnderlyingTypeCode(), Is.EqualTo(TypeCode.Int32));
            Assert.That(typeof(float?).GetUnderlyingTypeCode(), Is.EqualTo(TypeCode.Single));
            Assert.That(typeof(double?).GetUnderlyingTypeCode(), Is.EqualTo(TypeCode.Double));
            Assert.That(typeof(decimal?).GetUnderlyingTypeCode(), Is.EqualTo(TypeCode.Decimal));
            Assert.That(typeof(DateTime?).GetUnderlyingTypeCode(), Is.EqualTo(TypeCode.DateTime));
        }
    }
}