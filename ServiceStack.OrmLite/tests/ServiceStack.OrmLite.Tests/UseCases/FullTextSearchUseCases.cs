using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCases;

/// <summary>
/// [FullTextIndex] creates a full-text index of a table's text columns with each RDBMS's full-text search, which
/// Sql.Matches() searches for words and "quoted phrases" and Sql.MatchRank() orders by relevance, in typed queries
/// that are combined with any other condition, join or connection filter.
/// </summary>
[TestFixtureOrmLite]
public class FullTextSearchUseCases(DialectContext context) : OrmLiteProvidersTestBase(context)
{
    [FullTextIndex(nameof(Title), nameof(Body))]
    public class FtsArticle
    {
        [AutoIncrement]
        public long Id { get; set; }
        public string Title { get; set; }
        [StringLength(StringLengthAttribute.MaxText)]
        public string Body { get; set; }
        public string Category { get; set; }
        public int AuthorId { get; set; }
    }

    public class FtsAuthor
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // Indexed in English, which PostgreSQL and SQL Server use to match other forms of a word
    [FullTextIndex(nameof(Text), Language = "english")]
    public class FtsPost
    {
        [AutoIncrement]
        public long Id { get; set; }
        public string Text { get; set; }
    }

    // A table that's searched without a full-text index
    public class FtsNote
    {
        [AutoIncrement]
        public long Id { get; set; }
        public string Text { get; set; }
    }

    static readonly FtsArticle[] Articles = [
        new() { Title = "Vector search in SQL", Body = "Find similar rows with embeddings", Category = "ai", AuthorId = 1 },
        new() { Title = "Full-text search", Body = "Search words in documents and rank them by relevance. Search search search.", Category = "db", AuthorId = 2 },
        new() { Title = "Database migrations", Body = "Keep the database schema in sync with your models", Category = "db", AuthorId = 2 },
        new() { Title = "Getting started", Body = "A quick introduction", Category = "intro", AuthorId = 1 },
    ];

    IDbConnection OpenSeededDb()
    {
        var db = OpenDbConnection();
        db.DropAndCreateTable<FtsAuthor>();
        db.DropAndCreateTable<FtsArticle>();
        db.InsertAll(new[] { new FtsAuthor { Name = "Ada" }, new FtsAuthor { Name = "Grace" } });
        db.InsertAll(Articles.Map(x => x.CreatedBy()));
        // SQL Server indexes rows in the background
        db.WaitForFullTextIndex<FtsArticle>();
        return db;
    }

    static List<string> Titles(IEnumerable<FtsArticle> articles) => articles.Map(x => x.Title);

    [Test]
    public void The_test_databases_support_full_text_search()
    {
        using var db = OpenDbConnection();
        Assert.That(db.SupportsFullTextSearch(), Is.True);
    }

    [Test]
    public void Rows_with_every_word_match()
    {
        using var db = OpenSeededDb();

        var search = "search rows";
        var rows = db.Select(db.From<FtsArticle>().Where(x => Sql.Matches(x, search)));
        Assert.That(Titles(rows), Is.EqualTo(new[] { "Vector search in SQL" }));

        // Words are prefixes of the words they match, in any column of the index, ignoring case
        rows = db.Select(db.From<FtsArticle>().Where(x => Sql.Matches(x, "DATA")).OrderBy(x => x.Id));
        Assert.That(Titles(rows), Is.EqualTo(new[] { "Database migrations" }));

        rows = db.Select(db.From<FtsArticle>().Where(x => Sql.Matches(x, "embed")));
        Assert.That(Titles(rows), Is.EqualTo(new[] { "Vector search in SQL" }));

        Assert.That(db.Select(db.From<FtsArticle>().Where(x => Sql.Matches(x, "nothing"))), Is.Empty);
    }

    [Test]
    public void Short_and_common_words_do_not_stop_a_search_matching()
    {
        using var db = OpenSeededDb();

        // MySQL doesn't index words shorter than 3 characters or its stop words, which are optional in its searches
        var rows = db.Select(db.From<FtsArticle>().Where(x => Sql.Matches(x, "search in SQL")));
        Assert.That(Titles(rows), Is.EqualTo(new[] { "Vector search in SQL" }));

        rows = db.Select(db.From<FtsArticle>().Where(x => Sql.Matches(x, "the database")));
        Assert.That(Titles(rows), Is.EqualTo(new[] { "Database migrations" }));
    }

    [Test]
    public void Quoted_phrases_match_words_that_follow_each_other()
    {
        using var db = OpenSeededDb();

        var rows = db.Select(db.From<FtsArticle>().Where(x => Sql.Matches(x, "\"database schema\"")));
        Assert.That(Titles(rows), Is.EqualTo(new[] { "Database migrations" }));

        Assert.That(db.Select(db.From<FtsArticle>().Where(x => Sql.Matches(x, "\"schema database\""))), Is.Empty);
    }

    [Test]
    public void Searches_are_combined_with_other_conditions_and_joins()
    {
        using var db = OpenSeededDb();

        var rows = db.Select(db.From<FtsArticle>().Where(x => Sql.Matches(x, "search") && x.Category == "db"));
        Assert.That(Titles(rows), Is.EqualTo(new[] { "Full-text search" }));

        // The columns of the index are qualified with their table, so joined tables can have the same columns
        var q = db.From<FtsArticle>()
            .Join<FtsAuthor>((a, b) => a.AuthorId == b.Id)
            .Where<FtsArticle, FtsAuthor>((a, b) => Sql.Matches(a, "search") && b.Name == "Ada");
        Assert.That(Titles(db.Select(q)), Is.EqualTo(new[] { "Vector search in SQL" }));
    }

    [Test]
    public void Rows_are_ordered_by_relevance()
    {
        using var db = OpenSeededDb();

        var search = "search";
        var q = db.From<FtsArticle>()
            .Where(x => Sql.Matches(x, search))
            .OrderByDescending(x => Sql.MatchRank(x, search));
        Assert.That(Titles(db.Select(q)), Is.EqualTo(new[] { "Full-text search", "Vector search in SQL" }));

        // The rank can be selected, where higher is more relevant
        var ranks = db.Select<(string Title, double Rank)>(db.From<FtsArticle>()
            .Where(x => Sql.Matches(x, search))
            .Select(x => new { x.Title, Rank = Sql.MatchRank(x, search) }));
        var byTitle = ranks.ToDictionary(x => x.Title, x => x.Rank);
        Assert.That(byTitle["Full-text search"], Is.GreaterThan(byTitle["Vector search in SQL"]));
    }

    [Test]
    public void The_index_is_updated_when_rows_are_written()
    {
        using var db = OpenSeededDb();

        db.UpdateOnly(() => new FtsArticle { Body = "Learn how to search" }, x => x.Title == "Getting started");
        db.Delete<FtsArticle>(x => x.Title == "Full-text search");
        db.Insert(new FtsArticle { Title = "Searching logs", Body = "Find errors", Category = "ops" });
        db.WaitForFullTextIndex<FtsArticle>();

        var rows = db.Select(db.From<FtsArticle>().Where(x => Sql.Matches(x, "search")).OrderBy(x => x.Id));
        Assert.That(Titles(rows), Is.EqualTo(new[] { "Vector search in SQL", "Getting started", "Searching logs" }));
    }

    [Test]
    public void Searches_are_never_read_as_full_text_query_syntax()
    {
        using var db = OpenSeededDb();

        // Everything other than letters and digits only separates words
        foreach (var search in new[] { "search*", "(search)", "search:*", "+search", "search -rows", "search' OR '1'='1",
                     "search\"; DROP TABLE FtsArticle; --", "search & rows | (a)", "NEAR(search rows)" })
        {
            Assert.DoesNotThrow(() => db.Select(db.From<FtsArticle>().Where(x => Sql.Matches(x, search))), search);
        }
        Assert.That(db.Count<FtsArticle>(), Is.EqualTo(Articles.Length));

        var rows = db.Select(db.From<FtsArticle>().Where(x => Sql.Matches(x, "search -rows")));
        Assert.That(Titles(rows), Is.EqualTo(new[] { "Vector search in SQL" })); // "rows" is a word to match, not excluded

        // A search without words throws
        Assert.Throws<ArgumentException>(() => db.From<FtsArticle>().Where(x => Sql.Matches(x, " -- ")));
        Assert.Throws<ArgumentException>(() => db.From<FtsArticle>().Where(x => Sql.Matches(x, null)));
    }

    static readonly CompiledQuery<FtsArticle, string> ArticlesMatching = OrmLiteQuery.Compile<FtsArticle, string>(
        (q, search) => q.Where(x => Sql.Matches(x, search)).OrderByDescending(x => Sql.MatchRank(x, search)));

    [Test]
    public void Compiled_queries_search_for_their_argument()
    {
        using var db = OpenSeededDb();

        Assert.That(Titles(db.Select(ArticlesMatching, "search")), Is.EqualTo(new[] { "Full-text search", "Vector search in SQL" }));
        Assert.That(Titles(db.Select(ArticlesMatching, "database")), Is.EqualTo(new[] { "Database migrations" }));
        Assert.That(ArticlesMatching.NotCachedReason, Is.Null);
    }

    [Test]
    public void Add_and_drop_the_index_of_a_table_that_has_rows()
    {
        using var db = OpenSeededDb();

        db.DropFullTextIndex<FtsArticle>();
        Assert.That(db.HasFullTextIndex<FtsArticle>(), Is.False);

        // The rows the table has are indexed
        db.CreateFullTextIndex<FtsArticle>();
        db.WaitForFullTextIndex<FtsArticle>();
        Assert.That(db.HasFullTextIndex<FtsArticle>(), Is.True);
        Assert.That(db.Count(db.From<FtsArticle>().Where(x => Sql.Matches(x, "search"))), Is.EqualTo(2));

        db.DropTable<FtsArticle>();
        Assert.That(db.TableExists<FtsArticle>(), Is.False);
        db.CreateTable<FtsArticle>();
        Assert.That(db.HasFullTextIndex<FtsArticle>(), Is.True);
    }

    [Test]
    public void Schema_diffs_find_a_missing_full_text_index()
    {
        using var db = OpenSeededDb();

        // The full-text index isn't an index that's not in the model
        var diff = db.GetSchemaDiff<FtsArticle>();
        Assert.That(diff.Changes, Is.Empty, diff.ToString());

        db.DropFullTextIndex<FtsArticle>();
        diff = db.GetSchemaDiff<FtsArticle>();
        var change = diff.Changes.Single();
        Assert.That(change.Type, Is.EqualTo(SchemaChangeType.CreateFullTextIndex), diff.ToString());
        Assert.That(change.ModelColumn, Is.EqualTo("(title, body)").IgnoreCase);
        Assert.That(diff.ToString(), Does.Contain("+ full-text index "));
        Assert.That(diff.ToMigration("Migration1013"), Does.Contain("Db.CreateFullTextIndex<FtsArticle>();")
            .And.Contain("[FullTextIndex("));

        db.ApplySchemaDiff(diff);
        db.WaitForFullTextIndex<FtsArticle>();
        Assert.That(db.GetSchemaDiff<FtsArticle>().Changes, Is.Empty);
        Assert.That(db.Count(db.From<FtsArticle>().Where(x => Sql.Matches(x, "search"))), Is.EqualTo(2));
    }

    [Test]
    public void SQLite_tables_that_are_rebuilt_keep_their_full_text_index_in_sync()
    {
        if (DialectProvider.Kind != DbKind.Sqlite)
            Assert.Ignore("Only SQLite rebuilds its tables");
        using var db = OpenSeededDb();

        // The triggers that keep the index in sync are created again
        db.RebuildTable<FtsArticle>();
        db.Insert(new FtsArticle { Title = "Searching logs", Body = "Find errors" });
        db.Delete<FtsArticle>(x => x.Title == "Full-text search");

        var rows = db.Select(db.From<FtsArticle>().Where(x => Sql.Matches(x, "search")).OrderBy(x => x.Id));
        Assert.That(Titles(rows), Is.EqualTo(new[] { "Vector search in SQL", "Searching logs" }));
    }

    [Test]
    public void Words_are_indexed_in_a_language()
    {
        using var db = OpenDbConnection();
        db.DropAndCreateTable<FtsPost>();
        db.Insert(new FtsPost { Text = "Searching the documentation" });
        db.WaitForFullTextIndex<FtsPost>();

        Assert.That(db.Count(db.From<FtsPost>().Where(x => Sql.Matches(x, "searching"))), Is.EqualTo(1));

        // PostgreSQL's english configuration indexes the stem of each word, e.g. search for searching and searches
        if (DialectProvider.Kind == DbKind.PostgreSql)
            Assert.That(db.Count(db.From<FtsPost>().Where(x => Sql.Matches(x, "searches"))), Is.EqualTo(1));
    }

    [Test]
    public void Tables_without_a_full_text_index_are_not_searched()
    {
        using var db = OpenDbConnection();
        var ex = Assert.Throws<NotSupportedException>(() => db.From<FtsNote>().Where(x => Sql.Matches(x, "search")));
        Assert.That(ex.Message, Does.Contain("[FullTextIndex]"));
    }
}

static class FtsArticleExtensions
{
    // A copy, so the seed rows aren't given the ids of each test's rows
    public static FullTextSearchUseCases.FtsArticle CreatedBy(this FullTextSearchUseCases.FtsArticle x) => new() {
        Title = x.Title, Body = x.Body, Category = x.Category, AuthorId = x.AuthorId,
    };
}
