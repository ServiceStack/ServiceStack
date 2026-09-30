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
            .Where<BookReview>(r => r.Reviewer == "Alice")
            .OrderBy(x => x.Title);

        for (var i = 0; i < 2; i++)
        {
            var results = db.SelectMulti<Book, BookReview>(q);
            Assert.That(results.Map(x => x.Item1.Title), Is.EqualTo(new[] { "Dune", "The Hobbit" }));
            Assert.That(results.All(x => x.Item2.Reviewer == "Alice" && x.Item2.BookId == x.Item1.Id));
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

    [Test]
    public void Can_page_set_operations_without_an_explicit_order()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        var q = db.From<Book>().Select(x => x.Title)
            .UnionAll(db.From<BookReview>().Select(x => x.Reviewer))
            .Skip(10).Take(5);

        Assert.That(db.Column<string>(q).Count, Is.EqualTo(3)); // 13 rows, skip 10
    }

    [Test]
    public void Set_operations_use_dialect_specific_SQL()
    {
        var oracle = OracleDialect.Provider.SqlExpression<Book>().Select(x => x.Id)
            .Except(OracleDialect.Provider.SqlExpression<BookReview>().Select(x => x.BookId));
        Assert.That(oracle.ToSelectStatement(), Does.Contain("\nMINUS\n"));

        Assert.Throws<NotSupportedException>(() => FirebirdDialect.Provider.SqlExpression<Book>()
            .Intersect(FirebirdDialect.Provider.SqlExpression<BookReview>()));
    }

    [Test]
    public void Set_operations_rename_params_of_combined_queries()
    {
        var dialect = SqliteDialect.Provider;
        var q = dialect.SqlExpression<Book>().Where(x => x.Year > 1950 && x.Price < 10m).Select(x => x.Title)
            .Union(dialect.SqlExpression<Book>().Where(x => x.Year < 1940 && x.Price > 5m).Select(x => x.Title));

        var sql = q.ToSelectStatement();
        Assert.That(sql, Does.Contain("@0").And.Contain("@1").And.Contain("@2").And.Contain("@3"));
        Assert.That(q.Params.Map(x => x.Value), Is.EqualTo(new object[] { 1950, 10m, 1940, 5m }));

        // Regenerating the SQL doesn't add the params again
        Assert.That(q.ToSelectStatement(), Is.EqualTo(sql));
        Assert.That(q.Params.Count, Is.EqualTo(4));
        Assert.Throws<ArgumentException>(() => q.Union(q));

        // Param-like text in string literals isn't renamed
        q = dialect.SqlExpression<Book>().Where("Year > {0}", 1900).Select(x => x.Title)
            .Union(dialect.SqlExpression<Book>().Where("Title <> '@0' AND Year > {0}", 1900).Select(x => x.Title));
        sql = q.ToSelectStatement();
        Assert.That(sql, Does.Contain("<> '@0' AND Year > @1"));
    }

    [Test]
    public void Set_operations_rename_params_that_are_prefixes_of_other_params()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // 12+ params in each query, so renaming @1 must not affect @10, @11
        var ids = db.Column<int>(db.From<Book>().Select(x => x.Id)).ToArray();
        var decoys = Enumerable.Range(1000, 12).ToArray();
        var first = ids.Take(3).Concat(decoys).ToArray();
        var second = ids.Skip(5).Concat(decoys).ToArray();

        var q = db.From<Book>().Where(x => first.Contains(x.Id)).Select(x => x.Id)
            .Union(db.From<Book>().Where(x => second.Contains(x.Id)).Select(x => x.Id));

        Assert.That(q.Params.Count, Is.EqualTo(first.Length));
        Assert.That(db.Column<int>(q), Is.EquivalentTo(ids.Take(3).Concat(ids.Skip(5))));
        Assert.That(q.Params.Count, Is.EqualTo(first.Length + second.Length));
    }

    [Test]
    public void Sql_In_sub_queries_with_named_params()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        // Named params referenced with '@' in the sub query, which the outer query's params mustn't clash with
        var fiveStars = db.From<BookReview>().Select(r => r.BookId);
        fiveStars.Params.Add(fiveStars.CreateParam("rating", 5));
        fiveStars.Where("Rating = @rating");

        var q = db.From<Book>().Where(x => x.Year > 1900 && Sql.In(x.Id, fiveStars)).Select(x => x.Title);
        Assert.That(db.Column<string>(q), Is.EquivalentTo(new[] { "The Hobbit", "Dune" }));

        // Names that are prefixes of other names, e.g. @min and @minRating
        var highlyRated = db.From<BookReview>().Select(r => r.BookId);
        highlyRated.Params.Add(highlyRated.CreateParam("min", 2));
        highlyRated.Params.Add(highlyRated.CreateParam("minRating", 4));
        highlyRated.Where("Rating >= @min AND Rating >= @minRating");

        q = db.From<Book>().Where(x => x.Year > 1900 && Sql.In(x.Id, highlyRated)).Select(x => x.Title);
        Assert.That(db.Column<string>(q), Is.EquivalentTo(new[] { "The Hobbit", "Dune" }));

        // Same params added in the reverse order
        highlyRated = db.From<BookReview>().Select(r => r.BookId);
        highlyRated.Params.Add(highlyRated.CreateParam("minRating", 4));
        highlyRated.Params.Add(highlyRated.CreateParam("min", 2));
        highlyRated.Where("Rating >= @min AND Rating >= @minRating");

        q = db.From<Book>().Where(x => x.Year > 1900 && Sql.In(x.Id, highlyRated)).Select(x => x.Title);
        Assert.That(db.Column<string>(q), Is.EquivalentTo(new[] { "The Hobbit", "Dune" }));

        // Sub queries are unchanged
        Assert.That(db.Column<int>(fiveStars).Count, Is.EqualTo(2));
    }

    [Test]
    public void SeekAfter_resolves_table_prefixed_ORDER_BY_columns_in_joins()
    {
        using var db = OpenDbConnection();
        Bookstore.Seed(db);

        SqlExpression<Book> Query() => db.From<Book>()
            .Join<BookReview>((b, r) => b.Id == r.BookId)
            .OrderByDescending(x => x.Year).ThenBy(x => x.Id);
        var all = db.Select(Query());
        Assert.That(Query().OrderByExpression, Does.Contain(DialectProvider.GetQuotedTableName(typeof(Book))));

        var rest = db.Select(Query().SeekAfter(all[0]));
        Assert.That(rest.Map(x => x.Id), Is.EqualTo(all.Skip(1).Map(x => x.Id)));
    }

    [Test]
    public void SeekAfter_works_with_OrderBySafe()
    {
        using var db = OpenDbConnection();
        Bookstore.SeedMany(db, 200);

        // e.g. ?orderBy=-Price,Id&after={cursor}
        SqlExpression<Book> Query() => db.From<Book>().OrderBySafe("-Price,Id", nameof(Book.Price), nameof(Book.Id));
        var all = db.Select(Query());

        var page = db.Select(Query().SeekAfter(all[49]).Take(50));
        Assert.That(page.Map(x => x.Id), Is.EqualTo(all.Skip(50).Take(50).Map(x => x.Id)));
    }

    [Test]
    public void MaxInListParams_of_0_always_uses_standard_IN_lists()
    {
        var ids = Enumerable.Range(1, 2000).ToArray();
        foreach (var dialect in new[] { PostgreSqlDialect.Provider, SqlServer2016Dialect.Provider, SqliteDialect.Provider })
        {
            var hold = dialect.MaxInListParams;
            dialect.MaxInListParams = 0;
            try
            {
                var q = dialect.SqlExpression<Book>().Where(x => ids.Contains(x.Id));
                Assert.That(q.WhereExpression, Does.Not.Contain("ANY(").And.Not.Contain("OPENJSON").And.Not.Contain(" OR "));
                Assert.That(q.Params.Count, Is.EqualTo(ids.Length));
            }
            finally
            {
                dialect.MaxInListParams = hold;
            }
        }
    }

    [Test]
    public void Recursive_CTEs_use_dialect_specific_SQL()
    {
        string Sql(IOrmLiteDialectProvider dialect) => dialect.SqlExpression<Subject>()
            .WithRecursive(dialect.SqlExpression<Subject>().Where(x => x.Id == 1), (p, c) => c.ParentId == p.Id)
            .ToSelectStatement();

        Assert.That(Sql(PostgreSqlDialect.Provider), Does.StartWith("WITH RECURSIVE "));
        Assert.That(Sql(SqliteDialect.Provider), Does.StartWith("WITH RECURSIVE "));
        Assert.That(Sql(SqlServer2016Dialect.Provider), Does.StartWith("WITH \"cte\" (").And.Not.Contain("RECURSIVE"));
        Assert.That(Sql(OracleDialect.Provider), Does.StartWith("WITH ").And.Not.Contain("RECURSIVE").And.Contain("\nUNION ALL\n"));
    }

    [Test]
    public void Raw_SQL_WITH_statements_are_executed_as_is()
    {
        using var db = OpenDbConnection();
        Subjects.Seed(db);

        var table = db.GetQuotedTableName<Subject>();
        var parentId = DialectProvider.GetQuotedColumnName(nameof(Subject.ParentId));
        var sql = $"WITH roots AS (SELECT * FROM {table} WHERE {parentId} IS NULL) SELECT * FROM roots";

        Assert.That(db.Select<Subject>(sql).Count, Is.EqualTo(2));
        Assert.That(db.SelectLazy<Subject>(sql).Count(), Is.EqualTo(2));
    }
}
