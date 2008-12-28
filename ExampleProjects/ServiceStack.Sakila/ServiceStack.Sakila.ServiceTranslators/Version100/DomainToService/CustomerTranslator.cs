using Ddn.Common.DesignPatterns.Translator;
using Sakila.DomainModel.SakilaService;

namespace ServiceStack.Sakila.ServiceTranslators.Version100.DomainToService
{
	public class CustomerTranslator : ITranslator<ServiceModel.Version100.Types.Customer, Customer>
	{
		public static readonly CustomerTranslator Instance = new CustomerTranslator();

		public ServiceModel.Version100.Types.Customer Parse(Customer from)
		{
			if (from == null) return null;
			var to = new ServiceModel.Version100.Types.Customer {
				Id = from.Id,
				GlobalId = from.GlobalId,
				CustomerDetails = CustomerDetailsTranslator.Instance.Parse(from.CustomerDetails),
				Balance = from.Balance,
			};
			return to;
		}
	}
}