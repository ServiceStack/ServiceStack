using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Expression;

public class BarJoin : IHasGuidId
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; }
}

public class FooBar : IHasIntId
{
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }

    // And a foreign key to the foo table as well, but that is not necessary to show the problem.

    [Alias("fkBarId")]
    [ForeignKey(typeof(BarJoin), ForeignKeyName = "fk_FooBar_Bar")]
    public Guid BarId { get; set; }
}

public class Baz : IHasIntId
{
    [PrimaryKey]
    [AutoIncrement]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }
}

public class FooBarBaz : IHasIntId
{
    [AutoIncrement]
    [PrimaryKey]
    public int Id { get; set; }

    [Alias("fkFooBarId")]
    [ForeignKey(typeof(FooBar), ForeignKeyName = "fk_FooBarbaz_FooBar", OnDelete = "CASCADE")]
    public int FooBarId { get; set; }

    [Alias("fkBazId")]
    [ForeignKey(typeof(Baz), ForeignKeyName = "fk_FooBarBaz_Baz", OnDelete = "CASCADE")]
    public int BazId { get; set; }

    [Required]
    public decimal Amount { get; set; }
}

internal class JoinResult
{
    [BelongTo(typeof(FooBar))]
    public int Id { get; set; }

    [BelongTo(typeof(FooBarBaz))]
    public int FooBarBazId { get; set; }

    [BelongTo(typeof(FooBarBaz))]
    public decimal Amount { get; set; }

    [BelongTo(typeof(BarJoin))]
    public Guid BarId { get; set; }

    [BelongTo(typeof(BarJoin))]
    public string BarName { get; set; }

    [BelongTo(typeof(Baz))]
    public int BazId { get; set; }

    [BelongTo(typeof(Baz))]
    public string BazName { get; set; }
}

public class Product : IHasGuidId
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; }

    public string Code { get; set; }

    [Required]
    [Alias("fkManufacturerId")]
    [ForeignKey(typeof(Manufacturer), ForeignKeyName = "fk_Product_Manufacturer")]
    public Guid ManufacturerId { get; set; }
}

public class Manufacturer : IHasGuidId
{
    [PrimaryKey]
    public Guid Id { get; set; }

    public string Name { get; set; }
}

public class ProductWithManufacturer
{
    [BelongTo(typeof(Product))]
    public Guid ProductId { get; set; }

    [BelongTo(typeof(Product))]
    public string ProductName { get; set; }

    [BelongTo(typeof(Product))]
    public string Code { get; set; }

    [BelongTo(typeof(Product))]
    [Alias("fkManufacturerId")]
    public Guid ManufacturerId { get; set; }

    [BelongTo(typeof(Manufacturer))]
    public string ManufacturerName { get; set; }
}

[TestFixtureOrmLite]
public class ComplexJoinTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    private static int _baz1Id;
    private static int _baz2Id;
    private static int _fooBar1Id;
    private static int _fooBar2Id;
    private static int _fooBarBaz1Id;
    private static int _fooBarBaz2Id;
    private static int _fooBarBaz3Id;

    private static void InitTables(IDbConnection db)
    {
        db.DropTable<FooBarBaz>();
        db.DropTable<FooBar>();
        db.DropTable<BarJoin>();
        db.DropTable<Baz>();

        db.CreateTable<Baz>();
        db.CreateTable<BarJoin>();
        db.CreateTable<FooBar>();
        db.CreateTable<FooBarBaz>();

        var bar1Id = new Guid("5bd67b84-bfdb-4057-9799-5e7a72a6eaa9");
        var bar2Id = new Guid("a8061d08-6816-4e1e-b3d7-1178abcefa0d");

        db.Insert(new BarJoin { Id = bar1Id, Name = "Banana", });
        db.Insert(new BarJoin { Id = bar2Id, Name = "Orange", });

        _baz1Id = (int)db.Insert(new Baz { Name = "Large" }, true);
        _baz2Id = (int)db.Insert(new Baz { Name = "Huge" }, true);

        _fooBar1Id = (int)db.Insert(new FooBar { BarId = bar1Id, }, true);
        _fooBar2Id = (int)db.Insert(new FooBar { BarId = bar2Id, }, true);

        _fooBarBaz1Id = (int)db.Insert(new FooBarBaz { Amount = 42, FooBarId = _fooBar1Id, BazId = _baz2Id }, true);
        _fooBarBaz2Id = (int)db.Insert(new FooBarBaz { Amount = 50, FooBarId = _fooBar1Id, BazId = _baz1Id }, true);
        _fooBarBaz3Id = (int)db.Insert(new FooBarBaz { Amount = 2, FooBarId = _fooBar2Id, BazId = _baz1Id }, true);
    }

    [Test]
    public void Can_query_contains_on_joined_table_column()
    {
        using (var db = OpenDbConnection())
        {
            InitTables(db);

            var q = db.From<FooBar>()
                .Join<BarJoin>((dp, p) => dp.BarId == p.Id)
                .Where<BarJoin>(x => x.Name.Contains("an"));

            var results = db.Select<JoinResult>(q);
            Assert.That(results.Count, Is.EqualTo(2));

            q = db.From<FooBar>()
                .Join<BarJoin>((dp, p) => dp.BarId == p.Id)
                .Where<FooBar, BarJoin>((f, x) => x.Name.Contains("an"));

            results = db.Select<JoinResult>(q);
            Assert.That(results.Count, Is.EqualTo(2));
        }
    }

#pragma warning disable 618
    [Test]
    public void ComplexJoin_with_JoinSqlBuilder()
    {
        using var db = OpenDbConnection();
        InitTables(db);
        // OrmLiteUtils.PrintSql();

        /* This gives the expected values for BazId */
        var jn = new JoinSqlBuilder<JoinResult, FooBar>(DialectProvider)
            .Join<FooBar, BarJoin>(
                sourceColumn: dp => dp.BarId,
                destinationColumn: p => p.Id,
                destinationTableColumnSelection: p => new { BarName = p.Name, BarId = p.Id })
            .Join<FooBar, FooBarBaz>(
                sourceColumn: dp => dp.Id,
                destinationColumn: dpss => dpss.FooBarId,
                destinationTableColumnSelection: dpss => new { dpss.Amount, FooBarBazId = dpss.Id });
        jn.Join<FooBarBaz, Baz>(
            sourceColumn: dpss => dpss.BazId,
            destinationColumn: ss => ss.Id,
            destinationTableColumnSelection: ss => new { BazId = ss.Id, BazName = ss.Name });
        jn.Select<FooBar>(dp => new { dp.Id, });

        var results = db.Select<JoinResult>(jn.ToSql());
        db.GetLastSql().Print();

        results.PrintDump();

        var fooBarBaz = results.First(x => x.FooBarBazId == _fooBarBaz1Id);
        Assert.That(fooBarBaz.BazId, Is.EqualTo(_baz2Id));
        fooBarBaz = results.First(x => x.FooBarBazId == _fooBarBaz2Id);
        Assert.That(fooBarBaz.BazId, Is.EqualTo(_baz1Id));
        fooBarBaz = results.First(x => x.FooBarBazId == _fooBarBaz2Id);
        Assert.That(fooBarBaz.BazId, Is.EqualTo(_baz1Id));
    }
#pragma warning restore 618

    [Test]
    public void ComplexJoin_with_SqlExpression()
    {
        using (var db = OpenDbConnection())
        {
            InitTables(db);

            var q = db.From<FooBar>()
                .Join<BarJoin>((dp, p) => dp.BarId == p.Id)
                .Join<FooBarBaz>((dp, dpss) => dp.Id == dpss.FooBarId)
                .Join<FooBarBaz, Baz>((dpss, ss) => dpss.BazId == ss.Id);

            var results = db.Select<JoinResult>(q);

            db.GetLastSql().Replace("INNER", "\n INNER").Print();

            results.PrintDump();

            var fooBarBaz = results.First(x => x.FooBarBazId == _fooBarBaz1Id);
            Assert.That(fooBarBaz.BazId, Is.EqualTo(_baz2Id));
            fooBarBaz = results.First(x => x.FooBarBazId == _fooBarBaz2Id);
            Assert.That(fooBarBaz.BazId, Is.EqualTo(_baz1Id));
        }
    }

    [Test]
    public void Can_select_dictionary_from_multiple_tables()
    {
        using (var db = OpenDbConnection())
        {
            InitTables(db);

            var q = db.From<FooBar>()
                .Join<BarJoin>()
                .Select<FooBar, BarJoin>((f, b) => new { f.Id, b.Name });

            var results = db.Dictionary<int, string>(q);

            var sql = db.GetLastSql();
            sql.Print();

            results.PrintDump();

            Assert.That(results, Is.EquivalentTo(new Dictionary<int, string> {
                {_fooBar1Id,"Banana"},
                {_fooBar2Id,"Orange"},
            }));
        }
    }

    [Test]
    public void Can_limit_ComplexJoin_query()
    {
        using (var db = OpenDbConnection())
        {
            db.DropTable<Product>();
            db.DropTable<Manufacturer>();

            db.CreateTable<Manufacturer>();
            db.CreateTable<Product>();

            var manufacturer = new Manufacturer
            {
                Id = Guid.NewGuid(),
                Name = "ManName",
            };
            db.Insert(manufacturer);
            db.Insert(new Product
            {
                Id = Guid.NewGuid(),
                Name = "SA1 ProductName",
                Code = "CODE A",
                ManufacturerId = manufacturer.Id,
            });
            db.Insert(new Product
            {
                Id = Guid.NewGuid(),
                Name = "SA2 ProductName",
                Code = "CODE B",
                ManufacturerId = manufacturer.Id,
            });

            var q = db.From<Product>()
                .Join<Manufacturer>((p, m) => p.ManufacturerId == m.Id)
                .Where<Product>(p => p.Name.Contains("SA"))
                .OrderBy<Product>(p => p.Name)
                .Limit(1, 25);

            var results = db.Select<ProductWithManufacturer>(q);

            db.GetLastSql().Print();

            Assert.That(results[0].ManufacturerName, Is.EqualTo(manufacturer.Name));
            Assert.That(results[0].ManufacturerId, Is.EqualTo(manufacturer.Id));
            Assert.That(results[0].Code, Is.EqualTo("CODE B"));

            results.PrintDump();
        }
    }
}