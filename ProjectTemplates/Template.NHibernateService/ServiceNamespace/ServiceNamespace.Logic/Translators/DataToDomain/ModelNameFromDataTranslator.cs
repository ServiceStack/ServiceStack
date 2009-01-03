using @DomainModelNamespace@;
using ServiceStack.DesignPatterns.Translator;

namespace @ServiceNamespace@.Logic.Translators.DataToDomain
{
	public class @ModelName@FromDataTranslator : ITranslator<@ModelName@, DataAccess.DataModel.@ModelName@>
	{
		public static readonly @ModelName@FromDataTranslator Instance = new @ModelName@FromDataTranslator();

		public @ModelName@ Parse(DataAccess.DataModel.@ModelName@ from)
		{
			var to = new @ModelName@ {
				Id = from.Id,
			};

			return to;
		}
	}
}