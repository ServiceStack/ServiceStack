using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using @DomainModelNamespace@.Validation;
using @DomainModelNamespace@.Validation.Attributes;

namespace @ServiceNamespace@.Tests.Validation
{
	[TestFixture]
	public class RequiredTextValidationTests
	{
		private class ExampleModel1
		{
			public ExampleModel1()
			{
				EmptyString = "";
				NonEmptyString = "NonEmptyString";
			}

			[RequiredText]
			public string EmptyString { get; set; }

			[RequiredText]
			public string NullString { get; set; }

			[RequiredText]
			public string NonEmptyString { get; set; }
		}

		[Test]
		public void NullAndEmptyStringFailsRequiredTextValidationTest()
		{
			var errorFieldNames = new[] { "EmptyString", "NullString" }.ToList();
			var errors = ObjectValidator.ValidateObject(new ExampleModel1()).Errors;
			Assert.That(errors.Count, Is.EqualTo(errorFieldNames.Count));

			Assert.That(errors.ToList().All(x => x.ErrorCode == ErrorCodes.FieldIsRequired.ToString()));
			Assert.That(errors.ToList().All(x => errorFieldNames.Contains(x.FieldName)));
		}
	}
}