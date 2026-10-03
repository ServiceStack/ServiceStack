using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// With() names a sub query as a common table expression (CTE), which the query can then join, filter by and select
/// from like a table, as often as it's needed. With&lt;TCte&gt;() names it after a class with a property for each
/// column the sub query selects, in the same order, so it's read with the typed APIs.
/// </summary>
[TestFixtureOrmLite]
public class CteUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    // The columns of the sub queries below, in the order they select them
    public class AuthorTotal
    {
        public string Author { get; set; }
        public int Books { get; set; }
        public decimal Total { get; set; }
    }

    public class TopAuthor
    {
        public string Author { get; set; }
    }

    public class BookTotal
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public int Books { get; set; }
    }

    // How many books each author has, and what they cost together
    static SqlExpression<Book> AuthorTotals(System.Data.IDbConnection db) => db.From<Book>()
        .GroupBy(x => x.Author)
        .Select(x => new { x.Author, Books = Sql.Count("*"), Total = Sql.Sum(x.Price) });

    [Test]
    public void Join_a_query_to_a_named_sub_query()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Each book since 1970 with how many books its author has
        var q = db.From<Book>()
            .With<AuthorTotal>(AuthorTotals(db))
            .Join<AuthorTotal>((b, t) => b.Author == t.Author)
            .Where(b => b.Year >= 1970)
            .OrderBy(b => b.Title)
            .Select<Book, AuthorTotal>((b, t) => new { b.Title, b.Author, t.Books });

        var results = db.Select<BookTotal>(q);

        Assert.That(results.Map(x => x.Title), Is.EqualTo(new[] {
            "A Brief History of Time", "Cosmos", "Neuromancer", "SPQR", "The Silmarillion" }));
        Assert.That(results.First(x => x.Title == "The Silmarillion").Books, Is.EqualTo(2));
        Assert.That(results.First(x => x.Title == "Cosmos").Books, Is.EqualTo(1));
    }

    [Test]
    public void Query_a_named_sub_query()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Authors with more than 1 book, which can't be filtered in the query that counts them without HAVING
        var q = db.From<AuthorTotal>()
            .With<AuthorTotal>(AuthorTotals(db))
            .Where(x => x.Books > 1);

        var prolific = db.Select(q);

        Assert.That(prolific.Count, Is.EqualTo(1));
        Assert.That(prolific[0].Author, Is.EqualTo("J.R.R. Tolkien"));
        Assert.That(prolific[0].Books, Is.EqualTo(2));
        Assert.That(prolific[0].Total, Is.EqualTo(28.49m).Within(0.001m));

        Assert.That(db.Count(q), Is.EqualTo(1));
    }

    [Test]
    public void Use_a_named_sub_query_more_than_once()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // The only book of each author that has one, with what it costs. The sub query is written once and read by
        // both the join and the IN sub query.
        var q = db.From<Book>()
            .With<AuthorTotal>(AuthorTotals(db))
            .Join<AuthorTotal>((b, t) => b.Author == t.Author)
            .Where(b => Sql.In(b.Author, db.From<AuthorTotal>().Where(t => t.Books == 1).Select(t => t.Author)))
            .OrderBy(b => b.Title)
            .Select<Book, AuthorTotal>((b, t) => new { b.Title, b.Author, t.Books });

        var results = db.Select<BookTotal>(q);

        Assert.That(results.Map(x => x.Title), Is.EqualTo(new[] {
            "A Brief History of Time", "Cosmos", "Dune", "Neuromancer", "SPQR", "The Guns of August" }));
        Assert.That(results.All(x => x.Books == 1));
    }

    [Test]
    public void Build_a_query_in_named_steps()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // A sub query can read the sub queries named before it
        var q = db.From<Book>()
            .With<AuthorTotal>(AuthorTotals(db))
            .With<TopAuthor>(db.From<AuthorTotal>().Where(x => x.Books > 1).Select(x => x.Author))
            .Join<TopAuthor>((b, a) => b.Author == a.Author)
            .Where(b => b.Available)
            .OrderBy(b => b.Title);

        Assert.That(db.Select(q).Map(x => x.Title), Is.EqualTo(new[] { "The Hobbit" }));
    }

    [Test]
    public void Params_of_sub_queries_are_kept()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var since = 1970;
        var maxPrice = 14m;
        var recent = db.From<Book>().Where(x => x.Year >= since).GroupBy(x => x.Author)
            .Select(x => new { x.Author, Books = Sql.Count("*"), Total = Sql.Sum(x.Price) });

        var q = db.From<Book>()
            .Where(b => b.Price <= maxPrice)
            .With<AuthorTotal>(recent)
            .Join<AuthorTotal>((b, t) => b.Author == t.Author)
            .And(b => b.Available)
            .OrderBy(b => b.Title);

        Assert.That(db.Select(q).Map(x => x.Title), Is.EqualTo(new[] {
            "A Brief History of Time", "Cosmos", "Neuromancer", "The Hobbit" }));
    }

    [Test]
    public void Connection_filters_apply_to_sub_queries()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Only books in stock are counted
        db.UseFilters(FilterSet.Create(f => f.Filter<Book>(x => x.Available)));

        var q = db.From<AuthorTotal>()
            .With<AuthorTotal>(AuthorTotals(db))
            .OrderBy(x => x.Author);
        var totals = db.Select(q);

        Assert.That(totals.Map(x => x.Author), Does.Not.Contain("Barbara Tuchman"));
        Assert.That(totals.First(x => x.Author == "J.R.R. Tolkien").Books, Is.EqualTo(1));
    }

    [Test]
    public void Read_a_named_sub_query_from_custom_sql()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Without a class, the sub query is read by its name in custom SQL
        var q = db.From<Book>()
            .With("fantasy", db.From<Book>().Where(x => x.Genre == Genre.Fantasy))
            .From("fantasy")
            .OrderBy(x => x.Year);

        Assert.That(db.Select(q).Map(x => x.Title), Is.EqualTo(new[] { "The Hobbit", "The Silmarillion" }));
    }

    [Test]
    public void Combine_with_a_recursive_query()
    {
        using var db = OpenDbConnection();
        Subjects.Seed(db);

        // The active subjects under Books, where the recursive query reads a named sub query
        var q = db.From<Subject>()
            .With<ActiveSubject>(db.From<Subject>().Where(x => x.Active).Select(x => x.Id))
            .WithRecursive(
                seed: db.From<Subject>().Where(x => x.Name == "Books"),
                recurse: (parent, child) => child.ParentId == parent.Id)
            .Where(x => Sql.In(x.Id, db.From<ActiveSubject>().Select(a => a.Id)))
            .OrderBy(x => x.Id);

        Assert.That(db.Select(q).Map(x => x.Name), Is.EqualTo(new[] {
            "Books", "Fiction", "Fantasy", "Epic Fantasy", "Non-Fiction", "History", "Ancient History", "Science" }));
    }

    public class ActiveSubject
    {
        public int Id { get; set; }
    }

    [Test]
    public async Task Query_a_named_sub_query_async()
    {
        using var db = await OpenDbConnectionAsync();
        Bookstore.Seed(db);

        var q = db.From<AuthorTotal>()
            .With<AuthorTotal>(AuthorTotals(db))
            .OrderByDescending(x => x.Total)
            .Take(1);

        var top = await db.SingleAsync(q);
        Assert.That(top.Author, Is.EqualTo("J.R.R. Tolkien"));
    }

    [Test]
    public void Names_are_unique_and_sub_queries_have_no_sub_queries_of_their_own()
    {
        using var db = OpenDbConnection();

        var q = db.From<Book>().With<AuthorTotal>(AuthorTotals(db));
        Assert.That(() => q.With<AuthorTotal>(AuthorTotals(db)), Throws.ArgumentException);

        // Name them on the query that reads them instead
        var nested = db.From<AuthorTotal>().With<AuthorTotal>(AuthorTotals(db));
        Assert.That(() => db.From<Book>().With<TopAuthor>(nested), Throws.TypeOf<NotSupportedException>());

        // A clone has its own
        var clone = q.Clone().With<TopAuthor>(db.From<AuthorTotal>().Select(x => x.Author));
        Assert.That(clone.ToSelectStatement(), Does.Contain("AS (").And.Contain(","));
        Assert.That(() => q.With<TopAuthor>(db.From<AuthorTotal>().Select(x => x.Author)), Throws.Nothing);
    }
}
