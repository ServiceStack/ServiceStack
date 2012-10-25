using System;
using System.Collections;
using System.Collections.Generic;

namespace ServiceStack.DataAccess
{
	public interface IPersistenceProvider : IBasicPersistenceProvider, IDisposable
	{
		IList<T> GetAll<T>()
			where T : class, new();

		IList<T> GetAllOrderedBy<T>(string fieldName, bool sortAsc)
			where T : class, new();

		T FindByValue<T>(string name, object value)
			where T : class, new();

		IList<T> FindAllByValue<T>(string name, object value)
			where T : class, new();

		IList<T> FindByValues<T>(string name, ICollection values)
			where T : class, new();

		void Flush();

		IList<T> StoreAll<T>(IList<T> entities)
			where T : class, new();
		
		void DeleteAll<T>(IList<T> entities)
			where T : class, new();

		ITransactionContext BeginTransaction();
	}
}