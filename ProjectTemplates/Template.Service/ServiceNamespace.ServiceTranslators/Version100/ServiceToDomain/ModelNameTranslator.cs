using System;
using Ddn.Common.DesignPatterns.Translator;
using @DomainModelNamespace@.@ServiceName@;
using DtoTypes=@ServiceModelNamespace@.Version100.Types;
namespace @ServiceNamespace@.ServiceTranslators.Version100.ServiceToDomain
{
	public class @ModelName@Translator : ITranslator<@ModelName@, DtoTypes.@ModelName@>
	{
		public static readonly @ModelName@Translator Instance = new @ModelName@Translator();

		public @ModelName@ Parse(DtoTypes.@ModelName@ from)
		{
			if (from == null) return null;
			var to = new @ModelName@ {
				Id = from.Id,
				GlobalId = from.GlobalId,
				@ModelName@Details = @ModelName@DetailsTranslator.Instance.Parse(from.@ModelName@Details),
				Balance = from.Balance,
			};
			return to;
		}
	}
}