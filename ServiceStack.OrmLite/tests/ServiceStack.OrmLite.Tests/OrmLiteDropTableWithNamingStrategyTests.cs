using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.Tests
{
    [TestFixtureOrmLite]
    public class OrmLiteDropTableWithNamingStrategyTests : OrmLiteProvidersTestBase
    {
        public OrmLiteDropTableWithNamingStrategyTests(DialectContext context) : base(context) {}

        [Test]
        public void Can_drop_TableWithNamingStrategy_table_prefix()
        {
            using (new TemporaryNamingStrategy(DialectProvider, new PrefixNamingStrategy { TablePrefix = "tab_", ColumnPrefix = "col_" }))
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);

                db.DropTable<ModelWithOnlyStringFields>();

                Assert.False(db.TableExists("tab_ModelWithOnlyStringFields"));
            }
        }

        [Test]
        public void Can_drop_TableWithNamigStrategy_table_lowered()
        {
            using (new TemporaryNamingStrategy(DialectProvider, new LowercaseNamingStrategy()))
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);

                db.DropTable<ModelWithOnlyStringFields>();

                Assert.False(db.TableExists("modelwithonlystringfields"));
            }
        }


        [Test]
        public void Can_drop_TableWithNamigStrategy_table_nameUnderscoreCompound()
        {
            using (new TemporaryNamingStrategy(DialectProvider, new UnderscoreSeparatedCompoundNamingStrategy()))
            using (var db = OpenDbConnection())
            {
                db.CreateTable<ModelWithOnlyStringFields>(true);

                db.DropTable<ModelWithOnlyStringFields>();

                Assert.False(db.TableExists("model_with_only_string_fields"));
            }
        }
    }
}