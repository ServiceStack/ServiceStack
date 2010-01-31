using System;
using System.Collections.Generic;

namespace ServiceStack.DataAccess
{
	/// <summary>
	/// For providers that want a cleaner API with a little more perf
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IBasicPersistenceProvider<T> 
		: IDisposable
	{
		T GetById(string id);
	
		IList<T> GetByIds(ICollection<string> ids);

		IList<T> GetAll();

		T Store(T entity);

		void StoreAll(IEnumerable<T> entities);

		void Delete(T entity);

		void DeleteById(string id);

		void DeleteByIds(ICollection<string> ids);

		void DeleteAll();
	}
}