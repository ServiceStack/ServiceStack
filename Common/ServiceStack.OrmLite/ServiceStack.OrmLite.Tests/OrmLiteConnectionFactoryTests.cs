using Northwind.Common.DataModel;
using NUnit.Framework;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class OrmLiteConnectionFactoryTests
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			OrmLiteConfig.DialectProvider = SqliteOrmLiteDialectProvider.Instance;
		}

		[Test]
		public void AutoDispose_ConnectionFactory_disposes_connection()
		{
			var factory = new OrmLiteConnectionFactory(":memory:", true);

			using (var dbConn = factory.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.CreateTable<Shipper>(false);
				dbCmd.Insert(new Shipper { CompanyName = "I am shipper" });
			}

			using (var dbConn = factory.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.CreateTable<Shipper>(false);
				Assert.That(dbCmd.Select<Shipper>(), Has.Count.EqualTo(0));
			}
		}

		[Test]
		public void NonAutoDispose_ConnectionFactory_reuses_connection()
		{
			var factory = new OrmLiteConnectionFactory(":memory:", false);

			using (var dbConn = factory.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.CreateTable<Shipper>(false);
				dbCmd.Insert(new Shipper { CompanyName = "I am shipper" });
			}

			using (var dbConn = factory.OpenDbConnection())
			using (var dbCmd = dbConn.CreateCommand())
			{
				dbCmd.CreateTable<Shipper>(false);
				Assert.That(dbCmd.Select<Shipper>(), Has.Count.EqualTo(1));
			}
		}

	}
}