using System.Data;

namespace ServiceStack.OrmLite
{
	/// <summary>
	/// Wrapper IDbConnection class to allow for connection sharing, mocking, etc.
	/// </summary>
	public class OrmLiteConnection
		: IDbConnection
	{
		private readonly OrmLiteConnectionFactory factory;
		private IDbConnection dbConnection;
		private bool isOpen;

		public OrmLiteConnection(OrmLiteConnectionFactory factory)
		{
			this.factory = factory;
		}

		public IDbConnection DbConnection
		{
			get
			{
				if (dbConnection == null)
				{
					dbConnection = factory.ConnectionString.ToDbConnection();
				}
				return dbConnection;
			}
		}

		public void Dispose()
		{
			if (!factory.AutoDisposeConnection) return;

			DbConnection.Dispose();
			dbConnection = null;
			isOpen = false;
		}

		public IDbTransaction BeginTransaction()
		{
			if (factory.AlwaysReturnTransaction != null)
				return factory.AlwaysReturnTransaction;

			return DbConnection.BeginTransaction();
		}

		public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			if (factory.AlwaysReturnTransaction != null)
				return factory.AlwaysReturnTransaction;

			return DbConnection.BeginTransaction(isolationLevel);
		}

		public void Close()
		{
			DbConnection.Close();
		}

		public void ChangeDatabase(string databaseName)
		{
			DbConnection.ChangeDatabase(databaseName);
		}

		public IDbCommand CreateCommand()
		{
			if (factory.AlwaysReturnCommand != null)
				return factory.AlwaysReturnCommand;

			return DbConnection.CreateCommand();
		}

		public void Open()
		{
			if (isOpen) return;
			
			DbConnection.Open();
			isOpen = true;
		}

		public string ConnectionString
		{
			get { return factory.ConnectionString; }
			set { factory.ConnectionString = value; }
		}

		public int ConnectionTimeout
		{
			get { return DbConnection.ConnectionTimeout; }
		}

		public string Database
		{
			get { return DbConnection.Database; }
		}

		public ConnectionState State
		{
			get { return DbConnection.State; }
		}
	}
}