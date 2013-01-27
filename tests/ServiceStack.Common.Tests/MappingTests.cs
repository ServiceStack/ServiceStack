using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
    public class User
    {
        public string FirstName { get; set; }
        [DataMember]
        public string LastName { get; set; }
        public Car Car { get; set; }
    }

    public class UserFields
    {
        public string FirstName;
        public string LastName;
        public Car Car;
    }

    public class SubUser : User { }
    public class SubUserFields : UserFields { }

    public class Car
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class UserDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Car { get; set; }
    }

    public enum Color
    {
        Red,
        Green,
        Blue
    }

    public enum OtherColor
    {
        Red,
        Green,
        Blue
    }


    public class IntNullableId
    {
        public int? Id { get; set; }
    }

    public class IntId
    {
        public int Id { get; set; }
    }

    public class BclTypes
    {
        public int Int { get; set; }
        public long Long { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
    }

    public class BclTypeStrings
    {
        public string Int { get; set; }
        public string Long { get; set; }
        public string Double { get; set; }
        public string Decimal { get; set; }
    }

    public class NullableConversion
    {
        public decimal Amount { get; set; }
    }

    public class NullableConversionDto
    {
        public decimal? Amount { get; set; }
    }

    public class NullableEnumConversion
    {
        public Color Color { get; set; }
    }

    public class EnumConversion
    {
        public Color Color { get; set; }
    }

    public class NullableEnumConversionDto
    {
        public OtherColor? Color { get; set; }
    }

    public class EnumConversionDto
    {
        public OtherColor Color { get; set; }
    }

    public class EnumConversionStringDto
    {
        public string Color { get; set; }
    }

    public class EnumConversionIntDto
    {
        public int Color { get; set; }
    }

    [TestFixture]
    public class MappingTests
    {
        [Test]
        public void Does_populate()
        {
            var user = new User() {
                FirstName = "Demis",
                LastName = "Bellot",
                Car = new Car() { Name = "BMW X6", Age = 3 }
            };

            var userDto = new UserDto().PopulateWith(user);

            Assert.That(userDto.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(userDto.LastName, Is.EqualTo(user.LastName));
            Assert.That(userDto.Car, Is.EqualTo("{Name:BMW X6,Age:3}"));
        }

        [Test]
        public void Does_translate()
        {
            var user = new User() {
                FirstName = "Demis",
                LastName = "Bellot",
                Car = new Car() { Name = "BMW X6", Age = 3 }
            };

            var userDto = user.TranslateTo<UserDto>();

            Assert.That(userDto.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(userDto.LastName, Is.EqualTo(user.LastName));
            Assert.That(userDto.Car, Is.EqualTo("{Name:BMW X6,Age:3}"));
        }

        [Test]
        public void Does_enumstringconversion_translate()
        {
            var conversion = new EnumConversion { Color = Color.Blue };
            var conversionDto = conversion.TranslateTo<EnumConversionStringDto>();

            Assert.That(conversionDto.Color, Is.EqualTo("Blue"));
        }

        [Test]
        public void Does_enumintconversion_translate()
        {
            var conversion = new EnumConversion { Color = Color.Green };
            var conversionDto = conversion.TranslateTo<EnumConversionIntDto>();

            Assert.That(conversionDto.Color, Is.EqualTo(1));
        }

        [Test]
        public void Does_nullableconversion_translate()
        {
            var conversion = new NullableConversion { Amount = 123.45m };
            var conversionDto = conversion.TranslateTo<NullableConversionDto>();

            Assert.That(conversionDto.Amount, Is.EqualTo(123.45m));
        }

        [Test]
        public void Does_Enumnullableconversion_translate()
        {
            var conversion = new NullableEnumConversion { Color = Color.Green };
            var conversionDto = conversion.TranslateTo<NullableEnumConversionDto>();

            Assert.That(conversionDto.Color, Is.EqualTo(OtherColor.Green));

        }

        [Test]
        public void Does_Enumconversion_translate()
        {
            var conversion = new NullableEnumConversion { Color = Color.Green };
            var conversionDto = conversion.TranslateTo<EnumConversionDto>();

            Assert.That(conversionDto.Color, Is.EqualTo(OtherColor.Green));

        }

        [Test]
        public void Does_translate_nullableInt_to_and_from()
        {
            var nullable = new IntNullableId();

            var nonNullable = nullable.TranslateTo<IntId>();

            nonNullable.Id = 10;

            var expectedNullable = nonNullable.TranslateTo<IntNullableId>();

            Assert.That(expectedNullable.Id.Value, Is.EqualTo(nonNullable.Id));
        }

        [Test]
        public void Does_translate_from_properties_to_fields()
        {
            var user = new User {
                FirstName = "Demis",
                LastName = "Bellot",
                Car = new Car { Name = "BMW X6", Age = 3 }
            };

            var to = user.TranslateTo<UserFields>();
            Assert.That(to.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(to.LastName, Is.EqualTo(user.LastName));
            Assert.That(to.Car.Name, Is.EqualTo(user.Car.Name));
            Assert.That(to.Car.Age, Is.EqualTo(user.Car.Age));
        }

        [Test]
        public void Does_translate_from_fields_to_properties()
        {
            var user = new UserFields {
                FirstName = "Demis",
                LastName = "Bellot",
                Car = new Car { Name = "BMW X6", Age = 3 }
            };

            var to = user.TranslateTo<UserFields>();
            Assert.That(to.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(to.LastName, Is.EqualTo(user.LastName));
            Assert.That(to.Car.Name, Is.EqualTo(user.Car.Name));
            Assert.That(to.Car.Age, Is.EqualTo(user.Car.Age));
        }

        [Test]
        public void Does_translate_from_inherited_propeties()
        {
            var user = new SubUser {
                FirstName = "Demis",
                LastName = "Bellot",
                Car = new Car { Name = "BMW X6", Age = 3 }
            };

            var to = user.TranslateTo<UserFields>();
            Assert.That(to.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(to.LastName, Is.EqualTo(user.LastName));
            Assert.That(to.Car.Name, Is.EqualTo(user.Car.Name));
            Assert.That(to.Car.Age, Is.EqualTo(user.Car.Age));
        }

        [Test]
        public void Does_translate_to_inherited_propeties()
        {
            var user = new User {
                FirstName = "Demis",
                LastName = "Bellot",
                Car = new Car { Name = "BMW X6", Age = 3 }
            };

            var to = user.TranslateTo<SubUserFields>();
            Assert.That(to.FirstName, Is.EqualTo(user.FirstName));
            Assert.That(to.LastName, Is.EqualTo(user.LastName));
            Assert.That(to.Car.Name, Is.EqualTo(user.Car.Name));
            Assert.That(to.Car.Age, Is.EqualTo(user.Car.Age));
        }

        [Test]
        public void Does_coerce_from_BclTypes_to_strings()
        {
            var from = new BclTypes {
                Int = 1,
                Long = 2,
                Double = 3.3,
                Decimal = 4.4m,                
            };

            var to = from.TranslateTo<BclTypeStrings>();
            Assert.That(to.Int, Is.EqualTo("1"));
            Assert.That(to.Long, Is.EqualTo("2"));
            Assert.That(to.Double, Is.EqualTo("3.3"));
            Assert.That(to.Decimal, Is.EqualTo("4.4"));
        }

        [Test]
        public void Does_coerce_from_strings_to_BclTypes()
        {
            var from = new BclTypeStrings {
                Int = "1",
                Long = "2",
                Double = "3.3",
                Decimal = "4.4",
            };

            var to = from.TranslateTo<BclTypes>();
            Assert.That(to.Int, Is.EqualTo(1));
            Assert.That(to.Long, Is.EqualTo(2));
            Assert.That(to.Double, Is.EqualTo(3.3d));
            Assert.That(to.Decimal, Is.EqualTo(4.4m));
        }

        [Test]
        public void Does_map_only_properties_with_specified_Attribute()
        {
            var user = new User {
                FirstName = "Demis",
                LastName = "Bellot",
                Car = new Car { Name = "BMW X6", Age = 3 }
            };

            var to = new User();
            to.PopulateFromPropertiesWithAttribute(user, typeof(DataMemberAttribute));

            Assert.That(to.LastName, Is.EqualTo(user.LastName));
            Assert.That(to.FirstName, Is.Null);
            Assert.That(to.Car, Is.Null);
        }
    }
}
