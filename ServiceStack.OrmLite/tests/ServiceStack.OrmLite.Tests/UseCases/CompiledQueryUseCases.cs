using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// OrmLiteQuery.Compile() generates the SQL of a typed query once, then only creates db params from its arguments
/// each time it's run. It's for queries that are run often, where generating the SQL each time is a cost.
/// </summary>
[TestFixtureOrmLite]
public class CompiledQueryUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    public class BookTitle
    {
        public string Title { get; set; }
        public int Year { get; set; }
    }

    public class BookFilter
    {
        public string Author { get; set; }
        public int MinYear { get; set; }
    }

    // Declared once, typically in a static field
    static readonly CompiledQuery<Book, string> BooksByAuthor = OrmLiteQuery.Compile<Book, string>(
        (q, author) => q.Where(x => x.Author == author).OrderBy(x => x.Title));

    static List<string> Titles(IEnumerable<Book> books) => books.Select(x => x.Title).ToList();

    // The SQL and db params of the compiled query are the same as the query it was compiled from
    static void AssertSameSql<T>(BoundQuery<T> compiled, SqlExpression<T> query)
    {
        Assert.That(compiled.SelectInto<T>(), Is.EqualTo(query.SelectInto<T>()));
        Assert.That(compiled.Params.Map(x => x.ParameterName), Is.EqualTo(query.Params.Map(x => x.ParameterName)));
        Assert.That(compiled.Params.Map(x => x.Value), Is.EqualTo(query.Params.Map(x => x.Value)));
        Assert.That(compiled.Params.Map(x => x.DbType), Is.EqualTo(query.Params.Map(x => x.DbType)));
    }

    [Test]
    public void Run_a_compiled_query_with_its_argument()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var tolkien = db.Select(BooksByAuthor, "J.R.R. Tolkien");
        Assert.That(Titles(tolkien), Is.EqualTo(new[] { "The Hobbit", "The Silmarillion" }));

        var sagan = db.Select(BooksByAuthor, "Carl Sagan");
        Assert.That(Titles(sagan), Is.EqualTo(new[] { "Cosmos" }));

        Assert.That(db.Select(BooksByAuthor, "Nobody"), Is.Empty);

        // It's the same query each time
        foreach (var author in new[] { "Frank Herbert", "O'Brien", "Mary Beard" })
        {
            AssertSameSql(BooksByAuthor.Bind(db, author),
                db.From<Book>().Where(x => x.Author == author).OrderBy(x => x.Title));
        }
        Assert.That(BooksByAuthor.NotCachedReason, Is.Null);
    }

    [Test]
    public void Generates_its_SQL_once()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var query = OrmLiteQuery.Compile<Book, int>((q, year) => q.Where(x => x.Year >= year));
        Assert.That(query.CachedStatements, Is.EqualTo(0));

        for (var year = 1930; year < 2020; year += 10)
        {
            var expected = Bookstore.Books.Count(x => x.Year >= year);
            Assert.That(db.Select(query, year).Count, Is.EqualTo(expected));
        }

        Assert.That(query.CachedStatements, Is.EqualTo(1));
        Assert.That(query.NotCachedReason, Is.Null);
    }

    [Test]
    public void Compile_a_query_with_several_arguments()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var query = OrmLiteQuery.Compile<Book, Genre, int, bool>((q, genre, since, available) => q
            .Where(x => x.Genre == genre && x.Year >= since && x.Available == available)
            .OrderByDescending(x => x.Year));

        Assert.That(Titles(db.Select(query, Genre.Science, 1900, true)), Is.EqualTo(new[] {
            "A Brief History of Time", "Cosmos" }));
        Assert.That(Titles(db.Select(query, Genre.Fantasy, 1900, false)), Is.EqualTo(new[] { "The Silmarillion" }));
        Assert.That(Titles(db.Select(query, Genre.Fiction, 1980, true)), Is.EqualTo(new[] { "Neuromancer" }));

        AssertSameSql(query.Bind(db, Genre.History, 1960, false), db.From<Book>()
            .Where(x => x.Genre == Genre.History && x.Year >= 1960 && x.Available == false)
            .OrderByDescending(x => x.Year));

        Assert.That(query.CachedStatements, Is.EqualTo(1));
        Assert.That(query.NotCachedReason, Is.Null);
    }

    [Test]
    public void Compile_a_query_with_no_arguments()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var query = OrmLiteQuery.Compile<Book>(q => q.Where(x => x.Available && x.Price < 10).OrderBy(x => x.Price));

        Assert.That(Titles(db.Select(query)), Is.EqualTo(new[] { "Neuromancer", "Dune" }));
        Assert.That(Titles(db.Select(query)), Is.EqualTo(new[] { "Neuromancer", "Dune" }));
        Assert.That(query.CachedStatements, Is.EqualTo(1));
    }

    [Test]
    public void Count_and_check_for_rows_and_read_the_first()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        Assert.That(db.Count(BooksByAuthor, "J.R.R. Tolkien"), Is.EqualTo(2));
        Assert.That(db.Count(BooksByAuthor, "Nobody"), Is.EqualTo(0));

        Assert.That(db.Exists(BooksByAuthor, "Carl Sagan"));
        Assert.That(db.Exists(BooksByAuthor, "Nobody"), Is.False);

        Assert.That(db.Single(BooksByAuthor, "J.R.R. Tolkien").Title, Is.EqualTo("The Hobbit"));
        Assert.That(db.Single(BooksByAuthor, "Nobody"), Is.Null);
    }

    [Test]
    public async Task Run_a_compiled_query_async()
    {
        using var db = await OpenDbConnectionAsync();
        Bookstore.Seed(db);

        var books = await db.SelectAsync(BooksByAuthor, "J.R.R. Tolkien");
        Assert.That(Titles(books), Is.EqualTo(new[] { "The Hobbit", "The Silmarillion" }));

        Assert.That(await db.CountAsync(BooksByAuthor, "J.R.R. Tolkien"), Is.EqualTo(2));
        Assert.That(await db.ExistsAsync(BooksByAuthor, "Carl Sagan"));
        Assert.That(await db.ExistsAsync(BooksByAuthor, "Nobody"), Is.False);
        Assert.That((await db.SingleAsync(BooksByAuthor, "Carl Sagan")).Title, Is.EqualTo("Cosmos"));
    }

    [Test]
    public void Use_a_compiled_query_in_other_APIs()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var query = OrmLiteQuery.Compile<Book, int>((q, since) => q
            .Where(x => x.Year >= since)
            .OrderBy(x => x.Year)
            .Select(x => new { x.Title, x.Year }));

        // Bind() is the query with its arguments, for the APIs that take a query
        var titles = db.Select<BookTitle>(query.Bind(db, 1980));
        Assert.That(titles.Map(x => x.Title), Is.EqualTo(new[] {
            "Cosmos", "Neuromancer", "A Brief History of Time", "SPQR" }));
        Assert.That(titles[0].Year, Is.EqualTo(1980));

        Assert.That(db.Column<string>(query.Bind(db, 1988)), Is.EqualTo(new[] { "A Brief History of Time", "SPQR" }));
        Assert.That(db.Scalar<string>(query.Bind(db, 2000)), Is.EqualTo("SPQR"));

        var years = db.Dictionary<string, int>(query.Bind(db, 1988));
        Assert.That(years["SPQR"], Is.EqualTo(2015));

        // ToQuery() is the typed query, to change it or use it in the APIs that need one
        var typed = query.Bind(db, 1980).ToQuery().Take(2);
        Assert.That(db.Select<BookTitle>(typed).Map(x => x.Title), Is.EqualTo(new[] { "Cosmos", "Neuromancer" }));
        Assert.That(Titles(db.LoadSelect(BooksByAuthor.Bind(db, "Carl Sagan").ToQuery())), Is.EqualTo(new[] { "Cosmos" }));

        Assert.That(query.NotCachedReason, Is.Null);
    }

    [Test]
    public void Compile_a_query_that_joins_tables()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var query = OrmLiteQuery.Compile<Book, int, string>((q, rating, reviewer) => q
            .Join<BookReview>((b, r) => b.Id == r.BookId)
            .Where<BookReview>(r => r.Rating >= rating && r.Reviewer == reviewer)
            .OrderBy(b => b.Title));

        Assert.That(Titles(db.Select(query, 5, "Alice")), Is.EqualTo(new[] { "Dune", "The Hobbit" }));
        Assert.That(Titles(db.Select(query, 1, "Bob")), Is.EqualTo(new[] { "SPQR", "The Hobbit" }));
        Assert.That(db.Count(query, 3, "Carol"), Is.EqualTo(1));

        Assert.That(query.CachedStatements, Is.EqualTo(2)); // the SELECT and the COUNT
        Assert.That(query.NotCachedReason, Is.Null);
    }

    [Test]
    public void Search_text_with_an_argument()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);
        db.Insert(new Book { Title = "100% Proof", Author = "A_B", Year = 2000 });
        db.Insert(new Book { Title = "1000 Proofs", Author = "AxB", Year = 2001 });

        var startsWith = OrmLiteQuery.Compile<Book, string>((q, text) => q
            .Where(x => x.Title.StartsWith(text)).OrderBy(x => x.Title));
        var contains = OrmLiteQuery.Compile<Book, string>((q, text) => q
            .Where(x => x.Title.Contains(text)).OrderBy(x => x.Title));
        var endsWith = OrmLiteQuery.Compile<Book, string>((q, text) => q
            .Where(x => x.Author.EndsWith(text)).OrderBy(x => x.Title));

        Assert.That(Titles(db.Select(startsWith, "The ")), Is.EqualTo(new[] {
            "The Guns of August", "The Hobbit", "The Silmarillion" }));
        Assert.That(Titles(db.Select(startsWith, "Dun")), Is.EqualTo(new[] { "Dune" }));
        Assert.That(Titles(db.Select(contains, "of")), Is.EquivalentTo(new[] {
            "100% Proof", "1000 Proofs", "A Brief History of Time", "The Guns of August" }));
        Assert.That(Titles(db.Select(endsWith, "Sagan")), Is.EqualTo(new[] { "Cosmos" }));

        // Wildcards in the argument are text to search for
        Assert.That(Titles(db.Select(startsWith, "100%")), Is.EqualTo(new[] { "100% Proof" }));
        Assert.That(Titles(db.Select(contains, "0% P")), Is.EqualTo(new[] { "100% Proof" }));
        Assert.That(Titles(db.Select(endsWith, "_B")), Is.EqualTo(new[] { "100% Proof" }));

        // Always escaped, as the SQL can't change with the text
        Assert.That(startsWith.Bind(db, "Dun").SelectInto<Book>().ToLower(), Does.Contain("escape '^'"));

        Assert.That(startsWith.CachedStatements, Is.EqualTo(1));
        Assert.That(contains.CachedStatements, Is.EqualTo(1));
        Assert.That(endsWith.CachedStatements, Is.EqualTo(1));
        Assert.That(startsWith.NotCachedReason, Is.Null);
    }

    [Test]
    public void Use_values_from_an_argument()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // The properties of an argument, and expressions that use it
        var query = OrmLiteQuery.Compile<Book, BookFilter, int>((q, filter, years) => q
            .Where(x => x.Author == filter.Author && x.Year >= filter.MinYear && x.Year < filter.MinYear + years)
            .OrderBy(x => x.Year));

        var tolkien = new BookFilter { Author = "J.R.R. Tolkien", MinYear = 1930 };
        Assert.That(Titles(db.Select(query, tolkien, 100)), Is.EqualTo(new[] { "The Hobbit", "The Silmarillion" }));
        Assert.That(Titles(db.Select(query, tolkien, 10)), Is.EqualTo(new[] { "The Hobbit" }));

        tolkien.MinYear = 1970; // read each time it's run
        Assert.That(Titles(db.Select(query, tolkien, 10)), Is.EqualTo(new[] { "The Silmarillion" }));
        Assert.That(Titles(db.Select(query, new BookFilter { Author = "Carl Sagan" }, 3000)), Is.EqualTo(new[] { "Cosmos" }));

        Assert.That(query.CachedStatements, Is.EqualTo(1));
        Assert.That(query.NotCachedReason, Is.Null);
    }

    [Test]
    public void Null_arguments_have_their_own_SQL()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);
        db.Insert(new Book { Title = "Beowulf", Author = null, Year = 1000 });

        // A null is compared with IS NULL, which is different SQL to a value
        Assert.That(Titles(db.Select(BooksByAuthor, null)), Is.EqualTo(new[] { "Beowulf" }));
        Assert.That(Titles(db.Select(BooksByAuthor, "Carl Sagan")), Is.EqualTo(new[] { "Cosmos" }));
        Assert.That(Titles(db.Select(BooksByAuthor, null)), Is.EqualTo(new[] { "Beowulf" }));

        Assert.That(BooksByAuthor.Bind(db, null).ToSelectStatement().ToLower(), Does.Contain("is null"));
        Assert.That(BooksByAuthor.Bind(db, "Carl Sagan").ToSelectStatement().ToLower(), Does.Not.Contain("is null"));

        var byYear = OrmLiteQuery.Compile<Book, int?, string>((q, year, author) => q
            .Where(x => x.Year == year || x.Author == author).OrderBy(x => x.Title));

        Assert.That(Titles(db.Select(byYear, 1965, null)), Is.EqualTo(new[] { "Beowulf", "Dune" }));
        Assert.That(Titles(db.Select(byYear, null, "Carl Sagan")), Is.EqualTo(new[] { "Cosmos" }));
        Assert.That(Titles(db.Select(byYear, 1984, "Carl Sagan")), Is.EqualTo(new[] { "Cosmos", "Neuromancer" }));
        Assert.That(Titles(db.Select(byYear, null, null)), Is.EqualTo(new[] { "Beowulf" }));
        Assert.That(Titles(db.Select(byYear, 1937, "Mary Beard")), Is.EqualTo(new[] { "SPQR", "The Hobbit" }));

        Assert.That(byYear.CachedStatements, Is.EqualTo(4)); // one for each combination of nulls
        Assert.That(byYear.NotCachedReason, Is.Null);
    }

    [Test]
    public void Null_values_of_an_argument_are_compared_with_IS_NULL()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);
        db.Insert(new Book { Title = "Beowulf", Author = null, Year = 1000 });

        var query = OrmLiteQuery.Compile<Book, BookFilter>((q, filter) => q
            .Where(x => x.Author == filter.Author && x.Year >= filter.MinYear).OrderBy(x => x.Title));

        // Only arguments have SQL for when they're null, the SQL for a null value is generated each time
        Assert.That(Titles(db.Select(query, new BookFilter())), Is.EqualTo(new[] { "Beowulf" }));
        Assert.That(Titles(db.Select(query, new BookFilter { Author = "Carl Sagan" })), Is.EqualTo(new[] { "Cosmos" }));
        Assert.That(Titles(db.Select(query, new BookFilter())), Is.EqualTo(new[] { "Beowulf" }));
        Assert.That(Titles(db.Select(query, new BookFilter { Author = "Frank Herbert" })), Is.EqualTo(new[] { "Dune" }));

        Assert.That(query.CachedStatements, Is.EqualTo(1));
    }

    [Test]
    public void Collection_arguments_have_SQL_for_each_size()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var inYears = OrmLiteQuery.Compile<Book, int[]>((q, years) => q
            .Where(x => Sql.In(x.Year, years)).OrderBy(x => x.Year));
        var contains = OrmLiteQuery.Compile<Book, List<string>>((q, authors) => q
            .Where(x => authors.Contains(x.Author)).OrderBy(x => x.Year));

        Assert.That(Titles(db.Select(inYears, [1937, 1965])), Is.EqualTo(new[] { "The Hobbit", "Dune" }));
        Assert.That(Titles(db.Select(inYears, [1980, 1984])), Is.EqualTo(new[] { "Cosmos", "Neuromancer" }));
        Assert.That(Titles(db.Select(inYears, [2015])), Is.EqualTo(new[] { "SPQR" }));
        Assert.That(Titles(db.Select(inYears, [1962, 1988, 1])), Is.EqualTo(new[] {
            "The Guns of August", "A Brief History of Time" }));
        Assert.That(db.Select(inYears, []), Is.Empty);
        Assert.That(db.Select(inYears, null), Is.Empty);
        Assert.That(Titles(db.Select(inYears, [1965, 1937])), Is.EqualTo(new[] { "The Hobbit", "Dune" }));

        Assert.That(Titles(db.Select(contains, ["Carl Sagan", "Mary Beard"])), Is.EqualTo(new[] { "Cosmos", "SPQR" }));
        Assert.That(Titles(db.Select(contains, ["Frank Herbert", "Nobody"])), Is.EqualTo(new[] { "Dune" }));
        Assert.That(db.Select(contains, []), Is.Empty);

        AssertSameSql(inYears.Bind(db, [1, 2, 3]), db.From<Book>()
            .Where(x => Sql.In(x.Year, new[] { 1, 2, 3 })).OrderBy(x => x.Year));

        // IN lists of 1, 2 and 3 values. The SQL for no values and null has no db params to reuse it for
        Assert.That(inYears.CachedStatements, Is.GreaterThanOrEqualTo(3));
        Assert.That(inYears.NotCachedReason, Is.Null);
        Assert.That(contains.NotCachedReason, Is.Null);
    }

    [Test]
    public void Arguments_used_outside_lambdas_have_SQL_for_each_value()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // The page size is in the SQL, the year is a db param
        var query = OrmLiteQuery.Compile<Book, int, int>((q, since, take) => q
            .Where(x => x.Year >= since).OrderBy(x => x.Year).Take(take));

        Assert.That(Titles(db.Select(query, 1960, 2)), Is.EqualTo(new[] { "The Guns of August", "Dune" }));
        Assert.That(Titles(db.Select(query, 1980, 2)), Is.EqualTo(new[] { "Cosmos", "Neuromancer" }));
        Assert.That(Titles(db.Select(query, 1980, 3)), Is.EqualTo(new[] {
            "Cosmos", "Neuromancer", "A Brief History of Time" }));
        Assert.That(Titles(db.Select(query, 2000, 2)), Is.EqualTo(new[] { "SPQR" }));
        Assert.That(Titles(db.Select(query, 1930, 1)), Is.EqualTo(new[] { "The Hobbit" }));

        Assert.That(query.CachedStatements, Is.EqualTo(3)); // for 2, 3 and 1 rows
        Assert.That(query.NotCachedReason, Is.Null);
    }

    [Test]
    public void Stops_keeping_SQL_after_MaxCachedStatements()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var hold = OrmLiteQuery.MaxCachedStatements;
        OrmLiteQuery.MaxCachedStatements = 3;
        try
        {
            var query = OrmLiteQuery.Compile<Book, int>((q, skip) => q.OrderBy(x => x.Year).Skip(skip).Take(1));

            var titles = Enumerable.Range(0, 8).Select(skip => db.Select(query, skip)[0].Title).ToList();
            Assert.That(titles, Is.EqualTo(Bookstore.Books.OrderBy(x => x.Year).Select(x => x.Title)));
            Assert.That(db.Select(query, 2)[0].Title, Is.EqualTo("Dune"));

            Assert.That(query.CachedStatements, Is.EqualTo(3));
            Assert.That(query.NotCachedReason, Does.Contain("more than 3 SQL statements"));
        }
        finally
        {
            OrmLiteQuery.MaxCachedStatements = hold;
        }
    }

    static int minYear;

    [Test]
    public void Values_that_are_not_arguments_are_read_once()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        minYear = 1980;
        var query = OrmLiteQuery.Compile<Book, bool>((q, available) => q
            .Where(x => x.Year >= minYear && x.Available == available).OrderBy(x => x.Year));

        Assert.That(Titles(db.Select(query, true)), Is.EqualTo(new[] {
            "Cosmos", "Neuromancer", "A Brief History of Time", "SPQR" }));

        // Not seen by the query, values that change have to be arguments
        minYear = 2000;
        Assert.That(Titles(db.Select(query, true)), Is.EqualTo(new[] {
            "Cosmos", "Neuromancer", "A Brief History of Time", "SPQR" }));
    }

    static int reads;
    static int NextYear() => 1960 + reads++ * 10;

    [Test]
    public void Queries_with_values_that_change_as_they_are_read_generate_their_SQL_each_time()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        reads = 0;
        // e.g. DateTime.UtcNow, which is a different value each time the SQL is generated
        var query = OrmLiteQuery.Compile<Book, bool>((q, available) => q
            .Where(x => x.Year >= NextYear() && x.Available == available).OrderBy(x => x.Year));

        var first = db.Select(query, true);
        Assert.That(query.CachedStatements, Is.EqualTo(0));
        Assert.That(query.NotCachedReason, Does.Contain("isn't the same each time"));

        var readsBefore = reads;
        var second = db.Select(query, true);
        Assert.That(reads, Is.EqualTo(readsBefore + 1));
        Assert.That(second.Count, Is.LessThan(first.Count));
    }

    [Test]
    public void Queries_that_need_the_value_of_an_argument_generate_their_SQL_each_time()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // A condition that isn't on a column decides what the SQL is
        var query = OrmLiteQuery.Compile<Book, bool, int>((q, all, since) => q
            .Where(x => all || x.Year >= since).OrderBy(x => x.Year));

        Assert.That(db.Select(query, true, 2000).Count, Is.EqualTo(8));
        Assert.That(Titles(db.Select(query, false, 2000)), Is.EqualTo(new[] { "SPQR" }));
        Assert.That(db.Select(query, true, 1980).Count, Is.EqualTo(8));
        Assert.That(Titles(db.Select(query, false, 1988)), Is.EqualTo(new[] { "A Brief History of Time", "SPQR" }));

        Assert.That(query.CachedStatements, Is.EqualTo(0));
        Assert.That(query.NotCachedReason, Is.Not.Null);
    }

    [Test]
    public void Arguments_in_join_conditions_generate_their_SQL_each_time()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Values in a join condition are in the SQL. Filter the joined table with Where<BookReview>() to reuse it
        var query = OrmLiteQuery.Compile<Book, int>((q, rating) => q
            .Join<BookReview>((b, r) => b.Id == r.BookId && r.Rating >= rating)
            .OrderBy(b => b.Title));

        Assert.That(Titles(db.Select(query, 5)), Is.EqualTo(new[] { "Dune", "The Hobbit" }));
        Assert.That(Titles(db.Select(query, 4)), Is.EqualTo(new[] { "Dune", "The Hobbit", "The Hobbit" }));
        Assert.That(Titles(db.Select(query, 5)), Is.EqualTo(new[] { "Dune", "The Hobbit" }));

        Assert.That(query.CachedStatements, Is.EqualTo(0));
        Assert.That(query.NotCachedReason, Is.Not.Null);
    }

    [Test]
    public void Global_filters_are_applied()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var query = OrmLiteQuery.Compile<Book, int>((q, since) => q.Where(x => x.Year >= since).OrderBy(x => x.Year));
        Assert.That(db.Select(query, 1970).Count, Is.EqualTo(5));
        Assert.That(query.CachedStatements, Is.EqualTo(1));

        // Filters can have values that change, so queries generate their SQL each time when there is one
        var maxYear = 2000;
        OrmLiteConfig.SqlExpressionSelectFilter = q => {
            if (q.ModelDef.ModelType == typeof(Book))
                q.Where<Book>(x => x.Year < maxYear);
        };
        try
        {
            Assert.That(db.Select(query, 1970).Count, Is.EqualTo(4));
            maxYear = 1985;
            Assert.That(Titles(db.Select(query, 1970)), Is.EqualTo(new[] {
                "The Silmarillion", "Cosmos", "Neuromancer" }));
        }
        finally
        {
            OrmLiteConfig.SqlExpressionSelectFilter = null;
        }

        Assert.That(db.Select(query, 1970).Count, Is.EqualTo(5));
        Assert.That(query.CachedStatements, Is.EqualTo(1));
    }

    [Test]
    public void Connection_filters_are_applied()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var query = OrmLiteQuery.Compile<Book, int>((q, since) => q.Where(x => x.Year >= since).OrderBy(x => x.Year));
        Assert.That(db.Select(query, 1970).Count, Is.EqualTo(5));

        // Filters have values of their own, so queries on connections that have them generate their SQL each time
        db.UseFilters(FilterSet.Create(f => f.Filter<Book>(x => x.Available)));
        Assert.That(Titles(db.Select(query, 1970)), Is.EqualTo(new[] {
            "Cosmos", "Neuromancer", "A Brief History of Time", "SPQR" }));
        Assert.That(db.Count(query, 1930), Is.EqualTo(6));
        Assert.That(db.Exists(query, 1977));
        Assert.That(db.Single(query, 1970).Title, Is.EqualTo("Cosmos"));

        Assert.That(db.WithoutFilters().Select(query, 1970).Count, Is.EqualTo(5));
    }

    [Test]
    public void Write_rules_and_filters_of_other_tables_dont_stop_SQL_being_reused()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Write rules only change inserts and updates, and the filter is for another table
        db.UseFilters(FilterSet.Create(f => {
            f.OnInsert<Book>(x => x.Author, () => "Unknown");
            f.Filter<BookReview>(x => x.Rating >= 4);
        }));

        var query = OrmLiteQuery.Compile<Book, int>((q, since) => q.Where(x => x.Year >= since).OrderBy(x => x.Year));
        foreach (var since in new[] { 1970, 1980 })
        {
            Assert.That(Titles(db.Select(query, since)),
                Is.EqualTo(Titles(db.Select(db.From<Book>().Where(x => x.Year >= since).OrderBy(x => x.Year)))));
        }
        Assert.That(query.CachedStatements, Is.EqualTo(1));
        Assert.That(query.NotCachedReason, Is.Null);
    }

    static readonly FilterSet<string> ReviewerFilters = FilterSet.Create<string>(f =>
        f.Filter<BookReview>((r, reviewer) => r.Reviewer == reviewer));

    [Test]
    public void Filters_of_joined_tables_are_applied_each_time()
    {
        static CompiledQuery<Book, int> Compile() => OrmLiteQuery.Compile<Book, int>((q, rating) => q
            .Join<BookReview>((b, r) => b.Id == r.BookId)
            .Where<BookReview>(r => r.Rating >= rating)
            .OrderBy(b => b.Title));
        static List<string> Expected(IDbConnection db, int rating) => Titles(db.Select(db.From<Book>()
            .Join<BookReview>((b, r) => b.Id == r.BookId)
            .Where<BookReview>(r => r.Rating >= rating)
            .OrderBy(b => b.Title)));
        // A connection that filters the reviews to a reviewer's. Each is opened on its own, as SQLite's :memory:
        // connections are shared and keep their filters until they're disposed.
        IDbConnection OpenReviewer(string reviewer)
        {
            var db = OpenDbConnection();
            db.UseFilters(ReviewerFilters.For(reviewer));
            return db;
        }

        var query = Compile();
        List<string> all;
        using (var db = OpenDbConnection())
        {
            Bookstore.Seed(db);
            // Its SQL is kept on a connection that doesn't filter its tables
            all = Titles(db.Select(query, 1));
            Assert.That(all, Is.EqualTo(Expected(db, 1)));
            Assert.That(query.CachedStatements, Is.EqualTo(1));
        }

        // It isn't used on connections that filter the joined table, whose filters have values of their own
        List<string> aliceTitles, bobTitles;
        using (var alice = OpenReviewer("Alice"))
        {
            aliceTitles = Titles(alice.Select(query, 1));
            Assert.That(aliceTitles, Is.EqualTo(Expected(alice, 1)));
            Assert.That(Titles(alice.Select(query, 1)), Is.EqualTo(aliceTitles));
        }
        using (var bob = OpenReviewer("Bob"))
        {
            bobTitles = Titles(bob.Select(query, 1));
            Assert.That(bobTitles, Is.EqualTo(Expected(bob, 1)));
        }
        Assert.That(aliceTitles, Is.Not.Empty);
        Assert.That(aliceTitles, Is.Not.EqualTo(all));
        Assert.That(aliceTitles, Is.Not.EqualTo(bobTitles));

        using (var db = OpenDbConnection())
            Assert.That(Titles(db.Select(query, 1)), Is.EqualTo(all));
        Assert.That(query.CachedStatements, Is.EqualTo(1));

        // Nor is it kept when it's first run on a connection that filters the joined table
        var other = Compile();
        using (var alice = OpenReviewer("Alice"))
        {
            Assert.That(Titles(alice.Select(other, 1)), Is.EqualTo(aliceTitles));
            Assert.That(other.CachedStatements, Is.EqualTo(0));
        }
        using (var db = OpenDbConnection())
            Assert.That(Titles(db.Select(other, 1)), Is.EqualTo(all));
        Assert.That(other.CachedStatements, Is.EqualTo(1));
    }

    [Test]
    public void Arguments_are_sent_as_db_params()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var query = BooksByAuthor.Bind(db, "x' OR '1'='1");
        Assert.That(query.ToSelectStatement(), Does.Not.Contain("1'='1"));
        Assert.That(query.Params.Count, Is.EqualTo(1));
        Assert.That(db.Select(query), Is.Empty);
    }

    [Test]
    public void Can_be_run_by_many_threads()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var query = OrmLiteQuery.Compile<Book, string, int>((q, author, since) => q
            .Where(x => x.Author == author && x.Year >= since).OrderBy(x => x.Title));

        var authors = Bookstore.Books.Select(x => x.Author).Distinct().ToArray();
        Parallel.For(0, 2000, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i => {
            var author = i % 9 == 0 ? null : authors[i % authors.Length];
            var since = 1900 + i % 100;
            AssertSameSql(query.Bind(db, author, since),
                db.From<Book>().Where(x => x.Author == author && x.Year >= since).OrderBy(x => x.Title));
        });

        Assert.That(Titles(db.Select(query, "J.R.R. Tolkien", 1950)), Is.EqualTo(new[] { "The Silmarillion" }));
        Assert.That(query.CachedStatements, Is.EqualTo(2)); // for an author and a null
        Assert.That(query.NotCachedReason, Is.Null);
    }
}
