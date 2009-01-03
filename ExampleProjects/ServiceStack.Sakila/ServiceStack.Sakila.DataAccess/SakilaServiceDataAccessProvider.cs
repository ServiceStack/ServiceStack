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
			Data = provider;
		}

		public IPersistenceProvider Data { get; set; }

		/// <summary>
		/// Creates the user.
		/// 
		/// Also sets the audit properties here, which can be overriden later.
		/// </summary>
		/// <param name="name">Name of the user.</param>
		/// <returns></returns>
		public Customer CreateNewCustomer(string name)
		{
			var now = DateTime.Now;
			var user = new Customer {
				CreateDate = now,
				LastUpdate = now,
			};
			return user;
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
				results.AddRange(Data.GetByIds<Customer>(ids.ConvertAll(x => (ushort)x)));
			}
			return results;
		}

		public List<Film> GetFilms(List<int> ids)
		{
			var results = new List<Film>();
			if (ids != null && ids.Count > 0)
			{
				results.AddRange(Data.GetByIds<Film>(ids.ConvertAll(x => (ushort)x)));
			}
			return results;
		}

		public Customer GetCustomerByName(string name)
		{
			return Data.FindByValue<Customer>("CustomerName", name);
		}

		public void Store(object entity)
		{
			Data.Save(entity);
		}

		public ITransactionContext BeginTransaction()
		{
			return Data.BeginTransaction();
		}
	}
}
