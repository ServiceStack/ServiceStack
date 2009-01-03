using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.LogicFacade;
using ServiceStack.SakilaDb4o.Logic.LogicInterface.Requests;

namespace ServiceStack.SakilaDb4o.Logic.LogicInterface
{
	public interface ISakilaDb4oServiceFacade : ILogicFacade
	{
		IList<Customer> GetAllCustomers();

		IList<Customer> GetCustomers(CustomersRequest request);

		void StoreCustomer(Customer entity);
	}
}