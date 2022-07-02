using System;
using System.Data;
using ServiceStack.Data;

namespace ServiceStack.OrmLite
{
    public class OrmLiteCommand : IDbCommand, IHasDbCommand, IHasDialectProvider
    {
        private readonly OrmLiteConnection dbConn;
        private readonly IDbCommand dbCmd;
        public IOrmLiteDialectProvider DialectProvider { get; set; }
        public bool IsDisposed { get; private set; }

        public OrmLiteCommand(OrmLiteConnection dbConn, IDbCommand dbCmd)
        {
            this.dbConn = dbConn;
            this.dbCmd = dbCmd;
            this.DialectProvider = dbConn.GetDialectProvider();
        }

        public Guid ConnectionId => dbConn.ConnectionId;

        public void Dispose()
        {
            IsDisposed = true;
            dbCmd.Dispose();
        }

        public void Prepare()
        {
            dbCmd.Prepare();
        }

        public void Cancel()
        {
            dbCmd.Cancel();
        }

        public IDbDataParameter CreateParameter()
        {
            return dbCmd.CreateParameter();
        }

        public int ExecuteNonQuery()
        {
            return dbCmd.ExecuteNonQuery();
        }

        public IDataReader ExecuteReader()
        {
            return dbCmd.ExecuteReader();
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            return dbCmd.ExecuteReader(behavior);
        }

        public object ExecuteScalar()
        {
            return dbCmd.ExecuteScalar();
        }

        public IDbConnection Connection
        {
            get => dbCmd.Connection;
            set => dbCmd.Connection = value;
        }
        public IDbTransaction Transaction
        {
            get => dbCmd.Transaction;
            set => dbCmd.Transaction = value;
        }
        public string CommandText
        {
            get => dbCmd.CommandText;
            set => dbCmd.CommandText = value;
        }
        public int CommandTimeout
        {
            get => dbCmd.CommandTimeout;
            set => dbCmd.CommandTimeout = value;
        }
        public CommandType CommandType
        {
            get => dbCmd.CommandType;
            set => dbCmd.CommandType = value;
        }
        public IDataParameterCollection Parameters => dbCmd.Parameters;

        public UpdateRowSource UpdatedRowSource
        {
            get => dbCmd.UpdatedRowSource;
            set => dbCmd.UpdatedRowSource = value;
        }

        public IDbCommand DbCommand => dbCmd;
    }
}