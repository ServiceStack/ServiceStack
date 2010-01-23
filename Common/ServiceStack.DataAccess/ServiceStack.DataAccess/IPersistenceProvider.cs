using System;
using System.Collections;
using System.Collections.Generic;

namespace ServiceStack.DataAccess
{
	public interface IPersistenceProvider : IDisposable
	{
		IList<T> GetAll<T>();
		IList<T> GetAllOrderedBy<T>(string orderBy);
		
		T GetById<T>(object id) where T : class;
		IList<T> GetByIds<T>(object[] ids);
		IList<T> GetByIds<T>(ICollection ids);

		T FindByValue<T>(string name, object value) where T : class;
		IList<T> FindByValues<T>(string name, object[] values) where T : class;
		IList<T> FindByValues<T>(string name, ICollection values) where T : class;

		void Flush();

		T Insert<T>(T entity) where T : class;
		T Save<T>(T entity) where T : class;
		T Update<T>(T entity) where T : class;
		void Delete<T>(T entity) where T : class;

		ITransactionContext BeginTransaction();
	}
}