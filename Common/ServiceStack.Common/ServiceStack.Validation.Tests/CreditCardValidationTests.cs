using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Validation;
using ServiceStack.Validation.Model;

namespace ServiceStack.Validation.Tests
{
	[TestFixture]
	public class CreditCardValidationTests
	{
		private class ExampleModel1
		{
			[ValidCreditCard]
			public ValidatableCreditCard ValidCardDetails
			{
				get
				{
					return new ValidatableCreditCard {
						CardType = CardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "4462616876152220",
						CardCvv = "100",
						ExpiryDate = DateTime.Now.AddYears(1)
					};
				}
			}
		}

		[Test]
		public void ValidCardInfoPasses()
		{
			var errors = ModelValidator.ValidateObject(new ExampleModel1()).Errors;
			Assert.That(errors.Count, Is.EqualTo(0));
		}

		private class ExampleModel2
		{
			[ValidCreditCard]
			public ValidatableCreditCard WrongCardTypeForCardNumber
			{
				get
				{
					return new ValidatableCreditCard {
						CardType = CardType.Mastercard,
						CardHolderName = "FirstName LastName",
						CardNumber = "4462616876152220",
						CardCvv = "100",
						ExpiryDate = DateTime.Now.AddYears(1)
					};
				}
			}
		}

		[Test]
		public void UsingWrongCardTypeFails()
		{
			var errors = ModelValidator.ValidateObject(new ExampleModel2()).Errors;
			Assert.That(errors.Count, Is.EqualTo(1));

			Assert.That(errors[0].ErrorCode, Is.EqualTo(CreditCardErrorCodes.CreditCardTypeIsInvalid.ToString()));
			Assert.That(errors[0].FieldName, Is.EqualTo("WrongCardTypeForCardNumber"));
		}

		private class ExampleModel3
		{
			[ValidCreditCard]
			public ValidatableCreditCard CardNumberTooShort
			{
				get
				{
					return new ValidatableCreditCard {
						CardType = CardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "446261687615222",
						CardCvv = "100",
						ExpiryDate = DateTime.Now.AddYears(1)
					};
				}
			}

			[ValidCreditCard]
			public ValidatableCreditCard CardNumberTooLong
			{
				get
				{
					return new ValidatableCreditCard {
						CardType = CardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "44626168761522200",
						CardCvv = "100",
						ExpiryDate = DateTime.Now.AddYears(1)
					};
				}
			}

			[ValidCreditCard]
			public ValidatableCreditCard CardNumberWithLetters
			{
				get
				{
					return new ValidatableCreditCard {
						CardType = CardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "A462616876152220",
						CardCvv = "100",
						ExpiryDate = DateTime.Now.AddYears(1)
					};
				}
			}
		}

		[Test]
		public void UsingInvalidCardNumbersFails()
		{
			var errorFieldNames = new[] {
			                            	"CardNumberTooShort", "CardNumberTooLong", "CardNumberWithLetters" }.ToList();
			var errors = ModelValidator.ValidateObject(new ExampleModel3()).Errors;
			Assert.That(errors.Count, Is.EqualTo(errorFieldNames.Count));

			Assert.That(errors.ToList().All(x => x.ErrorCode == CreditCardErrorCodes.CreditCardNumberIsInvalid.ToString()));
			Assert.That(errors.ToList().All(x => errorFieldNames.Contains(x.FieldName)));
		}

		private class ExampleModel4
		{
			[ValidCreditCard]
			public ValidatableCreditCard ExpiredByOneYear
			{
				get
				{
					return new ValidatableCreditCard {
						CardType = CardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "4462616876152220",
						CardCvv = "100",
						ExpiryDate = DateTime.Now.AddYears(-1)
					};
				}
			}

			[ValidCreditCard]
			public ValidatableCreditCard ExpiredByOneMonth
			{
				get
				{
					return new ValidatableCreditCard {
						CardType = CardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "4462616876152220",
						CardCvv = "100",
						ExpiryDate = DateTime.Now.AddMonths(-1)
					};
				}
			}

			[ValidCreditCard]
			public ValidatableCreditCard SameMonth
			{
				get
				{
					return new ValidatableCreditCard {
						CardType = CardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "4462616876152220",
						CardCvv = "100",
						ExpiryDate = DateTime.Now,
					};
				}
			}
			[ValidCreditCard]
			public ValidatableCreditCard OneMonthToGo
			{
				get
				{
					return new ValidatableCreditCard {
						CardType = CardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "4462616876152220",
						CardCvv = "100",
						ExpiryDate = DateTime.Now.AddMonths(1),
					};
				}
			}
		}

		[Test]
		public void CardWithExpiredDateFails()
		{
			var errorFieldNames = new[] { "ExpiredByOneYear", "ExpiredByOneMonth" }.ToList();
			var errors = ModelValidator.ValidateObject(new ExampleModel4()).Errors;
			Assert.That(errors.Count, Is.EqualTo(errorFieldNames.Count));

			Assert.That(errors.ToList().All(x => x.ErrorCode == CreditCardErrorCodes.CreditCardHasExpired.ToString()));
			Assert.That(errors.ToList().All(x => errorFieldNames.Contains(x.FieldName)));
		}

		private class ExampleModel5
		{
			[ValidCreditCard]
			public ValidatableCreditCard NoCardCvv
			{
				get
				{
					return new ValidatableCreditCard {
						CardType = CardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "4462616876152220",
						CardCvv = "",
						ExpiryDate = DateTime.Now.AddYears(1)
					};
				}
			}

			[ValidCreditCard]
			public ValidatableCreditCard CardCvvTooLong
			{
				get
				{
					return new ValidatableCreditCard {
						CardType = CardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "4462616876152220",
						CardCvv = "12345",
						ExpiryDate = DateTime.Now.AddYears(1)
					};
				}
			}
		}

		[Test]
		public void UsingWrongCardCvvFails()
		{
			var errorFieldNames = new[] { "NoCardCvv", "CardCvvTooLong" }.ToList();
			var errors = ModelValidator.ValidateObject(new ExampleModel5()).Errors;
			Assert.That(errors.Count, Is.EqualTo(errorFieldNames.Count));

			Assert.That(errors.ToList().All(x => x.ErrorCode == CreditCardErrorCodes.CreditCardCvvIsInvalid.ToString()));
			Assert.That(errors.ToList().All(x => errorFieldNames.Contains(x.FieldName)));
		}

		private class ExampleModel6
		{
			[ValidCreditCard]
			public ValidatableCreditCard NoCardHoldersName
			{
				get
				{
					return new ValidatableCreditCard {
						CardType = CardType.Visa,
						CardHolderName = "",
						CardNumber = "4462616876152220",
						CardCvv = "100",
						ExpiryDate = DateTime.Now.AddYears(1)
					};
				}
			}
		}

		[Test]
		public void UsingWrongCardHoldersNameFails()
		{
			var errorFieldNames = new[] { "NoCardHoldersName" }.ToList();
			var errors = ModelValidator.ValidateObject(new ExampleModel6()).Errors;
			Assert.That(errors.Count, Is.EqualTo(errorFieldNames.Count));

			Assert.That(errors.ToList().All(x => x.ErrorCode == CreditCardErrorCodes.CreditCardHolderNameIsInvalid.ToString()));
			Assert.That(errors.ToList().All(x => errorFieldNames.Contains(x.FieldName)));
		}
	}
}