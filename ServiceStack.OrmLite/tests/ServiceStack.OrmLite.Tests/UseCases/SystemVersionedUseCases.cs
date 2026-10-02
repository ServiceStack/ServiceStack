using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// A [SystemVersioned] table keeps every previous version of its rows, so it can be queried as it was at any time
/// with AsOf(), and the changes to a row can be read with AllVersions() and VersionsBetween(). The RDBMS keeps the
/// versions itself when rows are updated and deleted, in SQL Server 2016+ and MariaDB 10.3+.
/// </summary>
[TestFixtureOrmLite]
public class SystemVersionedUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [SystemVersioned]
    public class PricedItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }

        // When each version of a row was current, which the RDBMS sets
        [RowStart]
        public DateTime ValidFrom { get; set; }
        [RowEnd]
        public DateTime ValidTo { get; set; }
    }

    // Without properties for them, the times of each version are kept but aren't selected
    [SystemVersioned]
    public class AppSetting
    {
        public int Id { get; set; }
        public string Value { get; set; }
    }

    public class ItemStock
    {
        public int Id { get; set; }
        public int PricedItemId { get; set; }
        public int Quantity { get; set; }
    }

    bool IsMariaDb(IDbConnection db) =>
        Dialect.AnyMySql.HasFlag(Dialect) && db.Scalar<string>("SELECT VERSION()").Contains("MariaDB");

    bool SupportsSystemVersioning(IDbConnection db) => Dialect.AnySqlServer.HasFlag(Dialect) || IsMariaDb(db);

    // The time of the database, in the time zone its versions are compared in
    DateTime DbNow(IDbConnection db) => Dialect.AnySqlServer.HasFlag(Dialect)
        ? db.Scalar<DateTime>("SELECT SYSUTCDATETIME()")
        : db.Scalar<DateTime>("SELECT NOW(6)");

    // Versions are kept by the time of each change, so changes are made at different times
    static void Wait() => Thread.Sleep(60);

    IDbConnection OpenVersionedDb()
    {
        var db = OpenDbConnection();
        if (!SupportsSystemVersioning(db))
        {
            db.Dispose();
            Assert.Ignore($"{Dialect} doesn't have system-versioned tables");
        }

        db.DropAndCreateTable<PricedItem>();
        db.Insert(new PricedItem { Id = 1, Name = "Keyboard", Price = 50m });
        db.Insert(new PricedItem { Id = 2, Name = "Mouse", Price = 20m });
        Wait();
        db.UpdateOnly(() => new PricedItem { Price = 55m }, where: x => x.Id == 1);
        Wait();
        db.UpdateOnly(() => new PricedItem { Price = 60m }, where: x => x.Id == 1);
        db.DeleteById<PricedItem>(2);
        return db;
    }

    // A time while a version was current
    static DateTime During(PricedItem version) => version.ValidFrom.AddTicks((version.ValidTo - version.ValidFrom).Ticks / 2);

    [Test]
    public void Read_every_version_of_a_row()
    {
        using var db = OpenVersionedDb();

        var versions = db.Select(db.From<PricedItem>()
            .AllVersions()
            .Where(x => x.Id == 1)
            .OrderBy(x => x.ValidFrom));

        Assert.That(versions.Map(x => x.Price), Is.EqualTo(new[] { 50m, 55m, 60m }));

        // Each version was current until the next replaced it
        Assert.That(versions[0].ValidTo, Is.EqualTo(versions[1].ValidFrom));
        Assert.That(versions[1].ValidTo, Is.EqualTo(versions[2].ValidFrom));
        Assert.That(versions[2].ValidTo, Is.GreaterThan(DateTime.UtcNow.AddYears(50))); // the current version

        // Queries only read current rows unless they ask for their versions
        Assert.That(db.Select<PricedItem>().Map(x => x.Price), Is.EqualTo(new[] { 60m }));
    }

    [Test]
    public void Query_a_table_as_it_was_at_a_time()
    {
        using var db = OpenVersionedDb();
        var versions = db.Select(db.From<PricedItem>().AllVersions().Where(x => x.Id == 1).OrderBy(x => x.ValidFrom));

        // Before the first price change, when the deleted Mouse still existed
        var original = db.Select(db.From<PricedItem>().AsOf(During(versions[0])).OrderBy(x => x.Id));
        Assert.That(original.Map(x => x.Name), Is.EqualTo(new[] { "Keyboard", "Mouse" }));
        Assert.That(original.Map(x => x.Price), Is.EqualTo(new[] { 50m, 20m }));

        // After it
        var q = db.From<PricedItem>().AsOf(During(versions[1])).Where(x => x.Id == 1);
        Assert.That(db.Single(q).Price, Is.EqualTo(55m));
        Assert.That(db.Count(q), Is.EqualTo(1));

        // With the rest of a typed query
        var cheap = db.From<PricedItem>()
            .AsOf(During(versions[0]))
            .Where(x => x.Price < 30m)
            .Select(x => x.Name);
        Assert.That(db.Column<string>(cheap), Is.EqualTo(new[] { "Mouse" }));
    }

    [Test]
    public void Read_the_versions_between_two_times()
    {
        using var db = OpenVersionedDb();
        var versions = db.Select(db.From<PricedItem>().AllVersions().Where(x => x.Id == 1).OrderBy(x => x.ValidFrom));

        // The versions that were current at any time from the 1st price to the 2nd
        var q = db.From<PricedItem>()
            .VersionsBetween(During(versions[0]), During(versions[1]))
            .Where(x => x.Id == 1)
            .OrderBy(x => x.ValidFrom);

        Assert.That(db.Select(q).Map(x => x.Price), Is.EqualTo(new[] { 50m, 55m }));
    }

    [Test]
    public void Join_a_table_as_it_was_to_current_tables()
    {
        using var db = OpenVersionedDb();
        db.DropAndCreateTable<ItemStock>();
        db.Insert(new ItemStock { Id = 1, PricedItemId = 1, Quantity = 5 });
        db.Insert(new ItemStock { Id = 2, PricedItemId = 2, Quantity = 9 });
        var versions = db.Select(db.From<PricedItem>().AllVersions().Where(x => x.Id == 1).OrderBy(x => x.ValidFrom));

        // The original prices of what's in stock now, including the item that was deleted since
        var q = db.From<PricedItem>()
            .Join<ItemStock>((p, s) => p.Id == s.PricedItemId)
            .AsOf(During(versions[0]))
            .Where<ItemStock>(s => s.Quantity > 0)
            .OrderBy(x => x.Id);

        Assert.That(db.Select(q).Map(x => x.Price), Is.EqualTo(new[] { 50m, 20m }));
    }

    [Test]
    public void Version_a_table_without_properties_for_its_times()
    {
        using var db = OpenDbConnection();
        if (!SupportsSystemVersioning(db))
            Assert.Ignore($"{Dialect} doesn't have system-versioned tables");

        db.DropAndCreateTable<AppSetting>();
        db.Insert(new AppSetting { Id = 1, Value = "light" });
        Wait();
        var before = DbNow(db);
        Wait();
        db.Update(new AppSetting { Id = 1, Value = "dark" });

        Assert.That(db.SingleById<AppSetting>(1).Value, Is.EqualTo("dark"));
        Assert.That(db.Single(db.From<AppSetting>().AsOf(before)).Value, Is.EqualTo("light"));
        Assert.That(db.Count(db.From<AppSetting>().AllVersions()), Is.EqualTo(2));

        // It can be dropped and created again like any other table
        db.DropAndCreateTable<AppSetting>();
        Assert.That(db.Count(db.From<AppSetting>().AllVersions()), Is.EqualTo(0));
    }

    [Test]
    public async Task Query_a_table_as_it_was_async()
    {
        using var db = OpenVersionedDb();
        var versions = await db.SelectAsync(db.From<PricedItem>().AllVersions().Where(x => x.Id == 1).OrderBy(x => x.ValidFrom));

        var original = await db.SingleAsync(db.From<PricedItem>().AsOf(During(versions[0])).Where(x => x.Id == 1));
        Assert.That(original.Price, Is.EqualTo(50m));
    }

    [Test]
    public void System_versioned_tables_need_an_RDBMS_that_has_them()
    {
        using var db = OpenDbConnection();
        if (SupportsSystemVersioning(db))
        {
            // A query reads its table as of one time
            var q = db.From<PricedItem>().AsOf(DateTime.UtcNow);
            Assert.Throws<InvalidOperationException>(() => q.AllVersions());
            Assert.Throws<NotSupportedException>(() => q.ForUpdate().ToSelectStatement());
            return;
        }

        db.DropTable<PricedItem>();
        Assert.Throws<NotSupportedException>(() => db.CreateTable<PricedItem>());
        Assert.Throws<NotSupportedException>(() => db.From<PricedItem>().AsOf(DateTime.UtcNow));
        Assert.Throws<NotSupportedException>(() => db.From<PricedItem>().AllVersions());
    }
}
