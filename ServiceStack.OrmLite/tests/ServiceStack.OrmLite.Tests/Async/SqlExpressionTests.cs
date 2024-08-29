using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.UseCase;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Async;

[TestFixtureOrmLite]
public class SqlExpressionTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public static void InitLetters(IDbConnection db)
    {
        db.DropAndCreateTable<LetterFrequency>();

        db.Insert(new LetterFrequency { Letter = "A" });
        db.Insert(new LetterFrequency { Letter = "B" });
        db.Insert(new LetterFrequency { Letter = "B" });
        db.Insert(new LetterFrequency { Letter = "C" });
        db.Insert(new LetterFrequency { Letter = "C" });
        db.Insert(new LetterFrequency { Letter = "C" });
        db.Insert(new LetterFrequency { Letter = "D" });
        db.Insert(new LetterFrequency { Letter = "D" });
        db.Insert(new LetterFrequency { Letter = "D" });
        db.Insert(new LetterFrequency { Letter = "D" });
    }

    [Test]
    public async Task Can_Select_as_List_Object_Async()
    {
        using var db = await OpenDbConnectionAsync();
        InitLetters(db);

        var query = db.From<LetterFrequency>()
            .Select("COUNT(*), MAX(Id), MIN(Id), Sum(Id)");

        query.ToSelectStatement().Print();

        var results = await db.SelectAsync<List<object>>(query);

        Assert.That(results.Count, Is.EqualTo(1));

        var result = results[0];
        Assert.That(result[0], Is.EqualTo(10));
        Assert.That(result[1], Is.EqualTo(10));
        Assert.That(result[2], Is.EqualTo(1));
        Assert.That(result[3], Is.EqualTo(55));

        results.PrintDump();
    }

    [Test]
    public async Task Can_Select_as_Dictionary_Object_Async()
    {
        using var db = await OpenDbConnectionAsync();
        InitLetters(db);

        var query = db.From<LetterFrequency>()
            .Select("COUNT(*) count, MAX(Id) max, MIN(Id) min, Sum(Id) sum");

        query.ToSelectStatement().Print();

        var results = await db.SelectAsync<Dictionary<string, object>>(query);

        Assert.That(results.Count, Is.EqualTo(1));
        //results.PrintDump();

        var result = results[0];
        Assert.That(result["count"], Is.EqualTo(10));
        Assert.That(result["max"], Is.EqualTo(10));
        Assert.That(result["min"], Is.EqualTo(1));
        Assert.That(result["sum"], Is.EqualTo(55));
    }

    [Test]
    [IgnoreDialect(Tests.Dialect.AnyMySql, "doesn't yet support 'LIMIT & IN/ALL/ANY/SOME subquery")]
    [IgnoreDialect(Tests.Dialect.AnySqlServer, "generates Windowing function \"... WHERE CustomerId IN (SELECT * FROM ...)\" when should generate \"... WHERE CustomerId IN (SELECT Id FROM ...)\"")]
    public void Can_select_limit_on_Table_with_References()
    {
        using var db = OpenDbConnection();
        CustomerOrdersUseCase.DropTables(db); //Has conflicting 'Order' table
        db.DropAndCreateTable<Order>();
        db.DropAndCreateTable<Customer>();
        db.DropAndCreateTable<CustomerAddress>();

        var customer1 = LoadReferencesTests.GetCustomerWithOrders("1");
        db.Save(customer1, references: true);

        var customer2 = LoadReferencesTests.GetCustomerWithOrders("2");
        db.Save(customer2, references: true);

        var results = db.LoadSelect(db.From<Customer>()
            .OrderBy(x => x.Id)
            .Limit(1, 1));

        //db.GetLastSql().Print();

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("Customer 2"));
        Assert.That(results[0].PrimaryAddress.AddressLine1, Is.EqualTo("2 Humpty Street"));
        Assert.That(results[0].Orders.Count, Is.EqualTo(2));

        results = db.LoadSelect(db.From<Customer>()
            .Join<CustomerAddress>()
            .OrderBy(x => x.Id)
            .Limit(1, 1));

        db.GetLastSql().Print();

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("Customer 2"));
        Assert.That(results[0].PrimaryAddress.AddressLine1, Is.EqualTo("2 Humpty Street"));
        Assert.That(results[0].Orders.Count, Is.EqualTo(2));
    }

    [Test]
    [IgnoreDialect(Tests.Dialect.AnyMySql, "doesn't yet support 'LIMIT & IN/ALL/ANY/SOME subquery")]
    [IgnoreDialect(Tests.Dialect.AnySqlServer, "generates Windowing function \"... WHERE CustomerId IN (SELECT * FROM ...)\" when should generate \"... WHERE CustomerId IN (SELECT Id FROM ...)\"")]
    public async Task Can_select_limit_on_Table_with_References_Async()
    {
        using var db = await OpenDbConnectionAsync();
        CustomerOrdersUseCase.DropTables(db); //Has conflicting 'Order' table
        db.DropAndCreateTable<Order>();
        db.DropAndCreateTable<Customer>();
        db.DropAndCreateTable<CustomerAddress>();

        var customer1 = LoadReferencesTests.GetCustomerWithOrders("1");
        db.Save(customer1, references: true);

        var customer2 = LoadReferencesTests.GetCustomerWithOrders("2");
        db.Save(customer2, references: true);

        var results = await db.LoadSelectAsync(db.From<Customer>()
            .OrderBy(x => x.Id)
            .Limit(1, 1));

        //db.GetLastSql().Print();

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("Customer 2"));
        Assert.That(results[0].PrimaryAddress.AddressLine1, Is.EqualTo("2 Humpty Street"));
        Assert.That(results[0].Orders.Count, Is.EqualTo(2));

        results = await db.LoadSelectAsync(db.From<Customer>()
            .Join<CustomerAddress>()
            .OrderBy(x => x.Id)
            .Limit(1, 1));

        db.GetLastSql().Print();

        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("Customer 2"));
        Assert.That(results[0].PrimaryAddress.AddressLine1, Is.EqualTo("2 Humpty Street"));
        Assert.That(results[0].Orders.Count, Is.EqualTo(2));
    }

    [Test]
    public void Can_select_custom_GroupBy()
    {
        using var db = OpenDbConnection();
        InitLetters(db);

        var q = db.From<LetterFrequency>()
            .GroupBy("Letter")
            .Select(x => new { x.Letter, Count = Sql.Count("*") });

        var results = db.Dictionary<string, int>(q);

        Assert.That(results, Is.EquivalentTo(new Dictionary<string,int>
        {
            { "A", 1 },
            { "B", 2 },
            { "C", 3 },
            { "D", 4 },
        }));
    }

    [Test]
    public void Can_select_custom_GroupBy_KeyValuePairs()
    {
        using var db = OpenDbConnection();
        InitLetters(db);

        var q = db.From<LetterFrequency>()
            .GroupBy("Letter")
            .Select(x => new { x.Letter, Count = Sql.Count("*") });

        var results = db.KeyValuePairs<string, int>(q);

        Assert.That(results, Is.EquivalentTo(new List<KeyValuePair<string,int>>
        {
            new KeyValuePair<string, int>("A", 1),
            new KeyValuePair<string, int>("B", 2),
            new KeyValuePair<string, int>("C", 3),
            new KeyValuePair<string, int>("D", 4),
        }));
    }
}