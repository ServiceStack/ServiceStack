#nullable enable
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace ServiceStack.OrmLite;

/// <summary>
/// Whether a statement writes, from its first keyword
/// </summary>
internal static class OrmLiteWriteStatement
{
    private static readonly string[] WriteKeywords = [
        "INSERT", "UPDATE", "DELETE", "MERGE", "UPSERT", "REPLACE", "CREATE", "ALTER", "DROP", "TRUNCATE", "RENAME",
        "GRANT", "REVOKE",
    ];

    private static readonly Regex IntoRegex = new(@"\bINTO\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, OrmLiteUtils.DefaultRegexTimeout);
    private static readonly Regex WriteRegex = new(@"\b(INSERT|UPDATE|DELETE|MERGE|INTO)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, OrmLiteUtils.DefaultRegexTimeout);

    /// <summary>
    /// Whether the command's statement writes. Statements that write in a CTE, e.g. WITH ... INSERT, are found too,
    /// and SELECT ... INTO, which creates a table, when exact. Stored procedures aren't known to write.
    /// </summary>
    internal static bool IsWrite(IDbCommand dbCmd, bool exact)
    {
        if (dbCmd.CommandType != CommandType.Text || dbCmd.CommandText is not { } sql)
            return false;

        var span = SkipComments(sql.AsSpan());
        if (span.Length == 0)
            return false;
        // Most statements are SELECTs, and no write keyword starts with S
        if (span[0] is 'S' or 's')
            return exact && StartsWithKeyword(span, "SELECT") && IntoRegex.IsMatch(sql);
        foreach (var keyword in WriteKeywords)
        {
            if (StartsWithKeyword(span, keyword))
                return true;
        }
        return StartsWithKeyword(span, "WITH") && WriteRegex.IsMatch(sql);
    }

    private static bool StartsWithKeyword(ReadOnlySpan<char> sql, string keyword) =>
        sql.StartsWith(keyword.AsSpan(), StringComparison.OrdinalIgnoreCase)
        && (sql.Length == keyword.Length || !char.IsLetterOrDigit(sql[keyword.Length]) && sql[keyword.Length] != '_');

    // The statement after its leading whitespace and comments, e.g. tags added with q.TagWith()
    private static ReadOnlySpan<char> SkipComments(ReadOnlySpan<char> sql)
    {
        while (true)
        {
            sql = sql.TrimStart();
            if (sql.StartsWith("--".AsSpan()))
            {
                var end = sql.IndexOf('\n');
                if (end < 0)
                    return ReadOnlySpan<char>.Empty;
                sql = sql.Slice(end + 1);
            }
            else if (sql.StartsWith("/*".AsSpan()))
            {
                var end = sql.IndexOf("*/".AsSpan());
                if (end < 0)
                    return ReadOnlySpan<char>.Empty;
                sql = sql.Slice(end + 2);
            }
            else
            {
                return sql;
            }
        }
    }
}
