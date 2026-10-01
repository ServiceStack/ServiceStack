using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// db.Explain() returns the RDBMS's query plan of a query, e.g. to check that it uses an index, using EXPLAIN in
/// PostgreSQL and MySQL, EXPLAIN QUERY PLAN in SQLite and SHOWPLAN_TEXT in SQL Server.
/// </summary>
[TestFixtureOrmLite]
public class ExplainUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    // How each RDBMS describes a lookup by primary key
    const string PrimaryKeyLookup = "(?i)primary key|index scan|index seek|primary";

    [Test]
    public void Explain_a_typed_query()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var q = db.From<Book>().Where(x => x.Author == "J.R.R. Tolkien").OrderBy(x => x.Year);
        var plan = db.Explain(q);

        // The plan is text in the RDBMS's own format, e.g. in SQLite:
        //   SCAN Book
        //   USE TEMP B-TREE FOR ORDER BY
        Assert.That(plan, Does.Contain("Book").IgnoreCase);

        // The query isn't modified and can still be run
        Assert.That(db.Select(q).Map(x => x.Title), Is.EqualTo(new[] { "The Hobbit", "The Silmarillion" }));
    }

    [Test]
    public void Check_that_a_query_uses_an_index()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var byId = db.Explain(db.From<Book>().Where(x => x.Id == 1));
        Assert.That(byId, Does.Match(PrimaryKeyLookup));

        var join = db.Explain(db.From<Book>()
            .Join<BookReview>((b, r) => b.Id == r.BookId)
            .Where<BookReview>(r => r.Rating >= 4));
        Assert.That(join, Does.Contain("BookReview").IgnoreCase.Or.Contain("book_review"));
        Assert.That(join, Does.Match(PrimaryKeyLookup).Or.Contain("Hash Join"));
    }

    [Test]
    public void Explain_custom_sql()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var table = db.GetQuotedTableName<Book>();
        var plan = db.Explain($"SELECT * FROM {table} WHERE 1 = @one", new { one = 1 });
        Assert.That(plan, Does.Contain("Book").IgnoreCase);

        Assert.That(db.Explain($"SELECT * FROM {table}"), Does.Contain("Book").IgnoreCase);
    }

    [Test]
    public void Analyze_runs_the_query_to_include_actual_row_counts()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var q = db.From<Book>().Where(x => x.Author == "J.R.R. Tolkien");
        if (Dialect == Dialect.Sqlite)
        {
            Assert.Throws<NotSupportedException>(() => db.Explain(q, analyze: true));
            return;
        }

        var plan = db.Explain(q, analyze: true);
        Assert.That(plan, Does.Contain("Book").IgnoreCase);

        // Other queries on the connection aren't affected
        Assert.That(db.Count<Book>(), Is.EqualTo(8));
    }

    [Test]
    public async Task Async_APIs()
    {
        using var db = await OpenDbConnectionAsync();
        Bookstore.Seed(db);

        var plan = await db.ExplainAsync(db.From<Book>().Where(x => x.Id == 1));
        Assert.That(plan, Does.Match(PrimaryKeyLookup));

        var table = db.GetQuotedTableName<Book>();
        Assert.That(await db.ExplainAsync($"SELECT * FROM {table} WHERE 1 = @one", new { one = 1 }), Is.Not.Empty);
        Assert.That(await db.CountAsync<Book>(), Is.EqualTo(8));
    }
}
