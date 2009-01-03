using System;
using System.Collections.Generic;
using ServiceStack.DataAccess;
using ServiceStack.SakilaDb4o.DataAccess.DataModel;

namespace ServiceStack.SakilaDb4o.DataAccess
{
	public class SakilaDb4oServiceDataAccessProvider
	{
		public SakilaDb4oServiceDataAccessProvider(IPersistenceProvider provider)
		{
			Data = provider;
		}

		public IPersistenceProvider Data { get; set; }

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
