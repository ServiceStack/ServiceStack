using System;
using System.Configuration;
using System.Data;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.OrmLite.Firebird;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.FirebirdTests
{
	public class OrmLiteTestBase
	{		
		protected virtual string ConnectionString { get; set; }

		protected virtual string GetFileConnectionString() => FirebirdDb.DefaultConnection;
		protected virtual IOrmLiteDialectProvider GetDialectProvider() => FirebirdOrmLiteDialectProvider.Instance;

		protected void CreateNewDatabase()
		{
			ConnectionString = GetFileConnectionString();
		}

		[OneTimeSetUp]
		public void TestFixtureSetUp()
		{
			LogManager.LogFactory = new ConsoleLogFactory();

			OrmLiteConfig.DialectProvider = GetDialectProvider();
			ConnectionString = GetFileConnectionString();
		}

		public void Log(string text)
		{
			Console.WriteLine(text);
		}

        public IDbConnection OpenDbConnection(string connString = null)
        {
            connString ??= ConnectionString;
            return connString.OpenDbConnection();
        }
    }
}
