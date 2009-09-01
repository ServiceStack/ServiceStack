using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Db4objects.Db4o;
using Db4objects.Db4o.Query;
using ServiceStack.DataAccess.Criteria;
using ServiceStack.Logging;

namespace ServiceStack.DataAccess.Db4oProvider
{
	public class Db4OPersistenceProvider : IQueryablePersistenceProvider
	{
		private readonly ILog log = LogManager.GetLogger(typeof(Db4OPersistenceProvider));

		private static Dictionary<string, string> fieldNameTypeMappings;

		private const string IdPropertyName = "Id";

		public IObjectContainer ObjectContainer { get; private set; }

		private Db4OFileProviderManager manager;

		public Db4OPersistenceProvider(IObjectContainer provider, Db4OFileProviderManager manager)
		{
			this.ObjectContainer = provider;
			this.manager = manager;
			fieldNameTypeMappings = new Dictionary<string, string>();
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

		public void DeleteAll<T>(IList<T> entities) where T : class
		{
			foreach (var entity in entities)
			{
				this.Delete(entity);
			}
		}

		public ITransactionContext BeginTransaction()
		{
			return new Db4OTransaction(this.ObjectContainer);
		}

		public void CommitTransaction()
		{
			this.ObjectContainer.Commit();
		}

		public void RollbackTransaction()
		{
			this.ObjectContainer.Rollback();
		}

		public IList<T> GetAll<T>() where T : class
		{
			var query = this.ObjectContainer.Query();
			query.Constrain(typeof(T));
			return ConvertToList<T>(query.Execute());
		}

		public IList<T> GetAllOrderedBy<T>(string name, bool sortAsc) where T : class
		{
			var type = typeof(T);
			var query = this.ObjectContainer.Query();
			query.Constrain(type);
			var fieldName = GetFieldName(name, type);
			if (sortAsc)
			{
				query.Descend(fieldName).OrderAscending();
			}
			else
			{
				query.Descend(fieldName).OrderDescending();
			}
			return ConvertToList<T>(query.Execute());
		}

		public T GetById<T>(object id) where T : class
		{
			if (id == null)
				throw new ArgumentNullException("id");

			var entity = GetByInternalId<T>(id);
			return entity ?? FindByValue<T>(IdPropertyName, id);
		}

		private T GetByInternalId<T>(object id) where T : class
		{
			if (id.GetType().IsAssignableFrom(typeof(long)))
			{
				var type = id.GetType();
				var fieldInfo = GetFieldInfo(IdPropertyName, typeof(T));
				var isPotentialInternalId = fieldInfo != null
											&& fieldInfo.FieldType.IsAssignableFrom(typeof(long));
				if (isPotentialInternalId)
				{
					var idValue = (long)id;
					object entity;
					try
					{
						entity = this.ObjectContainer.Ext().GetByID(idValue);
					}
					catch (Exception ex)
					{
						log.Error(string.Format("Error calling 'this.ObjectContainer.Ext().GetByID({0})'", idValue), ex);
						return default(T);
					}
					//As internal Id's can differ from the entity id after defragmentation of the database,
					//It is only valid if the entity with the internal id is of the same type as T and
					//that entity.Id == entity.InternalId
					if (entity != null && entity.GetType() == type)
					{
						log.DebugFormat("this.ObjectContainer.Ext().Activate(entity)");
						this.ObjectContainer.Ext().Activate(entity);
						var entityIdValue = fieldInfo.GetValue(entity);
						if (idValue.Equals(entityIdValue))
						{
							log.DebugFormat("GetByInternalId match {0}.Id == {1}", type.Name, idValue);
							return (T)entity;
						}
					}
				}
			}
			return null;
		}

		public IList<T> GetByIds<T>(object[] ids) where T : class
		{
			if (ids.Count() == 0)
			{
				return new[] { GetByInternalId<T>(ids[0]) };
			}
			return GetByIds<T>((ICollection)ids);
		}

		public IList<T> GetByIds<T>(ICollection ids) where T : class
		{
			return FindByValues<T>(IdPropertyName, ids);
		}

		public T FindByValue<T>(string name, object value) where T : class
		{
			var results = FindAllByValue<T>(name, value);
			return results.Count > 0 ? results[0] : null;
		}

		public IList<T> FindAllByValue<T>(string name, object value) where T : class
		{
			if (name == null)
				throw new ArgumentNullException("name");
			if (value == null)
				throw new ArgumentNullException("value");

			var type = typeof(T);
			var query = this.ObjectContainer.Query();
			query.Constrain(type);

			var fieldName = GetFieldName(name, type);
			query.Descend(fieldName).Constrain(value);

			return ConvertToList<T>(query.Execute());
		}

		/// <summary>
		/// Gets the name of the underlying fieldname for a property. 
		/// Supports most conventions, e.g. for property 'Id' it will find
		/// Id, &gt;Id&lt;k__BackingField, id, _id, m_id
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		private static string GetFieldName(string name, Type type)
		{
			var fieldNameKey = type.FullName + ":" + name;
			string registeredFieldName;
			if (fieldNameTypeMappings.TryGetValue(fieldNameKey, out registeredFieldName))
			{
				return registeredFieldName;
			}

			FieldInfo fieldInfo;
			return GetFieldName(name, type, out fieldInfo);
		}

		private static FieldInfo GetFieldInfo(string name, Type type)
		{
			FieldInfo fieldInfo;
			GetFieldName(name, type, out fieldInfo);
			return fieldInfo;
		}

		private static string GetFieldName(string name, Type type, out FieldInfo fieldInfo)
		{
			var fieldNameKey = type.FullName + ":" + name;

			const string BACKING_FIELD = "<{0}>k__BackingField";
			var backingField = string.Format(BACKING_FIELD, name);
			var camelCaseName = name.Substring(0, 1).ToLower() + name.Substring(1);
			var possibleMatches = new[] { name, backingField, camelCaseName, "_" + name, "m_" + name }.ToList();

			//Walk the object heirachy
			var baseType = type;
			do
			{
				var typeFields = baseType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				foreach (var typeField in typeFields)
				{
					var fieldName = typeField.Name;
					if (!possibleMatches.Contains(typeField.Name)) continue;

					fieldNameTypeMappings[fieldNameKey] = fieldName;
					fieldInfo = typeField;
					return fieldName;
				}
			}
			while ((baseType = baseType.BaseType) != null);

			throw new NotSupportedException(
				string.Format("Could not find underlying field name '{0}' in type '{1}'", name, type.FullName));
		}

		public IList<T> FindByValues<T>(string name, object[] values) where T : class
		{
			return FindByValues<T>(name, (ICollection)values);
		}

		private class CustomEqualityComparer : IEqualityComparer
		{
			private readonly Type fieldType;

			public CustomEqualityComparer(Type fieldType)
			{
				this.fieldType = fieldType;
			}

			bool IEqualityComparer.Equals(object x, object y)
			{
				if (x == null && y == null)
				{
					return true;
				}

				if (x == null || y == null)
				{
					return false;
				}

				if (x is ValueType && y is ValueType)
				{
					var xx = Convert.ChangeType(x, this.fieldType);
					var yy = Convert.ChangeType(y, this.fieldType);

					return xx.Equals(yy);
				}

				return x.Equals(y);
			}
            
			int IEqualityComparer.GetHashCode(object obj)
			{
				if (obj is ValueType)
				{
					var value = Convert.ChangeType(obj, this.fieldType);

					return value.GetHashCode();
				}

				return obj.GetHashCode();
			}
		}

		public IList<T> FindByValues<T>(string name, ICollection values) where T : class
		{
			if (name == null)
				throw new ArgumentNullException("name");
			if (values == null || values.Count == 0)
				throw new ArgumentNullException("values");

			var type = typeof(T);
			var fieldInfo = GetFieldInfo(name, type);
			if (fieldInfo == null)
				throw new ArgumentException(string.Format("cannot find field '{0}' in type '{1}'", name, type.FullName));
			var valuesList = new Hashtable(new CustomEqualityComparer(fieldInfo.FieldType));

			foreach (var obj in values)
			{
				valuesList[obj] = obj;
			}

			var results = this.ObjectContainer.Query(delegate(T item) {
				var fieldValue = fieldInfo.GetValue(item);
				return valuesList.ContainsKey(fieldValue);
			});

			return results;
		}

		public void Flush() { }

		public T Store<T>(T entity) where T : class
		{
			this.ObjectContainer.Store(entity);
			try
			{
				var type = typeof(T);
				var fieldInfo = GetFieldInfo(IdPropertyName, type);
				if (fieldInfo.FieldType.IsAssignableFrom(typeof(long)))
				{
					var existingId = (long)fieldInfo.GetValue(entity);
					if (existingId == default(long))
					{
						long uniqueInternalId = this.ObjectContainer.Ext().GetID(entity);
						fieldInfo.SetValue(entity, uniqueInternalId);
						this.ObjectContainer.Store(entity);
					}
				}
			}
			catch (Exception ex)
			{
				throw new Exception("Unable to set unique id field on type '{0}'", ex);
			}
			return entity;
		}

		public IList<T> StoreAll<T>(IList<T> entities) where T : class
		{
			foreach (var entity in entities)
			{
				this.Store(entity);
			}
			return entities;
		}

		public void StoreAll<T>(IEnumerable<T> entities) where T : class
		{
			foreach (var entity in entities)
			{
				Store(entity);
			}
		}

		public IList<T> GetAll<T>(ICriteria criteria) where T : class
		{
			var query = this.ObjectContainer.Query();
			query.Constrain(typeof(T));

			SortResults(query, criteria);
			var db4oResults = query.Execute();
			var results = new List<T>();

			var paging = criteria as IPagingCriteria;
			if (paging != null)
			{
				var index = paging.ResultOffset;
				var limit = paging.ResultLimit;
				var resultCount = db4oResults.Count;
				while (index < resultCount && results.Count < limit)
				{
					results.Add((T)db4oResults[(int)index++]);
				}
			}
			else
			{
				foreach (T result in db4oResults)
				{
					results.Add(result);
				}
			}

			return results;
		}

		private static void SortResults(IQuery query, ICriteria criteria)
		{
			var orderByAsc = criteria as IOrderAscendingCriteria;
			if (orderByAsc != null)
			{
				query.Descend(orderByAsc.OrderedAscendingBy).OrderAscending();
			}
			else
			{
				var orderByDesc = criteria as IOrderDescendingCriteria;
				if (orderByDesc != null)
				{
					query.Descend(orderByDesc.OrderedDescendingBy).OrderDescending();
				}
			}
		}

		public void Delete<T>(T entity) where T : class
		{
			this.ObjectContainer.Delete(entity);
		}

		public void DeleteAll<T>(IEnumerable<T> entities) where T : class
		{
			foreach (var entity in entities)
			{
				Delete(entity);
			}
		}

		~Db4OPersistenceProvider()
		{
			Dispose(false);
		}

		public IList<Extent> Query<Extent>(IComparer<Extent> comparer)
		{
			return ObjectContainer.Query(comparer);
		}

		public IList<Extent> Query<Extent>(Predicate<Extent> match)
		{
			return ObjectContainer.Query(match);
		}

		public IList<Extent> QueryByExample<Extent>(object template)
		{
			return ConvertToList<Extent>(ObjectContainer.QueryByExample(template));
		}

		public void Dispose()
		{
			Dispose(true);
		}

		public void Dispose(bool disposing)
		{
			if (disposing)
				GC.SuppressFinalize(this);

			manager.ProviderDispose(this);
		}
	}
}