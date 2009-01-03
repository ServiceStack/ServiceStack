using Sakila.DomainModel;
using ServiceStack.DesignPatterns.Translator;

namespace ServiceStack.Sakila.Logic.Translators.DataToDomain
{
	public class CustomerFromDataTranslator : ITranslator<Customer, DataAccess.DataModel.Customer>
	{
		public static readonly CustomerFromDataTranslator Instance = new CustomerFromDataTranslator();

		public Customer Parse(DataAccess.DataModel.Customer from)
		{
			var to = new Customer {
				Id = from.Id,
				Email = from.Email,
				FirstName = from.FirstName,
				LastName = from.LastName,
				Address = AddressFromDataTranslator.Instance.Parse(from.addressMember),
			};

			return to;
		}
	}
}