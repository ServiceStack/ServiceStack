using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.OrmLite.Tests.UseCases;

namespace ServiceStack.OrmLite.Tests;

/// <summary>
/// Implementation details behind SqlExpression param reuse, the reader column mapping cache and
/// the dialect-specific SQL generated for large IN lists
/// </summary>
[TestFixtureOrmLite]
public class QueryInternalsTests(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public async Task Can_reexecute_the_same_SqlExpression()
    {
        using var db = await OpenDbConnectionAsync();
        Bookstore.Seed(db);

        // SQL Server + PostgreSQL don't allow the same db param instance in multiple commands
        var ids = new[] { 1, 2, 3 };
        var q = db.From<Book>().Where(x => x.Price > 10m && ids.Contains(x.Id));
        var expected = db.Select(q).Count;
        for (var i = 0; i < 3; i++)
        {
            Assert.That(db.Select(q).Count, Is.EqualTo(expected));
            Assert.That(db.Count(q), Is.EqualTo(expected));
            Assert.That(db.Exists(q), Is.EqualTo(expected > 0));
            Assert.That((await db.SelectAsync(q)).Count, Is.EqualTo(expected));
            Assert.That((await db.ExistsAsync(q)), Is.EqualTo(expected > 0));
            Assert.That((await db.SingleAsync(q)), Is.Not.Null);
            Assert.That(db.Single(q), Is.Not.Null);
        }
        // Exists / Single don't modify the query
        Assert.That(q.Rows, Is.Null);
        Assert.That(q.SelectExpression, Does.Not.Contain("exists"));
    }

    [Test]
    public void Params_already_added_to_a_command_are_cloned()
    {
        using var db = OpenDbConnection();
        var q = db.From<Book>().Where(x => x.Title == "Dune");
        var param = q.Params[0];

        using var cmd1 = db.CreateCommand();
        q.CopyParamsTo(cmd1);
        Assert.That(cmd1.Parameters[0], Is.SameAs(param));

        using var cmd2 = db.CreateCommand();
        q.CopyParamsTo(cmd2);
        var clone = (IDbDataParameter)cmd2.Parameters[0];
        Assert.That(clone, Is.Not.SameAs(param));
        Assert.That(clone.ParameterName, Is.EqualTo(param.ParameterName));
        Assert.That(clone.Value, Is.EqualTo(param.Value));
        Assert.That(clone.DbType, Is.EqualTo(param.DbType));
    }

    [Test]
    [IgnoreDialect(Dialect.Sqlite, "SQLite doesn't support Output params")]
    public void Output_params_are_never_cloned_so_their_values_can_be_read()
    {
        using var db = OpenDbConnection();
        using var cmd1 = db.CreateCommand();
        var output = cmd1.CreateParameter();
        output.ParameterName = "out";
        output.Direction = ParameterDirection.Output;

        cmd1.AddParams([output]);
        cmd1.Parameters.Clear();

        using var cmd2 = db.CreateCommand();
        cmd2.AddParams([output]);
        Assert.That(cmd2.Parameters[0], Is.SameAs(output));
    }

    [Test]
    public void Reader_column_mappings_are_cached_per_column_set()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);
        var table = db.GetQuotedTableName<Book>();
        var col = (string name) => DialectProvider.GetQuotedColumnName(name);
        var dune = $"WHERE {col("Title")} = 'Dune'";

        // Same model mapped from different columns and column orders
        for (var i = 0; i < 2; i++)
        {
            var all = db.SqlList<Book>($"SELECT * FROM {table} {dune}")[0];
            var partial = db.SqlList<Book>($"SELECT {col("Year")}, {col("Id")} FROM {table} {dune}")[0];
            var reordered = db.SqlList<Book>($"SELECT {col("Author")}, {col("Year")}, {col("Title")} FROM {table} {dune}")[0];

            Assert.That(all.Title, Is.EqualTo("Dune"));
            Assert.That(all.Author, Is.EqualTo("Frank Herbert"));
            Assert.That(partial.Title, Is.Null);
            Assert.That(partial.Year, Is.EqualTo(1965));
            Assert.That(reordered.Author, Is.EqualTo("Frank Herbert"));
            Assert.That(reordered.Title, Is.EqualTo("Dune"));
            Assert.That(reordered.Id, Is.EqualTo(0));
        }
    }

    [Test]
    public void Multi_table_column_mappings_are_cached()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var q = db.From<Book>()
            .Join<BookReview>((b, r) => b.Id == r.BookId)
            .Where<BookReview>(r => r.Reviewer == "alice")
            .OrderBy(x => x.Title);

        for (var i = 0; i < 2; i++)
        {
            var results = db.SelectMulti<Book, BookReview>(q);
            Assert.That(results.Map(x => x.Item1.Title), Is.EqualTo(new[] { "Dune", "The Hobbit" }));
            Assert.That(results.All(x => x.Item2.Reviewer == "alice" && x.Item2.BookId == x.Item1.Id));
        }
    }

    [Test]
    public void PostgreSql_uses_a_single_array_param_for_large_IN_lists()
    {
        var dialect = PostgreSqlDialect.Provider;
        var ids = Enumerable.Range(1, dialect.MaxInListParams + 1).ToArray();
        var q = dialect.SqlExpression<Book>().Where(x => ids.Contains(x.Id));
        Assert.That(q.WhereExpression, Does.Contain("= ANY(:0)"));
        Assert.That(q.Params.Count, Is.EqualTo(1));
        Assert.That(q.Params[0].Value, Is.EqualTo(ids));

        // lists within the limit are unchanged
        var few = new[] { 1, 2 };
        q = dialect.SqlExpression<Book>().Where(x => few.Contains(x.Id));
        Assert.That(q.WhereExpression, Does.Contain("IN (:0,:1)"));

        // mixed or unsupported value types fall back to OR'd IN lists
        var mixed = ids.Map(x => x % 2 == 0 ? (object)x : (long)x);
        q = dialect.SqlExpression<Book>().Where(x => mixed.Contains(x.Id));
        Assert.That(q.WhereExpression, Does.Not.Contain("ANY("));
        Assert.That(q.WhereExpression, Does.Contain(" OR "));
    }

    [Test]
    public void SqlServer2016_uses_a_single_OPENJSON_param_for_large_IN_lists()
    {
        var dialect = SqlServer2016Dialect.Provider;
        var names = Enumerable.Range(1, dialect.MaxInListParams).Map(x => "n" + x);
        names.AddRange(["b\"c", "d\\e", "tab\t"]);
        var q = dialect.SqlExpression<Book>().Where(x => names.Contains(x.Title));
        Assert.That(q.WhereExpression, Does.Contain("IN (SELECT value FROM OPENJSON(@0))"));
        Assert.That(q.Params.Count, Is.EqualTo(1));
        Assert.That(q.Params[0].Size, Is.EqualTo(-1)); // NVARCHAR(MAX)
        Assert.That((string)q.Params[0].Value, Does.EndWith(",\"b\\\"c\",\"d\\\\e\",\"tab\\u0009\"]"));
        Assert.That(((string)q.Params[0].Value).FromJson<string[]>(), Is.EqualTo(names));

        var guids = Enumerable.Range(0, dialect.MaxInListParams + 1).Map(_ => Guid.NewGuid());
        q = dialect.SqlExpression<Book>().Where(x => guids.Contains(Guid.Empty));
        Assert.That((string)q.Params.Last().Value, Does.StartWith($"[\"{guids[0]:D}\""));
    }
}
