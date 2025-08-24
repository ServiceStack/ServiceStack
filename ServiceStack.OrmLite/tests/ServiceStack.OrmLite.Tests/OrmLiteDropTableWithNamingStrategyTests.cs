using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class OrmLiteDropTableWithNamingStrategyTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
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
    public void Can_drop_TableWithNamingStrategy_table_lowered()
    {
        var strategy = new LowercaseNamingStrategy();
        using (new TemporaryNamingStrategy(DialectProvider, strategy))
        using (var db = OpenDbConnection())
        {
            var dialect = db.Dialect();
            Assert.That(strategy.GetTableName(nameof(ModelWithOnlyStringFields)), Is.EqualTo("model_with_only_string_fields"));
            Assert.That(dialect.GetQuotedTableName(typeof(ModelWithOnlyStringFields)), Is.EqualTo(dialect.GetQuotedName("model_with_only_string_fields")));
            Assert.That(dialect.GetQuotedTableName(nameof(ModelWithOnlyStringFields)), Is.EqualTo(dialect.GetQuotedName("model_with_only_string_fields")));
            
            db.CreateTable<ModelWithOnlyStringFields>(true);

            db.DropTable<ModelWithOnlyStringFields>();

            Assert.False(db.TableExists("model_with_only_string_fields"));
        }
    }


    [Test]
    public void Can_drop_TableWithNamingStrategy_table_nameUnderscoreCompound()
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