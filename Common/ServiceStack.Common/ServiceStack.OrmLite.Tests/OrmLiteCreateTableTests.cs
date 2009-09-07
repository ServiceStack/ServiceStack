using System;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.OrmLite.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
	[TestFixture]
	public class OrmLiteCreateTableTests 
		: OrmLiteTestBase
	{

		[Test]
		public void Can_create_ModelWithIdOnly_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithIdOnly>(true);
			}
		}

		[Test]
		public void Can_create_ModelWithOnlyStringFields_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithOnlyStringFields>(true);
			}
		}

		[Test]
		public void Can_create_ModelWithLongIdAndStringFields_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithLongIdAndStringFields>(true);
			}
		}

		[Test]
		public void Can_create_ModelWithFieldsOfDifferentTypes_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithFieldsOfDifferentTypes>(true);
			}
		}

		[Test]
		public void Can_preserve_ModelWithIdOnly_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithIdOnly>(true);

				dbCmd.Insert(new ModelWithIdOnly(1));
				dbCmd.Insert(new ModelWithIdOnly(2));

				dbCmd.CreateTable<ModelWithIdOnly>(false);

				var rows = dbCmd.Select<ModelWithIdOnly>();

				Assert.That(rows, Has.Count(2));
			}
		}

		[Test]
		public void Can_preserve_ModelWithIdAndName_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithIdAndName>(false);

				dbCmd.Insert(new ModelWithIdAndName(1));
				dbCmd.Insert(new ModelWithIdAndName(2));

				dbCmd.CreateTable<ModelWithIdAndName>(false);

				var rows = dbCmd.Select<ModelWithIdAndName>();

				Assert.That(rows, Has.Count(2));
			}
		}

		[Test]
		public void Can_overwrite_ModelWithIdOnly_table()
		{
			using (var db = ConnectionString.OpenDbConnection())
			using (var dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<ModelWithIdOnly>(true);

				dbCmd.Insert(new ModelWithIdOnly(1));
				dbCmd.Insert(new ModelWithIdOnly(2));

				dbCmd.CreateTable<ModelWithIdOnly>(true);

				var rows = dbCmd.Select<ModelWithIdOnly>();

				Assert.That(rows, Has.Count(0));
			}
		}

	}
}