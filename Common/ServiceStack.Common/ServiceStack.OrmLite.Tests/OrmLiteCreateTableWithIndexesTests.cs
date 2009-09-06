using System;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class OrmLiteCreateTableWithIndexesTests 
		: OrmLiteTestBase
	{

		[Test]
		public void Can_create_ModelWithIndexFields_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbConn = db.CreateCommand())
			{
				dbConn.CreateTable<ModelWithIndexFields>(true);

				var sql = typeof(ModelWithIndexFields).ToCreateTableStatement();

				Assert.IsTrue(sql.Contains("idx_modelwithindexfields_name"));
				Assert.IsTrue(sql.Contains("uidx_modelwithindexfields_uniquename"));
			}
		}

		[Test]
		public void Can_create_ModelWithCompositeIndexFields_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbConn = db.CreateCommand())
			{
				dbConn.CreateTable<ModelWithCompositeIndexFields>(true);

				var sql = typeof(ModelWithCompositeIndexFields).ToCreateTableStatement();

				Assert.IsTrue(sql.Contains("idx_modelwithcompositeindexfields_name"));
				Assert.IsTrue(sql.Contains("idx_modelwithcompositeindexfields_composite1_composite2"));
			}
		}


	}
}