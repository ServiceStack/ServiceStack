using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

public class Site
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Warehouse
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public string Name { get; set; }
    public string Country { get; set; }
    public decimal Markup { get; set; }
    public bool Active { get; set; }
}

public class Part
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int WarehouseId { get; set; }
    public string Country { get; set; }
    public string Location { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

public static class Warehouses
{
    public static void Seed(IDbConnection db)
    {
        db.DropAndCreateTable<Site>();
        db.DropAndCreateTable<Warehouse>();
        db.DropAndCreateTable<Part>();
        db.InsertAll(new List<Site> {
            new() { Id = 1, Name = "North Hub" },
            new() { Id = 2, Name = "South Hub" },
        });
        db.InsertAll(new List<Warehouse> {
            new() { Id = 1, SiteId = 1, Name = "Leeds", Country = "UK", Markup = 1.2m, Active = true },
            new() { Id = 2, SiteId = 2, Name = "Porto", Country = "PT", Markup = 1.1m, Active = false },
            new() { Id = 3, SiteId = 1, Name = "York",  Country = "UK", Markup = 1.0m, Active = true },
        });
        db.InsertAll(new List<Part> {
            new() { Id = 1, Name = "Bolt",   WarehouseId = 1, Price = 10, Stock = 100 },
            new() { Id = 2, Name = "Nut",    WarehouseId = 1, Price = 5,  Stock = 50 },
            new() { Id = 3, Name = "Gear",   WarehouseId = 2, Price = 20, Stock = 7 },
            new() { Id = 4, Name = "Spring", WarehouseId = 3, Price = 2,  Stock = 0 },
            new() { Id = 5, Name = "Axle",   WarehouseId = 2, Price = 40, Stock = 3 },
        });
    }
}

/// <summary>
/// UpdateFrom() updates rows with values from joined tables in a single statement, without reading the rows into .NET.
/// The query's joins and filters select the rows to update, and the set expression can use columns of any joined table.
/// </summary>
[TestFixtureOrmLite]
public class UpdateFromUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    static Dictionary<int, Part> PartsById(IDbConnection db) => db.Select<Part>().ToDictionary(x => x.Id);

    [Test]
    public void Copy_values_from_a_joined_table()
    {
        using var db = OpenDbConnection();
        Warehouses.Seed(db);

        // Each part's country is the country of its warehouse
        var updated = db.UpdateFrom<Part, Warehouse>((p, w) => new Part { Country = w.Country },
            db.From<Part>().Join<Warehouse>((p, w) => p.WarehouseId == w.Id));

        Assert.That(updated, Is.EqualTo(5));
        var parts = PartsById(db);
        Assert.That(new[] { 1, 2, 4 }.All(id => parts[id].Country == "UK"));
        Assert.That(new[] { 3, 5 }.All(id => parts[id].Country == "PT"));
    }

    [Test]
    public void Filter_the_rows_to_update_by_a_joined_table()
    {
        using var db = OpenDbConnection();
        Warehouses.Seed(db);

        // Clear the stock of parts in inactive warehouses
        var q = db.From<Part>()
            .Join<Warehouse>((p, w) => p.WarehouseId == w.Id)
            .Where<Warehouse>(w => !w.Active);
        var updated = db.UpdateFrom(p => new Part { Stock = 0 }, q);

        Assert.That(updated, Is.EqualTo(2));
        var parts = PartsById(db);
        Assert.That(parts[3].Stock, Is.EqualTo(0));
        Assert.That(parts[5].Stock, Is.EqualTo(0));
        Assert.That(parts[1].Stock, Is.EqualTo(100)); // other rows are unchanged
    }

    [Test]
    public void Calculate_values_from_columns_of_both_tables()
    {
        using var db = OpenDbConnection();
        Warehouses.Seed(db);

        // Apply each UK warehouse's markup to its parts
        var q = db.From<Part>()
            .Join<Warehouse>((p, w) => p.WarehouseId == w.Id)
            .Where<Warehouse>(w => w.Country == "UK");
        var updated = db.UpdateFrom<Part, Warehouse>((p, w) => new Part { Price = p.Price * w.Markup }, q);

        Assert.That(updated, Is.EqualTo(3));
        var parts = PartsById(db);
        Assert.That(parts[1].Price, Is.EqualTo(12m).Within(0.001m));
        Assert.That(parts[2].Price, Is.EqualTo(6m).Within(0.001m));
        Assert.That(parts[4].Price, Is.EqualTo(2m).Within(0.001m));
        Assert.That(parts[3].Price, Is.EqualTo(20m)); // PT warehouse
    }

    [Test]
    public void Update_multiple_columns_with_values_and_params()
    {
        using var db = OpenDbConnection();
        Warehouses.Seed(db);

        // Constants and captured values are sent as params
        var discount = 0.5m;
        var q = db.From<Part>()
            .Join<Warehouse>((p, w) => p.WarehouseId == w.Id)
            .Where<Warehouse>(w => w.Name == "Leeds");
        db.UpdateFrom<Part, Warehouse>((p, w) => new Part {
            Country = w.Country,
            Location = "Clearance",
            Price = p.Price * discount,
        }, q);

        var bolt = PartsById(db)[1];
        Assert.That(bolt.Country, Is.EqualTo("UK"));
        Assert.That(bolt.Location, Is.EqualTo("Clearance"));
        Assert.That(bolt.Price, Is.EqualTo(5m).Within(0.001m));
    }

    [Test]
    public void Use_values_from_multiple_joined_tables()
    {
        using var db = OpenDbConnection();
        Warehouses.Seed(db);

        // Each part in stock is located at the site of its warehouse
        var q = db.From<Part>()
            .Join<Warehouse>((p, w) => p.WarehouseId == w.Id)
            .Join<Warehouse, Site>((w, s) => w.SiteId == s.Id)
            .Where(p => p.Stock > 0);
        var updated = db.UpdateFrom<Part, Warehouse, Site>((p, w, s) => new Part { Location = s.Name }, q);

        Assert.That(updated, Is.EqualTo(4));
        var parts = PartsById(db);
        Assert.That(parts[1].Location, Is.EqualTo("North Hub"));
        Assert.That(parts[3].Location, Is.EqualTo("South Hub"));
        Assert.That(parts[4].Location, Is.Null); // out of stock
    }

    [Test]
    public async Task Async_and_reusing_the_query()
    {
        using var db = await OpenDbConnectionAsync();
        Warehouses.Seed(db);

        var q = db.From<Part>()
            .Join<Warehouse>((p, w) => p.WarehouseId == w.Id)
            .Where<Warehouse>(w => w.Active);

        Assert.That(await db.UpdateFromAsync<Part, Warehouse>((p, w) => new Part { Country = w.Country }, q), Is.EqualTo(3));
        Assert.That(await db.UpdateFromAsync(p => new Part { Stock = p.Stock + 1 }, q), Is.EqualTo(3));

        // The query isn't changed, so it can still be used to select the same rows
        var parts = await db.SelectAsync(q);
        Assert.That(parts.Map(x => x.Stock), Is.EquivalentTo(new[] { 101, 51, 1 }));
        Assert.That(parts.All(x => x.Country == "UK"));
    }

    [Test]
    public void Invalid_usage_throws()
    {
        using var db = OpenDbConnection();
        var q = db.From<Part>().Join<Warehouse>((p, w) => p.WarehouseId == w.Id);

        Assert.Throws<ArgumentException>(() => q.Clone().ToUpdateFromStatement(
            (System.Linq.Expressions.Expression<Func<Part, Warehouse, Part>>)((p, w) => new Part { Id = w.Id })));
        Assert.Throws<ArgumentException>(() => q.Clone().ToUpdateFromStatement(
            (System.Linq.Expressions.Expression<Func<Part, Part>>)(p => p)));
        Assert.Throws<NotSupportedException>(() => q.Clone().Take(10).ToUpdateFromStatement(
            (System.Linq.Expressions.Expression<Func<Part, Part>>)(p => new Part { Stock = 0 })));
    }
}
