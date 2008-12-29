using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.LogicFacade;
using ServiceStack.Sakila.Logic.LogicInterface.Requests;

namespace ServiceStack.Sakila.Logic.LogicInterface
{
	public interface ISakilaServiceFacade : ILogicFacade
	{
		List<Customer> GetCustomers(CustomersRequest request);
		void StoreCustomer(Customer customer);
	}
}