//
// ServiceStack.OrmLite: Light-weight POCO ORM for .NET and Mono
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System.Collections;
using System.Collections.Generic;
using System.Data;
using ServiceStack.Data;

namespace ServiceStack.OrmLite
{
    /// <summary>
    /// Allow for code-sharing between OrmLite, IPersistenceProvider and ICacheClient
    /// </summary>
    public class OrmLitePersistenceProvider
        : IEntityStore
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
                    var connStr = this.ConnectionString;
                    connection = connStr.OpenDbConnection();
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

        private IDbCommand CreateCommand()
        {
            var cmd = this.Connection.CreateCommand();
            cmd.CommandTimeout = OrmLiteConfig.CommandTimeout;
            return cmd;
        }

        public T GetById<T>(object id)
        {
            return this.Connection.SingleById<T>(id);
        }

        public IList<T> GetByIds<T>(ICollection ids)
        {
            return this.Connection.SelectByIds<T>(ids);
        }

        public T Store<T>(T entity)
        {
            this.Connection.Save(entity);
            return entity;
        }

        public void StoreAll<TEntity>(IEnumerable<TEntity> entities)
        {
            this.Connection.SaveAll(entities);
        }

        public void Delete<T>(T entity)
        {
            this.Connection.DeleteById<T>(entity.GetId());
        }

        public void DeleteById<T>(object id)
        {
            this.Connection.DeleteById<T>(id);
        }

        public void DeleteByIds<T>(ICollection ids)
        {
            this.Connection.DeleteByIds<T>(ids);
        }

        public void DeleteAll<TEntity>()
        {
            this.Connection.DeleteAll<TEntity>();
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