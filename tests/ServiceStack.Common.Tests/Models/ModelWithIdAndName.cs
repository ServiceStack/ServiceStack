using System;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Common.Tests.Models
{
	public class ModelWithIdAndName
	{
		public ModelWithIdAndName()
		{
		}

		public ModelWithIdAndName(int id)
		{
			Id = id;
			Name = "Name" + id;
		}

        [AutoIncrement]
		public int Id { get; set; }

		public string Name { get; set; }

		public static ModelWithIdAndName Create(int id)
		{
			return new ModelWithIdAndName(id);
		}

		public static void AssertIsEqual(ModelWithIdAndName actual, ModelWithIdAndName expected)
		{
			if (actual == null || expected == null)
			{
				Assert.That(actual == expected, Is.True);
				return;
			}

			Assert.That(actual.Id, Is.EqualTo(expected.Id));
			Assert.That(actual.Name, Is.EqualTo(expected.Name));
		}

		public bool Equals(ModelWithIdAndName other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return other.Id == Id && Equals(other.Name, Name);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (ModelWithIdAndName)) return false;
			return Equals((ModelWithIdAndName) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Id*397) ^ (Name != null ? Name.GetHashCode() : 0);
			}
		}
	}
}