using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.Issues;

public class Product
{
    [AutoIncrement]
    public int Id { get; set; }
    public string ProductType { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool LimitedToStores { get; set; }
    public string Sku { get; set; }
    public decimal Price { get; set; }
    public decimal OldPrice { get; set; }
    public decimal SpecialPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public DateTime? DateChanged { get; set; }
    public DateTime? DateCreated { get; set; }

    [Reference]
    public List<StockItem> StockItems { get; set; } = new List<StockItem>();
}

public class StockItem
{
    [AutoIncrement]
    public int Id { get; set; }
    [References(typeof(Product))]
    public int ProductId { get; set; }
    public string Size { get; set; }
    public int TotalStockQuantity { get; set; }
    public string Gtin { get; set; }
    public int DisplayOrder { get; set; }

    [Reference]
    public Product Product { get; set; }
}
    
[TestFixtureOrmLite]
public class AutoQueryJoinTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Can_select_references_with_join()
    {
        using var db = OpenDbConnection();
        db.DropTable<StockItem>();
        db.DropTable<Product>();
        db.CreateTable<Product>();
        db.CreateTable<StockItem>();

        db.Save(new Product
        {
            ProductType = "A",
            Name = "Name A",
            DisplayOrder = 1,
            Sku = "SKU A",
            Price = 1,
            DateCreated = DateTime.UtcNow,
            StockItems = new List<StockItem>
            {
                new StockItem { Size = "1", TotalStockQuantity = 1, DisplayOrder = 1 },
                new StockItem { Size = "2", TotalStockQuantity = 2, DisplayOrder = 2 },
            }
        }, references: true);

        db.Save(new Product
        {
            ProductType = "B",
            Name = "Name B",
            DisplayOrder = 2,
            Sku = "SKU B",
            Price = 2,
            DateCreated = DateTime.UtcNow,
            StockItems = new List<StockItem>
            {
                new StockItem { Size = "3", TotalStockQuantity = 3, DisplayOrder = 3 },
                new StockItem { Size = "4", TotalStockQuantity = 4, DisplayOrder = 4 },
            }
        }, references: true);

        db.Insert(new Product
        {
            ProductType = "C",
            Name = "Name C",
            DisplayOrder = 3,
            Sku = "SKU C",
            Price = 3,
            DateCreated = DateTime.UtcNow,
        });

        var results = db.LoadSelect<Product>();
        Assert.That(results.Count, Is.EqualTo(3));

        var q = db.From<Product>().Join<StockItem>();
        var products = db.Select(q.SelectDistinct());
        var stockItems = db.Select<StockItem>();

        products.Merge(stockItems);

        Assert.That(products.Count, Is.EqualTo(2));
        Assert.That(products[0].StockItems.Count, Is.EqualTo(2));
        Assert.That(products[1].StockItems.Count, Is.EqualTo(2));

        db.DropTable<StockItem>();
    }
}