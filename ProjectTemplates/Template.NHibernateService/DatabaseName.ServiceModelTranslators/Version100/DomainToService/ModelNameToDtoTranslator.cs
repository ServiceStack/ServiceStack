using @DomainModelNamespace@;
using ServiceStack.DesignPatterns.Translator;
using DtoTypes = @ServiceModelNamespace@.Version100.Types;

namespace @ServiceModelNamespace@Translators.Version100.DomainToService
{
	public class @ModelName@ToDtoTranslator : ITranslator<DtoTypes.@ModelName@, @ModelName@>
	{
		public static readonly @ModelName@ToDtoTranslator Instance = new @ModelName@ToDtoTranslator();

		public DtoTypes.@ModelName@ Parse(@ModelName@ from)
		{
			if (from == null) return null;
			var to = new DtoTypes.@ModelName@ {
				Id = from.Id,
				FirstName = from.FirstName,
				LastName = from.LastName,
				Email = from.Email,
				Address = AddressToDtoTranslator.Instance.Parse(from.Address),
			};
			return to;
		}
	}
}
