using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Common.Utils;
using ServiceStack.DataAccess;

namespace ServiceStack.Redis
{
	public class RedisPersistenceProvider
		: RedisClient, IBasicPersistenceProvider
	{
		public RedisPersistenceProvider()
		{
		}

		public RedisPersistenceProvider(string host, int port) 
			: base(host, port)
		{
		}

		public T GetById<T>(object id) where T : class, new()
		{
			var key = IdUtils.CreateUrn<T>(id);
			var valueString = base.GetString(key);
			var value = StringConverterUtils.Parse<T>(valueString);
			return value;
		}

		public IList<T> GetByIds<T>(ICollection ids) 
			where T : class, new()
		{
			var keys = new List<string>();
			foreach (var id in ids)
			{
				var key = IdUtils.CreateUrn<T>(id);
				keys.Add(key);
			}

			return GetKeyValues<T>(keys);
		}

		public T Store<T>(T entity) 
			where T : class, new()
		{
			var urnKey = entity.CreateUrn();
			var valueString = StringConverterUtils.ToString(entity);
			base.SetString(urnKey, valueString);

			return entity;
		}

		public void StoreAll<TEntity>(IEnumerable<TEntity> entities)
			where TEntity : class, new()
		{
			if (entities == null) return;

			foreach (var entity in entities)
			{
				Store(entity);
			}
		}

		public void Delete<T>(T entity) 
			where T : class, new()
		{
			var urnKey = entity.CreateUrn();
			base.Remove(urnKey);
		}

		public void DeleteById<T>(object id) where T : class, new()
		{
			var key = IdUtils.CreateUrn<T>(id);
			base.Remove(key);
		}

		public void DeleteByIds<T>(ICollection ids) where T : class, new()
		{
			if (ids == null) return;

			var keysLength = ids.Count;
			var keys = new string[keysLength];

			var i = 0;
			foreach (var id in ids)
			{
				var key = IdUtils.CreateUrn<T>(id);
				keys[i++] = key;
			}

			base.Remove(keys);
		}

		public void DeleteAll<TEntity>() where TEntity : class, new()
		{
			//TODO: replace with DeleteAll of TEntity
			base.FlushDb();
		}
	}
}