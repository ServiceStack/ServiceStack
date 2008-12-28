using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Sakila.DomainModel;

namespace ServiceStack.Sakila.Tests.Logic.Validation
{
	[TestFixture]
	public class CustomerDetailsValidationTests : ValidationTestBase
	{
		static Customer ValidCustomerDetails
		{
			get
			{
				return new Customer {
					Email = "CustomerDetailsValidationTests@host.com",
					FirstName = "CustomerDetails",
					LastName = "ValidationTests",
				};
			}
		}

		[Test]
		public void ValidCustomerDetailsPassesValidation()
		{
			Assert.That(ValidCustomerDetails.Validate().Errors.Count, Is.EqualTo(0));
		}

		[Test]
		public void EmptyCustomerDetailsProvidesCorrectValidationErrors()
		{
			var requiredFields = new[] { "CustomerName", "FirstName", "Title", "LastName", "CountryName", "LanguageCode" }.ToList();
			var validEmailFields = new[] { "Email" }.ToList();
			
			var emptyCustomerDetails = new Customer();

			var errorMap = CreateErrorMap(emptyCustomerDetails.Validate().Errors);
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