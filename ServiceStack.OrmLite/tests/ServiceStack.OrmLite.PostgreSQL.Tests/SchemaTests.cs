using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class SchemaTests : OrmLiteProvidersTestBase
    {
        public SchemaTests(DialectContext context) : base(context) {}

        [Alias("TestSchemaUser")]
        [Schema("TestSchema")]
        public class User
        {
            [AutoIncrement]
            public int Id { get; set; }

            [Index]
            public string Name { get; set; }

            public DateTime CreatedDate { get; set; }
        }

        private void CreateSchemaIfNotExists()
        {
            const string createSchemaSQL = @"DO $$
BEGIN

    IF NOT EXISTS(
        SELECT 1
          FROM INFORMATION_SCHEMA.SCHEMATA
          WHERE SCHEMA_NAME = 'TestSchema'
      )
    THEN
      EXECUTE 'CREATE SCHEMA ""TestSchema""';
    END IF;

END
$$;";
            using (var db = OpenDbConnection())
            {
                db.ExecuteSql(createSchemaSQL);
            }
        }

        [Test]
        public void Can_Create_Tables_With_Schema_in_PostgreSQL()
        {
            using (var db = OpenDbConnection())
            {
                CreateSchemaIfNotExists();
                db.DropAndCreateTable<User>();

                var tables = db.Single<string>(@"SELECT '[' || n.nspname || '].[' || c.relname ||']' FROM pg_class c LEFT JOIN pg_namespace n ON n.oid = c.relnamespace WHERE c.relname = 'test_schema_user' AND n.nspname = 'TestSchema'");
                
                // PostgreSQL dialect should create the table in the schema
                Assert.
                    That(tables.Contains("[TestSchema].[test_schema_user]"));
            }
        }

        [Test]
        public void Can_Perform_CRUD_Operations_On_Table_With_Schema()
        {
            using (var db = OpenDbConnection())
            using (var dbCmd = db.CreateCommand())
            {
                CreateSchemaIfNotExists();
                db.CreateTable<User>(true);

                db.Insert(new User { Id = 1, Name = "A", CreatedDate = DateTime.Now });
                db.Insert(new User { Id = 2, Name = "B", CreatedDate = DateTime.Now });
                db.Insert(new User { Id = 3, Name = "B", CreatedDate = DateTime.Now });

                var lastInsertId = db.LastInsertId();
                Assert.That(lastInsertId, Is.GreaterThan(0));

                var rowsB = db.Select<User>("\"name\" = @name", new { name = "B" });
                Assert.That(rowsB, Has.Count.EqualTo(2));

                var rowIds = rowsB.ConvertAll(x => x.Id);
                Assert.That(rowIds, Is.EquivalentTo(new List<long> { 2, 3 }));

                rowsB.ForEach(x => db.Delete(x));

                rowsB = db.Select<User>("\"name\" = @name", new { name = "B" });
                Assert.That(rowsB, Has.Count.EqualTo(0));

                var rowsLeft = db.Select<User>();
                Assert.That(rowsLeft, Has.Count.EqualTo(1));

                Assert.That(rowsLeft[0].Name, Is.EqualTo("A"));
            }
        }
    }
}
