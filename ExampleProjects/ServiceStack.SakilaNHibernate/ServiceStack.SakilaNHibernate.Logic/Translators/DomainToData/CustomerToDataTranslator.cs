using Sakila.DomainModel;
using ServiceStack.DesignPatterns.Translator;

namespace ServiceStack.SakilaNHibernate.Logic.Translators.DomainToData
{
	public class CustomerToDataTranslator : ITranslator<DataAccess.DataModel.Customer, Customer>
	{
		public static readonly CustomerToDataTranslator Instance = new CustomerToDataTranslator();

		public DataAccess.DataModel.Customer Parse(Customer from)
		{
			var to = new DataAccess.DataModel.Customer {
				Id = (ushort)from.Id,
				FirstName = from.FirstName,
				LastName = from.LastName,
				Email = from.Email,
			};

			return to;
		}
	}
}