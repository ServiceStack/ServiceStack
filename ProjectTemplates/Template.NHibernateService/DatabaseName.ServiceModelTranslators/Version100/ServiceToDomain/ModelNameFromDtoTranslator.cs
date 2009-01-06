using @DomainModelNamespace@;
using DtoTypes = @ServiceModelNamespace@.Version100.Types;
using ServiceStack.DesignPatterns.Translator;

namespace @ServiceModelNamespace@Translators.Version100.ServiceToDomain
{
	public class @ModelName@FromDtoTranslator : ITranslator<@ModelName@, DtoTypes.@ModelName@>
	{
		public static readonly @ModelName@FromDtoTranslator Instance = new @ModelName@FromDtoTranslator();

		public @ModelName@ Parse(DtoTypes.@ModelName@ from)
		{
			if (from == null) return null;
			var to = new @ModelName@ {
				Id = from.Id,
			};
			return to;
		}
	}
}