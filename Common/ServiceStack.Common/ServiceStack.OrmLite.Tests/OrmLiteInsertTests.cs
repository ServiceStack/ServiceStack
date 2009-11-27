using System;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class OrmLiteInsertTests
		: OrmLiteTestBase
	{

		[Test]
		public void Can_insert_into_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbConn = db.CreateCommand())
			{
				dbConn.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

				var row = ModelWithFieldsOfDifferentTypes.Create(1);

				dbConn.Insert(row);
			}
		}

		[Test]
		public void Can_insert_and_select_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbConn = db.CreateCommand())
			{
				dbConn.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

				var row = ModelWithFieldsOfDifferentTypes.Create(1);

				dbConn.Insert(row);

				var rows = dbConn.Select<ModelWithFieldsOfDifferentTypes>();

				Assert.That(rows, Has.Count(1));

				ModelWithFieldsOfDifferentTypes.AssertIsEqual(rows[0], row);
			}
		}

		[Test]
		public void Can_insert_table_with_null_fields()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbConn = db.CreateCommand())
			{
				dbConn.CreateTable<ModelWithIdAndName>(true);

				var row = ModelWithIdAndName.Create(1);
				row.Name = null;

				dbConn.Insert(row);

				var rows = dbConn.Select<ModelWithIdAndName>();

				Assert.That(rows, Has.Count(1));

				ModelWithIdAndName.AssertIsEqual(rows[0], row);
			}			
		}

		[Test]
		public void Can_insert_TaskQueue_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbConn = db.CreateCommand())
			{
				dbConn.CreateTable<TaskQueue>(true);

				var row = TaskQueue.Create(1);

				dbConn.Insert(row);

				var rows = dbConn.Select<TaskQueue>();

				Assert.That(rows, Has.Count(1));

				//Update the auto-increment id
				row.Id = rows[0].Id;

				TaskQueue.AssertIsEqual(rows[0], row);
			}
		}

	}

}