using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.OrmLite.Tests.Models
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

		public int Id { get; set; }

		public string Name { get; set; }

		public static ModelWithIdAndName Create(int id)
		{
			return new ModelWithIdAndName(id);
		}

		public static void AssertIsEqual(ModelWithIdAndName actual, ModelWithIdAndName expected)
		{
			Assert.That(actual.Id, Is.EqualTo(expected.Id));
			Assert.That(actual.Name, Is.EqualTo(expected.Name));
		}
	}
}