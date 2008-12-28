using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using @DomainModelNamespace@.@ServiceName@;
using @DomainModelNamespace@.Validation;
using @DomainModelNamespace@.Validation.Attributes;

namespace @ServiceNamespace@.Tests.Validation
{
	[TestFixture]
	public class CreditCardValidationTests
	{
		private class ExampleModel1
		{
			[ValidCreditCard]
			public CreditCardInfo ValidCardDetails
			{
				get
				{
					return new CreditCardInfo {
						CardType = CreditCardType.Visa,
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
			var errors = ObjectValidator.ValidateObject(new ExampleModel1()).Errors;
			Assert.That(errors.Count, Is.EqualTo(0));
		}

		private class ExampleModel2
		{
			[ValidCreditCard]
			public CreditCardInfo WrongCardTypeForCardNumber
			{
				get
				{
					return new CreditCardInfo {
						CardType = CreditCardType.Mastercard,
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
			var errors = ObjectValidator.ValidateObject(new ExampleModel2()).Errors;
			Assert.That(errors.Count, Is.EqualTo(1));

			Assert.That(errors[0].ErrorCode, Is.EqualTo(ErrorCodes.CreditCardTypeIsInvalid.ToString()));
			Assert.That(errors[0].FieldName, Is.EqualTo("WrongCardTypeForCardNumber"));
		}

		private class ExampleModel3
		{
			[ValidCreditCard]
			public CreditCardInfo CardNumberTooShort
			{
				get
				{
					return new CreditCardInfo {
						CardType = CreditCardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "446261687615222",
						CardCvv = "100",
						ExpiryDate = DateTime.Now.AddYears(1)
					};
				}
			}

			[ValidCreditCard]
			public CreditCardInfo CardNumberTooLong
			{
				get
				{
					return new CreditCardInfo {
						CardType = CreditCardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "44626168761522200",
						CardCvv = "100",
						ExpiryDate = DateTime.Now.AddYears(1)
					};
				}
			}

			[ValidCreditCard]
			public CreditCardInfo CardNumberWithLetters
			{
				get
				{
					return new CreditCardInfo {
						CardType = CreditCardType.Visa,
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
			var errors = ObjectValidator.ValidateObject(new ExampleModel3()).Errors;
			Assert.That(errors.Count, Is.EqualTo(errorFieldNames.Count));

			Assert.That(errors.ToList().All(x => x.ErrorCode == ErrorCodes.CreditCardNumberIsInvalid.ToString()));
			Assert.That(errors.ToList().All(x => errorFieldNames.Contains(x.FieldName)));
		}

		private class ExampleModel4
		{
			[ValidCreditCard]
			public CreditCardInfo ExpiredByOneYear
			{
				get
				{
					return new CreditCardInfo {
						CardType = CreditCardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "4462616876152220",
						CardCvv = "100",
						ExpiryDate = DateTime.Now.AddYears(-1)
					};
				}
			}

			[ValidCreditCard]
			public CreditCardInfo ExpiredByOneMonth
			{
				get
				{
					return new CreditCardInfo {
						CardType = CreditCardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "4462616876152220",
						CardCvv = "100",
						ExpiryDate = DateTime.Now.AddMonths(-1)
					};
				}
			}

			[ValidCreditCard]
			public CreditCardInfo SameMonth
			{
				get
				{
					return new CreditCardInfo {
						CardType = CreditCardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "4462616876152220",
						CardCvv = "100",
						ExpiryDate = DateTime.Now,
					};
				}
			}
			[ValidCreditCard]
			public CreditCardInfo OneMonthToGo
			{
				get
				{
					return new CreditCardInfo {
						CardType = CreditCardType.Visa,
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
			var errors = ObjectValidator.ValidateObject(new ExampleModel4()).Errors;
			Assert.That(errors.Count, Is.EqualTo(errorFieldNames.Count));

			Assert.That(errors.ToList().All(x => x.ErrorCode == ErrorCodes.CreditCardHasExpired.ToString()));
			Assert.That(errors.ToList().All(x => errorFieldNames.Contains(x.FieldName)));
		}

		private class ExampleModel5
		{
			[ValidCreditCard]
			public CreditCardInfo NoCardCvv
			{
				get
				{
					return new CreditCardInfo {
						CardType = CreditCardType.Visa,
						CardHolderName = "FirstName LastName",
						CardNumber = "4462616876152220",
						CardCvv = "",
						ExpiryDate = DateTime.Now.AddYears(1)
					};
				}
			}

			[ValidCreditCard]
			public CreditCardInfo CardCvvTooLong
			{
				get
				{
					return new CreditCardInfo {
						CardType = CreditCardType.Visa,
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
			var errors = ObjectValidator.ValidateObject(new ExampleModel5()).Errors;
			Assert.That(errors.Count, Is.EqualTo(errorFieldNames.Count));

			Assert.That(errors.ToList().All(x => x.ErrorCode == ErrorCodes.CreditCardCvvIsInvalid.ToString()));
			Assert.That(errors.ToList().All(x => errorFieldNames.Contains(x.FieldName)));
		}

		private class ExampleModel6
		{
			[ValidCreditCard]
			public CreditCardInfo NoCardHoldersName
			{
				get
				{
					return new CreditCardInfo {
						CardType = CreditCardType.Visa,
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
			var errors = ObjectValidator.ValidateObject(new ExampleModel6()).Errors;
			Assert.That(errors.Count, Is.EqualTo(errorFieldNames.Count));

			Assert.That(errors.ToList().All(x => x.ErrorCode == ErrorCodes.CreditCardHolderNameIsInvalid.ToString()));
			Assert.That(errors.ToList().All(x => errorFieldNames.Contains(x.FieldName)));
		}
	}
}