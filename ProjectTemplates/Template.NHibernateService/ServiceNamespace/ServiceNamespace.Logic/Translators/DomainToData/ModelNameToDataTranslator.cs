using @DomainModelNamespace@;
using ServiceStack.DesignPatterns.Translator;

namespace @ServiceNamespace@.Logic.Translators.DomainToData
{
	public class @ModelName@ToDataTranslator : ITranslator<DataAccess.DataModel.@ModelName@, @ModelName@>
	{
		public static readonly @ModelName@ToDataTranslator Instance = new @ModelName@ToDataTranslator();

		public DataAccess.DataModel.@ModelName@ Parse(@ModelName@ from)
		{
			var to = new DataAccess.DataModel.@ModelName@ {
				Id = (ushort)from.Id,
			};

			return to;
		}
	}
}