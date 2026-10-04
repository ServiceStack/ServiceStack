using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite;

public static partial class Sql
{
    /// <summary>
    /// Whether a row's [FullTextIndex] has every word of a search, as a prefix of a word, and every "quoted phrase", e.g:
    /// <para>db.From&lt;Article&gt;().Where(x => Sql.Matches(x, search))</para>
    /// The search is only ever sent as a db param, in the full-text query syntax of the RDBMS.
    /// </summary>
    public static bool Matches<T>(T table, string search) =>
        throw new NotSupportedException($"{nameof(Matches)}() is only used in OrmLite's typed queries");

    /// <summary>
    /// How relevant a row is to a search of its [FullTextIndex], where higher is more relevant, to order the rows that
    /// match it, e.g:
    /// <para>db.From&lt;Article&gt;().Where(x => Sql.Matches(x, search)).OrderByDescending(x => Sql.MatchRank(x, search))</para>
    /// Ranks are only comparable to the ranks of the same RDBMS.
    /// </summary>
    public static double MatchRank<T>(T table, string search) =>
        throw new NotSupportedException($"{nameof(MatchRank)}() is only used in OrmLite's typed queries");
}

/// <summary>
/// The table and columns of a [FullTextIndex] that a query searches, as they're quoted in the query
/// </summary>
public class FullTextColumns
{
    public ModelDefinition ModelDef { get; set; }

    /// <summary>
    /// The quoted name of the table, e.g. "Article"
    /// </summary>
    public string Table { get; set; }

    /// <summary>
    /// The columns that are searched, quoted with the table or alias they're qualified with in the query
    /// </summary>
    public List<string> Columns { get; set; } = [];

    /// <summary>
    /// The primary key, quoted with the table or alias it's qualified with in the query
    /// </summary>
    public string PrimaryKey { get; set; }
}

/// <summary>
/// A word or "quoted phrase" of a full-text search
/// </summary>
public class FullTextTerm
{
    /// <summary>
    /// The words of the term, which only have letters and digits
    /// </summary>
    public string[] Words { get; set; }

    /// <summary>
    /// Whether it's a quoted phrase, whose words are matched in order. Words that aren't in a phrase are matched as the
    /// prefix of a word.
    /// </summary>
    public bool IsPhrase { get; set; }
}

public static class OrmLiteFullTextSearch
{
    private static readonly Regex TermRegex = new(@"""([^""]*)""?|([\p{L}\p{N}]+)", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex WordRegex = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>
    /// The words and "quoted phrases" of a search. Everything other than letters and digits separates words, so the
    /// search can never be read as the full-text query syntax of an RDBMS. Throws an ArgumentException when it has
    /// no words.
    /// </summary>
    public static List<FullTextTerm> ParseSearch(string search)
    {
        var terms = new List<FullTextTerm>();
        foreach (Match match in TermRegex.Matches(search ?? ""))
        {
            if (match.Groups[2].Success)
            {
                terms.Add(new FullTextTerm { Words = [match.Groups[2].Value] });
                continue;
            }
            var words = WordRegex.Matches(match.Groups[1].Value).Cast<Match>().Select(x => x.Value).ToArray();
            if (words.Length > 0)
                terms.Add(new FullTextTerm { Words = words, IsPhrase = true });
        }
        if (terms.Count == 0)
            throw new ArgumentException($"The search '{search}' has no words to search for", nameof(search));
        return terms;
    }

    /// <summary>
    /// The fields of a model's [FullTextIndex], which have to be strings
    /// </summary>
    public static List<FieldDefinition> GetFullTextFields(this ModelDefinition modelDef)
    {
        var index = modelDef.FullTextIndex
            ?? throw new NotSupportedException($"{modelDef.Name} doesn't have a [FullTextIndex]");
        if (index.FieldNames == null || index.FieldNames.Length == 0)
            throw new ArgumentException($"The [FullTextIndex] of {modelDef.Name} needs the properties to index");
        return index.FieldNames.Map(name => {
            var fieldDef = modelDef.GetFieldDefinition(name)
                ?? throw new ArgumentException($"{name} of the [FullTextIndex] isn't a property of {modelDef.Name}");
            if (fieldDef.FieldType != typeof(string))
                throw new ArgumentException($"{name} of the [FullTextIndex] of {modelDef.Name} isn't a string");
            return fieldDef;
        });
    }

    /// <summary>
    /// Create the full-text index of a model's [FullTextIndex] in its table, and index the rows it has, e.g. in a
    /// migration. SQL Server can't create it in a transaction.
    /// </summary>
    public static void CreateFullTextIndex<T>(this IDbConnection db) => db.CreateFullTextIndex(typeof(T));

    /// <summary>
    /// Create the full-text index of a model's [FullTextIndex] in its table, and index the rows it has
    /// </summary>
    public static void CreateFullTextIndex(this IDbConnection db, Type modelType)
    {
        var modelDef = modelType.GetModelDefinition();
        foreach (var sql in db.GetDialectProvider().ToCreateFullTextIndexStatements(modelDef))
            db.ExecuteSql(sql);
    }

    /// <summary>
    /// Drop the full-text index of a model's [FullTextIndex] from its table
    /// </summary>
    public static void DropFullTextIndex<T>(this IDbConnection db) => db.DropFullTextIndex(typeof(T));

    /// <summary>
    /// Drop the full-text index of a model's [FullTextIndex] from its table
    /// </summary>
    public static void DropFullTextIndex(this IDbConnection db, Type modelType)
    {
        var modelDef = modelType.GetModelDefinition();
        foreach (var sql in db.GetDialectProvider().ToDropFullTextIndexStatements(modelDef))
            db.ExecuteSql(sql);
    }

    /// <summary>
    /// Whether the table of a model has the full-text index of its [FullTextIndex]
    /// </summary>
    public static bool HasFullTextIndex<T>(this IDbConnection db) =>
        db.GetDialectProvider().HasFullTextIndex(db, typeof(T).GetModelDefinition());

    /// <summary>
    /// Whether the database can create full-text indexes, e.g. SQL Server's Full-Text Search is an optional component
    /// that may not be installed. Apps can fall back to Contains() when it can't.
    /// </summary>
    public static bool SupportsFullTextSearch(this IDbConnection db) =>
        db.GetDialectProvider().SupportsFullTextSearch(db);

    /// <summary>
    /// Wait until the rows that were written are in the full-text index of a model, which SQL Server indexes in the
    /// background after they're written, e.g. in tests. The other RDBMS index them as they're written.
    /// </summary>
    public static void WaitForFullTextIndex<T>(this IDbConnection db, TimeSpan? timeout = null) =>
        db.WaitForFullTextIndex(typeof(T), timeout);

    /// <summary>
    /// Wait until the rows that were written are in the full-text index of a model, see WaitForFullTextIndex&lt;T&gt;()
    /// </summary>
    public static void WaitForFullTextIndex(this IDbConnection db, Type modelType, TimeSpan? timeout = null)
    {
        var dialect = db.GetDialectProvider();
        var modelDef = modelType.GetModelDefinition();
        var stopwatch = Stopwatch.StartNew();
        var wait = timeout ?? TimeSpan.FromSeconds(30);
        while (!dialect.IsFullTextIndexUpToDate(db, modelDef))
        {
            if (stopwatch.Elapsed > wait)
                throw new TimeoutException($"The full-text index of {modelDef.Name} wasn't up to date after {wait}");
            Thread.Sleep(50);
        }
    }
}
