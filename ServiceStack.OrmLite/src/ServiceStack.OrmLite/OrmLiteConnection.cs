using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Data;
using ServiceStack.Text;

namespace ServiceStack.OrmLite
{
    /// <summary>
    /// Wrapper IDbConnection class to allow for connection sharing, mocking, etc.
    /// </summary>
    public class OrmLiteConnection
        : IDbConnection, IHasDbConnection, IHasDbTransaction, ISetDbTransaction, IHasDialectProvider
    {
        public readonly OrmLiteConnectionFactory Factory;
        public IDbTransaction Transaction { get; set; }
        public IDbTransaction DbTransaction => Transaction;
        private IDbConnection dbConnection;

        public IOrmLiteDialectProvider DialectProvider { get; set; }
        public string LastCommandText { get; set; }
        public int? CommandTimeout { get; set; }
        public Guid ConnectionId { get; set; }

        public OrmLiteConnection(OrmLiteConnectionFactory factory)
        {
            this.Factory = factory;
            this.DialectProvider = factory.DialectProvider;
        }

        public IDbConnection DbConnection => dbConnection ??= ConnectionString.ToDbConnection(Factory.DialectProvider);

        public void Dispose()
        {
            Factory.OnDispose?.Invoke(this);
            if (!Factory.AutoDisposeConnection) return;

            DbConnection.Dispose();
            dbConnection = null;
        }

        public IDbTransaction BeginTransaction()
        {
            if (Factory.AlwaysReturnTransaction != null)
                return Factory.AlwaysReturnTransaction;

            return DbConnection.BeginTransaction();
        }

        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            if (Factory.AlwaysReturnTransaction != null)
                return Factory.AlwaysReturnTransaction;

            return DbConnection.BeginTransaction(isolationLevel);
        }

        public void Close()
        {
#if NET472 || NET6_0_OR_GREATER
            var id = Diagnostics.OrmLite.WriteConnectionCloseBefore(DbConnection);
#endif
            var connectionId = DbConnection.GetConnectionId();
            Exception e = null;
            try
            {
                DbConnection.Close();
            }
            catch (Exception ex)
            {
                e = ex;
                throw;
            }
            finally
            {
#if NET472 || NET6_0_OR_GREATER
                if (e != null)
                    Diagnostics.OrmLite.WriteConnectionCloseError(id, connectionId, DbConnection, e);
                else
                    Diagnostics.OrmLite.WriteConnectionCloseAfter(id, connectionId, DbConnection);
#endif
            }
        }

        public void ChangeDatabase(string databaseName)
        {
            DbConnection.ChangeDatabase(databaseName);
        }

        public IDbCommand CreateCommand()
        {
            if (Factory.AlwaysReturnCommand != null)
                return Factory.AlwaysReturnCommand;

            var cmd = DbConnection.CreateCommand();

            return cmd;
        }

        public void Open()
        {
            if (DbConnection.State == ConnectionState.Broken)
                DbConnection.Close();

            if (DbConnection.State == ConnectionState.Closed)
            {
#if NET472 || NET6_0_OR_GREATER
                var id = Diagnostics.OrmLite.WriteConnectionOpenBefore(DbConnection);
#endif
                Exception e = null;
                try
                {
                    DbConnection.Open();
                    //so the internal connection is wrapped for example by miniprofiler
                    if (Factory.ConnectionFilter != null)
                        dbConnection = Factory.ConnectionFilter(dbConnection);

                    DialectProvider.InitConnection(dbConnection);
                }
                catch (Exception ex)
                {
                    e = ex;
                    throw;
                }
                finally
                {
#if NET472 || NET6_0_OR_GREATER
                    if (e != null)
                        Diagnostics.OrmLite.WriteConnectionOpenError(id, DbConnection, e);
                    else
                        Diagnostics.OrmLite.WriteConnectionOpenAfter(id, DbConnection);
#endif
                }
            }
        }

        public async Task OpenAsync(CancellationToken token = default)
        {
            if (DbConnection.State == ConnectionState.Broken)
                DbConnection.Close();

            if (DbConnection.State == ConnectionState.Closed)
            {
#if NET472 || NET6_0_OR_GREATER
                var id = Diagnostics.OrmLite.WriteConnectionOpenBefore(DbConnection);
#endif
                Exception e = null;
                try
                {
                    await DialectProvider.OpenAsync(DbConnection, token).ConfigAwait();
                    //so the internal connection is wrapped for example by miniprofiler
                    if (Factory.ConnectionFilter != null)
                        dbConnection = Factory.ConnectionFilter(dbConnection);

                    DialectProvider.InitConnection(dbConnection);
                }
                catch (Exception ex)
                {
                    e = ex;
                    throw;
                }
                finally
                {
#if NET472 || NET6_0_OR_GREATER
                    if (e != null)
                        Diagnostics.OrmLite.WriteConnectionOpenError(id, DbConnection, e);
                    else
                        Diagnostics.OrmLite.WriteConnectionOpenAfter(id, DbConnection);
#endif
                }
            }
        }

        private string connectionString;
        public string ConnectionString
        {
            get => connectionString ?? Factory.ConnectionString;
            set => connectionString = value;
        }

        public int ConnectionTimeout => DbConnection.ConnectionTimeout;

        public string Database => DbConnection.Database;

        public ConnectionState State => DbConnection.State;

        public bool AutoDisposeConnection { get; set; }

        public static explicit operator DbConnection(OrmLiteConnection dbConn)
        {
            return (DbConnection)dbConn.DbConnection;
        }
    }

    internal interface ISetDbTransaction
    {
        IDbTransaction Transaction { get; set; }
    }

    public static class OrmLiteConnectionUtils
    {
        public static bool InTransaction(this IDbConnection db) => 
            db is IHasDbTransaction { DbTransaction: {} };

        public static IDbTransaction GetTransaction(this IDbConnection db) => 
            db is IHasDbTransaction setDb ? setDb.DbTransaction : null;
    }
}