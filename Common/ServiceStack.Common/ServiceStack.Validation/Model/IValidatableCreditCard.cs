using System;

namespace ServiceStack.Validation.Model
{
	public interface IValidatableCreditCard
	{
		CardType CardType { get; set; }
		string CardHolderName { get; set; }
		string CardNumber { get; set; }
		string CardCvv { get; set; }
		DateTime ExpiryDate { get; set; }
	}
}