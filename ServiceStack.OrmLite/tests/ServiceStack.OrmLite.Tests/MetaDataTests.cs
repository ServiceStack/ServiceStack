using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.Shared;
using ServiceStack.OrmLite.Tests.UseCase;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class MetaDataTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        if (DialectFeatures.SchemaSupport)
        {
            using var db = OpenDbConnection();
            db.CreateSchema<Schematable1>();
        }
    }

    [Test]
    public void Can_get_TableNames()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TableMetadata1>();
        db.DropAndCreateTable<TableMetadata2>();

        3.Times(i => db.Insert(new TableMetadata1 {Id = i + 1, Field1 = $"Field{i+1}"}) );
        1.Times(i => db.Insert(new TableMetadata2 {Id = i + 1, Field2 = $"Field{i+1}"}) );
                
        var tableNames = db.GetTableNames();
        tableNames.TextDump().Print();
        Assert.That(tableNames.Count, Is.GreaterThan(0));
        var table1Name = db.GetTableName(typeof(TableMetadata1));
        Assert.That(tableNames.Any(x => x.EqualsIgnoreCase(table1Name)));
        var table2Name = db.GetTableName(typeof(TableMetadata2));
        Assert.That(tableNames.Any(x => x.EqualsIgnoreCase(table2Name)));
    }

    [Test]
    public async Task Can_get_TableNames_Async()
    {
        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<TableMetadata1>();
        db.DropAndCreateTable<TableMetadata2>();

        3.Times(i => db.Insert(new TableMetadata1 {Id = i + 1, Field1 = $"Field{i+1}"}) );
        1.Times(i => db.Insert(new TableMetadata2 {Id = i + 1, Field2 = $"Field{i+1}"}) );
                
        var tableNames = await db.GetTableNamesAsync();
        tableNames.TextDump().Print();
        Assert.That(tableNames.Count, Is.GreaterThan(0));
        var table1Name = db.GetTableName(typeof(TableMetadata1));
        Assert.That(tableNames.Any(x => x.EqualsIgnoreCase(table1Name)));
        var table2Name = db.GetTableName(typeof(TableMetadata2));
        Assert.That(tableNames.Any(x => x.EqualsIgnoreCase(table2Name)));
    }

    [Test]
    public void Can_get_TableNames_in_Schema()
    {
        var schema = "Schema";
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Schematable1>();
        db.DropAndCreateTable<Schematable2>();

        3.Times(i => db.Insert(new Schematable1 {Id = i + 1, Field1 = $"Field{i+1}"}) );
        1.Times(i => db.Insert(new Schematable2 {Id = i + 1, Field2 = $"Field{i+1}"}) );

        var tableNames = db.GetTableNames(schema);
        tableNames.TextDump().Print();
        Assert.That(tableNames.Count, Is.GreaterThan(0));
        Assert.That(tableNames.Any(x => x.IndexOf(nameof(Schematable1), StringComparison.OrdinalIgnoreCase) >= 0));
        Assert.That(tableNames.Any(x => x.IndexOf(nameof(Schematable2), StringComparison.OrdinalIgnoreCase) >= 0));
    }

    int IndexOf(List<KeyValuePair<string, long>> tableResults, Func<KeyValuePair<string, long>, bool> fn)
    {
        for (int i = 0; i < tableResults.Count; i++)
        {
            if (fn(tableResults[i]))
                return i;
        }
        return -1;
    }

    [Test]
    public void Can_get_GetTableNamesWithRowCounts()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<TableMetadata1>();
        db.DropAndCreateTable<TableMetadata2>();

        3.Times(i => db.Insert(new TableMetadata1 {Id = i + 1, Field1 = $"Field{i+1}"}) );
        1.Times(i => db.Insert(new TableMetadata2 {Id = i + 1, Field2 = $"Field{i+1}"}) );
                
        var tableNames = db.GetTableNamesWithRowCounts(live:true);
        tableNames.TextDump().Print();
        Assert.That(tableNames.Count, Is.GreaterThan(0));

        var table1Name = db.GetTableName(typeof(TableMetadata1));
        var table2Name = db.GetTableName(typeof(TableMetadata2));
                
        var table1Pos = IndexOf(tableNames, x => x.Key.EqualsIgnoreCase(table1Name) && x.Value == 3);
        Assert.That(table1Pos, Is.GreaterThanOrEqualTo(0));

        var table2Pos = IndexOf(tableNames, x => x.Key.EqualsIgnoreCase(table2Name) && x.Value == 1);
        Assert.That(table2Pos, Is.GreaterThanOrEqualTo(0));
                
        Assert.That(table1Pos < table2Pos); //is sorted desc

        tableNames = db.GetTableNamesWithRowCounts(live:false);
        Assert.That(tableNames.Any(x => x.Key.EqualsIgnoreCase(table1Name)));
        Assert.That(tableNames.Any(x => x.Key.EqualsIgnoreCase(table2Name)));
    }

    [Test]
    public void Can_get_GetTableNamesWithRowCounts_of_keyword_table()
    {
        using var db = OpenDbConnection();
        CustomerOrdersUseCase.DropTables(db); //Has conflicting 'Order' table
        db.CreateTable<Order>();

        3.Times(i => db.Insert(new Order { CustomerId = i + 1, LineItem = $"Field{i+1}"}) );
                
        var tableNames = db.GetTableNamesWithRowCounts(live:true);
        Assert.That(tableNames.Count, Is.GreaterThan(0));

        var table1Name = db.GetTableName(typeof(Order));
                
        var table1Pos = IndexOf(tableNames, x => x.Key.EqualsIgnoreCase(table1Name) && x.Value == 3);
        Assert.That(table1Pos, Is.GreaterThanOrEqualTo(0));
                
        tableNames = db.GetTableNamesWithRowCounts(live:false);
        Assert.That(tableNames.Any(x => x.Key.EqualsIgnoreCase(table1Name)));
    }

    [Test]
    public async Task Can_get_GetTableNamesWithRowCounts_Async()
    {
        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<TableMetadata1>();
        db.DropAndCreateTable<TableMetadata2>();

        3.Times(i => db.Insert(new TableMetadata1 {Id = i + 1, Field1 = $"Field{i+1}"}) );
        1.Times(i => db.Insert(new TableMetadata2 {Id = i + 1, Field2 = $"Field{i+1}"}) );
                
        var tableNames = await db.GetTableNamesWithRowCountsAsync(live:true);
        tableNames.TextDump().Print();
        Assert.That(tableNames.Count, Is.GreaterThan(0));

        var table1Name = db.GetTableName(typeof(TableMetadata1));
        var table2Name = db.GetTableName(typeof(TableMetadata2));
                
        var table1Pos = IndexOf(tableNames, x => x.Key.EqualsIgnoreCase(table1Name) && x.Value == 3);
        Assert.That(table1Pos, Is.GreaterThanOrEqualTo(0));

        var table2Pos = IndexOf(tableNames, x => x.Key.EqualsIgnoreCase(table2Name) && x.Value == 1);
        Assert.That(table2Pos, Is.GreaterThanOrEqualTo(0));
                
        Assert.That(table1Pos < table2Pos); //is sorted desc
                
        tableNames = await db.GetTableNamesWithRowCountsAsync(live:false);
        Assert.That(tableNames.Any(x => x.Key.EqualsIgnoreCase(table1Name)));
        Assert.That(tableNames.Any(x => x.Key.EqualsIgnoreCase(table2Name)));
    }

    [Test]
    public void Can_get_GetTableNamesWithRowCounts_in_Schema()
    {
        var schema = "Schema";
        using var db = OpenDbConnection();
        db.DropAndCreateTable<Schematable1>();
        db.DropAndCreateTable<Schematable2>();

        3.Times(i => db.Insert(new Schematable1 {Id = i + 1, Field1 = $"Field{i+1}"}) );
        1.Times(i => db.Insert(new Schematable2 {Id = i + 1, Field2 = $"Field{i+1}"}) );
                
        var tableNames = db.GetTableNamesWithRowCounts(live:true,schema:schema);
        tableNames.TextDump().Print();
        Assert.That(tableNames.Count, Is.GreaterThan(0));

        var table1Name = db.GetTableName(typeof(Schematable1)).LastRightPart('.').StripDbQuotes();
        var table2Name = db.GetTableName(typeof(Schematable2)).LastRightPart('.').StripDbQuotes();

        var table1Pos = IndexOf(tableNames, x => x.Key.IndexOf(table1Name, StringComparison.OrdinalIgnoreCase) >=0 && x.Value == 3);
        Assert.That(table1Pos, Is.GreaterThanOrEqualTo(0));

        var table2Pos = IndexOf(tableNames, x => x.Key.IndexOf(table2Name, StringComparison.OrdinalIgnoreCase) >=0 && x.Value == 1);
        Assert.That(table2Pos, Is.GreaterThanOrEqualTo(0));
                
        Assert.That(table1Pos < table2Pos); //is sorted desc
                
        tableNames = db.GetTableNamesWithRowCounts(live:true,schema:schema);
        Assert.That(tableNames.Any(x => x.Key.IndexOf(table1Name, StringComparison.OrdinalIgnoreCase) >= 0));
        Assert.That(tableNames.Any(x => x.Key.IndexOf(table2Name, StringComparison.OrdinalIgnoreCase) >= 0));
    }
}
    
public class TableMetadata1
{
    public int Id { get; set; }
    public string String { get; set; }
    public string Field1 { get; set; }
}
public class TableMetadata2
{
    public int Id { get; set; }
    public string String { get; set; }
    public string Field2 { get; set; }
}