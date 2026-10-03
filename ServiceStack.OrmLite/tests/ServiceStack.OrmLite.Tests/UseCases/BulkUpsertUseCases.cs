using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// BulkUpsert() inserts the rows with a new primary key and updates those with an existing one, for large numbers of
/// rows, e.g. when importing or synchronizing data. The rows are bulk loaded into a temporary table, then upserted
/// from it in a single statement.
/// </summary>
[TestFixtureOrmLite]
public class BulkUpsertUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }

        [IgnoreOnUpdate]
        public DateTime CreatedDate { get; set; }
    }

    public class Contact
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
    }

    public class Device
    {
        [AutoId]
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    static readonly DateTime Imported = new(2026, 1, 1);
    static readonly DateTime Synced = new(2026, 6, 1);

    static void SeedProducts(System.Data.IDbConnection db)
    {
        db.DropAndCreateTable<Product>();
        db.InsertAll(new[] {
            new Product { Id = 1, Name = "Keyboard", Price = 50m, Stock = 10, CreatedDate = Imported },
            new Product { Id = 2, Name = "Mouse", Price = 20m, Stock = 30, CreatedDate = Imported },
            new Product { Id = 3, Name = "Monitor", Price = 200m, Stock = 5, CreatedDate = Imported },
        });
    }

    [Test]
    public void Insert_new_rows_and_update_existing_rows()
    {
        using var db = OpenDbConnection();
        SeedProducts(db);

        db.BulkUpsert(new[] {
            new Product { Id = 2, Name = "Wireless Mouse", Price = 25m, Stock = 40, CreatedDate = Synced },
            new Product { Id = 3, Name = "Monitor", Price = 180m, Stock = 4, CreatedDate = Synced },
            new Product { Id = 4, Name = "Webcam", Price = 60m, Stock = 12, CreatedDate = Synced },
            new Product { Id = 5, Name = "Headset", Price = 80m, Stock = 7, CreatedDate = Synced },
        });

        var products = db.Select(db.From<Product>().OrderBy(x => x.Id));
        Assert.That(products.Map(x => x.Name), Is.EqualTo(new[] { "Keyboard", "Wireless Mouse", "Monitor", "Webcam", "Headset" }));
        Assert.That(products.Map(x => x.Stock), Is.EqualTo(new[] { 10, 40, 4, 12, 7 }));
        Assert.That(products[2].Price, Is.EqualTo(180m));

        // [IgnoreOnUpdate] fields are only set when the row is inserted
        Assert.That(products.Map(x => x.CreatedDate), Is.EqualTo(new[] { Imported, Imported, Imported, Synced, Synced }));
    }

    [Test]
    public void Only_update_selected_fields_of_existing_rows()
    {
        using var db = OpenDbConnection();
        SeedProducts(db);

        var prices = new[] {
            new Product { Id = 1, Name = "ignored", Price = 55m, CreatedDate = Synced },
            new Product { Id = 6, Name = "Dock", Price = 120m, Stock = 3, CreatedDate = Synced },
        };

        // New rows are inserted with all their fields
        db.BulkUpsert(prices, updateOnly: x => new { x.Price });

        var keyboard = db.SingleById<Product>(1);
        Assert.That(keyboard.Price, Is.EqualTo(55m));
        Assert.That(keyboard.Name, Is.EqualTo("Keyboard"));
        Assert.That(keyboard.Stock, Is.EqualTo(10));

        var dock = db.SingleById<Product>(6);
        Assert.That(dock.Name, Is.EqualTo("Dock"));
        Assert.That(dock.Stock, Is.EqualTo(3));

        // The fields can also be named at runtime
        db.BulkUpsert(new[] { new Product { Id = 1, Name = "Mechanical Keyboard", Stock = 99, CreatedDate = Synced } },
            updateOnly: [nameof(Product.Name)]);

        keyboard = db.SingleById<Product>(1);
        Assert.That(keyboard.Name, Is.EqualTo("Mechanical Keyboard"));
        Assert.That(keyboard.Stock, Is.EqualTo(10));
    }

    [Test]
    public void Insert_only_the_rows_that_dont_exist()
    {
        using var db = OpenDbConnection();
        SeedProducts(db);

        // Without any fields to update, existing rows are left as they are
        db.BulkUpsert(new[] {
            new Product { Id = 1, Name = "Changed", Price = 1m, Stock = 1, CreatedDate = Synced },
            new Product { Id = 7, Name = "Speakers", Price = 45m, Stock = 9, CreatedDate = Synced },
        }, updateOnly: Array.Empty<string>());

        Assert.That(db.SingleById<Product>(1).Name, Is.EqualTo("Keyboard"));
        Assert.That(db.SingleById<Product>(7).Name, Is.EqualTo("Speakers"));
        Assert.That(db.Count<Product>(), Is.EqualTo(4));
    }

    [Test]
    public void Upsert_thousands_of_rows()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Product>();
        db.BulkInsert(Enumerable.Range(1, 1500).Select(i =>
            new Product { Id = i, Name = "Product " + i, Price = i, Stock = 1, CreatedDate = Imported }));

        // The second half of the existing rows, and as many new ones
        var rows = Enumerable.Range(751, 1500).Select(i =>
            new Product { Id = i, Name = "Synced " + i, Price = i + 0.5m, Stock = 2, CreatedDate = Synced });
        db.BulkUpsert(rows);

        Assert.That(db.Count<Product>(), Is.EqualTo(2250));
        Assert.That(db.Count<Product>(x => x.Stock == 1), Is.EqualTo(750));
        Assert.That(db.Count<Product>(x => x.Stock == 2), Is.EqualTo(1500));
        Assert.That(db.SingleById<Product>(751).Name, Is.EqualTo("Synced 751"));
        Assert.That(db.SingleById<Product>(751).CreatedDate, Is.EqualTo(Imported));
        Assert.That(db.SingleById<Product>(2250).Price, Is.EqualTo(2250.5m));
    }

    [Test]
    public void Load_rows_with_sql_instead_of_the_bulk_loader()
    {
        using var db = OpenDbConnection();
        SeedProducts(db);

        // Rows are loaded with multi-row INSERT statements of the batch size, with names that need escaping
        db.BulkUpsert(new[] {
            new Product { Id = 1, Name = "O'Reilly's \"Keyboard\"", Price = 51m, Stock = 11, CreatedDate = Synced },
            new Product { Id = 8, Name = "Mat, large; -- 50%", Price = 15m, Stock = 20, CreatedDate = Synced },
            new Product { Id = 9, Name = null, Price = 5m, Stock = 1, CreatedDate = Synced },
        }, new BulkInsertConfig { Mode = BulkInsertMode.Sql, BatchSize = 2 });

        Assert.That(db.SingleById<Product>(1).Name, Is.EqualTo("O'Reilly's \"Keyboard\""));
        Assert.That(db.SingleById<Product>(8).Name, Is.EqualTo("Mat, large; -- 50%"));
        Assert.That(db.SingleById<Product>(9).Name, Is.Null);
        Assert.That(db.Count<Product>(), Is.EqualTo(5));
    }

    [Test]
    public void Rows_without_an_auto_increment_id_are_inserted()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Contact>();
        var aliceId = (int)db.Insert(new Contact { Email = "alice@example.org", Name = "Alice" }, selectIdentity: true);

        db.BulkUpsert(new[] {
            new Contact { Id = aliceId, Email = "alice@example.org", Name = "Alice Smith" },
            new Contact { Email = "bob@example.org", Name = "Bob" },
            new Contact { Email = "carol@example.org", Name = "Carol" },
        });

        var contacts = db.Select(db.From<Contact>().OrderBy(x => x.Email));
        Assert.That(contacts.Map(x => x.Name), Is.EqualTo(new[] { "Alice Smith", "Bob", "Carol" }));
        Assert.That(contacts.Map(x => x.Id).Distinct().Count(), Is.EqualTo(3));
        Assert.That(contacts[0].Id, Is.EqualTo(aliceId));

        // Rows with an id that doesn't exist keep it
        db.BulkUpsert(new[] { new Contact { Id = 500, Email = "dave@example.org", Name = "Dave" } });
        Assert.That(db.SingleById<Contact>(500).Name, Is.EqualTo("Dave"));
    }

    [Test]
    public void Rows_keep_their_guid_ids()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Device>();
        var laptop = new Device { Id = Guid.NewGuid(), Name = "Laptop" };
        db.Insert(laptop);

        var tablet = new Device { Id = Guid.NewGuid(), Name = "Tablet" };
        db.BulkUpsert(new[] {
            new Device { Id = laptop.Id, Name = "Work Laptop" },
            tablet,
            new Device { Name = "Phone" }, // gets a new id
        });

        Assert.That(db.SingleById<Device>(laptop.Id).Name, Is.EqualTo("Work Laptop"));
        Assert.That(db.SingleById<Device>(tablet.Id).Name, Is.EqualTo("Tablet"));
        Assert.That(db.Count<Device>(), Is.EqualTo(3));
        Assert.That(db.Single<Device>(x => x.Name == "Phone").Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task Upsert_rows_async()
    {
        using var db = await OpenDbConnectionAsync();
        SeedProducts(db);

        await db.BulkUpsertAsync(new[] {
            new Product { Id = 3, Name = "Curved Monitor", Price = 250m, Stock = 2, CreatedDate = Synced },
            new Product { Id = 10, Name = "Cable", Price = 9m, Stock = 100, CreatedDate = Synced },
        });
        await db.BulkUpsertAsync(new[] { new Product { Id = 10, Name = "ignored", Stock = 90, CreatedDate = Synced } },
            updateOnly: x => new { x.Stock });

        Assert.That((await db.SingleByIdAsync<Product>(3)).Name, Is.EqualTo("Curved Monitor"));
        var cable = await db.SingleByIdAsync<Product>(10);
        Assert.That(cable.Name, Is.EqualTo("Cable"));
        Assert.That(cable.Stock, Is.EqualTo(90));
        Assert.That(await db.CountAsync<Product>(), Is.EqualTo(4));
    }

    [Test]
    public void Connection_rules_are_applied()
    {
        using var db = OpenDbConnection();
        SeedProducts(db);

        // Tables with connection filters or write rules are upserted a row at a time, so they're applied to each
        db.UseFilters(FilterSet.Create(f => f.OnInsert<Product>(x => x.Stock, () => 7)));

        db.BulkUpsert(new[] {
            new Product { Id = 2, Name = "Trackball", Price = 35m, Stock = 1, CreatedDate = Synced },
            new Product { Id = 11, Name = "Stand", Price = 30m, Stock = 1, CreatedDate = Synced },
        });

        Assert.That(db.SingleById<Product>(2).Name, Is.EqualTo("Trackball"));
        Assert.That(db.SingleById<Product>(2).Stock, Is.EqualTo(1));
        Assert.That(db.SingleById<Product>(11).Stock, Is.EqualTo(7));
    }

    [Test]
    public void Upserts_are_part_of_a_transaction()
    {
        using var db = OpenDbConnection();
        SeedProducts(db);

        using (var trans = db.OpenTransaction())
        {
            db.BulkUpsert(new[] {
                new Product { Id = 1, Name = "Rolled back", Price = 1m, Stock = 1, CreatedDate = Synced },
                new Product { Id = 12, Name = "Rolled back", Price = 1m, Stock = 1, CreatedDate = Synced },
            });
            Assert.That(db.Count<Product>(x => x.Name == "Rolled back"), Is.EqualTo(2));
            trans.Rollback();
        }
        Assert.That(db.Count<Product>(), Is.EqualTo(3));
        Assert.That(db.SingleById<Product>(1).Name, Is.EqualTo("Keyboard"));

        using (var trans = db.OpenTransaction())
        {
            db.BulkUpsert(new[] { new Product { Id = 12, Name = "Committed", Price = 1m, Stock = 1, CreatedDate = Synced } });
            trans.Commit();
        }
        Assert.That(db.SingleById<Product>(12).Name, Is.EqualTo("Committed"));
    }

    [Test]
    public void Upserting_no_rows_does_nothing()
    {
        using var db = OpenDbConnection();
        SeedProducts(db);

        db.BulkUpsert(new List<Product>());

        Assert.That(db.Count<Product>(), Is.EqualTo(3));
    }
}
