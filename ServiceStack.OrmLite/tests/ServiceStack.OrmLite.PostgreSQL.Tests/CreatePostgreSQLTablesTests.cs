using System;
using Npgsql;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]
    public class CreatePostgreSQLTablesTests : OrmLiteProvidersTestBase
    {
        public CreatePostgreSQLTablesTests(DialectContext context) : base(context) {}

        [Test]
        public void DropAndCreateTable_DropsTableAndCreatesTable()
        {
            using (var db = OpenDbConnection())
            {
                db.DropTable<TestData>();
                db.CreateTable<TestData>();
                db.Insert<TestData>(new TestData { Id = Guid.NewGuid() });
                db.DropAndCreateTable<TestData>();
                db.Insert<TestData>(new TestData { Id = Guid.NewGuid() });
            }
        }


        [Test]
        public void can_create_tables_after_UseUnicode_or_DefaultStringLength_changed()
        {
            //first one passes
            _reCreateTheTable();

            //all of these pass now:
            var stringConverter = DialectProvider.GetStringConverter();
            stringConverter.UseUnicode = true;
            _reCreateTheTable();

            stringConverter.UseUnicode = false;
            _reCreateTheTable();

            stringConverter.StringLength = 98765;

            _reCreateTheTable();
        }

        private void _reCreateTheTable()
        {
            using(var db = OpenDbConnection()) {
                db.CreateTable<CreatePostgreSQLTablesTests_dummy_table>(true);
            }
        }

        private class CreatePostgreSQLTablesTests_dummy_table
        {
            [AutoIncrement]
            public int Id { get; set; }

            public String StringNoExplicitLength { get; set; }

            [StringLength(100)]
            public String String100Characters { get; set; }
        }
          
        [Test]
        public void can_create_same_table_in_multiple_schemas_based_on_conn_string_search_path()
        {
            NpgsqlConnectionStringBuilder builder;
            var schema1 = "postgres_schema_1";
            var schema2 = "postgres_schema_2";   
            using (var db = OpenDbConnection())
            {
                builder = new NpgsqlConnectionStringBuilder(db.ConnectionString);
                CreateSchemaIfNotExists(db, schema1);
                CreateSchemaIfNotExists(db, schema2);
            
                builder.SearchPath = schema1;
                DbFactory.RegisterConnection(schema1, builder.ToString(), DialectProvider);
                builder.SearchPath = schema2;
                DbFactory.RegisterConnection(schema2, builder.ToString(), DialectProvider);
            }

            using (var dbS1 = DbFactory.OpenDbConnection(schema1))
            {
                dbS1.DropTable<CreatePostgreSQLTablesTests_dummy_table>();
                dbS1.CreateTable<CreatePostgreSQLTablesTests_dummy_table>();
                Assert.That(dbS1.Count<CreatePostgreSQLTablesTests_dummy_table>(), Is.EqualTo(0));
                dbS1.DropTable<CreatePostgreSQLTablesTests_dummy_table>();
            }

            using (var dbS2 = DbFactory.OpenDbConnection(schema2))
            {
                dbS2.DropTable<CreatePostgreSQLTablesTests_dummy_table>();
                dbS2.CreateTable<CreatePostgreSQLTablesTests_dummy_table>();
                Assert.That(dbS2.Count<CreatePostgreSQLTablesTests_dummy_table>(), Is.EqualTo(0));
                dbS2.DropTable<CreatePostgreSQLTablesTests_dummy_table>();
            }

        }

        public class TestData
        {
            [PrimaryKey]
            public Guid Id { get; set; }

            public string Name { get; set; }

            public string Surname { get; set; }
        }

        private void CreateSchemaIfNotExists(System.Data.IDbConnection db, string name)
        {
            string createSchemaSQL = @"DO $$
BEGIN

    IF NOT EXISTS(
        SELECT 1
          FROM INFORMATION_SCHEMA.SCHEMATA
          WHERE SCHEMA_NAME = '{0}'
      )
    THEN
      EXECUTE 'CREATE SCHEMA ""{0}""';
    END IF;

END
$$;"
                .Fmt(name);
            db.ExecuteSql(createSchemaSQL);
        }
    }
}
