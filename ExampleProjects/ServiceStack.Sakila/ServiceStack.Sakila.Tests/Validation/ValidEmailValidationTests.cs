using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Sakila.DomainModel;
using ServiceStack.Validation;

namespace ServiceStack.Sakila.Tests.Validation
{
	[TestFixture]
	public class ValidEmailValidationTests
	{
		private class ExampleModel1
		{
			[ValidEmail]
			public string ValidEmail { get { return "user@domain.com"; } }

			[ValidEmail]
			public string EmailWithNumbersOnly { get { return "123@456.798"; } }

			[ValidEmail]
			public string EmailWithoutTld { get { return "user@domain"; } }

			[ValidEmail]
			public string EmptyString { get { return ""; } }

			[ValidEmail]
			public string SimpleText { get { return "user"; } }

			[ValidEmail] 
			public string EmailWithPlusChar { get { return "user+last@domain.com"; } }
		}


		[Test]
		public void ValidEmailValidationTest()
		{
			var errorFieldNames = new[] {
				"EmailWithoutTld", "EmptyString", "SimpleText", "EmailWithPlusChar" }.ToList();
			var errors = ModelValidator.ValidateObject(new ExampleModel1()).Errors;
			Assert.That(errors.Count, Is.EqualTo(errorFieldNames.Count));

			Assert.That(errors.ToList().All(x => x.ErrorCode == ErrorCodes.EmailAddressIsNotValid.ToString()));
			Assert.That(errors.ToList().All(x => errorFieldNames.Contains(x.FieldName)));
		}
	}
}