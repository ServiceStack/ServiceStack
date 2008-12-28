using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using @DomainModelNamespace@.@ServiceName@;
using @DomainModelNamespace@.Validation;

namespace @ServiceNamespace@.Tests.Logic.Validation
{
	[TestFixture]
	public class @ModelName@DetailsValidationTests : ValidationTestBase
	{
		static @ModelName@Details Valid@ModelName@Details
		{
			get
			{
				return new @ModelName@Details {
					GlobalId = Guid.NewGuid(),
					@ModelName@Name = "@ModelName@DetailsValidationTests",
					Email = "@ModelName@DetailsValidationTests@host.com",
					FirstName = "@ModelName@Details",
					LastName = "ValidationTests",
					Country = "Country",
					LanguageCode = "en",
					Title = "Title",
					SingleClickBuyEnabled = true,
					CanNotifyEmail = true,
				};
			}
		}

		[Test]
		public void Valid@ModelName@DetailsPassesValidation()
		{
			Assert.That(Valid@ModelName@Details.Validate().Errors.Count, Is.EqualTo(0));
		}

		[Test]
		public void Empty@ModelName@DetailsProvidesCorrectValidationErrors()
		{
			var requiredFields = new[] { "@ModelName@Name", "FirstName", "Title", "LastName", "Country", "LanguageCode" }.ToList();
			var validEmailFields = new[] { "Email" }.ToList();
			
			var empty@ModelName@Details = new @ModelName@Details();

			var errorMap = CreateErrorMap(empty@ModelName@Details.Validate().Errors);
			Assert.That(errorMap[ErrorCodes.FieldIsRequired.ToString()].Count, Is.EqualTo(requiredFields.Count));
			Assert.That(errorMap[ErrorCodes.EmailAddressIsNotValid.ToString()].Count, Is.EqualTo(validEmailFields.Count));

			var actualRequiredFields = errorMap[ErrorCodes.FieldIsRequired.ToString()].Select(x => x.FieldName);
			Assert.That(actualRequiredFields, Is.EquivalentTo(requiredFields));

			var actualValidEmailFields = errorMap[ErrorCodes.EmailAddressIsNotValid.ToString()].Select(x => x.FieldName);
			Assert.That(actualValidEmailFields, Is.EquivalentTo(validEmailFields));

			Assert.That(errorMap.Values.SelectMany(x => x).Count(), 
				Is.EqualTo(requiredFields.Count + validEmailFields.Count));
		}
	}
}