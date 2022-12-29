using System;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.OrmLite.Firebird;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.FirebirdTests
{
	[TestFixture]
	public class FB4CreateTableTests : OrmLiteTestBase
	{
		protected override string GetFileConnectionString() => FirebirdDb.V4Connection;
		protected override IOrmLiteDialectProvider GetDialectProvider() => Firebird4OrmLiteDialectProvider.Instance;
        
		[Test]
		public void Does_table_Exists()
		{
            using (var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection())
			{
				db.DropTable<ModelWithAutoIncrement>();

				Assert.That(
					OrmLiteConfig.DialectProvider.DoesTableExist(db, nameof(ModelWithAutoIncrement)),
					Is.False);
				
				db.CreateTable<ModelWithAutoIncrement>(true);

				Assert.That(
					OrmLiteConfig.DialectProvider.DoesTableExist(db, nameof(ModelWithAutoIncrement)),
					Is.True);
			}
		}

		[Test]
		public void Can_create_ModelWithAutoIncrement_table()
		{
            using (var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection())
			{
				db.CreateTable<ModelWithAutoIncrement>(true);
				Assert.That(
					OrmLiteConfig.DialectProvider.DoesTableExist(db, nameof(ModelWithAutoIncrement)),
					Is.True);

				db.DropTable<ModelWithAutoIncrement>();
				Assert.That(
					OrmLiteConfig.DialectProvider.DoesTableExist(db, nameof(ModelWithAutoIncrement)),
					Is.False);
			}
		}

		[Test]
		public void Can_create_ModelWithAutoId_table()
		{
            using (var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection())
			{
				db.CreateTable<ModelWithAutoId>(true);
                Assert.That(
                    OrmLiteConfig.DialectProvider.DoesTableExist(db, nameof(ModelWithAutoId)),
                    Is.True);

                db.DropTable<ModelWithAutoId>();
                Assert.That(
                    OrmLiteConfig.DialectProvider.DoesTableExist(db, nameof(ModelWithAutoId)),
                    Is.False);
            }
        }

		[Test]
		public void Check_AutoIncrement_with_Identity()
		{
            using (var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection())
			{
				db.CreateTable<ModelWithAutoIncrement>(true);

				var id = db.Insert(new ModelWithAutoIncrement { Id = 1000, Name = "Isaac Newton"});
				var model = db.SingleById<ModelWithAutoIncrement>(1000);
				Assert.That(model, !Is.Null);
				Assert.That(model.Name, Is.EqualTo("Isaac Newton"));
				
				id = db.Insert(new ModelWithAutoIncrement { Name = "Alan Kay"});
				model = db.SingleById<ModelWithAutoIncrement>(1);
				Assert.That(model, !Is.Null);
				Assert.That(model.Name, Is.EqualTo("Alan Kay"));

				var rows = db.Select<ModelWithAutoIncrement>();
				Assert.That(rows, Has.Count.EqualTo(2));
			}
		}

        [Test]
        public void Check_AutoId_generation()
        {
            using (var db = new OrmLiteConnectionFactory(ConnectionString, Firebird4Dialect.Provider).OpenDbConnection())
            {
                db.CreateTable<ModelWithAutoId>(true);

                db.Insert(new ModelWithAutoId { Name = "Isaac Newton" });
                db.Insert(new ModelWithAutoId { Name = "Alan Kay" });
                var rows = db.Select<ModelWithAutoId>();
                Assert.That(rows, Has.Count.EqualTo(2));
            }
        }
    }
}