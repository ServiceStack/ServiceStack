using System;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class OrmLiteComplexTypesTests
		: OrmLiteTestBase
	{

		[Test]
		public void Can_insert_into_ModelWithComplexTypes_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbConn = db.CreateCommand())
			{
				dbConn.CreateTable<ModelWithComplexTypes>(true);

				var row = ModelWithComplexTypes.Create(1);

				dbConn.Insert(row);
			}
		}

		[Test]
		public void Can_insert_and_select_from_ModelWithComplexTypes_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbConn = db.CreateCommand())
			{
				dbConn.CreateTable<ModelWithComplexTypes>(true);

				var row = ModelWithComplexTypes.Create(1);

				dbConn.Insert(row);

				var rows = dbConn.Select<ModelWithComplexTypes>();

				Assert.That(rows, Has.Count(1));

				ModelWithComplexTypes.AssertIsEqual(rows[0], row);
			}
		}


	}

}