using Ddn.Common.DesignPatterns.Translator;
using @DomainModelNamespace@.@ServiceName@;

namespace @ServiceNamespace@.Logic.Translators.DataToDomain
{
	public class @ModelName@DetailsTranslator : ITranslator<@ModelName@Details, DataAccess.DataModel.@ModelName@>
	{
		public static readonly @ModelName@DetailsTranslator Instance = new @ModelName@DetailsTranslator();

		public @ModelName@Details Parse(DataAccess.DataModel.@ModelName@ from)
		{
			if (from == null) return null;
			var to = new @ModelName@Details {
				@ModelName@Name = from.@ModelName@Name,
				Email = from.Email,
				Title = from.Title,
				FirstName = from.FirstName,
				LastName = from.LastName,
				Country = from.Country,
				CanNotifyEmail = from.CanNotifyEmail != 0,
				SingleClickBuyEnabled = from.SingleClickBuyEnabled != 0,
			};
			return to;
		}
	}
}