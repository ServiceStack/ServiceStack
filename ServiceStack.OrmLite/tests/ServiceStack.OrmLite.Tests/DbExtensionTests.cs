using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Issues;
using ServiceStack.OrmLite.Tests.Shared;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    public class DbExtensionTests : OrmLiteProvidersTestBase
    {
        public DbExtensionTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_get_TableName()
        {
            using var db = OpenDbConnection();
            var table1 = db.GetTableName<Table1>();
            var quotedTable1 = db.GetQuotedTableName<Table1>();

            Assert.That(table1.ToLower(), Is.EqualTo("table1"));
            Assert.That(quotedTable1.ToLower(), Is.EqualTo("\"table1\"").Or.EqualTo("`table1`"));

            if (Dialect == Dialect.Sqlite)
            {
                var tableSchema = db.GetTableName<ModelWithSchema>();
                var quotedTableSchema = db.GetQuotedTableName<ModelWithSchema>();

                Assert.That(tableSchema, Is.EqualTo("Schema_ModelWithSchema"));
                Assert.That(quotedTableSchema, Is.EqualTo("\"Schema_ModelWithSchema\""));
            }
        }
    }
}