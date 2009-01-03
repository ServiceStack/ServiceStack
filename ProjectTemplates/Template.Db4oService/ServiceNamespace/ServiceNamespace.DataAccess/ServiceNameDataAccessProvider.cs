using System;
using System.Collections.Generic;
using ServiceStack.DataAccess;
using @ServiceNamespace@.DataAccess.DataModel;

namespace @ServiceNamespace@.DataAccess
{
	public class @ServiceName@DataAccessProvider
	{
		public @ServiceName@DataAccessProvider(IPersistenceProvider provider)
		{
			Data = provider;
		}

		public IPersistenceProvider Data { get; set; }

		/// <summary>
		/// Get a union of users identified by the ids supplied
		/// </summary>
		/// <param name="ids"></param>
		/// <returns></returns>
		public List<@ModelName@> Get@ModelName@s(List<int> ids)
		{
			//This can be optimized to use 1 query
			var results = new List<@ModelName@>();
			if (ids != null && ids.Count > 0)
			{
				results.AddRange(Data.GetByIds<@ModelName@>(ids.ConvertAll(x => (ushort)x)));
			}
			return results;
		}

		public @ModelName@ Get@ModelName@ByName(string name)
		{
			return Data.FindByValue<@ModelName@>("@ModelName@Name", name);
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
