using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class OrmLiteSaveTests
		: OrmLiteTestBase
	{

		[Test]
		public void Can_Save_into_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbConn = db.CreateCommand())
			{
				dbConn.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

				var row = ModelWithFieldsOfDifferentTypes.Create(1);

				dbConn.Save(row);
			}
		}

		[Test]
		public void Can_Save_and_select_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbConn = db.CreateCommand())
			{
				dbConn.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

				var row = ModelWithFieldsOfDifferentTypes.Create(1);

				dbConn.Save(row);

				var rows = dbConn.Select<ModelWithFieldsOfDifferentTypes>();

				Assert.That(rows, Has.Count(1));

				ModelWithFieldsOfDifferentTypes.AssertIsEqual(rows[0], row);
			}
		}

		[Test]
		public void Can_SaveAll_and_select_from_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbConn = db.CreateCommand())
			{
				dbConn.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

				var rowIds = new List<int> { 1, 2, 3, 4, 5 };
				var newRows = rowIds.ConvertAll(x => ModelWithFieldsOfDifferentTypes.Create(x));

				dbConn.SaveAll(newRows);

				var rows = dbConn.Select<ModelWithFieldsOfDifferentTypes>();

				Assert.That(rows, Has.Count(newRows.Count));
			}
		}

		[Test]
		public void Can_SaveAll_and_select_from_ModelWithFieldsOfDifferentTypes_table_with_no_ids()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbConn = db.CreateCommand())
			{
				dbConn.CreateTable<ModelWithFieldsOfDifferentTypes>(true);

				var rowIds = new List<int> { 1, 2, 3, 4, 5 };
				var newRows = rowIds.ConvertAll(x => ModelWithFieldsOfDifferentTypes.Create(default(int)));

				dbConn.SaveAll(newRows);

				var rows = dbConn.Select<ModelWithFieldsOfDifferentTypes>();

				Assert.That(rows, Has.Count(newRows.Count));
			}
		}

		[Test]
		public void Can_Save_table_with_null_fields()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbConn = db.CreateCommand())
			{
				dbConn.CreateTable<ModelWithIdAndName>(true);

				var row = ModelWithIdAndName.Create(1);
				row.Name = null;

				dbConn.Save(row);

				var rows = dbConn.Select<ModelWithIdAndName>();

				Assert.That(rows, Has.Count(1));

				ModelWithIdAndName.AssertIsEqual(rows[0], row);
			}
		}

		[Test]
		public void Can_Save_TaskQueue_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbConn = db.CreateCommand())
			{
				dbConn.CreateTable<TaskQueue>(true);

				var row = TaskQueue.Create(1);

				dbConn.Save(row);

				var rows = dbConn.Select<TaskQueue>();

				Assert.That(rows, Has.Count(1));

				//Update the auto-increment id
				row.Id = rows[0].Id;

				TaskQueue.AssertIsEqual(rows[0], row);
			}
		}

	}
}