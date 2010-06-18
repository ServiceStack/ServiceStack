using System;
using NUnit.Framework;

namespace ServiceStack.CacheAccess.Memcached.Tests
{
	[Serializable]
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
	}
}