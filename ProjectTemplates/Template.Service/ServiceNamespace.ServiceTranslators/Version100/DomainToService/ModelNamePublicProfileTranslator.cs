using Ddn.Common.DesignPatterns.Translator;
using @DomainModelNamespace@.@ServiceName@;
using DtoTypes = @ServiceModelNamespace@.Version100.Types;

namespace @ServiceNamespace@.ServiceTranslators.Version100.DomainToService
{
	public class @ModelName@PublicProfileTranslator : ITranslator<DtoTypes.@ModelName@PublicProfile, @ModelName@>
	{
		public static readonly @ModelName@PublicProfileTranslator Instance = new @ModelName@PublicProfileTranslator();

		public DtoTypes.@ModelName@PublicProfile Parse(@ModelName@ from)
		{
			if (from == null) return null;
			var to = new DtoTypes.@ModelName@PublicProfile {
				GlobalId = from.GlobalId,				
				@ModelName@Name = from.@ModelName@Details.@ModelName@Name,
                FirstName = from.@ModelName@Details.FirstName,
                LastName = from.@ModelName@Details.LastName,
                Country = from.@ModelName@Details.Country,
				LanguageCode = from.@ModelName@Details.LanguageCode,
			};
			return to;
		}
	}
}