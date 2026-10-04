using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Create a full-text index of a table's text columns, to search them for words with Sql.Matches() and order them by
/// relevance with Sql.MatchRank(), e.g:
/// <para>[FullTextIndex(nameof(Title), nameof(Content))]</para>
/// <para>db.From&lt;Article&gt;().Where(x => Sql.Matches(x, "vector search")).OrderByDescending(x => Sql.MatchRank(x, "vector search"))</para>
/// A table has one full-text index, which uses each RDBMS's full-text search: SQLite's FTS5, PostgreSQL's tsvector,
/// MySQL's FULLTEXT and SQL Server's Full-Text Search.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class FullTextIndexAttribute(params string[] fieldNames) : AttributeBase
{
    /// <summary>
    /// The properties whose text is searched, which have to be strings
    /// </summary>
    public string[] FieldNames { get; } = fieldNames;

    /// <summary>
    /// The name of the index, ftx_{table} by default. SQLite's full-text index is a table named {table}_fts.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The language words are indexed in, which PostgreSQL and SQL Server use to find words with the same stem, e.g.
    /// "english" in PostgreSQL. By default words are indexed as they're written, without stemming.
    /// </summary>
    public string Language { get; set; }
}
