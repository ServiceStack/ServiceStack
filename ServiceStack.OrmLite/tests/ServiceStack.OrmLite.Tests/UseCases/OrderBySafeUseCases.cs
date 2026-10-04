using System;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// OrderBySafe() sorts by user-supplied field names, e.g. from a ?orderBy= query string, by resolving them to
/// quoted columns instead of embedding them in SQL. Prefix a field with '-' or suffix it with DESC to sort descending.
/// </summary>
[TestFixtureOrmLite]
public class OrderBySafeUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    // Only allow sorting by these fields
    static readonly string[] SortableFields = [nameof(Book.Title), nameof(Book.Price), nameof(Book.Year)];

    [Test]
    public void Sort_by_user_supplied_field()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var orderBy = "Year"; // e.g. from ?orderBy=Year
        var q = db.From<Book>().OrderBySafe(orderBy, SortableFields);

        Assert.That(db.Select(q)[0].Title, Is.EqualTo("The Hobbit"));
    }

    [Test]
    public void Sort_descending_and_by_multiple_fields()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // '-' prefix or DESC suffix sorts descending, field names are case-insensitive
        var q = db.From<Book>().OrderBySafe("-price", SortableFields).Take(2);
        Assert.That(db.Column<string>(q.Select(x => x.Title)), Is.EqualTo(new[] { "SPQR", "The Guns of August" }));

        q = db.From<Book>().Where(x => x.Author == "J.R.R. Tolkien").OrderBySafe("Year DESC, Title", SortableFields);
        Assert.That(db.Column<string>(q.Select(x => x.Title)), Is.EqualTo(new[] { "The Silmarillion", "The Hobbit" }));
    }

    [Test]
    public void Paged_results_with_user_supplied_sort()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Typical paged API: ?orderBy=-Year&skip=2&take=3
        string orderBy = "-Year";
        int skip = 2, take = 3;
        var q = db.From<Book>()
            .OrderBySafe(orderBy, SortableFields)
            .Skip(skip).Take(take);

        Assert.That(db.Column<int>(q.Select(x => x.Year)), Is.EqualTo(new[] { 1984, 1980, 1977 }));
    }

    [Test]
    public void Missing_orderBy_keeps_the_default_order()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        string orderBy = null; // no ?orderBy= specified
        var q = db.From<Book>()
            .OrderBy(x => x.Title) // default order
            .OrderBySafe(orderBy, SortableFields);

        Assert.That(db.Select(q)[0].Title, Is.EqualTo("A Brief History of Time"));
    }

    [Test]
    public void Sort_by_fields_of_joined_tables()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Without allowed fields, any field of the queried tables can be used
        var q = db.From<Book>()
            .Join<BookReview>((b, r) => b.Id == r.BookId)
            .OrderBySafe("-Rating, Reviewer")
            .Select<Book, BookReview>((b, r) => new { b.Title, r.Reviewer, r.Rating });

        var rows = db.Select<(string Title, string Reviewer, int Rating)>(q);
        Assert.That(rows.Map(x => x.Rating), Is.EqualTo(new[] { 5, 5, 4, 3, 2 }));
        Assert.That(rows.Map(x => x.Reviewer), Is.EqualTo(new[] { "Alice", "Alice", "Bob", "Carol", "Bob" }));
    }

    [Test]
    public void The_allowed_fields_are_a_list()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var q = db.From<Book>().OrderBySafe("-Price", [nameof(Book.Title), nameof(Book.Price)]).Take(1);
        Assert.That(db.Single(q).Price, Is.EqualTo(db.Scalar<Book, decimal>(x => Sql.Max(x.Price))));

        // An empty list allows no fields, unlike OrderBySafe(orderBy) which allows any field of the query
        Assert.Throws<ArgumentException>(() => db.From<Book>().OrderBySafe("Price", []));

        // A mistake in the allowed fields is reported as one
        var ex = Assert.Throws<ArgumentException>(() => db.From<Book>().OrderBySafe("Pirce", ["Pirce"]));
        Assert.That(ex.Message, Does.Contain("isn't a field of the query"));
    }

    [Test]
    public void Invalid_or_malicious_input_throws_ArgumentException()
    {
        using var db = OpenDbConnection();
        var q = db.From<Book>();

        // Fields not in the allow list
        Assert.Throws<ArgumentException>(() => q.OrderBySafe("Author", SortableFields));
        // Unknown fields when no allow list is specified
        Assert.Throws<ArgumentException>(() => q.OrderBySafe("Unknown"));
        // SQL fragments are never embedded
        Assert.Throws<ArgumentException>(() => q.OrderBySafe("Id--"));
        Assert.Throws<ArgumentException>(() => q.OrderBySafe("Id;DROP TABLE Book"));
        Assert.Throws<ArgumentException>(() => q.OrderBySafe("(SELECT 1)"));
        Assert.Throws<ArgumentException>(() => q.OrderBySafe("Id DESC NULLS FIRST"));
        Assert.Throws<ArgumentException>(() => q.OrderBySafe("Id SIDEWAYS"));
    }
}
