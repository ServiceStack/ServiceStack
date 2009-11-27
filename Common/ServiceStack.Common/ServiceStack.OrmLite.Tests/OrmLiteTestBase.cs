using System;
using NUnit.Framework;
using ServiceStack.Common.Utils;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.OrmLite.SqlServer;

namespace ServiceStack.OrmLite.Tests
{
	public class OrmLiteTestBase
	{
		protected string ConnectionString { get; set; }

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			OrmLiteExtensions.DialectProvider = new SqliteOrmLiteDialectProvider();
			ConnectionString = ":memory:";

			//OrmLiteExtensions.DialectProvider = new SqlServerOrmLiteDialectProvider();
			//ConnectionString = "~/App_Data/Database1.mdf".MapAbsolutePath();			
		}

		public void Log(string text)
		{
			Console.WriteLine(text);
		}
	}
}