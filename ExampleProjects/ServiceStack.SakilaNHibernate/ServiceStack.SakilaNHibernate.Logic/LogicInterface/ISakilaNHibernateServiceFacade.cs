using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.LogicFacade;
using ServiceStack.SakilaNHibernate.Logic.LogicInterface.Requests;

namespace ServiceStack.SakilaNHibernate.Logic.LogicInterface
{
	public interface ISakilaNHibernateServiceFacade : ILogicFacade
	{
		List<Customer> GetAllCustomers();

		List<Customer> GetCustomers(CustomersRequest request);

		void StoreCustomer(Customer entity);
	}
}