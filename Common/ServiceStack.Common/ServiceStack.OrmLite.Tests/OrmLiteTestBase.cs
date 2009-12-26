using System;
using System.IO;
using NUnit.Framework;
using ServiceStack.Common.Utils;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.OrmLite.SqlServer;

namespace ServiceStack.OrmLite.Tests
{
	public class OrmLiteTestBase
	{
		protected virtual string ConnectionString { get; set; }

		protected string GetFileConnectionString()
		{
			var connectionString = "~/App_Data/db.sqlite".MapAbsolutePath();
			if (File.Exists(connectionString))
				File.Delete(connectionString);

			return connectionString;
		}

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			OrmLiteConfig.DialectProvider = new SqliteOrmLiteDialectProvider();
			ConnectionString = ":memory:";
			//ConnectionString = GetFileConnectionString();

			//OrmLiteWriteExtensions.DialectProvider = new SqlServerOrmLiteDialectProvider();
			//ConnectionString = "~/App_Data/Database1.mdf".MapAbsolutePath();			
		}

		public void Log(string text)
		{
			Console.WriteLine(text);
		}
	}
}