using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class ConflictResolutionTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_change_conflict_resolution_with_Insert()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithIdAndName>();

        var row = new ModelWithIdAndName(1);
        db.Insert(row, dbCmd => dbCmd.OnConflictIgnore());

        //Equivalent to: 
        db.Insert(row, dbCmd => dbCmd.OnConflict(ConflictResolution.Ignore));
    }

    [Test]
    public async Task Can_change_conflict_resolution_with_Insert_Async()
    {
        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<ModelWithIdAndName>();

        var row = new ModelWithIdAndName(1);
        await db.InsertAsync(row, dbCmd => dbCmd.OnConflictIgnore());

        //Equivalent to: 
        await db.InsertAsync(row, dbCmd => dbCmd.OnConflict(ConflictResolution.Ignore));
    }

    [Test]
    public void Can_change_conflict_resolution_with_Insert_AutoIncrement()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithIdAndName>();

        var row = new ModelWithIdAndName(1);
        var insertId = db.Insert(row, dbCmd => dbCmd.OnConflictIgnore(), selectIdentity: true);
        Assert.That(insertId, Is.GreaterThan(0));
    }

    [Test]
    public async Task Can_change_conflict_resolution_with_Insert_AutoIncrement_Async()
    {
        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<ModelWithIdAndName>();

        var row = new ModelWithIdAndName(1);
        var insertId = await db.InsertAsync(row, dbCmd => dbCmd.OnConflictIgnore(), selectIdentity: true);
        Assert.That(insertId, Is.GreaterThan(0));
    }

    [Test]
    public void Can_change_conflict_resolution_with_InsertAll()
    {
        using var db = OpenDbConnection();
        var rows = 5.Times(i => new ModelWithIdAndName(i));

        db.DropAndCreateTable<ModelWithIdAndName>();

        db.InsertAll(rows, dbCmd => dbCmd.OnConflictIgnore());
    }

    [Test]
    public async Task Can_change_conflict_resolution_with_InsertAll_Async()
    {
        using var db = await OpenDbConnectionAsync();
        var rows = 5.Times(i => new ModelWithIdAndName(i));

        db.DropAndCreateTable<ModelWithIdAndName>();

        await db.InsertAllAsync(rows, dbCmd => dbCmd.OnConflictIgnore());
    }
}