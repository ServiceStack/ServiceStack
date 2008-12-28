using Ddn.Common.DesignPatterns.Translator;
using @DomainModelNamespace@.@ServiceName@;
using Dto = @ServiceModelNamespace@.Version100.Types;

namespace @ServiceNamespace@.ServiceTranslators.Version100.ServiceToDomain
{
	public class @ModelName@DetailsTranslator : ITranslator<@ModelName@Details, Dto.@ModelName@Details>
	{
		public static readonly @ModelName@DetailsTranslator Instance = new @ModelName@DetailsTranslator();

		public @ModelName@Details Parse(Dto.@ModelName@Details from)
		{
			if (from == null) return null;
			var to = new @ModelName@Details {
				@ModelName@Name = from.@ModelName@Name,
				Email = from.Email,
				Title = from.Title,
				FirstName = from.FirstName,
				LastName = from.LastName,
				Country = from.Country,
				LanguageCode = from.LanguageCode,
				CanNotifyEmail = from.CanNotifyEmail,
				StoreCreditCard = from.StoreCreditCard,
				SingleClickBuyEnabled = from.SingleClickBuyEnabled,	
			};
			return to;
		}
	}
}