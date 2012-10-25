using System;
using System.Collections;
using System.Collections.Generic;

namespace ServiceStack.CacheAccess
{
	public interface IPersistenceProviderCache
	{
		TEntity GetById<TEntity>(object entityId)
			where TEntity : class, new();

		List<TEntity> GetByIds<TEntity>(ICollection entityIds)
			where TEntity : class, new();

		void SetCache<TEntity>(TEntity entity)
			where TEntity : class, new();

		void Store<TEntity>(TEntity entity)
			where TEntity : class, new();

		void StoreAll<TEntity>(params TEntity[] entities)
			where TEntity : class, new();

		void ClearAll<TEntity>(ICollection entityIds)
			where TEntity : class, new();

		void Clear<TEntity>(params object[] entityIds)
			where TEntity : class, new();
	}
}