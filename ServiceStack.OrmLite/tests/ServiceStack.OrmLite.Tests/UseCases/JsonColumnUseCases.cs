using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// Properties with a complex type, e.g. a class or a List, are stored in a single column. When they're stored as
/// JSON, which is the default of dialects configured with AddOrmLite(), typed queries can read into them like any
/// other property: x.Address.City, x.Tags.Contains("vip"), x.Lines.Count and x.Lines[0].Quantity.
/// </summary>
[TestFixtureOrmLite]
public class JsonColumnUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class Shopper
    {
        [ServiceStack.DataAnnotations.AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public ShopperAddress Address { get; set; }
        public List<string> Tags { get; set; }
        public List<BasketLine> Lines { get; set; }
    }

    public class ShopperAddress
    {
        public string City { get; set; }
        public ShopperCountry Country { get; set; }
    }

    public class ShopperCountry
    {
        public string Code { get; set; }
        public bool InEu { get; set; }
    }

    public class BasketLine
    {
        public string Sku { get; set; }
        public int Quantity { get; set; }
    }

    public class Purchase
    {
        [ServiceStack.DataAnnotations.AutoIncrement]
        public int Id { get; set; }
        public int ShopperId { get; set; }
        public PurchaseSource Source { get; set; }
    }

    public class PurchaseSource
    {
        public string Channel { get; set; }
    }

    public class ShopperCity
    {
        public string Name { get; set; }
        public string City { get; set; }
    }

    IStringSerializer originalSerializer;

    [SetUp]
    public void StoreComplexTypesAsJson()
    {
        // What AddOrmLite() configures: dialect.UseJson = true
        originalSerializer = DialectProvider.StringSerializer;
        DialectProvider.StringSerializer = new JsonStringSerializer();
    }

    [TearDown]
    public void RestoreSerializer() => DialectProvider.StringSerializer = originalSerializer;

    System.Data.IDbConnection OpenSeededDb()
    {
        var db = OpenDbConnection();
        db.DropTable<Purchase>();
        db.DropAndCreateTable<Shopper>();
        db.CreateTable<Purchase>();

        db.InsertAll(new[] {
            new Shopper {
                Name = "Alice", Tags = ["vip", "beta"],
                Address = new() { City = "London", Country = new() { Code = "UK" } },
                Lines = [new() { Sku = "A-1", Quantity = 2 }, new() { Sku = "B-2", Quantity = 1 }],
            },
            new Shopper {
                Name = "Bob", Tags = ["new"],
                Address = new() { City = "Paris", Country = new() { Code = "FR", InEu = true } },
                Lines = [new() { Sku = "A-1", Quantity = 1 }],
            },
            new Shopper {
                Name = "Carol", Tags = [],
                Address = new() { City = "London", Country = new() { Code = "UK" } },
                Lines = [],
            },
            new Shopper { Name = "Dave" },
        });
        return db;
    }

    [Test]
    public void Filter_by_the_properties_of_a_complex_type()
    {
        using var db = OpenSeededDb();

        var inLondon = db.Select<Shopper>(x => x.Address.City == "London");
        Assert.That(inLondon.Map(x => x.Name), Is.EquivalentTo(new[] { "Alice", "Carol" }));

        // At any depth, with values that are sent as db params
        var code = "FR";
        var french = db.Select<Shopper>(x => x.Address.Country.Code == code && x.Address.City != "Lyon");
        Assert.That(french.Map(x => x.Name), Is.EqualTo(new[] { "Bob" }));

        // Rows are read back with their complex types
        Assert.That(french[0].Address.Country.InEu);
        Assert.That(french[0].Lines[0].Sku, Is.EqualTo("A-1"));
    }

    [Test]
    public void Find_rows_whose_list_has_a_value()
    {
        using var db = OpenSeededDb();

        var vips = db.Select<Shopper>(x => x.Tags.Contains("vip"));
        Assert.That(vips.Map(x => x.Name), Is.EqualTo(new[] { "Alice" }));

        // The value is sent as a db param, so it can't change the query
        var malicious = "1=1 OR 1";
        var q = db.From<Shopper>().Where(x => x.Tags.Contains(malicious));
        Assert.That(db.Select(q), Is.Empty);
        Assert.That(q.ToSelectStatement(), Does.Not.Contain(malicious));
    }

    [Test]
    public void Sort_and_select_the_properties_of_a_complex_type()
    {
        using var db = OpenSeededDb();

        var q = db.From<Shopper>()
            .Where(x => x.Address.Country.Code == "UK" || x.Address.Country.Code == "FR")
            .OrderByDescending(x => x.Address.City)
            .ThenBy(x => x.Name)
            .Select(x => new { x.Name, City = x.Address.City });

        var cities = db.Select<ShopperCity>(q);

        Assert.That(cities.Map(x => x.Name), Is.EqualTo(new[] { "Bob", "Alice", "Carol" }));
        Assert.That(cities.Map(x => x.City), Is.EqualTo(new[] { "Paris", "London", "London" }));
    }

    [Test]
    public void Count_and_index_the_items_of_a_list()
    {
        using var db = OpenSeededDb();

        var severalLines = db.Select<Shopper>(x => x.Lines.Count > 1);
        Assert.That(severalLines.Map(x => x.Name), Is.EqualTo(new[] { "Alice" }));

        var emptyBaskets = db.Select<Shopper>(x => x.Lines.Count == 0);
        Assert.That(emptyBaskets.Map(x => x.Name), Is.EqualTo(new[] { "Carol" }));

        // The first line of each basket
        var firstLine = db.Select<Shopper>(x => x.Lines[0].Sku == "A-1" && x.Lines[0].Quantity >= 2);
        Assert.That(firstLine.Map(x => x.Name), Is.EqualTo(new[] { "Alice" }));
    }

    [Test]
    public void Use_them_with_other_typed_apis()
    {
        using var db = OpenSeededDb();

        Assert.That(db.Count<Shopper>(x => x.Address.City == "London"), Is.EqualTo(2));
        Assert.That(db.Exists<Shopper>(x => x.Tags.Contains("new")));
        Assert.That(db.Single<Shopper>(x => x.Address.City == "Paris").Name, Is.EqualTo("Bob"));

        db.UpdateOnly(() => new Shopper { Name = "Londoner" }, where: x => x.Address.City == "London");
        Assert.That(db.Count<Shopper>(x => x.Name == "Londoner"), Is.EqualTo(2));

        Assert.That(db.Delete<Shopper>(x => x.Address.Country.Code == "FR"), Is.EqualTo(1));
        Assert.That(db.Count<Shopper>(), Is.EqualTo(3));
    }

    [Test]
    public void Filter_by_the_complex_types_of_joined_tables()
    {
        using var db = OpenSeededDb();
        var shoppers = db.Dictionary<string, int>(db.From<Shopper>().Select(x => new { x.Name, x.Id }));
        db.InsertAll(new[] {
            new Purchase { ShopperId = shoppers["Alice"], Source = new() { Channel = "web" } },
            new Purchase { ShopperId = shoppers["Bob"], Source = new() { Channel = "store" } },
            new Purchase { ShopperId = shoppers["Carol"], Source = new() { Channel = "web" } },
        });

        var q = db.From<Shopper>()
            .Join<Purchase>((s, p) => s.Id == p.ShopperId)
            .Where<Shopper, Purchase>((s, p) => p.Source.Channel == "web" && s.Tags.Contains("vip"));

        Assert.That(db.Select(q).Map(x => x.Name), Is.EqualTo(new[] { "Alice" }));
    }

    [Test]
    public async Task Filter_by_the_properties_of_a_complex_type_async()
    {
        using var db = OpenSeededDb();

        var inLondon = await db.SelectAsync<Shopper>(x => x.Address.City == "London" && x.Tags.Contains("vip"));
        Assert.That(inLondon.Map(x => x.Name), Is.EqualTo(new[] { "Alice" }));
    }

    [Test]
    public void Complex_types_have_to_be_stored_as_json()
    {
        using var db = OpenSeededDb();

        // Dialects that aren't configured with AddOrmLite() or UseJson store complex types as JSV
        DialectProvider.StringSerializer = new JsvStringSerializer();

        var nested = Assert.Throws<NotSupportedException>(() => db.From<Shopper>().Where(x => x.Address.City == "London"));
        Assert.That(nested!.Message, Does.Contain("UseJson"));
        Assert.Throws<NotSupportedException>(() => db.From<Shopper>().Where(x => x.Tags.Contains("vip")));
        Assert.Throws<NotSupportedException>(() => db.From<Shopper>().Where(x => x.Lines.Count > 1));
        Assert.Throws<NotSupportedException>(() => db.From<Shopper>().OrderBy(x => x.Address.City));

        // The columns themselves are used as they always were
        var q = db.From<Shopper>().Where(x => x.Address != null).Select(x => new { x.Name, x.Address });
        Assert.That(q.ToSelectStatement(), Does.Contain("Address").IgnoreCase);
    }

    public class Invoice
    {
        [ServiceStack.DataAnnotations.AutoIncrement]
        public int Id { get; set; }
        public int ShopperId { get; set; }

        // A copy of the shopper as they were when the invoice was created
        public Shopper Shopper { get; set; }
    }

    [Test]
    public void Use_Sql_Json_for_a_property_with_the_type_of_a_joined_table()
    {
        using var db = OpenSeededDb();
        db.DropAndCreateTable<Invoice>();
        var alice = db.Single<Shopper>(x => x.Name == "Alice");
        db.Insert(new Invoice { ShopperId = alice.Id, Shopper = alice });
        db.UpdateOnly(() => new Shopper { Name = "Alice Smith" }, where: x => x.Id == alice.Id);

        // In a query that joins Shopper, x.Shopper.Name is the Name column of the Shopper table
        var current = db.From<Invoice>()
            .Join<Shopper>((i, s) => i.ShopperId == s.Id)
            .Where(x => x.Shopper.Name == "Alice Smith");
        Assert.That(db.Count(current), Is.EqualTo(1));

        // Sql.Json() reads the JSON of the invoice's own copy instead
        var snapshot = db.From<Invoice>()
            .Join<Shopper>((i, s) => i.ShopperId == s.Id)
            .Where(x => Sql.Json(x.Shopper).Name == "Alice" && Sql.Json(x.Shopper).Tags.Contains("vip"));
        Assert.That(db.Count(snapshot), Is.EqualTo(1));

        // Without the join it's a complex type like any other
        Assert.That(db.Count<Invoice>(x => x.Shopper.Name == "Alice"), Is.EqualTo(1));
    }

    [Test]
    public void Use_Sql_Json_for_json_the_dialect_doesnt_store()
    {
        using var db = OpenSeededDb();

        // The dialect stores complex types as JSV, but these rows were saved as JSON
        DialectProvider.StringSerializer = new JsvStringSerializer();

        Assert.Throws<NotSupportedException>(() => db.From<Shopper>().Where(x => x.Address.City == "London"));

        var inLondon = db.Column<string>(db.From<Shopper>()
            .Where(x => Sql.Json(x.Address).City == "London")
            .Select(x => x.Name));
        Assert.That(inLondon, Is.EquivalentTo(new[] { "Alice", "Carol" }));
    }

    public class Nickname
    {
        public int Id { get; set; }
        public string[] Aliases { get; set; }
    }

    [Test]
    public void Arrays_are_searched_like_lists()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Nickname>();
        db.Insert(new Nickname { Id = 1, Aliases = ["Ali", "Al"] });
        db.Insert(new Nickname { Id = 2, Aliases = ["Bobby"] });

        if (Dialect.AnyPostgreSql.HasFlag(Dialect))
        {
            // PostgreSQL stores arrays of strings and numbers in its own array types, which aren't JSON
            Assert.Throws<NotSupportedException>(() => db.From<Nickname>().Where(x => x.Aliases.Contains("Al")));
            return;
        }

        Assert.That(db.Select<Nickname>(x => x.Aliases.Contains("Al")).Map(x => x.Id), Is.EqualTo(new[] { 1 }));
        Assert.That(db.Select<Nickname>(x => x.Aliases.Length == 1).Map(x => x.Id), Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public void Searching_a_list_for_a_value_sends_it_as_a_param()
    {
        using var db = OpenSeededDb();

        // Neither is a column, so the value is a db param too
        var allowed = new[] { "Alice", "Bob" };
        var input = "Alice";
        var q = db.From<Shopper>().Where(x => allowed.Contains(input) && x.Name == input);
        Assert.That(db.Select(q).Map(x => x.Name), Is.EqualTo(new[] { "Alice" }));

        var malicious = "1=1 OR 1";
        q = db.From<Shopper>().Where(x => allowed.Contains(malicious));
        Assert.That(q.ToSelectStatement(), Does.Not.Contain(malicious));
        Assert.That(db.Select(q), Is.Empty);
    }
}
