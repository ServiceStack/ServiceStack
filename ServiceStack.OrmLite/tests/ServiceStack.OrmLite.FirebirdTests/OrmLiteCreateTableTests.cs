using System;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.OrmLite.Firebird;

namespace ServiceStack.OrmLite.FirebirdTests
{
	[TestFixture]
	public class OrmLiteCreateTableTests : OrmLiteTestBase
	{
		[Test]
		public void Does_table_Exists()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.DropTable<ModelWithIdOnly>();

			Assert.That(
				OrmLiteConfig.DialectProvider.DoesTableExist(db, nameof(ModelWithIdOnly)),
				Is.False);
				
			db.CreateTable<ModelWithIdOnly>(true);

			Assert.That(
				OrmLiteConfig.DialectProvider.DoesTableExist(db, nameof(ModelWithIdOnly)),
				Is.True);
		}

		[Test]
		public void Can_create_ModelWithIdOnly_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithIdOnly>(true);
		}

		[Test]
		public void Can_create_ModelWithOnlyStringFields_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithOnlyStringFields>(true);
		}

		[Test]
		public void Can_create_ModelWithLongIdAndStringFields_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithLongIdAndStringFields>(true);
		}

		[Test]
		public void Can_create_ModelWithFieldsOfDifferentTypes_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithFieldsOfDifferentTypes>(true);
		}

        [Test]
        public void Can_create_ModelWithReservedWords_table()
        {
            using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
            db.CreateTable<ModelWithReservedWords>(true);
        }

        [Test]
		public void Can_preserve_ModelWithIdOnly_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithIdOnly>(true);

			db.Insert(new ModelWithIdOnly(1));
			db.Insert(new ModelWithIdOnly(2));

			db.CreateTable<ModelWithIdOnly>(false);

			var rows = db.Select<ModelWithIdOnly>();

			Assert.That(rows, Has.Count.EqualTo(2));
		}

		[Test]
		public void Can_preserve_ModelWithIdAndName_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithIdAndName>(true);
			db.DeleteAll<ModelWithIdAndName>();

			db.Insert(new ModelWithIdAndName(0));
			db.Insert(new ModelWithIdAndName(0));

			db.CreateTable<ModelWithIdAndName>(false);

			var rows = db.Select<ModelWithIdAndName>();

			Assert.That(rows, Has.Count.EqualTo(2));
		}

		[Test]
		public void Can_overwrite_ModelWithIdOnly_table()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTable<ModelWithIdOnly>(true);

			db.Insert(new ModelWithIdOnly(1));
			db.Insert(new ModelWithIdOnly(2));

			db.CreateTable<ModelWithIdOnly>(true);

			var rows = db.Select<ModelWithIdOnly>();

			Assert.That(rows, Has.Count.EqualTo(0));
		}

		[Test]
		public void Can_create_multiple_tables()
		{
			using var db = new OrmLiteConnectionFactory(ConnectionString, FirebirdDialect.Provider).Open();
			db.CreateTables(true, typeof(ModelWithIdOnly), typeof(ModelWithIdAndName));

			db.Insert(new ModelWithIdOnly(1));
			db.Insert(new ModelWithIdOnly(2));

			db.Insert(new ModelWithIdAndName(0));
			db.Insert(new ModelWithIdAndName(0));

			var rows1 = db.Select<ModelWithIdOnly>();
			var rows2 = db.Select<ModelWithIdOnly>();

			Assert.That(rows1, Has.Count.EqualTo(2));
			Assert.That(rows2, Has.Count.EqualTo(2));
		}


		[Test]
		public void Can_create_ModelWithIdAndName_table_with_specified_DefaultStringLength()
		{
			OrmLiteConfig.DialectProvider.GetStringConverter().StringLength = 255;
			var createTableSql = OrmLiteConfig.DialectProvider.ToCreateTableStatement(typeof(ModelWithIdAndName));

			Console.WriteLine("createTableSql: " + createTableSql);
			Assert.That(createTableSql.Contains("VARCHAR(255)"), Is.True);
		}

	}
}