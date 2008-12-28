using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using @DomainModelNamespace@.Validation;
using @DomainModelNamespace@.Validation.Attributes;

namespace @ServiceNamespace@.Tests.Validation
{
	[TestFixture]
	public class BaseValidationTests
	{
		private class ExampleModel1
		{
			[NotNull]
			public string EmptyText { get; set; }
		}

		[Test]
		public void SingleNotNullValidationTest()
		{
			var result = ObjectValidator.ValidateObject(new ExampleModel1());
			Assert.That(result.Errors.Count, Is.EqualTo(1));

			var error = result.Errors[0];
			Assert.That(error.ErrorCode, Is.EqualTo(ErrorCodes.FieldIsRequired.ToString()));
			Assert.That(error.FieldName, Is.EqualTo("EmptyText"));
		}

		private class ExampleModel2
		{
			[NotNull]
			public string PublicProperty { get; set; }

			[NotNull]
			private string PrivateProperty { get; set; }

			[NotNull]
			protected string ProtectedProperty { get; set; }
		}


		[Test]
		public void OnlyPublicPropertiesAreValidatedTest()
		{
			var result = ObjectValidator.ValidateObject(new ExampleModel2());
			Assert.That(result.Errors.Count, Is.EqualTo(1));

			var error = result.Errors[0];
			Assert.That(error.ErrorCode, Is.EqualTo(ErrorCodes.FieldIsRequired.ToString()));
			Assert.That(error.FieldName, Is.EqualTo("PublicProperty"));
		}
	}
}