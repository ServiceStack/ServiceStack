using System;
using Ddn.Common.DesignPatterns.Translator;
using Sakila.DomainModel.SakilaService;
using DtoTypes=Sakila.ServiceModel.Version100.Types;
namespace ServiceStack.Sakila.ServiceTranslators.Version100.ServiceToDomain
{
	public class CustomerTranslator : ITranslator<Customer, DtoTypes.Customer>
	{
		public static readonly CustomerTranslator Instance = new CustomerTranslator();

		public Customer Parse(DtoTypes.Customer from)
		{
			if (from == null) return null;
			var to = new Customer {
				Id = from.Id,
				GlobalId = from.GlobalId,
				CustomerDetails = CustomerDetailsTranslator.Instance.Parse(from.CustomerDetails),
				Balance = from.Balance,
			};
			return to;
		}
	}
}