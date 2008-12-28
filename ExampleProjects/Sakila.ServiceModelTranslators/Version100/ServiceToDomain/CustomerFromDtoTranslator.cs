using Sakila.DomainModel;
using DtoTypes = Sakila.ServiceModel.Version100.Types;
using ServiceStack.DesignPatterns.Translator;

namespace Sakila.ServiceModelTranslators.Version100.ServiceToDomain
{
	public class CustomerFromDtoTranslator : ITranslator<Customer, DtoTypes.Customer>
	{
		public static readonly CustomerFromDtoTranslator Instance = new CustomerFromDtoTranslator();

		public Customer Parse(DtoTypes.Customer from)
		{
			if (from == null) return null;
			var to = new Customer {
				Id = from.Id,
                FirstName = from.FirstName,
                LastName = from.LastName,
                Email = from.Email,
                Address = AddressFromDtoTranslator.Instance.Parse(from.Address),
			};
			return to;
		}
	}
}