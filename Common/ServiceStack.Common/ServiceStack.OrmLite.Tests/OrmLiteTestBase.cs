using System;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Logging;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.OrmLite.Tests.Models;

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