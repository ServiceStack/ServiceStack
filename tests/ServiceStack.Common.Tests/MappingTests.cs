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
	}
}
