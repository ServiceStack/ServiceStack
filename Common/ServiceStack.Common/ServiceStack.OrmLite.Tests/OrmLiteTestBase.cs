using System;
using NUnit.Framework;
using ServiceStack.Common.Utils;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.OrmLite.SqlServer;

namespace ServiceStack.OrmLite.Tests
{
	public class OrmLiteTestBase
	{
		//protected const string ConnectionString = ":memory:";
		protected string ConnectionString = "~/App_Data/Database1.mdf".MapAbsolutePath();

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			OrmLiteExtensions.DialectProvider = new SqliteOrmLiteDialectProvider();
			OrmLiteExtensions.DialectProvider = new SqlServerOrmLiteDialectProvider();
		}

		public void Log(string text)
		{
			Console.WriteLine(text);
		}
	}
}