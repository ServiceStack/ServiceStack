using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// Sql.Fmt($"...") turns interpolated values into db params, making raw SQL as safe as typed queries.
/// Collections are expanded into IN lists, tables referenced with typeof(Table), ModelDefinitions or db.TableRef&lt;Table&gt;()
/// are embedded as quoted table names and Sql.Raw(...) embeds other trusted SQL like column names verbatim.
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
    public void Reference_columns_with_ColumnRef()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // db.ColumnRef<T>() is the dialect-quoted column name, embedded in the SQL instead of being sent as a param.
        // Naming table and column refs after the table and column keeps the SQL readable.
        var Price = db.ColumnRef<Book>(x => x.Price);
        var min = 15m;
        var q = db.From<Book>().Where(Sql.Fmt($"{Price} >= {min}"));

        Assert.That(db.Select(q).Map(x => x.Title),
            Is.EquivalentTo(new[] { "The Silmarillion", "SPQR", "The Guns of August" }));

        // The same as embedding q.Column<T>() with Sql.Raw()
        Assert.That(Price.Text, Is.EqualTo(Sql.Raw(q.Column<Book>(x => x.Price)).Text));
    }

    [Test]
    public void Reference_multiple_columns_with_ColumnRefs()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var Book = db.TableRef<Book>();
        var (Title, Author, Year) = db.ColumnRefs<Book>(x => new { x.Title, x.Author, x.Year });
        var author = "J.R.R. Tolkien";

        var titles = db.SqlColumn<string>(Sql.Fmt($"SELECT {Title} FROM {Book} WHERE {Author} = {author} ORDER BY {Year}"));
        Assert.That(titles, Is.EqualTo(new[] { "The Hobbit", "The Silmarillion" }));

        // Deconstructing into a different number of variables than columns throws
        Assert.Throws<ArgumentException>(() => {
            var (_, _) = db.ColumnRefs<Book>(x => new { x.Title, x.Author, x.Year });
        });
        // Only columns of the table can be referenced
        Assert.Throws<ArgumentException>(() => db.ColumnRefs<Book>(x => new { x.Title, Upper = x.Title.ToUpper() }));
        Assert.Throws<ArgumentException>(() => db.ColumnRef<Book>(x => x.Title.Length));
    }

    [Test]
    public void Aggregate_filters_with_Having()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Genres with at least 2 books, one of which was published since 1980
        var minBooks = 2;
        var minYear = 1980;
        var Year = db.ColumnRef<Book>(x => x.Year);
        var q = db.From<Book>()
            .GroupBy(x => x.Genre)
            .Having(Sql.Fmt($"COUNT(*) >= {minBooks} AND MAX({Year}) >= {minYear}"))
            .Select(x => x.Genre);

        Assert.That(db.Column<Genre>(q), Is.EquivalentTo(new[] { Genre.Fiction, Genre.History, Genre.Science }));
    }

    [Test]
    public void Raw_SQL_queries()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);
        var Book = db.TableRef<Book>();
        var (Title, Author, Available) = db.ColumnRefs<Book>(x => new { x.Title, x.Author, x.Available });
        var author = "J.R.R. Tolkien";

        // Complete SELECT statements into POCOs, columns or scalar values
        var tolkien = db.SqlList<Book>(Sql.Fmt($"SELECT * FROM {Book} WHERE {Author} = {author}"));
        Assert.That(tolkien.Count, Is.EqualTo(2));

        var titles = db.SqlColumn<string>(Sql.Fmt($"SELECT {Title} FROM {Book} WHERE {Author} = {author}"));
        Assert.That(titles, Is.EquivalentTo(new[] { "The Hobbit", "The Silmarillion" }));

        var count = db.SqlScalar<int>(Sql.Fmt($"SELECT COUNT(*) FROM {Book} WHERE {Author} = {author}"));
        Assert.That(count, Is.EqualTo(2));

        // WHERE-clause shorthand APIs
        Assert.That(db.Single<Book>(Sql.Fmt($"{Title} = {"Dune"}")).Author, Is.EqualTo("Frank Herbert"));
        Assert.That(db.Exists<Book>(Sql.Fmt($"{Author} = {author}")));
        Assert.That(db.Scalar<int>(Sql.Fmt($"SELECT COUNT(*) FROM {Book} WHERE {Available} = {true}")), Is.EqualTo(6));
    }

    [Test]
    public void Execute_updates_and_deletes()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);
        var Book = db.TableRef<Book>();
        var (Id, Price, Author) = db.ColumnRefs<Book>(x => new { x.Id, x.Price, x.Author });
        var author = "J.R.R. Tolkien";

        var updated = db.ExecuteSql(Sql.Fmt($"UPDATE {Book} SET {Price} = {Price} * {0.9m} WHERE {Author} = {author}"));
        Assert.That(updated, Is.EqualTo(2));
        Assert.That(db.Single<Book>(x => x.Title == "The Hobbit").Price, Is.EqualTo(11.691m).Within(0.001m));

        var ids = db.Column<int>(db.From<Book>().Where(x => !x.Available).Select(x => x.Id));
        var deleted = db.ExecuteSql(Sql.Fmt($"DELETE FROM {Book} WHERE {Id} IN ({ids})"));
        Assert.That(deleted, Is.EqualTo(2));
    }

    [Test]
    public void Raw_SQL_queries_into_collections()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);
        var Book = db.TableRef<Book>();
        var (Id, Title, Author, Year) = db.ColumnRefs<Book>(x => new { x.Id, x.Title, x.Author, x.Year });
        var authors = new[] { "J.R.R. Tolkien", "Frank Herbert" };

        var distinct = db.ColumnDistinct<string>(Sql.Fmt($"SELECT {Author} FROM {Book} WHERE {Author} IN ({authors})"));
        Assert.That(distinct, Is.EquivalentTo(authors));

        var lookup = db.Lookup<string, string>(Sql.Fmt($"SELECT {Author}, {Title} FROM {Book} WHERE {Author} IN ({authors})"));
        Assert.That(lookup["J.R.R. Tolkien"], Is.EquivalentTo(new[] { "The Hobbit", "The Silmarillion" }));
        Assert.That(lookup["Frank Herbert"], Is.EqualTo(new[] { "Dune" }));

        var years = db.Dictionary<string, int>(Sql.Fmt($"SELECT {Title}, {Year} FROM {Book} WHERE {Author} IN ({authors})"));
        Assert.That(years, Is.EquivalentTo(new Dictionary<string, int> {
            ["The Hobbit"] = 1937, ["The Silmarillion"] = 1977, ["Dune"] = 1965,
        }));

        var pairs = db.KeyValuePairs<string, int>(Sql.Fmt($"SELECT {Title}, {Year} FROM {Book} WHERE {Year} < {1950}"));
        Assert.That(pairs, Is.EqualTo(new[] { new KeyValuePair<string, int>("The Hobbit", 1937) }));

        Assert.That(db.RowCount(Sql.Fmt($"SELECT {Id} FROM {Book} WHERE {Author} IN ({authors})")), Is.EqualTo(3));

        // Results of a table into a different model
        var summaries = db.Select<BookTitle>(typeof(Book), Sql.Fmt($"{Author} IN ({authors})"));
        Assert.That(summaries.Map(x => x.Title), Is.EquivalentTo(new[] { "The Hobbit", "The Silmarillion", "Dune" }));

        // Lazily loaded streams
        Assert.That(db.SelectLazy<Book>(Sql.Fmt($"{Author} IN ({authors})")).Map(x => x.Title),
            Is.EquivalentTo(new[] { "The Hobbit", "The Silmarillion", "Dune" }));
        Assert.That(db.ColumnLazy<string>(Sql.Fmt($"SELECT {Title} FROM {Book} WHERE {Year} < {1950}")).ToList(),
            Is.EqualTo(new[] { "The Hobbit" }));
    }

    [Test]
    public void Delete_rows_matching_a_filter()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);
        var (Author, Year) = db.ColumnRefs<Book>(x => new { x.Author, x.Year });

        var malicious = "x' OR '1'='1";
        Assert.That(db.Delete<Book>(Sql.Fmt($"{Author} = {malicious}")), Is.EqualTo(0));

        Assert.That(db.Delete<Book>(Sql.Fmt($"{Author} = {"J.R.R. Tolkien"}")), Is.EqualTo(2));
        Assert.That(db.Delete(typeof(Book), Sql.Fmt($"{Year} IN ({new[] { 1965, 1984 }})")), Is.EqualTo(2));
        Assert.That(db.Count<Book>(), Is.EqualTo(4));
    }

    [Test]
    public async Task Async_APIs_into_collections()
    {
        using var db = await OpenDbConnectionAsync();
        Bookstore.Seed(db);
        var Book = db.TableRef<Book>();
        var (Id, Title, Author, Year) = db.ColumnRefs<Book>(x => new { x.Id, x.Title, x.Author, x.Year });
        var authors = new[] { "J.R.R. Tolkien", "Frank Herbert" };

        var distinct = await db.ColumnDistinctAsync<string>(Sql.Fmt($"SELECT {Author} FROM {Book} WHERE {Author} IN ({authors})"));
        Assert.That(distinct, Is.EquivalentTo(authors));

        var lookup = await db.LookupAsync<string, string>(Sql.Fmt($"SELECT {Author}, {Title} FROM {Book} WHERE {Author} IN ({authors})"));
        Assert.That(lookup["J.R.R. Tolkien"], Is.EquivalentTo(new[] { "The Hobbit", "The Silmarillion" }));

        var years = await db.DictionaryAsync<string, int>(Sql.Fmt($"SELECT {Title}, {Year} FROM {Book} WHERE {Author} IN ({authors})"));
        Assert.That(years["Dune"], Is.EqualTo(1965));
        Assert.That(years.Count, Is.EqualTo(3));

        var pairs = await db.KeyValuePairsAsync<string, int>(Sql.Fmt($"SELECT {Title}, {Year} FROM {Book} WHERE {Year} < {1950}"));
        Assert.That(pairs, Is.EqualTo(new[] { new KeyValuePair<string, int>("The Hobbit", 1937) }));

        Assert.That(await db.RowCountAsync(Sql.Fmt($"SELECT {Id} FROM {Book} WHERE {Author} IN ({authors})")), Is.EqualTo(3));

        var summaries = await db.SelectAsync<BookTitle>(typeof(Book), Sql.Fmt($"{Author} IN ({authors})"));
        Assert.That(summaries.Map(x => x.Title), Is.EquivalentTo(new[] { "The Hobbit", "The Silmarillion", "Dune" }));

        var titles = new List<string>();
        await foreach (var title in db.ColumnLazyAsync<string>(Sql.Fmt($"SELECT {Title} FROM {Book} WHERE {Author} IN ({authors})")))
            titles.Add(title);
        Assert.That(titles, Is.EquivalentTo(new[] { "The Hobbit", "The Silmarillion", "Dune" }));

        Assert.That(await db.DeleteAsync<Book>(Sql.Fmt($"{Author} = {"J.R.R. Tolkien"}")), Is.EqualTo(2));
        Assert.That(await db.DeleteAsync(typeof(Book), Sql.Fmt($"{Year} IN ({new[] { 1965, 1984 }})")), Is.EqualTo(2));
        Assert.That(await db.CountAsync<Book>(), Is.EqualTo(4));
    }

    [Test]
    public async Task Async_APIs()
    {
        using var db = await OpenDbConnectionAsync();
        Bookstore.Seed(db);
        var Book = db.TableRef<Book>();
        var (Title, Genre, Price, Year, Available) =
            db.ColumnRefs<Book>(x => new { x.Title, x.Genre, x.Price, x.Year, x.Available });
        var genre = UseCases.Genre.Science;

        var science = await db.SelectAsync<Book>(Sql.Fmt($"{Genre} = {genre}"));
        Assert.That(science.Count, Is.EqualTo(2));

        var first = await db.SingleAsync<Book>(Sql.Fmt($"{Title} = {"Cosmos"}"));
        Assert.That(first.Author, Is.EqualTo("Carl Sagan"));

        var max = await db.ScalarAsync<decimal>(Sql.Fmt($"SELECT MAX({Price}) FROM {Book} WHERE {Genre} = {genre}"));
        Assert.That(max, Is.EqualTo(14.00m));

        var titles = await db.SqlColumnAsync<string>(Sql.Fmt($"SELECT {Title} FROM {Book} WHERE {Year} < {1950}"));
        Assert.That(titles, Is.EqualTo(new[] { "The Hobbit" }));

        var rows = await db.ExecuteSqlAsync(Sql.Fmt($"UPDATE {Book} SET {Available} = {false} WHERE {Genre} = {genre}"));
        Assert.That(rows, Is.EqualTo(2));
    }

    [Test]
    public void Reference_tables_in_raw_SQL()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Types, ModelDefinitions and TableRefs are embedded as the dialect's quoted table name, incl. its schema,
        // [Alias] and naming strategy, instead of being sent as params
        Assert.That(db.SqlScalar<int>(Sql.Fmt($"SELECT COUNT(*) FROM {typeof(Book)}")), Is.EqualTo(8));

        // Queries with joins use prefixTable: true to qualify columns with their table
        var (Book, BookReview) = db.TableRefs<Book, BookReview>();
        var (Id, Title) = db.ColumnRefs<Book>(x => new { x.Id, x.Title }, prefixTable: true);
        var (BookId, Rating) = db.ColumnRefs<BookReview>(x => new { x.BookId, x.Rating }, prefixTable: true);
        var reviewed = db.SqlColumn<string>(Sql.Fmt(
            $"SELECT DISTINCT {Title} FROM {Book} JOIN {BookReview} ON {Id} = {BookId} WHERE {Rating} >= {4}"));
        Assert.That(reviewed, Is.EquivalentTo(new[] { "The Hobbit", "Dune" }));

        // e.g. in generic code
        Assert.That(CountRows<Book>(db), Is.EqualTo(8));
        Assert.That(CountRows<BookReview>(db), Is.EqualTo(5));

        // TableRefs can also reference a table by name, which is quoted using the naming strategy
        var byName = new TableRef(nameof(Book));
        Assert.That(db.SqlScalar<int>(Sql.Fmt($"SELECT COUNT(*) FROM {byName}")), Is.EqualTo(8));
        Assert.Throws<ArgumentException>(() => Sql.Fmt($"SELECT * FROM {new TableRef()}").ToSql(DialectProvider, out _));
    }

    [Test]
    public void Reference_multiple_tables_with_TableRefs()
    {
        using var db = OpenDbConnection();

        // TableRefs of up to 6 tables deconstruct into variables named after each table
        var (Book, BookReview, Subject) = db.TableRefs<Book, BookReview, Subject>();
        var sql = Sql.Fmt($"SELECT * FROM {Book}, {BookReview}, {Subject}").ToSql(DialectProvider, out var dbParams);
        Assert.That(sql, Is.EqualTo($"SELECT * FROM {DialectProvider.GetQuotedTableName(typeof(Book))}, " +
            $"{DialectProvider.GetQuotedTableName(typeof(BookReview))}, {DialectProvider.GetQuotedTableName(typeof(Subject))}"));
        Assert.That(dbParams, Is.Empty);

        var (t1, t2, t3, t4, t5, t6) = db.TableRefs<Book, BookReview, Subject, SalesOrder, WikiPage, StickyNote>();
        Assert.That(new[] { t1, t2, t3, t4, t5, t6 }.Map(x => x.ModelDef.ModelType), Is.EqualTo(new[] {
            typeof(Book), typeof(BookReview), typeof(Subject), typeof(SalesOrder), typeof(WikiPage), typeof(StickyNote) }));
    }

    static int CountRows<T>(System.Data.IDbConnection db) =>
        db.SqlScalar<int>(Sql.Fmt($"SELECT COUNT(*) FROM {ModelDefinition<T>.Definition}"));

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

public class BookTitle
{
    public int Id { get; set; }
    public string Title { get; set; }
}
