using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.OrmLite.Tests.Models
{
	public class ModelWithComplexTypes
	{
		public ModelWithComplexTypes()
		{
			this.StringValues = new List<string>();
			this.IntValues = new List<int>();
			this.StringMap = new Dictionary<string, string>();
			this.IntMap = new Dictionary<int, int>();
		}

		public long Id { get; set; }

		public List<string> StringValues { get; set; }

		public List<int> IntValues { get; set; }

		public Dictionary<string, string> StringMap { get; set; }

		public Dictionary<int, int> IntMap { get; set; }

		public static ModelWithComplexTypes Create(int id)
		{
			var row = new ModelWithComplexTypes {
				Id = id,
				StringValues = { "val1", "val2", "val3" },
				IntValues = { 1, 2, 3 },
				StringMap =
					{
						{"key1", "val1"},
						{"key2", "val2"},
						{"key3", "val3"},
					},
				IntMap =
					{
						{1, 2},
						{3, 4},
						{5, 6},
					},
			};

			return row;
		}

		public static void AssertIsEqual(ModelWithComplexTypes actual, ModelWithComplexTypes expected)
		{
			Assert.That(actual.Id, Is.EqualTo(expected.Id));
			Assert.That(actual.StringValues, Is.EquivalentTo(expected.StringValues));
			Assert.That(actual.IntValues, Is.EquivalentTo(expected.IntValues));
			Assert.That(actual.StringMap, Is.EquivalentTo(expected.StringMap));
			Assert.That(actual.IntMap, Is.EquivalentTo(expected.IntMap));
		}
	}
}