using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.OrmLite.Tests;

namespace ServiceStack.OrmLite.PostgreSQL.Tests
{
    [TestFixtureOrmLiteDialects(Dialect.AnyPostgreSql)]    
    public class OrmLiteDropTableWithNamingStrategyTests : OrmLiteProvidersTestBase
    {
        public OrmLiteDropTableWithNamingStrategyTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_drop_TableWithNamingStrategy_table_PostgreSqlNamingStrategy()
        {
            using (new TemporaryNamingStrategy(DialectProvider, new PostgreSqlNamingStrategy()))
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);
                db.DropTable<ModelWithOnlyStringFields>();
                Assert.False(db.TableExists("model_with_only_string_fields"));
            }
        }
    }
}