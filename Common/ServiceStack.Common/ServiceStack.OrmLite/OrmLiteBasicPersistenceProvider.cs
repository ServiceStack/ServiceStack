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
	public class OrmLiteBasicPersistenceProvider
		: IBasicPersistenceProvider
	{
		protected string ConnectionString { get; set; }
		protected bool disposeConnection = true;

		protected IDbConnection connection;
		protected IDbConnection Connection
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

		public OrmLiteBasicPersistenceProvider(string connectionString)
		{
			ConnectionString = connectionString;
		}

		public OrmLiteBasicPersistenceProvider(IDbConnection connection)
		{
			this.connection = connection;
			this.disposeConnection = false;
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
		}

		public void Delete<T>(T entity)
			where T : class, new()
		{
			using (var dbCmd = this.Connection.CreateCommand())
			{
				dbCmd.Delete(entity);
			}
		}

		public void Dispose()
		{
			if (!disposeConnection) return;
			if (this.connection == null) return;
			
			this.connection.Dispose();
			this.connection = null;
		}
	}
}