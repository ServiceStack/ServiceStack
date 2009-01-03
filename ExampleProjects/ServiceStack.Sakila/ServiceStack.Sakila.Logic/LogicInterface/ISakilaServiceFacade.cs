using System.Collections.Generic;
using Sakila.DomainModel;
using ServiceStack.LogicFacade;
using ServiceStack.Sakila.Logic.LogicInterface.Requests;

namespace ServiceStack.Sakila.Logic.LogicInterface
{
	public interface ISakilaServiceFacade : ILogicFacade
	{
		List<Customer> GetAllCustomers();

		List<Customer> GetCustomers(CustomersRequest request);

		List<Film> GetFilms(List<int> filmIds);

		void StoreCustomer(Customer customer);
	}
}