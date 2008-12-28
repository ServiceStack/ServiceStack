using System;
using ServiceStack.Validation.Model;

namespace ServiceStack.Validation
{
	/// <summary>
	/// Validates IValidatableCreditCard properties.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class ValidCreditCardAttribute : ValidationAttributeBase
	{
		/// <summary>
		/// Determines whether the credit card provided is valid.
		/// </summary>
		/// <param name="value">An instance of IValidatableCreditCard.</param>
		/// <returns>
		/// 	<c>true</c> if the specified value is valid; otherwise, <c>false</c>.
		/// </returns>
		public override string Validate(object value)
		{
			var cardInfo = (IValidatableCreditCard)value;

			if (string.IsNullOrEmpty(cardInfo.CardNumber) || cardInfo.CardNumber.Length != 16)
			{
				return CreditCardErrorCodes.CreditCardNumberIsInvalid.ToString();
			}
			if (!LuhnTest(cardInfo.CardNumber))
			{
				return CreditCardErrorCodes.CreditCardNumberIsInvalid.ToString();
			}
			if (cardInfo.CardType != CalculateCardType(cardInfo.CardNumber))
			{
				return CreditCardErrorCodes.CreditCardTypeIsInvalid.ToString();
			}
			if (cardInfo.ExpiryDate.Year < DateTime.Today.Year
			    || (cardInfo.ExpiryDate.Year == DateTime.Today.Year && cardInfo.ExpiryDate.Month < DateTime.Today.Month))
			{
				return CreditCardErrorCodes.CreditCardHasExpired.ToString();
			}
			if (string.IsNullOrEmpty(cardInfo.CardCvv) || cardInfo.CardCvv.Length < 3 || cardInfo.CardCvv.Length > 4)
			{
				return CreditCardErrorCodes.CreditCardCvvIsInvalid.ToString();
			}
			if (string.IsNullOrEmpty(cardInfo.CardHolderName))
			{
				return CreditCardErrorCodes.CreditCardHolderNameIsInvalid.ToString();
			}
			return null;
		}

		/// <summary>
		/// Calculates the type of the card based on the card number.
		/// </summary>
		/// <param name="cardNo">The card no.</param>
		/// <returns></returns>
		private static CardType CalculateCardType(string cardNo)
		{
			if (Convert.ToInt32(cardNo.Substring(0, 2)) >= 51 && Convert.ToInt32(cardNo.Substring(0, 2)) <= 55)
				return CardType.Mastercard;
			if (Convert.ToString(cardNo.Substring(0, 1)) == "4")
				return CardType.Visa;
			if (Convert.ToString(cardNo.Substring(0, 2)) == "34" || Convert.ToString(cardNo.Substring(0, 2)) == "37")
				return CardType.Amex;
			return CardType.Unknown;
		}

		/// <summary>
		/// The standard credit card Luhn test
		/// </summary>
		/// <param name="cardNo">The credit card number</param>
		/// <returns>A boolean value representing whether the given number passes the Luhn test</returns>
		private static bool LuhnTest(string cardNo)
		{
			int indicator = 1; // will be indicator for every other number
			int firstNumToAdd = 0; // will be used to store sum of first set of numbers
			int secondNumToAdd = 0; // will be used to store second set of numbers
			string num1; // will be used if every other number added is greater than 10, store the left-most integer here
			string num2; // will be used if ever yother number added is greater than 10, store the right-most integer here

			// Convert our creditNo string to a char array
			char[] ccArr = cardNo.ToCharArray();

			for (int i=ccArr.Length - 1; i >= 0; i--)
			{
				char ccNoAdd = ccArr[i];
				int ccAdd;
				if (!Int32.TryParse(ccNoAdd.ToString(), out ccAdd)) return false;
				if (indicator == 1)
				{
					// If we are on the odd number of numbers, add that number to our total
					firstNumToAdd += ccAdd;
					// set our indicator to 0 so that our code will know to skip to the next piece
					indicator = 0;
				}
				else
				{
					// if the current integer doubled is greater than 10
					// split the sum in to two integers and add them together
					// we then add it to our total here
					if ((ccAdd + ccAdd) >= 10)
					{
						int temporary = (ccAdd + ccAdd);
						num1 = temporary.ToString().Substring(0, 1);
						num2 = temporary.ToString().Substring(1, 1);
						secondNumToAdd += (Convert.ToInt32(num1) + Convert.ToInt32(num2));
					}
					else
					{
						// otherwise, just add them together and add them to our total
						secondNumToAdd += ccAdd + ccAdd;
					}
					// set our indicator to 1 so for the next integer we will perform a different set of code
					indicator = 1;
				}
			}
			// If the sum of our 2 numbers is divisible by 10, then the card is valid. Otherwise, it is not
			bool isValid = (firstNumToAdd + secondNumToAdd) % 10 == 0;
			return isValid;
		}
	}
}