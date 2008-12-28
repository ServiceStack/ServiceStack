using Ddn.Common.DesignPatterns.Translator;
using Ddn.Common.Services.Extensions;
using @DomainModelNamespace@.@ServiceName@;
using DtoTypes = @ServiceModelNamespace@.Version100.Types;

namespace @ServiceNamespace@.ServiceTranslators.Version100.ServiceToDomain
{
	public class CreditCardTranslator : ITranslator<CreditCardInfo, DtoTypes.CreditCardInfo>
	{
		public static readonly CreditCardTranslator Instance = new CreditCardTranslator();

		public CreditCardInfo Parse(DtoTypes.CreditCardInfo from)
		{
			if (from == null) return null;
			var to = new CreditCardInfo {
				CardType = from.CardType != null ? from.CardType.ToEnum<CreditCardType>() : CreditCardType.Unknown,
				CardHolderName = from.CardHolderName,
				CardNumber = from.CardNumber,
				CardCvv = from.CardCvv,
				ExpiryDate = from.ExpiryDate,

				IssueNo = from.IssueNo,
				IssueDate = from.IssueDate,

				BillingAddressLine1 = from.BillingAddressLine1,
				BillingAddressLine2 = from.BillingAddressLine2,
				BillingAddressTown = from.BillingAddressTown,
				BillingAddressCounty = from.BillingAddressCounty,
				BillingAddressPostCode = from.BillingAddressPostCode,
			};
			return to;
		}
	}
}