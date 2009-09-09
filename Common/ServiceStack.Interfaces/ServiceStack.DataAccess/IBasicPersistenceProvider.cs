using System;
using System.Collections;
using System.Collections.Generic;

namespace ServiceStack.DataAccess
{
	public interface IBasicPersistenceProvider : IDisposable
	{
		T GetById<T>(object id)
			where T : class, new();
	
		IList<T> GetByIds<T>(ICollection ids) 
			where T : class, new();

		T Store<T>(T entity)
			where T : class, new();

		void StoreAll<TEntity>(params TEntity[] entities)
			where TEntity : class, new();

		void Delete<T>(T entity)
			where T : class, new();
	}
}