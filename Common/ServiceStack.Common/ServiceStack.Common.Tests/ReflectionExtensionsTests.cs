using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Tests.Text;

namespace ServiceStack.Common.Tests
{
	public class TestClassA
	{
		public IList<string> ToStringList { get; set; }
		public ArrayOfString FromStringList { get; set; }
	}

	public class TestClassB
	{
		public ArrayOfString ToStringList { get; set; }
		public IList<string> FromStringList { get; set; }
	}

	[TestFixture]
	public class ReflectionExtensionsTests
	{
		[Test]
		public void Can_translate_generic_lists()
		{
			var values = new[] {"A", "B", "C"};
			var testA = new TestClassA {
				FromStringList = new ArrayOfString(values),
				ToStringList = new List<string>(values),
			};

			var fromTestA = testA.TranslateTo<TestClassB>();

			AssertAreEqual(testA, fromTestA);

			var testB = new TestClassB {
				FromStringList = new List<string>(values),
				ToStringList = new ArrayOfString(values),
			};

			var fromTestB = testB.TranslateTo<TestClassA>();
			AssertAreEqual(fromTestB, testB);
		}

		private static void AssertAreEqual(TestClassA testA, TestClassB testB)
		{
			Assert.That(testA, Is.Not.Null);
			Assert.That(testB, Is.Not.Null);

			Assert.That(testA.FromStringList, Is.Not.Null);
			Assert.That(testB.FromStringList, Is.Not.Null);
			Assert.That(testA.FromStringList, 
				Is.EquivalentTo(new List<string>(testB.FromStringList)));

			Assert.That(testA.ToStringList, Is.Not.Null);
			Assert.That(testB.ToStringList, Is.Not.Null);
			Assert.That(testA.ToStringList, Is.EquivalentTo(testB.ToStringList));
		}
	}
}