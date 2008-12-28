using Sakila.DomainModel;
using ServiceStack.DesignPatterns.Translator;

namespace ServiceStack.Sakila.Logic.Translators.DataToDomain
{
	public class CustomerTranslator : ITranslator<Customer, DataAccess.DataModel.Customer>
	{
		public static readonly CustomerTranslator Instance = new CustomerTranslator();

		public Customer Parse(DataAccess.DataModel.Customer from)
		{
			var to = new Customer {
				Id = from.Id,
                Email = from.Email,
                FirstName = from.FirstName,
                LastName = from.LastName,
                Address = AddressTranslator.Instance.Parse(from.addressMember),
			};

			return to;
		}
	}
}