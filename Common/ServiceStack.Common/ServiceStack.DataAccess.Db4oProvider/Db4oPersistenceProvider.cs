using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Db4objects.Db4o;
using ServiceStack.Logging;

namespace ServiceStack.DataAccess.Db4oProvider
{
	public class Db4oPersistenceProvider : IPersistenceProvider
	{
		private readonly ILog log = LogManager.GetLogger(typeof(Db4oPersistenceProvider));

		private const string ID_PROPERTY_NAME = "Id";

		private readonly IObjectContainer provider;

		public Db4oPersistenceProvider(IObjectContainer provider)
		{
			this.provider = provider;
		}

		private static List<T> ConvertToList<T>(IObjectSet results)
		{
			var list = new List<T>();
			foreach (T result in results)
			{
				list.Add(result);
			}
			return list;
		}

		public ITransactionContext BeginTransaction()
		{
			return new Db4oTransaction(this.provider);
		}

		public void CommitTransaction()
		{
			provider.Commit();
		}

		public void RollbackTransaction()
		{
			provider.Rollback();
		}

		public ReturnType GetById<ReturnType, IdType>(IdType id) where ReturnType : class
		{
			var idProperty = GetIdProperty<ReturnType>();
			var results = provider.Query(delegate(ReturnType item) {
				var idValue = (IdType)idProperty.GetValue(item, null);
				return Equals(id, idValue);
			});
			return results.Count > 0 ? results[0] : null;
		}

		public IEnumerable<ReturnType> GetByIds<ReturnType, IdType>(IEnumerable<IdType> ids) where ReturnType : class
		{
			var idProperty = GetIdProperty<ReturnType>();
			var idsSet = new List<IdType>(ids);
			var results = provider.Query(delegate(ReturnType item) {
				var idValue = idProperty.GetValue(item, null);
				return idsSet.Contains((IdType)idValue);
			});
			return results;
		}

		public IList<ReturnType> GetAll<ReturnType>() 
		{
			var query = provider.Query();
			query.Constrain(typeof(ReturnType));
			return ConvertToList<ReturnType>(query.Execute());
		}

		public IList<ReturnType> GetAllOrderedBy<ReturnType>(string orderBy)
		{
			var query = provider.Query();
			query.Constrain(typeof(ReturnType));
			if (orderBy.ToLower().StartsWith("desc"))
				query.OrderDescending();
			else
				query.OrderAscending();

			return ConvertToList<ReturnType>(query.Execute());
		}

		public T GetById<T>(object id) where T : class
		{
			if (id == null)
				throw new ArgumentNullException("id");

			var idProperty = GetIdProperty(id.GetType());
			var results = provider.Query(delegate(object item) {
				var idValue = idProperty.GetValue(item, null);
				return Equals(id, idValue);
			});
			return results.Count > 0 ? (T)results[0] : null;
		}

		public IList<T> GetByIds<T>(object[] ids)
		{
			if (ids == null || ids.Length == 0)
				throw new ArgumentNullException("ids");

			var idProperty = GetIdProperty(ids[0].GetType());
			var idsSet = ids.ToList();
			var results = provider.Query(delegate(object item) {
				var idValue = idProperty.GetValue(item, null);
				return idsSet.Contains(idValue);
			});
			return results.ToList().ConvertAll(x => (T)x);
		}

		public IList<T> GetByIds<T>(ICollection ids)
		{
			var oIds = new object[ids.Count];
			ids.CopyTo(oIds, 0);
			return GetByIds<T>(oIds);
		}

		public T FindByValue<T>(string name, object value) where T : class
		{
			if (name == null)
				throw new ArgumentNullException("name");
			if (value == null)
				throw new ArgumentNullException("value");

			var query = provider.Query();
			query.Constrain(typeof (T));
			query.Descend(name).Constrain(value);
			var results = query.Execute();
			return results.Count > 0 ? (T)results[0] : null;
		}

		public IList<T> FindByValues<T>(string name, object[] values) where T : class
		{
			if (name == null)
				throw new ArgumentNullException("name");
			if (values == null || values.Length == 0)
				throw new ArgumentNullException("values");

			var query = provider.Query();
			query.Constrain(typeof(T));
			query.Descend(name).Constrain(values).Contains();
			return ConvertToList<T>(query.Execute());
		}

		public IList<T> FindByValues<T>(string name, ICollection values) where T : class
		{
			var oValues = new object[values.Count];
			values.CopyTo(oValues, 0);
			return FindByValues<T>(name, values);
		}

		public void Flush()
		{
			//throw new System.NotImplementedException();
		}

		public T Insert<T>(T entity) where T : class
		{
			return Save(entity);
		}

		public T Save<T>(T entity) where T : class
		{
			provider.Store(entity);
			return entity;
		}

		public T Update<T>(T entity) where T : class
		{
			return Save(entity);
		}

		public void StoreAll<T>(IEnumerable<T> entities) where T : class
		{
			foreach (var entity in entities)
			{
				Save(entity);
			}
		}

		//public IResultSet<ReturnType> GetAll<ReturnType>(ICriteria criteria) where ReturnType : class
		//{
		//    var query = provider.Query();
		//    query.Constrain(typeof(ReturnType));

		//    SortResults(query, criteria);
		//    var db4oResults = query.Execute();
		//    var results = new List<ReturnType>();

		//    long resultOffset = 1;
		//    var paging = criteria as IPagingCriteria;
		//    if (paging != null)
		//    {
		//        resultOffset = paging.ResultOffset;
		//        var i = paging.ResultOffset;
		//        while (db4oResults.Count > i++)
		//        {
		//            results.Add((ReturnType)db4oResults[i]);
		//        }
		//    }
		//    else
		//    {
		//        foreach (ReturnType result in db4oResults)
		//        {
		//            results.Add(result);
		//        }
		//    }

		//    return new Db4oResultSet<ReturnType>(results) {
		//        Offset = resultOffset,
		//        TotalCount = db4oResults.Count,
		//    };
		//}

		//private static void SortResults(IQuery query, ICriteria criteria)
		//{
		//    var orderByAsc = criteria as IOrderAscendingCriteria;
		//    if (orderByAsc != null)
		//    {
		//        query.Descend(orderByAsc.OrderedAscendingBy).OrderAscending();
		//    }
		//    else
		//    {
		//        var orderByDesc = criteria as IOrderDescendingCriteria;
		//        if (orderByDesc != null)
		//        {
		//            query.Descend(orderByDesc.OrderedDescendingBy).OrderDescending();
		//        }
		//    }
		//}

		public void Delete<T>(T entity) where T : class
		{
			provider.Delete(entity);
		}

		public void DeleteAll<T>(IEnumerable<T> entities) where T : class
		{
			foreach (var entity in entities)
			{
				Delete(entity);
			}
		}

		private static PropertyInfo GetIdProperty(Type returnType)
		{
			return returnType.GetProperty(ID_PROPERTY_NAME) ?? returnType.GetProperty(ID_PROPERTY_NAME.ToLower());
		}

		private static PropertyInfo GetIdProperty<ReturnType>()
		{
			var returnType = typeof(ReturnType);
			return GetIdProperty(returnType);
		}

		~Db4oPersistenceProvider()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		public void Dispose(bool disposing)
		{
			if (disposing)
				GC.SuppressFinalize(this);

			log.DebugFormat("Disposing Db4oPersistenceProvider...");
			provider.Close();
			provider.Dispose();
		}
		
	}
}