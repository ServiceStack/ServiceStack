using @DomainModelNamespace@;
using DtoTypes = @ServiceModelNamespace@.Version100.Types;
using ServiceStack.DesignPatterns.Translator;

namespace @ServiceModelNamespace@Translators.Version100.ServiceToDomain
{
	public class AddressFromDtoTranslator : ITranslator<Address, DtoTypes.Address>
	{
		public static readonly AddressFromDtoTranslator Instance = new AddressFromDtoTranslator();

		public Address Parse(DtoTypes.Address from)
		{
			if (from == null) return null;

			var to = new Address {
				Line1 = from.Line1,
				Line2 = from.Line2,
				Town = from.Town,
				PostCode = from.PostCode,
			};

			return to;
		}
	}
}