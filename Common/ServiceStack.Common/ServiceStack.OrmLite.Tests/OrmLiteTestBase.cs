using System;
using NUnit.Framework;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class OrmLiteTestBase
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			OrmLiteExtensions.DialectProvider = new SqliteOrmLiteDialectProvider();
		}

		public void Log(string text)
		{
			Console.WriteLine(text);
		}

		protected const string ConnectionString = ":memory:";
	}
}