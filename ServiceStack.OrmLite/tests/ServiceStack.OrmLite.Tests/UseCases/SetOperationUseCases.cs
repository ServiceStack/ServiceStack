using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// Union(), UnionAll(), Intersect() and Except() combine the results of typed queries. Each query must select the same
/// number of compatible columns. OrderBy(), Skip() and Take() on the first query apply to the combined results.
/// </summary>
[TestFixtureOrmLite]
public class SetOperationUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Union_distinct_values_from_multiple_tables()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Everyone who has written or reviewed a book
        var q = db.From<Book>().Select(x => x.Author)
            .Union(db.From<BookReview>().Select(x => x.Reviewer));

        var people = db.Column<string>(q);
        Assert.That(people.Count, Is.EqualTo(10)); // 7 distinct authors + 3 reviewers
        Assert.That(people, Does.Contain("Carl Sagan").And.Contain("Alice"));
    }

    [Test]
    public void UnionAll_keeps_duplicates()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var q = db.From<Book>().Select(x => x.Author)
            .UnionAll(db.From<BookReview>().Select(x => x.Reviewer));

        Assert.That(db.Column<string>(q).Count, Is.EqualTo(13)); // 8 books + 5 reviews
        Assert.That(db.Count(q), Is.EqualTo(13));
    }

    [Test]
    public void Combine_filtered_queries_with_params()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Each query's params are merged, e.g. fantasy books or anything under $9
        var q = db.From<Book>().Where(x => x.Genre == Genre.Fantasy).Select(x => x.Title)
            .Union(db.From<Book>().Where(x => x.Price < 9m).Select(x => x.Title));

        Assert.That(db.Column<string>(q), Is.EquivalentTo(new[] { "The Hobbit", "The Silmarillion", "Neuromancer" }));
    }

    [Test]
    public void Combine_queries_with_their_own_params()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Each query can use positional, named or Sql.Fmt params, which are renamed when combined so they don't clash,
        // e.g. both of these queries use a param named @year with different values
        var classics = db.From<Book>().Where("Year < {0}", 1950).Select(x => x.Title);

        var recent = db.From<Book>().Select(x => x.Title);
        recent.Params.Add(recent.CreateParam("year", 2000));
        recent.Where("Year > @year");

        var from1965 = db.From<Book>().Select(x => x.Title);
        from1965.Params.Add(from1965.CreateParam("year", 1965));
        from1965.Where("Year = @year");

        var authors = new[] { "Carl Sagan" };
        var byAuthors = db.From<Book>().Where(Sql.Fmt($"Author IN ({authors})")).Select(x => x.Title);

        var q = classics.Union(recent).Union(from1965).Union(byAuthors);
        Assert.That(db.Column<string>(q), Is.EquivalentTo(new[] { "The Hobbit", "SPQR", "Dune", "Cosmos" }));

        // Combined queries can themselves be combinations
        var nested = db.From<Book>().Where("Year < {0}", 1950).Select(x => x.Title)
            .Union(from1965.Clone().Union(byAuthors));
        Assert.That(db.Column<string>(nested), Is.EquivalentTo(new[] { "The Hobbit", "Dune", "Cosmos" }));

        // Queries are unchanged by being combined and can still be used on their own
        Assert.That(db.Column<string>(recent), Is.EqualTo(new[] { "SPQR" }));
        Assert.That(recent.Params.Count, Is.EqualTo(1));
    }

    [Test]
    public void Combine_queries_containing_sub_queries_with_params()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Each query filters by a sub query with its own params
        var highlyRated = db.From<Book>()
            .Where(x => x.Price > 5m && Sql.In(x.Id, db.From<BookReview>().Where(r => r.Rating >= 5).Select(r => r.BookId)))
            .Select(x => x.Title);
        var poorlyRated = db.From<Book>()
            .Where(x => x.Year > 1900 && Sql.In(x.Id, db.From<BookReview>().Where(r => r.Rating <= 2).Select(r => r.BookId)))
            .Select(x => x.Title);
        var reviewedByBob = db.From<Book>()
            .Where(x => Sql.In(x.Id, db.From<BookReview>().Where(r => r.Reviewer == "Bob").Select(r => r.BookId)))
            .Select(x => x.Title);

        var q = highlyRated.Clone().Union(poorlyRated);
        Assert.That(db.Column<string>(q), Is.EquivalentTo(new[] { "The Hobbit", "Dune", "SPQR" }));

        // Highly or poorly rated books, except those reviewed by Bob
        q = highlyRated.Clone().Union(poorlyRated).Except(reviewedByBob);
        Assert.That(db.Column<string>(q), Is.EquivalentTo(new[] { "Dune" }));

        // A sub query can also combine queries, e.g. books with 5 or 2 star reviews
        var reviewedIds = db.From<BookReview>().Where(r => r.Rating == 5).Select(r => r.BookId)
            .Union(db.From<BookReview>().Where(r => r.Rating == 2).Select(r => r.BookId));
        var extremes = db.From<Book>().Where(x => x.Year > 1900 && Sql.In(x.Id, reviewedIds)).Select(x => x.Title)
            .UnionAll(db.From<Book>().Where(x => x.Genre == Genre.Science).Select(x => x.Title));
        Assert.That(db.Column<string>(extremes),
            Is.EquivalentTo(new[] { "The Hobbit", "Dune", "SPQR", "A Brief History of Time", "Cosmos" }));

        // Queries are unchanged by being combined, and combined queries can be re-executed
        Assert.That(db.Column<string>(reviewedByBob), Is.EquivalentTo(new[] { "The Hobbit", "SPQR" }));
        Assert.That(db.Count(extremes), Is.EqualTo(5));
        Assert.That(db.Column<string>(extremes).Count, Is.EqualTo(5));
    }

    [Test]
    public void Intersect_and_Except()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var available = db.From<Book>().Where(x => x.Available).Select(x => x.Id);
        var reviewed = db.From<BookReview>().Select(x => x.BookId);

        // Available books that have been reviewed
        var q = db.From<Book>().Where(x => x.Available).Select(x => x.Id)
            .Intersect(db.From<BookReview>().Select(x => x.BookId));
        var reviewedIds = db.Column<int>(q);
        Assert.That(db.SelectByIds<Book>(reviewedIds).Map(x => x.Title),
            Is.EquivalentTo(new[] { "The Hobbit", "Dune", "Cosmos", "SPQR" }));

        // Available books that haven't been reviewed yet
        q = available.Except(reviewed);
        var unreviewedIds = db.Column<int>(q);
        Assert.That(db.SelectByIds<Book>(unreviewedIds).Map(x => x.Title),
            Is.EquivalentTo(new[] { "Neuromancer", "A Brief History of Time" }));
    }

    [Test]
    public void Order_and_page_the_combined_results()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var q = db.From<Book>().Select(x => x.Author)
            .Union(db.From<BookReview>().Select(x => x.Reviewer))
            .OrderBy(x => x.Author)   // ordered by the combined result's first column
            .Skip(1).Take(3);

        Assert.That(db.Column<string>(q), Is.EqualTo(new[] { "Barbara Tuchman", "Bob", "Carl Sagan" }));
    }

    [Test]
    public void Combine_queries_with_their_own_limits()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Fantasy books plus the 2 cheapest books. Queries being combined keep their own OrderBy() + Take(),
        // whereas OrderBy(), Skip() and Take() on the first query apply to the combined results
        var q = db.From<Book>().Where(x => x.Genre == Genre.Fantasy).Select(x => x.Title)
            .UnionAll(db.From<Book>().OrderBy(x => x.Price).Take(2).Select(x => x.Title));

        Assert.That(db.Column<string>(q),
            Is.EquivalentTo(new[] { "The Hobbit", "The Silmarillion", "Neuromancer", "Dune" }));
    }

    [Test]
    public void Select_combined_rows_into_POCOs()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var q = db.From<Book>().Where(x => x.Year < 1950)
            .Union(db.From<Book>().Where(x => x.Year > 2000))
            .OrderBy(x => x.Year);

        var books = db.Select(q);
        Assert.That(books.Map(x => x.Title), Is.EqualTo(new[] { "The Hobbit", "SPQR" }));
        Assert.That(books[0].Author, Is.EqualTo("J.R.R. Tolkien"));
    }

    [Test]
    public async Task Async_APIs_and_reusing_the_same_query()
    {
        using var db = await OpenDbConnectionAsync();
        Bookstore.Seed(db);

        var q = db.From<Book>().Where(x => x.Genre == Genre.Science).Select(x => x.Title)
            .Union(db.From<Book>().Where(x => x.Genre == Genre.History).Select(x => x.Title));

        for (var i = 0; i < 2; i++)
        {
            Assert.That((await db.ColumnAsync<string>(q)).Count, Is.EqualTo(4));
            Assert.That(await db.CountAsync(q), Is.EqualTo(4));
        }
        Assert.That(q.Params.Count, Is.EqualTo(1 + 1)); // each query's param, merged once
    }
}
