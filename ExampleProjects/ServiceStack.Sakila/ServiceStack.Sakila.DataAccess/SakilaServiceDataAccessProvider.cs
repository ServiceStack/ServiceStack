using System;
using System.Collections.Generic;
using ServiceStack.DataAccess;
using ServiceStack.Sakila.DataAccess.DataModel;

namespace ServiceStack.Sakila.DataAccess
{
	public class SakilaServiceDataAccessProvider
	{
		public SakilaServiceDataAccessProvider(IPersistenceProvider provider)
		{
			Provider = provider;
		}

		private IPersistenceProvider Provider { get; set; }

		/// <summary>
		/// Creates the user.
		/// 
		/// Also sets the audit properties here, which can be overriden later.
		/// </summary>
		/// <param name="userName">Name of the user.</param>
		/// <returns></returns>
		public Customer CreateNewCustomer(string userName)
		{
			var now = DateTime.Now;
			var user = new Customer {
				CreateDate = now,
				LastUpdate = now,
			};
			return user;
		}

		public Customer GetCustomer(int userId)
		{
			return Provider.GetById<Customer>((uint)userId);
		}

		/// <summary>
		/// Get a union of users identified by the ids supplied
		/// </summary>
		/// <param name="ids"></param>
		/// <returns></returns>
		public List<Customer> GetCustomers(List<int> ids)
		{
			//This can be optimized to use 1 query
			var results = new List<Customer>();
			if (ids != null && ids.Count > 0)
			{
				results.AddRange(Provider.GetByIds<Customer>(ids.ConvertAll(x => (ushort)x)));
			}
			return results;
		}

		public Customer GetCustomerByCustomerName(string userName)
		{
			return Provider.FindByValue<Customer>("CustomerName", userName);
		}

		public void Store(object entity)
		{
			Provider.Save(entity);
		}

		public ITransactionContext BeginTransaction()
		{
			return Provider.BeginTransaction();
		}
	}
}
