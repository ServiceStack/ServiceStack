using System;
using System.Collections.Generic;
using ServiceStack.DataAccess;
using ServiceStack.SakilaNHibernate.DataAccess.DataModel;

namespace ServiceStack.SakilaNHibernate.DataAccess
{
	public class SakilaNHibernateServiceDataAccessProvider
	{
		public SakilaNHibernateServiceDataAccessProvider(IPersistenceProvider provider)
		{
			Data = provider;
		}

		public IPersistenceProvider Data { get; set; }

		public List<Customer> GetCustomers(List<int> ids)
		{
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
