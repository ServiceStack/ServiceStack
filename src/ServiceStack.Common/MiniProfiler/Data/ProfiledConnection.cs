using System;
using System.Data;
using System.Data.Common;
using ServiceStack.Data;

namespace ServiceStack.MiniProfiler.Data
{
    /// <summary>
    /// Wraps a database connection, allowing sql execution timings to be collected when a <see cref="IDbProfiler"/> session is started.
    /// </summary>
    public class ProfiledConnection : DbConnection, IHasDbConnection
    {
        private DbConnection _conn;
        private IDbProfiler _profiler;

        /// <summary>
        /// Returns a new <see cref="ProfiledConnection"/> that wraps <paramref name="connection"/>, 
        /// providing query execution profiling.  If profiler is null, no profiling will occur.
        /// </summary>
        /// <param name="connection">Your provider-specific flavor of connection, e.g. SqlConnection, OracleConnection</param>
        /// <param name="profiler">The currently started <see cref="IDbProfiler"/> or null.</param>
        /// <param name="autoDisposeConnection">Determines whether the ProfiledDbConnection will dispose the underlying connection.</param>
        public ProfiledConnection(DbConnection connection, IDbProfiler profiler, bool autoDisposeConnection = true)
        {
        	Init(connection, profiler, autoDisposeConnection);
        }

        private void Init(DbConnection connection, IDbProfiler profiler, bool autoDisposeConnection)
    	{
    		if (connection == null) throw new ArgumentNullException("connection");

    	    AutoDisposeConnection = autoDisposeConnection;
    		_conn = connection;
    		_conn.StateChange += StateChangeHandler;

    		if (profiler != null)
    		{
    			_profiler = profiler;
    		}
    	}

        public ProfiledConnection(IDbConnection connection, IDbProfiler profiler, bool autoDisposeConnection = true)
        {
    		var hasConn = connection as IHasDbConnection;
			if (hasConn != null) connection = hasConn.DbConnection;
    		var dbConn = connection as DbConnection;

			if (dbConn == null)
				throw new ArgumentException(connection.GetType().FullName + " does not inherit DbConnection");
			
			Init(dbConn, profiler, autoDisposeConnection);
        }


#pragma warning disable 1591 // xml doc comments warnings

        /// <summary>
        /// The underlying, real database connection to your db provider.
        /// </summary>
        public DbConnection InnerConnection
        {
            get { return _conn; }
            protected set { _conn = value; }
        }

        public IDbConnection DbConnection
        {
            get { return _conn; }
        }

        /// <summary>
        /// The current profiler instance; could be null.
        /// </summary>
        public IDbProfiler Profiler
        {
            get { return _profiler; }
            protected set { _profiler = value; }
        }

        /// <summary>
        /// The raw connection this is wrapping
        /// </summary>
        public DbConnection WrappedConnection
        {
            get { return _conn; }
        }

        protected
#if !NETSTANDARD1_3
        override
#endif
        bool CanRaiseEvents
        {
            get { return true; }
        }

        public override string ConnectionString
        {
            get { return _conn.ConnectionString; }
            set { _conn.ConnectionString = value; }
        }

        public override int ConnectionTimeout
        {
            get { return _conn.ConnectionTimeout; }
        }

        public override string Database
        {
            get { return _conn.Database; }
        }

        public override string DataSource
        {
            get { return _conn.DataSource; }
        }

        public override string ServerVersion
        {
            get { return _conn.ServerVersion; }
        }

        public override ConnectionState State
        {
            get { return _conn.State; }
        }

        protected bool AutoDisposeConnection { get; set; }

        public override void ChangeDatabase(string databaseName)
        {
            _conn.ChangeDatabase(databaseName);
        }

        public override void Close()
        {
            if (AutoDisposeConnection)
                _conn.Close();
        }

		//public override void EnlistTransaction(System.Transactions.Transaction transaction)
		//{
		//    _conn.EnlistTransaction(transaction);
		//}
#if !NETSTANDARD1_3
        public override DataTable GetSchema()
        {
            return _conn.GetSchema();
        }

        public override DataTable GetSchema(string collectionName)
        {
            return _conn.GetSchema(collectionName);
        }

        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            return _conn.GetSchema(collectionName, restrictionValues);
        }
#endif

        public override void Open()
        {
            if (_conn.State != ConnectionState.Open)
                _conn.Open();
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return new ProfiledDbTransaction(_conn.BeginTransaction(isolationLevel), this);
        }

        protected override DbCommand CreateDbCommand()
        {
            return new ProfiledCommand(_conn.CreateCommand(), this, _profiler);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _conn != null)
            {
                _conn.StateChange -= StateChangeHandler;
                if (AutoDisposeConnection)
                    _conn.Dispose();
            }
            _conn = null;
            _profiler = null;
            base.Dispose(disposing);
        }

        void StateChangeHandler(object sender, StateChangeEventArgs e)
        {
            OnStateChange(e);
        }
    }
}

#pragma warning restore 1591 // xml doc comments warnings