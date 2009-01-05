using System;
using System.Collections;
using System.Collections.Generic;

namespace ServiceStack.DataAccess
{
	public interface IPersistenceProvider : IDisposable
	{
		IList<T> GetAll<T>() where T : class;
		IList<T> GetAllOrderedBy<T>(string fieldName, bool sortAsc) where T : class;
		
		T GetById<T>(object id) where T : class;
		IList<T> GetByIds<T>(ICollection ids) where T : class;

		T FindByValue<T>(string name, object value) where T : class;
		IList<T> FindAllByValue<T>(string name, object value) where T : class;
		IList<T> FindByValues<T>(string name, ICollection values) where T : class;

		void Flush();

		T Store<T>(T entity) where T : class;
		void Delete<T>(T entity) where T : class;

		ITransactionContext BeginTransaction();
	}
}