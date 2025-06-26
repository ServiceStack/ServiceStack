using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite.Tests.UseCase;
using ServiceStack.Text;

namespace ServiceStack.OrmLite.Tests.Expression;

public class LetterFrequency
{
    [AutoIncrement] public int Id { get; set; }

    public string Letter { get; set; }

    [Alias("AliasValue")] public int Value { get; set; }
}

public class LetterWeighting
{
    public long LetterFrequencyId { get; set; }
    public int Weighting { get; set; }
}

public class LetterStat
{
    [AutoIncrement] public int Id { get; set; }
    public long LetterFrequencyId { get; set; }
    public string Letter { get; set; }
    public int Weighting { get; set; }
}

[TestFixtureOrmLite]
public class SqlExpressionTests(DialectContext context) : ExpressionsTestBase(context)
{
    private int letterFrequencyMaxId;
    private int letterFrequencyMinId;
    private int letterFrequencySumId;

    private void GetIdStats(IDbConnection db)
    {
        letterFrequencyMaxId = db.Scalar<int>(db.From<LetterFrequency>().Select(Sql.Max("Id")));
        letterFrequencyMinId = db.Scalar<int>(db.From<LetterFrequency>().Select(Sql.Min("Id")));
        letterFrequencySumId = db.Scalar<int>(db.From<LetterFrequency>().Select(Sql.Sum("Id")));
    }

    public static int InitLetters(IDbConnection db)
    {
        db.DropAndCreateTable<LetterFrequency>();

        var firstId = db.Insert(new LetterFrequency { Letter = "A" }, selectIdentity: true);
        db.Insert(new LetterFrequency { Letter = "B" });
        db.Insert(new LetterFrequency { Letter = "B" });
        db.Insert(new LetterFrequency { Letter = "C" });
        db.Insert(new LetterFrequency { Letter = "C" });
        db.Insert(new LetterFrequency { Letter = "C" });
        db.Insert(new LetterFrequency { Letter = "D" });
        db.Insert(new LetterFrequency { Letter = "D" });
        db.Insert(new LetterFrequency { Letter = "D" });
        db.Insert(new LetterFrequency { Letter = "D" });
        return (int)firstId;
    }

    [Test]
    public void Can_select_Dictionary_with_SqlExpression()
    {
        using var db = OpenDbConnection();
        var firstId = InitLetters(db);
        var expected = new Dictionary<string, int>
        {
            { "A", 1 }, { "B", 2 }, { "C", 3 },
        };

        var query = db.From<LetterFrequency>()
            .Select(x => new { x.Letter, count = Sql.Count("*") })
            .Where(q => q.Letter != "D")
            .GroupBy(x => x.Letter);

        query.ToSelectStatement().Print();

        var map = db.Dictionary<string, int>(query);
        Assert.That(map.EquivalentTo(expected));

        // Same, but group by an anonymous type using an alias - this should not translate to "GROUP BY TheLetter AS Letter", which is invalid SQL

        query = db.From<LetterFrequency>()
            .Select(x => new { x.Letter, count = Sql.Count("*") })
            .Where(q => q.Letter != "D")
            .GroupBy(x => new { TheLetter = x.Letter });

        map = db.Dictionary<string, int>(query);
        Assert.That(map.EquivalentTo(expected));

        // Now group by all columns without listing them - effectively "SELECT DISTINCT *"

        query = db.From<LetterFrequency>()
            .Where(q => q.Letter != "D")
            .GroupBy(x => new { x })
            .Select(x => new { x.Id });

        var list = db.SqlList<int>(query);
        Assert.That(list, Is.EquivalentTo(new[] { 0, 1, 2, 3, 4, 5 }.AdjustIds(firstId)));
    }

    [Test]
    public void Can_select_ColumnDistinct_with_SqlExpression()
    {
        using var db = OpenDbConnection();
        InitLetters(db);

        var query = db.From<LetterFrequency>()
            .Where(q => q.Letter != "D")
            .Select(x => x.Letter);

        query.ToSelectStatement().Print();

        var uniqueLetters = db.ColumnDistinct<string>(query);
        Assert.That(uniqueLetters.EquivalentTo(new[] { "A", "B", "C" }));
    }

    [Test]
    public void Can_Select_as_List_Object()
    {
        using var db = OpenDbConnection();
        InitLetters(db);
        GetIdStats(db);

        var query = db.From<LetterFrequency>()
            .Select("COUNT(*), MAX(Id), MIN(Id), Sum(Id)");

        query.ToSelectStatement().Print();

        var results = db.Select<List<object>>(query);

        Assert.That(results.Count, Is.EqualTo(1));

        var result = results[0];
        CheckDbTypeInsensitiveEquivalency(result);

        var single = db.Single<List<object>>(query);
        CheckDbTypeInsensitiveEquivalency(single);

        result.PrintDump();
    }

    [Test]
    public void Can_Select_as_List_ValueTuple()
    {
        using var db = OpenDbConnection();
        InitLetters(db);
        GetIdStats(db);

        //var a = new ValueTuple<int, int, int, int>();
        //a.Item1 = 1;

        var query = db.From<LetterFrequency>()
            .Select("COUNT(*), MIN(Letter), MAX(Letter), Sum(Id)");

        var results = db.Select<(int count, string min, string max, int sum)>(query);

        Assert.That(results.Count, Is.EqualTo(1));

        var result = results[0];
        Assert.That(result.count, Is.EqualTo(10));
        Assert.That(result.min, Is.EqualTo("A"));
        Assert.That(result.max, Is.EqualTo("D"));
        Assert.That(result.sum, Is.EqualTo(letterFrequencySumId));

        var single = db.Single<(int count, string min, string max, int sum)>(query);
        Assert.That(single.count, Is.EqualTo(10));
        Assert.That(single.min, Is.EqualTo("A"));
        Assert.That(single.max, Is.EqualTo("D"));
        Assert.That(single.sum, Is.EqualTo(letterFrequencySumId));

        single = db.Single<(int count, string min, string max, int sum)>(
            db.From<LetterFrequency>()
                .Select(x => new
                {
                    Count = Sql.Count("*"),
                    Min = Sql.Min(x.Letter),
                    Max = Sql.Max(x.Letter),
                    Sum = Sql.Sum(x.Id)
                }));

        Assert.That(single.count, Is.EqualTo(10));
        Assert.That(single.min, Is.EqualTo("A"));
        Assert.That(single.max, Is.EqualTo("D"));
        Assert.That(single.sum, Is.EqualTo(letterFrequencySumId));
    }

    [Test]
    public async System.Threading.Tasks.Task Can_Select_as_List_ValueTuple_Async()
    {
        using var db = await OpenDbConnectionAsync();
        InitLetters(db);
        GetIdStats(db);

        //var a = new ValueTuple<int, int, int, int>();
        //a.Item1 = 1;

        var query = db.From<LetterFrequency>()
            .Select("COUNT(*), MIN(Letter), MAX(Letter), Sum(Id)");

        var results = await db.SelectAsync<(int count, string min, string max, int sum)>(query);

        Assert.That(results.Count, Is.EqualTo(1));

        var result = results[0];
        Assert.That(result.count, Is.EqualTo(10));
        Assert.That(result.min, Is.EqualTo("A"));
        Assert.That(result.max, Is.EqualTo("D"));
        Assert.That(result.sum, Is.EqualTo(letterFrequencySumId));

        var single = await db.SingleAsync<(int count, string min, string max, int sum)>(query);
        Assert.That(single.count, Is.EqualTo(10));
        Assert.That(single.min, Is.EqualTo("A"));
        Assert.That(single.max, Is.EqualTo("D"));
        Assert.That(single.sum, Is.EqualTo(letterFrequencySumId));

        single = await db.SingleAsync<(int count, string min, string max, int sum)>(
            db.From<LetterFrequency>()
                .Select(x => new
                {
                    Count = Sql.Count("*"),
                    Min = Sql.Min(x.Letter),
                    Max = Sql.Max(x.Letter),
                    Sum = Sql.Sum(x.Id)
                }));

        Assert.That(single.count, Is.EqualTo(10));
        Assert.That(single.min, Is.EqualTo("A"));
        Assert.That(single.max, Is.EqualTo("D"));
        Assert.That(single.sum, Is.EqualTo(letterFrequencySumId));
    }

    private void CheckDbTypeInsensitiveEquivalency(List<object> result)
    {
        Assert.That(Convert.ToInt64(result[0]), Is.EqualTo(10));
        Assert.That(Convert.ToInt64(result[1]), Is.EqualTo(letterFrequencyMaxId));
        Assert.That(Convert.ToInt64(result[2]), Is.EqualTo(letterFrequencyMinId));
        Assert.That(Convert.ToInt64(result[3]), Is.EqualTo(letterFrequencySumId));
    }

    [Test]
    public void Can_Select_as_Dictionary_Object()
    {
        using var db = OpenDbConnection();
        InitLetters(db);
        GetIdStats(db);

        var query = db.From<LetterFrequency>()
            .Select("COUNT(*) \"Count\", MAX(Id) \"Max\", MIN(Id) \"Min\", Sum(Id) \"Sum\"");

        query.ToSelectStatement().Print();

        var results = db.Select<Dictionary<string, object>>(query);

        Assert.That(results.Count, Is.EqualTo(1));

        var result = results[0];
        CheckDbTypeInsensitiveEquivalency(result);

        var single = db.Single<Dictionary<string, object>>(query);
        CheckDbTypeInsensitiveEquivalency(single);

        results.PrintDump();
    }

    private void CheckDbTypeInsensitiveEquivalency(Dictionary<string, object> result)
    {
        Assert.That(Convert.ToInt64(result["Count"]), Is.EqualTo(10));
        Assert.That(Convert.ToInt64(result["Max"]), Is.EqualTo(letterFrequencyMaxId));
        Assert.That(Convert.ToInt64(result["Min"]), Is.EqualTo(letterFrequencyMinId));
        Assert.That(Convert.ToInt64(result["Sum"]), Is.EqualTo(letterFrequencySumId));
    }

    [Test]
    public void Can_select_Object()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();
        var id = db.Insert(new LetterFrequency { Id = 1, Letter = "A" }, selectIdentity: true);

        var result = db.Scalar<object>(db.From<LetterFrequency>().Select(x => x.Letter));
        Assert.That(result, Is.EqualTo("A"));

        result = db.Scalar<object>(db.From<LetterFrequency>().Select(x => x.Id));
        Assert.That(Convert.ToInt64(result), Is.EqualTo(id));
    }

    [Test]
    public void Can_select_limit_with_SqlExpression()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();
        db.DropAndCreateTable<LetterWeighting>();

        var letters = "A,B,C,D,E".Split(',');
        var i = 0;
        letters.Each(letter =>
        {
            var id = db.Insert(new LetterFrequency { Letter = letter }, selectIdentity: true);
            db.Insert(new LetterWeighting { LetterFrequencyId = id, Weighting = ++i * 10 });
        });

        var results = db.Select(db.From<LetterFrequency>().Limit(3));
        Assert.That(results.Count, Is.EqualTo(3));

        results = db.Select(db.From<LetterFrequency>().Skip(3));
        Assert.That(results.Count, Is.EqualTo(2));

        results = db.Select(db.From<LetterFrequency>().Limit(1, 2));
        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results.ConvertAll(x => x.Letter), Is.EquivalentTo(new[] { "B", "C" }));

        results = db.Select(db.From<LetterFrequency>().Skip(1).Take(2));
        Assert.That(results.ConvertAll(x => x.Letter), Is.EquivalentTo(new[] { "B", "C" }));

        results = db.Select(db.From<LetterFrequency>()
            .OrderByDescending(x => x.Letter)
            .Skip(1).Take(2));
        Assert.That(results.ConvertAll(x => x.Letter), Is.EquivalentTo(new[] { "D", "C" }));
    }

    [Test]
    public void Can_select_limit_with_JoinSqlBuilder()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();
        db.DropAndCreateTable<LetterWeighting>();

        var letters = "A,B,C,D,E".Split(',');
        var i = 0;
        letters.Each(letter =>
        {
            var id = db.Insert(new LetterFrequency { Letter = letter }, selectIdentity: true);
            db.Insert(new LetterWeighting { LetterFrequencyId = id, Weighting = ++i * 10 });
        });

#pragma warning disable 618
        var joinFn = new Func<JoinSqlBuilder<LetterFrequency, LetterWeighting>>(() =>
            new JoinSqlBuilder<LetterFrequency, LetterWeighting>(DialectProvider)
                .Join<LetterFrequency, LetterWeighting>(x => x.Id, x => x.LetterFrequencyId)
        );
#pragma warning restore 618

        var results = db.Select<LetterFrequency>(joinFn());
        Assert.That(results.Count, Is.EqualTo(5));

        results = db.Select<LetterFrequency>(joinFn().Limit(3));
        Assert.That(results.Count, Is.EqualTo(3));

        results = db.Select<LetterFrequency>(joinFn().Skip(3));
        Assert.That(results.Count, Is.EqualTo(2));

        results = db.Select<LetterFrequency>(joinFn().Limit(1, 2));
        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results.ConvertAll(x => x.Letter), Is.EquivalentTo(new[] { "B", "C" }));

        results = db.Select<LetterFrequency>(joinFn().Skip(1).Take(2));
        Assert.That(results.ConvertAll(x => x.Letter), Is.EquivalentTo(new[] { "B", "C" }));

        results = db.Select<LetterFrequency>(joinFn()
            .OrderByDescending<LetterFrequency>(x => x.Letter)
            .Skip(1).Take(2));
        Assert.That(results.ConvertAll(x => x.Letter), Is.EquivalentTo(new[] { "D", "C" }));
    }

    [Test]
    public void Can_add_basic_joins_with_SqlExpression()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();
        db.DropAndCreateTable<LetterStat>();

        var letters = "A,B,C,D,E".Split(',');
        var i = 0;
        letters.Each(letter =>
        {
            var id = db.Insert(new LetterFrequency { Letter = letter }, selectIdentity: true);
            db.Insert(new LetterStat
            {
                LetterFrequencyId = id,
                Letter = letter,
                Weighting = ++i * 10
            });
        });

        db.Insert(new LetterFrequency { Letter = "F" });

        Assert.That(db.Count<LetterFrequency>(), Is.EqualTo(6));

        var results = db.Select(db.From<LetterFrequency, LetterStat>());
        db.GetLastSql().Print();
        Assert.That(results.Count, Is.EqualTo(5));

        results = db.Select(db.From<LetterFrequency, LetterStat>((x, y) => x.Id == y.LetterFrequencyId));
        db.GetLastSql().Print();
        Assert.That(results.Count, Is.EqualTo(5));

        results = db.Select(db.From<LetterFrequency>()
            .Join<LetterFrequency, LetterStat>((x, y) => x.Id == y.LetterFrequencyId));
        db.GetLastSql().Print();
        Assert.That(results.Count, Is.EqualTo(5));
    }

    [Test]
    public void Can_do_ToCountStatement_with_SqlExpression_if_where_expression_refers_to_joined_table()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();
        db.DropAndCreateTable<LetterStat>();

        var letterFrequency = new LetterFrequency { Letter = "A" };
        letterFrequency.Id = (int)db.Insert(letterFrequency, true);

        db.Insert(new LetterStat { Letter = "A", LetterFrequencyId = letterFrequency.Id, Weighting = 1 });

        var expr = db.From<LetterFrequency>()
            .Join<LetterFrequency, LetterStat>()
            .Where<LetterStat>(x => x.Id > 0);

        var count = db.SqlScalar<long>(expr.ToCountStatement(),
            expr.Params.ToDictionary(param => param.ParameterName, param => param.Value));

        Assert.That(count, Is.GreaterThan(0));

        count = db.Count(db.From<LetterFrequency>().Join<LetterStat>().Where<LetterStat>(x => x.Id > 0));

        Assert.That(count, Is.GreaterThan(0));

        Assert.That(
            db.Exists(db.From<LetterFrequency>().Join<LetterStat>().Where<LetterStat>(x => x.Id > 0)));
    }

    [Test]
    public void Can_do_ToCountStatement_with_SqlExpression_if_expression_has_groupby()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();

        db.Insert(new LetterFrequency { Letter = "A" });
        db.Insert(new LetterFrequency { Letter = "A" });
        db.Insert(new LetterFrequency { Letter = "A" });
        db.Insert(new LetterFrequency { Letter = "B" });
        db.Insert(new LetterFrequency { Letter = "B" });
        db.Insert(new LetterFrequency { Letter = "B" });
        db.Insert(new LetterFrequency { Letter = "B" });

        var query = db.From<LetterFrequency>()
            .Select(x => x.Letter)
            .GroupBy(x => x.Letter);

        var count = db.Count(query);
        db.GetLastSql().Print();
        Assert.That(count, Is.EqualTo(7)); //Sum of Counts returned [3,4]

        var rowCount = db.RowCount(query);
        db.GetLastSql().Print();
        Assert.That(rowCount, Is.EqualTo(2));

        rowCount = db.Select(query).Count;
        db.GetLastSql().Print();
        Assert.That(rowCount, Is.EqualTo(2));
    }

    [Test]
    public void Can_select_RowCount_with_db_params()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();

        db.Insert(new LetterFrequency { Letter = "A" });
        db.Insert(new LetterFrequency { Letter = "A" });
        db.Insert(new LetterFrequency { Letter = "A" });
        db.Insert(new LetterFrequency { Letter = "B" });
        db.Insert(new LetterFrequency { Letter = "B" });
        db.Insert(new LetterFrequency { Letter = "B" });
        db.Insert(new LetterFrequency { Letter = "B" });

        var query = db.From<LetterFrequency>()
            .Where(x => x.Letter == "B")
            .Select(x => x.Letter);

        var rowCount = db.RowCount(query);
        db.GetLastSql().Print();
        Assert.That(rowCount, Is.EqualTo(4));

        var table = nameof(LetterFrequency).SqlTable(DialectProvider);

        rowCount = db.RowCount("SELECT * FROM {0} WHERE Letter = @p1".PreNormalizeSql(db).Fmt(table), new { p1 = "B" });
        Assert.That(rowCount, Is.EqualTo(4));

        rowCount = db.RowCount("SELECT * FROM {0} WHERE Letter = @p1".PreNormalizeSql(db).Fmt(table),
            new[] { db.CreateParam("p1", "B") });
        Assert.That(rowCount, Is.EqualTo(4));
    }

    [Test]
    public void Can_select_RowCount_without_db_params()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();

        db.Insert(new LetterFrequency { Letter = "A" });
        db.Insert(new LetterFrequency { Letter = "A" });
        db.Insert(new LetterFrequency { Letter = "A" });
        db.Insert(new LetterFrequency { Letter = "B" });
        db.Insert(new LetterFrequency { Letter = "B" });
        db.Insert(new LetterFrequency { Letter = "B" });
        db.Insert(new LetterFrequency { Letter = "B" });

        var table = nameof(LetterFrequency).SqlTable(DialectProvider);

        var rowCount = db.RowCount<LetterFrequency>();
        Assert.That(rowCount, Is.EqualTo(7));
    }

    [Test]
    public void Can_get_RowCount_if_expression_has_OrderBy()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();

        db.Insert(new LetterFrequency { Letter = "A" });
        db.Insert(new LetterFrequency { Letter = "B" });
        db.Insert(new LetterFrequency { Letter = "B" });

        var query = db.From<LetterFrequency>()
            .Select(x => x.Letter)
            .OrderBy(x => x.Id);

        var rowCount = db.RowCount(query);
        Assert.That(rowCount, Is.EqualTo(3));

        rowCount = db.Select(query).Count;
        Assert.That(rowCount, Is.EqualTo(3));
    }

    [Test]
    public void Can_OrderBy_Fields_with_different_sort_directions()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();
        db.DropAndCreateTable<LetterStat>();

        var insertedIds = new List<long>();
        "A,B,B,C,C,C,D,D,E".Split(',').Each(letter =>
        {
            insertedIds.Add(db.Insert(new LetterFrequency { Letter = letter }, selectIdentity: true));
        });

        var rows = db.Select(db.From<LetterFrequency>().OrderByFields("Letter", "Id"));
        Assert.That(rows.Map(x => x.Letter), Is.EquivalentTo("A,B,B,C,C,C,D,D,E".Split(',')));
        Assert.That(rows.Map(x => x.Id), Is.EquivalentTo(insertedIds));

        rows = db.Select(db.From<LetterFrequency>().OrderByFields("Letter", "-Id"));
        Assert.That(rows.Map(x => x.Letter), Is.EquivalentTo("A,B,B,C,C,C,D,D,E".Split(',')));
        Assert.That(rows.Map(x => x.Id), Is.EquivalentTo(insertedIds));

        rows = db.Select(db.From<LetterFrequency>().OrderByFieldsDescending("Letter", "-Id"));
        Assert.That(rows.Map(x => x.Letter), Is.EquivalentTo("E,D,D,C,C,C,B,B,A".Split(',')));
        Assert.That(rows.Map(x => x.Id), Is.EquivalentTo(Enumerable.Reverse(insertedIds)));
    }

    [Test]
    public void Can_Select_with_List_Contains()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();

        var insertedIds = "A,B,B,C,C,C,D,D,E".Split(',').Map(letter =>
            db.Insert(new LetterFrequency { Letter = letter }, selectIdentity: true));

        var first3Ids = insertedIds.Take(3).ToList();
        var letters = db.Column<string>(db.From<LetterFrequency>()
            .Where(x => first3Ids.Contains(x.Id))
            .OrderBy(x => x.Id)
            .Select(x => x.Letter));

        Assert.That(letters.Join(","), Is.EqualTo("A,B,B"));

        letters = db.Column<string>(db.From<LetterFrequency>()
            .Where(x => !first3Ids.Contains(x.Id))
            .OrderBy(x => x.Id)
            .Select(x => x.Letter));

        db.GetLastSql().Print();

        Assert.That(letters.Join(","), Is.EqualTo("C,C,C,D,D,E"));
    }

    [Test]
    public void Can_select_limit_on_Table_with_References()
    {
        //This version of MariaDB doesn't yet support 'LIMIT & IN/ALL/ANY/SOME subquery'
        if (Dialect == Dialect.AnyMySql) return;

        //Only one expression can be specified in the select list when the subquery is not introduced with EXISTS.
        if ((Dialect & Dialect.AnySqlServer) == Dialect)
            return;

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
    public void Can_select_subselect()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();

        var insertedIds = "A,B,B,C,C,C,D,D,E".Split(',').Map(letter =>
            db.Insert(new LetterFrequency { Letter = letter }, selectIdentity: true));

        var q = db.From<LetterFrequency>(db.TableAlias("x"));
        q.Where(x => x.Letter == Sql.TableAlias(x.Letter, "obj"));
        var subSql = q.Select(Sql.Count("*")).ToSelectStatement();

        var rows = db.Select<Dictionary<string, object>>(db.From<LetterFrequency>(db.TableAlias("obj"))
            .Where(x => x.Letter == "C")
            .Select(x => new
            {
                x,
                count = Sql.Custom($"({subSql})"),
            }));

        rows.PrintDump();
        Assert.That(rows.Count, Is.EqualTo(3));
        Assert.That(rows.All(x => x["count"].ConvertTo<int>() == 3));
    }

    [Test]
    public void Can_use_SqlCustom_in_subselect_condition()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();
        db.DropAndCreateTable<LetterWeighting>();

        var letters = "A,B,C,D,E".Split(',');
        var i = 0;
        letters.Each(letter =>
        {
            var id = db.Insert(new LetterFrequency { Letter = letter }, selectIdentity: true);
            db.Insert(new LetterWeighting { LetterFrequencyId = id, Weighting = ++i * 10 });
        });
        db.Insert(new LetterFrequency { Letter = "F" });

        var q = db.From<LetterFrequency>(db.TableAlias("a"))
            .WhereNotExists(db.From<LetterWeighting>()
                .Where<LetterFrequency, LetterWeighting>((a, b) => b.LetterFrequencyId == Sql.TableAlias(a.Id, "a"))
                .Select(Sql.Custom("null")));

        var results = db.Select(q);
        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Letter, Is.EqualTo("F"));
    }

    [Test]
    public void Does_add_params_in_subselect_condition()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<LetterFrequency>();
        db.DropAndCreateTable<LetterWeighting>();

        // OrmLiteUtils.PrintSql();
        var q = db.From<LetterFrequency>(db.TableAlias("a"))
            .WhereNotExists(db.From<LetterWeighting>()
                .Where<LetterFrequency, LetterWeighting>((a, b) =>
                    b.LetterFrequencyId == Sql.TableAlias(a.Id, "a")
                    && b.Weighting > 0)
                .Select(Sql.Custom("null")));

        var results = db.Select(q);
    }
}