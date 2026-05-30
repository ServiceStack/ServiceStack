
using System;
using System.Data;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;

namespace ServiceStack.OrmLite.Tests;

[TestFixtureOrmLite]
public class ReturnOnInsertTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [OneTimeSetUp]
    public void FixtureSetUp()
    {
        LogManager.LogFactory = new ConsoleLogFactory(debugEnabled: true);
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithRowVersion>();
        db.DropAndCreateTable<ModelWithRowVersionBase>();
    }

    public class ModelWithReturnValues
    {
        [AutoIncrement]
        [ReturnOnInsert]
        public int Id { get; set; }
        
        public string Name { get; set; }
	
        [ReturnOnInsert]
        public ulong RowVersion { get; set; }

        public static void AssertIsEqual(ModelWithReturnValues actual, ModelWithReturnValues expected)
        {
            if (actual == null || expected == null)
            {
                Assert.That(actual == expected, Is.True);
                return;
            }
            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.RowVersion, Is.EqualTo(expected.RowVersion));
        }
	
        public bool Equals(ModelWithReturnValues other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return other.Id == Id && Equals(other.Name, Name) && other.RowVersion == RowVersion;
        }
	
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != typeof(ModelWithReturnValues))
                return false;
            return Equals((ModelWithReturnValues)obj);
        }
    }
    
    [Test]
    public void Does_populate_ReturnValues_Insert()
    {
        OrmLiteUtils.PrintSql();
        
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithReturnValues>();

        ModelWithReturnValues[] rows =
        [
            new() { Name = "Isaac Newton" },
            new() { Name = "Alan Kay" }
        ];

        db.Insert(rows[0]);
        db.Insert(rows[1]);
        var dbRows = db.Select<ModelWithReturnValues>();
        Assert.That(dbRows, Has.Count.EqualTo(2));
        
        dbRows.PrintDumpTable();
        ModelWithReturnValues.AssertIsEqual(dbRows[0], rows[0]);
        ModelWithReturnValues.AssertIsEqual(dbRows[1], rows[1]);
        
        OrmLiteUtils.UnPrintSql();
    }
    
    [Test]
    public async Task Does_populate_ReturnValues_InsertAsync()
    {
        OrmLiteUtils.PrintSql();
        
        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<ModelWithReturnValues>();

        ModelWithReturnValues[] rows =
        [
            new() { Name = "Isaac Newton" },
            new() { Name = "Alan Kay" }
        ];

        await db.InsertAsync(rows[0]);
        await db.InsertAsync(rows[1]);
        var dbRows = db.Select<ModelWithReturnValues>();
        Assert.That(dbRows, Has.Count.EqualTo(2));
        
        dbRows.PrintDumpTable();
        ModelWithReturnValues.AssertIsEqual(dbRows[0], rows[0]);
        ModelWithReturnValues.AssertIsEqual(dbRows[1], rows[1]);
        
        OrmLiteUtils.UnPrintSql();
    }
    
    [Test]
    public void Does_populate_ReturnValues_InsertAll()
    {
        OrmLiteUtils.PrintSql();
        
        using var db = OpenDbConnection();
        db.DropAndCreateTable<ModelWithReturnValues>();

        ModelWithReturnValues[] rows =
        [
            new() { Name = "Isaac Newton" },
            new() { Name = "Alan Kay" },
        ];

        db.InsertAll(rows);
        var dbRows = db.Select<ModelWithReturnValues>();
        Assert.That(dbRows, Has.Count.EqualTo(2));
        
        dbRows.PrintDumpTable();
        ModelWithReturnValues.AssertIsEqual(dbRows[0], rows[0]);
        ModelWithReturnValues.AssertIsEqual(dbRows[1], rows[1]);
        
        OrmLiteUtils.UnPrintSql();
    }
    
    [Test]
    public async Task Does_populate_ReturnValues_InsertAllAsync()
    {
        OrmLiteUtils.PrintSql();
        
        using var db = await OpenDbConnectionAsync();
        db.DropAndCreateTable<ModelWithReturnValues>();

        ModelWithReturnValues[] rows =
        [
            new() { Name = "Isaac Newton" },
            new() { Name = "Alan Kay" },
        ];

        await db.InsertAllAsync(rows);
        var dbRows = db.Select<ModelWithReturnValues>();
        Assert.That(dbRows, Has.Count.EqualTo(2));
        
        dbRows.PrintDumpTable();
        ModelWithReturnValues.AssertIsEqual(dbRows[0], rows[0]);
        ModelWithReturnValues.AssertIsEqual(dbRows[1], rows[1]);
        
        OrmLiteUtils.UnPrintSql();
    }
}
