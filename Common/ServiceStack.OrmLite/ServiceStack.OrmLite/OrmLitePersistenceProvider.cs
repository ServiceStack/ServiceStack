//
// ServiceStack.OrmLite: Light-weight POCO ORM for .NET and Mono
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.DataAccess;

namespace ServiceStack.OrmLite
{
	/// <summary>
	/// Allow for code-sharing between OrmLite, IPersistenceProvider and ICacheClient
	/// </summary>
	public class OrmLitePersistenceProvider
		: IBasicPersistenceProvider
	{
		protected string ConnectionString { get; set; }
		protected bool DisposeConnection = true;

		protected IDbConnection connection;
		public IDbConnection Connection
		{
			get
			{
				if (connection == null)
				{
					connection = this.ConnectionString.OpenDbConnection();
				}
				return connection;
			}
		}

		public OrmLitePersistenceProvider(string connectionString)
		{
			ConnectionString = connectionString;
		}

		public OrmLitePersistenceProvider(IDbConnection connection)
		{
			this.connection = connection;
			this.DisposeConnection = false;
		}

		public T GetById<T>(object id)
			where T : class, new()
		{
			using (var dbCmd = this.Connection.CreateCommand())
			{
				return dbCmd.GetByIdOrDefault<T>(id);
			}
		}

		public IList<T> GetByIds<T>(ICollection ids)
			where T : class, new()
		{
			using (var dbCmd = this.Connection.CreateCommand())
			{
				return dbCmd.GetByIds<T>(ids);
			}
		}

		public T Store<T>(T entity)
			where T : class, new()
		{
			using (var dbCmd = this.Connection.CreateCommand())
			{
				return InsertOrUpdate(dbCmd, entity);
			}
		}

		private static T InsertOrUpdate<T>(IDbCommand dbCmd, T entity)
			where T : class, new()
		{
			var id = IdUtils.GetId(entity);
			var existingEntity = dbCmd.GetByIdOrDefault<T>(id);
			if (existingEntity != null)
			{
				existingEntity.PopulateWith(entity);
				dbCmd.Update(entity);

				return existingEntity;
			}

			dbCmd.Insert(entity);
			return entity;
		}

		public void StoreAll<TEntity>(IEnumerable<TEntity> entities) 
			where TEntity : class, new()
		{
			using (var dbCmd = this.Connection.CreateCommand())
			using (var dbTrans = this.Connection.BeginTransaction())
			{
				foreach (var entity in entities)
				{
					InsertOrUpdate(dbCmd, entity);
				}
				dbTrans.Commit();
			}
		}

		public void Delete<T>(T entity)
			where T : class, new()
		{
			using (var dbCmd = this.Connection.CreateCommand())
			{
				dbCmd.Delete(entity);
			}
		}

		public void DeleteById<T>(object id) where T : class, new()
		{
			using (var dbCmd = this.Connection.CreateCommand())
			{
				dbCmd.DeleteById<T>(id);
			}
		}

		public void DeleteByIds<T>(ICollection ids) where T : class, new()
		{
			using (var dbCmd = this.Connection.CreateCommand())
			{
				dbCmd.DeleteByIds<T>(ids);
			}
		}

		public void DeleteAll<TEntity>() where TEntity : class, new()
		{
			using (var dbCmd = this.Connection.CreateCommand())
			{
				dbCmd.DeleteAll<TEntity>();
			}
		}

		public void Dispose()
		{
			if (!DisposeConnection) return;
			if (this.connection == null) return;
			
			this.connection.Dispose();
			this.connection = null;
		}
	}
}