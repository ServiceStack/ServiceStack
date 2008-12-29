using Sakila.DomainModel;
using ServiceStack.DesignPatterns.Translator;

namespace ServiceStack.Sakila.Logic.Translators.DomainToData
{
	public class CustomerTranslator : ITranslator<DataAccess.DataModel.Customer, Customer>
	{
		public static readonly CustomerTranslator Instance = new CustomerTranslator();

		public DataAccess.DataModel.Customer Parse(Customer from)
		{
			var to = new DataAccess.DataModel.Customer {
				Id = (ushort)from.Id,
			};

			return to;
		}
	}
}