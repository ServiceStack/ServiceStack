using System;

namespace ServiceStack.Validation.Model
{
	public class ValidatableCreditCard : IValidatableCreditCard
	{
		public CardType CardType { get; set; }
		public string CardHolderName { get; set; }
		public string CardNumber { get; set; }
		public string CardCvv { get; set; }
		public DateTime ExpiryDate { get; set; }
	}
}