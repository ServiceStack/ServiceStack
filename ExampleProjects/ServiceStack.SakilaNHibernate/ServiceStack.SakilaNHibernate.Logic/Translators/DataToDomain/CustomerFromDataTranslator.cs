using Sakila.DomainModel;
using ServiceStack.DesignPatterns.Translator;

namespace ServiceStack.SakilaNHibernate.Logic.Translators.DataToDomain
{
	public class CustomerFromDataTranslator : ITranslator<Customer, DataAccess.DataModel.Customer>
	{
		public static readonly CustomerFromDataTranslator Instance = new CustomerFromDataTranslator();

		public Customer Parse(DataAccess.DataModel.Customer from)
		{
			var to = new Customer {
				Id = from.Id,
			};

			return to;
		}
	}
}