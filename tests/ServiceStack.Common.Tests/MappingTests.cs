using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ServiceStack.Common.Tests
{
	public class User
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public Car Car { get; set; }
	}

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


	public class IntNullableIDObj
	{
		public int? Id { get; set; }
	}

	public class IntIDObj
	{
		public int Id { get; set; }
	}

    public class NullableConversion
    {
        public decimal Amount { get; set; }
    }

    public class NullableConversionDto
    {
        public decimal? Amount { get; set; }
    }

    public class EnumConversion
    {
        public Color Color { get; set; }
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
			var user = new User()
			{
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
			var user = new User()
			{
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
		public void Does_translate_nullableInt_to_and_from()
		{
			var nullable = new IntNullableIDObj();

			var nonNullable = nullable.TranslateTo<IntIDObj>();

			nonNullable.Id = 10;

			var expectedNullable = nonNullable.TranslateTo<IntNullableIDObj>();

			Assert.That(expectedNullable.Id.Value, Is.EqualTo(nonNullable.Id));
		}
	}
}
