using System.Linq;
using System.Runtime.Serialization;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Utils;
using ServiceStack.Validation.Validators;

namespace ServiceStack.Common.Tests
{
	[TestFixture]
	public class ReflectionUtilTests
	{
		public class TestClass
		{
			[NotNull]
			public string Member1 { get; set; }

			public string Member2 { get; set; }

			[NotNull]
			public string Member3 { get; set; }

			[RequiredText]
			public string Member4 { get; set; }
		}

		[Test]
		public void GetTest()
		{
			var propertyAttributes = ReflectionUtils.GetPropertyAttributes<NotNullAttribute>(typeof(TestClass));
			var propertyNames = propertyAttributes.ToList().ConvertAll(x => x.Key.Name);
			Assert.That(propertyNames, Is.EquivalentTo(new[] { "Member1", "Member3" }));
		}
	}
}
