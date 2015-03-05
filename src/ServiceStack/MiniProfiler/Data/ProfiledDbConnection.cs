using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using ServiceStack.Data;

namespace ServiceStack.MiniProfiler.Data
{
    /// <summary>
    /// Wraps a database connection, allowing sql execution timings to be collected when a <see cref="MiniProfiler.Profiler"/> session is started.
    /// </summary>
    public class ProfiledDbConnection : DbConnection, ICloneable, IHasDbConnection
    {
        /// <summary>
        /// This will be made private; use <see cref="InnerConnection"/>
        /// </summary>
        protected DbConnection _conn; // TODO: in MiniProfiler 2.0, make private

        protected bool autoDisposeConnection;

        /// <summary>
        /// The underlying, real database connection to your db provider.
        /// </summary>
        public DbConnection InnerConnection
        {
            get { return _conn; }
        }

        public IDbConnection DbConnection
        {
            get { return _conn; }
        }

        /// <summary>
        /// This will be made private; use <see cref="Profiler"/>
        /// </summary>
        protected IDbProfiler _profiler; // TODO: in MiniProfiler 2.0, make private
        /// <summary>
        /// The current profiler instance; could be null.
        /// </summary>
        public IDbProfiler Profiler
        {
            get { return _profiler; }
        }

        /// <summary>
        /// Returns a new <see cref="ProfiledDbConnection"/> that wraps <paramref name="connection"/>, 
        /// providing query execution profiling.  If profiler is null, no profiling will occur.
        /// </summary>
        /// <param name="connection">Your provider-specific flavor of connection, e.g. SqlConnection, OracleConnection</param>
        /// <param name="profiler">The currently started <see cref="MiniProfiler.Profiler"/> or null.</param>
        /// <param name="autoDisposeConnection">Determines whether the ProfiledDbConnection will dispose the underlying connection.</param>
        public ProfiledDbConnection(DbConnection connection, IDbProfiler profiler, bool autoDisposeConnection = true)
        {
        	Init(connection, profiler, autoDisposeConnection);
        }

        private void Init(DbConnection connection, IDbProfiler profiler, bool autoDisposeConnection)
    	{
    		if (connection == null) throw new ArgumentNullException("connection");

    	    this.autoDisposeConnection = autoDisposeConnection;
    		_conn = connection;
    		_conn.StateChange += StateChangeHandler;

    		if (profiler != null)
    		{
    			_profiler = profiler;
    		}
    	}

        public ProfiledDbConnection(IDbConnection connection, IDbProfiler profiler, bool autoDisposeConnection=true)
        {
    		var hasConn = connection as IHasDbConnection;
			if (hasConn != null) connection = hasConn.DbConnection;
    		var dbConn = connection as DbConnection;

			if (dbConn == null)
				throw new ArgumentException(connection.GetType().GetOperationName() + " does not inherit DbConnection");
			
			Init(dbConn, profiler, autoDisposeConnection);
        }


#pragma warning disable 1591 // xml doc comments warnings


        /// <summary>
        /// The raw connection this is wrapping
        /// </summary>
        public DbConnection WrappedConnection
        {
            get { return _conn; }
        }

        protected override bool CanRaiseEvents
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

        public override void ChangeDatabase(string databaseName)
        {
            _conn.ChangeDatabase(databaseName);
        }

        public override void Close()
        {
            if (autoDisposeConnection)
                _conn.Close();
        }

		//public override void EnlistTransaction(System.Transactions.Transaction transaction)
		//{
		//    _conn.EnlistTransaction(transaction);
		//}

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

        public override void Open()
        {
            if (_conn.State != ConnectionState.Open)
                _conn.Open();
        }

        protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel)
        {
            return new ProfiledDbTransaction(_conn.BeginTransaction(isolationLevel), this);
        }

        protected override DbCommand CreateDbCommand()
        {
            return new ProfiledDbCommand(_conn.CreateCommand(), this, _profiler);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _conn != null)
            {
                _conn.StateChange -= StateChangeHandler;
                if (autoDisposeConnection)
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

        public ProfiledDbConnection Clone()
        {
            ICloneable tail = _conn as ICloneable;
            if (tail == null) throw new NotSupportedException("Underlying " + _conn.GetType().GetOperationName() + " is not cloneable");
            return new ProfiledDbConnection((DbConnection)tail.Clone(), _profiler, autoDisposeConnection);
        }
        object ICloneable.Clone() { return Clone(); }
    }
}

#pragma warning restore 1591 // xml doc comments warnings