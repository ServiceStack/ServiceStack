using System;
using NUnit.Framework;
using ServiceStack.OrmLite.SqlServer;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class SqlServerProviderTests
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			OrmLiteConfig.DialectProvider = SqlServerOrmLiteDialectProvider.Instance;			
		}

		public class OrderDataSubset
		{
			public DateTime CreatedDate { get; set; }
		}

		[Ignore("Integration test")][Test]
		public void Can_do_SqlServer_DateTime_fields()
		{
			var connString = "Data Source=chi-prod-analytics-db;Initial Catalog=2010-04_MonthlySnapshot;User Id=admin;Password=xxx;";

			using (var dbConn = connString.OpenDbConnection())
			using (var cmd = dbConn.CreateCommand())
			{
				var order = cmd.First<OrderDataSubset>("SELECT TOP 1 CreatedDate FROM OrderData");
				Console.WriteLine(order);
			}
		}
	}
}