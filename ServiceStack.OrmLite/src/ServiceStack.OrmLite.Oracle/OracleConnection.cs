using System;
using System.Data;
using System.Data.Common;

namespace ServiceStack.OrmLite.Oracle
{
    public class OracleConnection : DbConnection
    {
        private readonly DbConnection _connection;
        public OracleConnection(DbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");

            _connection = connection;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return _connection.BeginTransaction(isolationLevel);
        }

        public override void Close()
        {
            _connection.Close();
        }

        public override void ChangeDatabase(string databaseName)
        {
            _connection.ChangeDatabase(databaseName);
        }

        public override void Open()
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }

        public override string ConnectionString
        {
            get { return _connection.ConnectionString; }
            set { _connection.ConnectionString = value; }
        }

        public override string Database
        {
            get { return _connection.Database; }
        }

        public override string DataSource
        {
            get { return _connection.DataSource; }
        }

        public override ConnectionState State
        {
            get { return _connection.State; }
        }

        public override string ServerVersion
        {
            get { return _connection.ServerVersion; }
        }

        protected override DbCommand CreateDbCommand()
        {
            return new OracleCommand(_connection.CreateCommand());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
