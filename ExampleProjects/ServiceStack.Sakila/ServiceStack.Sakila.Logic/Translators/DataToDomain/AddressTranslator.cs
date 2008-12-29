using Sakila.DomainModel;
using ServiceStack.DesignPatterns.Translator;

namespace ServiceStack.Sakila.Logic.Translators.DataToDomain
{
	public class AddressTranslator : ITranslator<Address, DataAccess.DataModel.Address>
	{
		public static readonly AddressTranslator Instance = new AddressTranslator();

		public Address Parse(DataAccess.DataModel.Address from)
		{
			if (from == null) return null;
			var to = new Address {
				Id = from.Id,
                Line1 = from.Line1,
				Line2 = from.Line2,
				Town = from.District,
				City = CityTranslator.Instance.Parse(from.CityMember),
                PostCode = from.PostalCode,
			};

			return to;
		}
	}
}