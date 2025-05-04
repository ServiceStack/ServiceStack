using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class OrmLiteDropTableTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_drop_existing_table()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable(typeof(ModelWithIdOnly));
        db.DropAndCreateTable<ModelWithIdAndName>();

        Assert.That(
            db.TableExists(nameof(ModelWithIdOnly).SqlTableRaw(DialectProvider)),
            Is.True);
        Assert.That(
            db.TableExists(nameof(ModelWithIdAndName).SqlTableRaw(DialectProvider)),
            Is.True);

        db.DropTable<ModelWithIdOnly>();
        db.DropTable(typeof(ModelWithIdAndName));

        Assert.That(
            db.TableExists(nameof(ModelWithIdOnly).SqlTableRaw(DialectProvider)),
            Is.False);
        Assert.That(
            db.TableExists(nameof(ModelWithIdAndName).SqlTableRaw(DialectProvider)),
            Is.False);
    }

    [Test]
    public void Can_drop_multiple_tables()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTables(typeof(ModelWithIdOnly), typeof(ModelWithIdAndName));

        Assert.That(
            db.TableExists(nameof(ModelWithIdOnly).SqlTableRaw(DialectProvider)),
            Is.True);
        Assert.That(
            db.TableExists(nameof(ModelWithIdAndName).SqlTableRaw(DialectProvider)),
            Is.True);

        db.DropTables(typeof(ModelWithIdOnly), typeof(ModelWithIdAndName));

        Assert.That(
            db.TableExists(nameof(ModelWithIdOnly).SqlTableRaw(DialectProvider)),
            Is.False);
        Assert.That(
            db.TableExists(nameof(ModelWithIdAndName).SqlTableRaw(DialectProvider)),
            Is.False);
    }

    [Test]
    public void Can_drop_table_with_index()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTables(typeof(ModelWithIdAndName));
        
        db.CreateIndex<ModelWithIdAndName>(x => x.Name, "ModelWithIdAndName_Name_idx");
        db.DropIndex<ModelWithIdAndName>("ModelWithIdAndName_Name_idx");
    }
}