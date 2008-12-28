using Ddn.Common.DesignPatterns.Translator;
using @DomainModelNamespace@.@ServiceName@;
using Dto = @ServiceModelNamespace@.Version100.Types;

namespace @ServiceNamespace@.ServiceTranslators.Version100.DomainToService
{
	public class @ModelName@DetailsTranslator : ITranslator<Dto.@ModelName@Details, @ModelName@Details>
	{
		public static readonly @ModelName@DetailsTranslator Instance = new @ModelName@DetailsTranslator();

		public Dto.@ModelName@Details Parse(@ModelName@Details from)
		{

			if (from == null) return null;
			var to = new Dto.@ModelName@Details {
				@ModelName@Name = from.@ModelName@Name,
				Email = from.Email,
				Title = from.Title,
				FirstName = from.FirstName,
				LastName = from.LastName,
				Country = from.Country,
				LanguageCode = from.LanguageCode,
				CanNotifyEmail = from.CanNotifyEmail,
				SingleClickBuyEnabled = from.SingleClickBuyEnabled,
			};
			return to;
		}
	}
}