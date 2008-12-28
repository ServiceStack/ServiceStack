using Ddn.Common.DesignPatterns.Translator;
using @DomainModelNamespace@.@ServiceName@;

namespace @ServiceNamespace@.ServiceTranslators.Version100.DomainToService
{
	public class @ModelName@Translator : ITranslator<ServiceModel.Version100.Types.@ModelName@, @ModelName@>
	{
		public static readonly @ModelName@Translator Instance = new @ModelName@Translator();

		public ServiceModel.Version100.Types.@ModelName@ Parse(@ModelName@ from)
		{
			if (from == null) return null;
			var to = new ServiceModel.Version100.Types.@ModelName@ {
				Id = from.Id,
				GlobalId = from.GlobalId,
				@ModelName@Details = @ModelName@DetailsTranslator.Instance.Parse(from.@ModelName@Details),
				Balance = from.Balance,
			};
			return to;
		}
	}
}