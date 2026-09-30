using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// Sql.Fmt($"...") turns interpolated values into db params, making raw SQL as safe as typed queries.
/// Collections are expanded into IN lists, Sql.Raw(...) embeds trusted SQL like column names verbatim.
/// </summary>
[TestFixtureOrmLite]
public class SqlFmtUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [Test]
    public void Search_with_user_input_is_sent_as_db_params()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // User input is never embedded in SQL, so injection attempts are just values that don't match
        var author = "J.R.R. Tolkien";
        var books = db.Select<Book>(Sql.Fmt($"Author = {author}"));
        Assert.That(books.Map(x => x.Title), Is.EquivalentTo(new[] { "The Hobbit", "The Silmarillion" }));

        var malicious = "x' OR '1'='1";
        Assert.That(db.Select<Book>(Sql.Fmt($"Author = {malicious}")), Is.Empty);
    }

    [Test]
    public void Filter_by_multiple_values()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Values of any type, including enums, dates and decimals
        var maxPrice = 15m;
        var since = 1960;
        var genre = Genre.Fiction;
        var books = db.Select<Book>(Sql.Fmt($"Genre = {genre} AND Price < {maxPrice} AND Year >= {since}"));

        Assert.That(books.Map(x => x.Title), Is.EquivalentTo(new[] { "Dune", "Neuromancer" }));
    }

    [Test]
    public void Collections_are_expanded_into_IN_lists()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var authors = new[] { "Frank Herbert", "Carl Sagan" };
        var books = db.Select<Book>(Sql.Fmt($"Author IN ({authors})"));
        Assert.That(books.Map(x => x.Title), Is.EquivalentTo(new[] { "Dune", "Cosmos" }));

        // Empty collections match nothing
        var none = new List<string>();
        Assert.That(db.Select<Book>(Sql.Fmt($"Author IN ({none})")), Is.Empty);
    }

    [Test]
    public void Combine_with_typed_SqlExpression()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var minYear = 1970;
        var q = db.From<Book>()
            .Where(x => x.Available)
            .And(Sql.Fmt($"Year >= {minYear}"))
            .OrderBy(x => x.Year);

        Assert.That(db.Column<string>(q.Select(x => x.Title)),
            Is.EqualTo(new[] { "Cosmos", "Neuromancer", "A Brief History of Time", "SPQR" }));

        // Values are converted the same way as in typed queries, e.g. enums
        var genre = Genre.Science;
        Assert.That(db.Count(db.From<Book>().Where(Sql.Fmt($"Genre = {genre}"))), Is.EqualTo(2));
    }

    [Test]
    public void Reference_quoted_columns_with_Sql_Raw()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // q.Column<T>() returns the dialect-quoted column name, Sql.Raw() embeds it instead of sending it as a param
        var q = db.From<Book>();
        var price = Sql.Raw(q.Column<Book>(x => x.Price));
        var min = 15m;
        q.Where(Sql.Fmt($"{price} >= {min}"));

        Assert.That(db.Select(q).Map(x => x.Title),
            Is.EquivalentTo(new[] { "The Silmarillion", "SPQR", "The Guns of August" }));
    }

    [Test]
    public void Aggregate_filters_with_Having()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Genres with at least 2 books, one of which was published since 1980
        var minBooks = 2;
        var minYear = 1980;
        var q = db.From<Book>();
        var year = Sql.Raw(q.Column<Book>(x => x.Year));
        q.GroupBy(x => x.Genre)
         .Having(Sql.Fmt($"COUNT(*) >= {minBooks} AND MAX({year}) >= {minYear}"))
         .Select(x => x.Genre);

        Assert.That(db.Column<Genre>(q), Is.EquivalentTo(new[] { Genre.Fiction, Genre.History, Genre.Science }));
    }

    [Test]
    public void Raw_SQL_queries()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);
        var book = db.GetQuotedTableName<Book>(); // dialect-quoted table name
        var title = db.GetDialectProvider().Column<Book>(x => x.Title);
        var author = "J.R.R. Tolkien";

        // Complete SELECT statements into POCOs, columns or scalar values
        var tolkien = db.SqlList<Book>(Sql.Fmt($"SELECT * FROM {Sql.Raw(book)} WHERE Author = {author}"));
        Assert.That(tolkien.Count, Is.EqualTo(2));

        var titles = db.SqlColumn<string>(Sql.Fmt($"SELECT {Sql.Raw(title)} FROM {Sql.Raw(book)} WHERE Author = {author}"));
        Assert.That(titles, Is.EquivalentTo(new[] { "The Hobbit", "The Silmarillion" }));

        var count = db.SqlScalar<int>(Sql.Fmt($"SELECT COUNT(*) FROM {Sql.Raw(book)} WHERE Author = {author}"));
        Assert.That(count, Is.EqualTo(2));

        // WHERE-clause shorthand APIs
        Assert.That(db.Single<Book>(Sql.Fmt($"Title = {"Dune"}")).Author, Is.EqualTo("Frank Herbert"));
        Assert.That(db.Exists<Book>(Sql.Fmt($"Author = {author}")));
        Assert.That(db.Scalar<int>(Sql.Fmt($"SELECT COUNT(*) FROM {Sql.Raw(book)} WHERE Available = {true}")), Is.EqualTo(6));
    }

    [Test]
    public void Execute_updates_and_deletes()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);
        var book = Sql.Raw(db.GetQuotedTableName<Book>());
        var author = "J.R.R. Tolkien";

        var updated = db.ExecuteSql(Sql.Fmt($"UPDATE {book} SET Price = Price * {0.9m} WHERE Author = {author}"));
        Assert.That(updated, Is.EqualTo(2));
        Assert.That(db.Single<Book>(x => x.Title == "The Hobbit").Price, Is.EqualTo(11.691m).Within(0.001m));

        var ids = db.Column<int>(db.From<Book>().Where(x => !x.Available).Select(x => x.Id));
        var deleted = db.ExecuteSql(Sql.Fmt($"DELETE FROM {book} WHERE Id IN ({ids})"));
        Assert.That(deleted, Is.EqualTo(2));
    }

    [Test]
    public async Task Async_APIs()
    {
        using var db = await OpenDbConnectionAsync();
        Bookstore.Seed(db);
        var book = Sql.Raw(db.GetQuotedTableName<Book>());
        var genre = Genre.Science;

        var science = await db.SelectAsync<Book>(Sql.Fmt($"Genre = {genre}"));
        Assert.That(science.Count, Is.EqualTo(2));

        var first = await db.SingleAsync<Book>(Sql.Fmt($"Title = {"Cosmos"}"));
        Assert.That(first.Author, Is.EqualTo("Carl Sagan"));

        var max = await db.ScalarAsync<decimal>(Sql.Fmt($"SELECT MAX(Price) FROM {book} WHERE Genre = {genre}"));
        Assert.That(max, Is.EqualTo(14.00m));

        var titles = await db.SqlColumnAsync<string>(Sql.Fmt($"SELECT Title FROM {book} WHERE Year < {1950}"));
        Assert.That(titles, Is.EqualTo(new[] { "The Hobbit" }));

        var rows = await db.ExecuteSqlAsync(Sql.Fmt($"UPDATE {book} SET Available = {false} WHERE Genre = {genre}"));
        Assert.That(rows, Is.EqualTo(2));
    }

    [Test]
    public void Literal_braces_and_format_specifiers()
    {
        // {{ and }} are literal braces as in any C# interpolated string
        var sql = Sql.Fmt($"SELECT '{{json}}' WHERE 1 = {1}").ToSql(DialectProvider, out var dbParams);
        Assert.That(sql, Is.EqualTo($"SELECT '{{json}}' WHERE 1 = {DialectProvider.ParamString}p0"));
        Assert.That(dbParams["p0"], Is.EqualTo(1));

        // Format specifiers would be ignored by db params so they're rejected, format values before interpolating
        var date = new DateTime(2026, 1, 1);
        Assert.Throws<FormatException>(() => Sql.Fmt($"Created = {date:yyyy-MM-dd}").ToSql(DialectProvider, out _));
        Assert.Throws<FormatException>(() => Sql.Fmt($"Name = {"x",10}").ToSql(DialectProvider, out _));
    }
}
