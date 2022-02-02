using System;
using System.Data;
using System.IO;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.OrmLite.Tests
{
    public class Config
    {
        public static string SqliteMemoryDb = ":memory:";
        public static string SqliteFileDir = "~/App_Data/".MapAbsolutePath();
        public static string SqliteFileDb = "~/App_Data/db.sqlite".MapAbsolutePath();
    }

	public class OrmLiteTestBase
	{
		protected virtual string ConnectionString { get; set; }

        private OrmLiteConnectionFactory DbFactory;

		protected virtual string GetFileConnectionString()
		{
            var connectionString = Config.SqliteFileDb;
			if (File.Exists(connectionString))
				File.Delete(connectionString);

			return connectionString;
		}

		protected void CreateNewDatabase()
		{
			if (ConnectionString.Contains(".sqlite"))
				ConnectionString = GetFileConnectionString();
		}

		[OneTimeSetUp]
		public void TestFixtureSetUp()
		{
			LogManager.LogFactory = new ConsoleLogFactory();

            OrmLiteConfig.DialectProvider = SqliteDialect.Provider;
			//ConnectionString = ":memory:";
			ConnectionString = GetFileConnectionString();
            
            DbFactory = new OrmLiteConnectionFactory(ConnectionString, SqliteDialect.Provider);

			//OrmLiteConfig.DialectProvider = SqlServerOrmLiteDialectProvider.Instance;
			//ConnectionString = "~/App_Data/Database1.mdf".MapAbsolutePath();			
		}

		public void Log(string text)
		{
			Console.WriteLine(text);
		}

        public IDbConnection InMemoryDbConnection { get; set; }

        public virtual IDbConnection OpenDbConnection()
        {
            if (ConnectionString == ":memory:")
            {
                if (InMemoryDbConnection == null)
                {
                    InMemoryDbConnection = new OrmLiteConnection(DbFactory);
                    InMemoryDbConnection.Open();
                }
                return InMemoryDbConnection;
            }

            return DbFactory.OpenDbConnection();
        }
    }
}