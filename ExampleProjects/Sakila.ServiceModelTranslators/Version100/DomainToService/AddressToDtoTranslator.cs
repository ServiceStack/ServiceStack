using Sakila.DomainModel;
using DtoTypes = Sakila.ServiceModel.Version100.Types;
using ServiceStack.DesignPatterns.Translator;

namespace Sakila.ServiceModelTranslators.Version100.DomainToService
{
	public class AddressToDtoTranslator : ITranslator<DtoTypes.Address, Address>
	{
		public static readonly AddressToDtoTranslator Instance = new AddressToDtoTranslator();

		public DtoTypes.Address Parse(Address from)
		{
			if (from == null) return null;

			var to = new DtoTypes.Address {
				Line1 = from.Line1,
				Line2 = from.Line2,
				Town = from.Town,
				PostCode = from.PostCode,
			};

			return to;
		}
	}
}