using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// UpdateOnlyReturning() and DeleteReturning() return the affected rows in the same statement, using RETURNING in
/// PostgreSQL and SQLite and OUTPUT in SQL Server, instead of a separate query that could see different rows.
/// </summary>
[TestFixtureOrmLite]
public class ReturningUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    const string Unsupported = "MySQL doesn't support returning rows from UPDATE and DELETE statements";

    [Test]
    [IgnoreDialect(Dialect.AnyMySql, Unsupported)]
    public void Update_and_return_the_updated_rows()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var updated = db.UpdateOnlyReturning(() => new Book { Price = 20m }, where: x => x.Author == "J.R.R. Tolkien");

        // Rows are returned with all their columns, as they are after the update
        Assert.That(updated.Map(x => x.Title), Is.EquivalentTo(new[] { "The Hobbit", "The Silmarillion" }));
        Assert.That(updated.All(x => x.Price == 20m && x.Author == "J.R.R. Tolkien" && x.Id > 0));
        Assert.That(db.Count<Book>(x => x.Price == 20m), Is.EqualTo(2));
    }

    [Test]
    [IgnoreDialect(Dialect.AnyMySql, Unsupported)]
    public void Update_rows_matching_a_query()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var q = db.From<Book>().Where(x => x.Genre == Genre.History && x.Available);
        var updated = db.UpdateOnlyReturning(() => new Book { Available = false }, q);

        Assert.That(updated.Map(x => x.Title), Is.EqualTo(new[] { "SPQR" }));
        Assert.That(updated[0].Available, Is.False);

        // No matching rows returns an empty list
        Assert.That(db.UpdateOnlyReturning(() => new Book { Price = 1m }, where: x => x.Year > 3000), Is.Empty);
    }

    [Test]
    [IgnoreDialect(Dialect.AnyMySql, Unsupported)]
    public void Delete_and_return_the_deleted_rows()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var deleted = db.DeleteReturning<Book>(x => !x.Available);

        Assert.That(deleted.Map(x => x.Title), Is.EquivalentTo(new[] { "The Silmarillion", "The Guns of August" }));
        Assert.That(db.Count<Book>(), Is.EqualTo(6));
    }

    [Test]
    [IgnoreDialect(Dialect.AnyMySql, Unsupported)]
    public void Take_items_from_a_queue()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Atomically remove and return the reviews to process, so no other worker can process the same rows
        var toProcess = db.DeleteReturning<BookReview>(x => x.Rating >= 4);

        Assert.That(toProcess.Map(x => x.Reviewer), Is.EquivalentTo(new[] { "Alice", "Bob", "Alice" }));
        Assert.That(db.DeleteReturning<BookReview>(x => x.Rating >= 4), Is.Empty); // already taken
    }

    [Test]
    [IgnoreDialect(Dialect.AnyMySql, Unsupported)]
    public void Delete_rows_matching_a_query_with_a_join()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Books reviewed by Bob
        var q = db.From<Book>()
            .Join<BookReview>((b, r) => b.Id == r.BookId)
            .Where<BookReview>(r => r.Reviewer == "Bob");
        var deleted = db.DeleteReturning(q);

        Assert.That(deleted.Map(x => x.Title), Is.EquivalentTo(new[] { "The Hobbit", "SPQR" }));
        Assert.That(db.Count<Book>(), Is.EqualTo(6));
    }

    [Test]
    [IgnoreDialect(Dialect.AnyMySql, Unsupported)]
    public async Task Async_APIs()
    {
        using var db = await OpenDbConnectionAsync();
        Bookstore.Seed(db);

        var updated = await db.UpdateOnlyReturningAsync(() => new Book { Price = 9m }, where: x => x.Genre == Genre.Science);
        Assert.That(updated.Count, Is.EqualTo(2));
        Assert.That(updated.All(x => x.Price == 9m));

        var deleted = await db.DeleteReturningAsync(db.From<Book>().Where(x => x.Price == 9m));
        Assert.That(deleted.Map(x => x.Title), Is.EquivalentTo(new[] { "A Brief History of Time", "Cosmos" }));
    }

    [Test]
    public void Unsupported_RDBMS_throw_NotSupportedException()
    {
        if ((Dialect & Dialect.AnyMySql) == 0)
            Assert.Ignore("Only applies to MySQL");

        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        Assert.Throws<NotSupportedException>(() => db.DeleteReturning<Book>(x => x.Id == 1));
        Assert.Throws<NotSupportedException>(() => db.UpdateOnlyReturning(() => new Book { Price = 1m }, where: x => x.Id == 1));
        Assert.That(db.Count<Book>(), Is.EqualTo(8)); // nothing was deleted or updated
        Assert.That(db.SingleById<Book>(1).Price, Is.Not.EqualTo(1m));
    }
}
