#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

/// <summary>
/// How an RDBMS returns the query plan of a statement
/// </summary>
public class ExplainQuery
{
    /// <summary>
    /// The statement that returns the query plan, e.g. EXPLAIN SELECT ...
    /// </summary>
    public string Sql { get; set; } = "";

    /// <summary>
    /// Statement to run on the connection before, e.g. SET SHOWPLAN_TEXT ON
    /// </summary>
    public string? BeforeSql { get; set; }

    /// <summary>
    /// Statement to run on the connection after, e.g. SET SHOWPLAN_TEXT OFF
    /// </summary>
    public string? AfterSql { get; set; }

    /// <summary>
    /// Whether the statement's params need to be merged into its SQL, for RDBMS that only return query plans of
    /// statements without params
    /// </summary>
    public bool MergeParams { get; set; }

    /// <summary>
    /// Reads the query plan from the results, by default the first column of each row, e.g. the lines of a plan
    /// </summary>
    public Func<IDataReader, string> ReadPlan { get; set; } = ReadLines;

    /// <summary>
    /// The first column of each row of the last result set, one per line
    /// </summary>
    public static string ReadLines(IDataReader reader)
    {
        var sb = new StringBuilder();
        do
        {
            sb.Clear(); // results before the last are the query's own results
            while (reader.Read())
            {
                if (sb.Length > 0)
                    sb.Append('\n');
                sb.Append(reader.IsDBNull(0) ? "" : Convert.ToString(reader.GetValue(0)));
            }
        } while (reader.NextResult());
        return sb.ToString();
    }

    /// <summary>
    /// The last result set as a text table, of all its columns or only the specified columns
    /// </summary>
    public static string ReadTable(IDataReader reader, params string[] columns)
    {
        var rows = new List<string[]>();
        do
        {
            var indexes = new List<int>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (columns.Length == 0 || Array.Exists(columns, x => x.Equals(reader.GetName(i), StringComparison.OrdinalIgnoreCase)))
                    indexes.Add(i);
            }
            if (indexes.Count == 0)
            {
                while (reader.Read()) {} // skip results without the plan's columns, e.g. the query's own results
                continue;
            }

            rows.Clear();
            rows.Add(indexes.ConvertAll(reader.GetName).ToArray());
            while (reader.Read())
            {
                rows.Add(indexes.ConvertAll(i => reader.IsDBNull(i)
                    ? ""
                    : ToSingleLine(Convert.ToString(reader.GetValue(i)) ?? "")).ToArray());
            }
        } while (reader.NextResult());

        if (rows.Count == 0)
            return "";

        return FormatTable(rows);
    }

    // e.g. a multi-line SQL statement in a cell
    private static string ToSingleLine(string value)
    {
        var sb = new StringBuilder();
        foreach (var line in value.Split('\n'))
        {
            var text = line.Trim();
            if (text.Length == 0)
                continue;
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append(text);
        }
        return sb.ToString();
    }

    private static string FormatTable(List<string[]> rows)
    {
        var widths = new int[rows[0].Length];
        foreach (var row in rows)
        {
            for (var i = 0; i < row.Length; i++)
                widths[i] = Math.Max(widths[i], row[i].Length);
        }

        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            if (sb.Length > 0)
                sb.Append('\n');
            for (var i = 0; i < row.Length; i++)
            {
                if (i > 0)
                    sb.Append(" | ");
                // the last column isn't padded
                sb.Append(i < row.Length - 1 ? row[i].PadRight(widths[i]) : row[i]);
            }
        }
        return sb.ToString();
    }
}

/// <summary>
/// Returns the RDBMS's query plan of a query, e.g. to check which indexes it uses:
/// <para>string plan = db.Explain(db.From&lt;Order&gt;().Where(x =&gt; x.CustomerId == 1));</para>
/// Uses EXPLAIN in PostgreSQL and MySQL, EXPLAIN QUERY PLAN in SQLite and SHOWPLAN_TEXT in SQL Server.
/// Other RDBMS throw a NotSupportedException.
/// </summary>
public static class OrmLiteExplainApi
{
    /// <summary>
    /// The query plan of the query. The query isn't run unless analyze is true, which runs it to include actual
    /// row counts and timings in RDBMS that support it.
    /// </summary>
    public static string Explain(this IDbConnection dbConn, ISqlExpression q, bool analyze = false)
    {
        if (q == null)
            throw new ArgumentNullException(nameof(q));
        return dbConn.Exec(dbCmd => dbCmd.Explain(q.ToSelectStatement(), q.Params, analyze));
    }

    /// <summary>
    /// The query plan of the SQL statement, e.g:
    /// <para>db.Explain("SELECT * FROM Orders WHERE CustomerId = @id", new { id = 1 })</para>
    /// The statement isn't run unless analyze is true.
    /// </summary>
    public static string Explain(this IDbConnection dbConn, string sql, object? anonType = null, bool analyze = false)
    {
        if (string.IsNullOrEmpty(sql))
            throw new ArgumentNullException(nameof(sql));
        return dbConn.Exec(dbCmd => {
            if (anonType != null)
                dbCmd.SetParameters(anonType.ToObjectDictionary(), excludeDefaults: false, sql: ref sql);
            return dbCmd.Explain(sql, null, analyze);
        });
    }

    public static Task<string> ExplainAsync(this IDbConnection dbConn, ISqlExpression q, bool analyze = false,
        CancellationToken token = default)
    {
        if (q == null)
            throw new ArgumentNullException(nameof(q));
        return dbConn.Exec(dbCmd => dbCmd.ExplainAsync(q.ToSelectStatement(), q.Params, analyze, token));
    }

    public static Task<string> ExplainAsync(this IDbConnection dbConn, string sql, object? anonType = null,
        bool analyze = false, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(sql))
            throw new ArgumentNullException(nameof(sql));
        return dbConn.Exec(dbCmd => {
            if (anonType != null)
                dbCmd.SetParameters(anonType.ToObjectDictionary(), excludeDefaults: false, sql: ref sql);
            return dbCmd.ExplainAsync(sql, null, analyze, token);
        });
    }

    internal static string Explain(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter>? sqlParams, bool analyze)
    {
        var explain = dbCmd.PrepareExplain(sql, sqlParams, analyze);

        dbCmd.ExecSessionSql(explain.BeforeSql);
        try
        {
            using var reader = dbCmd.ExecReader(explain.Sql);
            return explain.ReadPlan(reader);
        }
        finally
        {
            dbCmd.ExecSessionSql(explain.AfterSql);
        }
    }

    internal static async Task<string> ExplainAsync(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter>? sqlParams,
        bool analyze, CancellationToken token)
    {
        var explain = dbCmd.PrepareExplain(sql, sqlParams, analyze);

        dbCmd.ExecSessionSql(explain.BeforeSql);
        try
        {
            using var reader = await dbCmd.ExecReaderAsync(explain.Sql, token).ConfigAwait();
            return explain.ReadPlan(reader);
        }
        finally
        {
            dbCmd.ExecSessionSql(explain.AfterSql);
        }
    }

    private static ExplainQuery PrepareExplain(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter>? sqlParams, bool analyze)
    {
        var dialect = dbCmd.GetDialectProvider();
        var explain = dialect.ToExplainQuery(dbCmd.Connection!, sql, analyze);
        dbCmd.SetParameters(sqlParams);
        if (explain.MergeParams && dbCmd.Parameters.Count > 0)
        {
            var dbParams = new List<IDbDataParameter>();
            foreach (IDbDataParameter dbParam in dbCmd.Parameters)
            {
                // Params can be named without their prefix, e.g. id instead of @id
                if (!dbParam.ParameterName.StartsWith(dialect.ParamString))
                    dbParam.ParameterName = dialect.ParamString + dbParam.ParameterName;
                dbParams.Add(dbParam);
            }
            explain.Sql = dialect.MergeParamsIntoSql(explain.Sql, dbParams);
            dbCmd.Parameters.Clear();
        }
        return explain;
    }

    // Session statements are run without the query's params, e.g. SET SHOWPLAN_TEXT ON needs to be the only
    // statement in its batch
    private static void ExecSessionSql(this IDbCommand dbCmd, string? sql)
    {
        if (string.IsNullOrEmpty(sql))
            return;
        using var cmd = dbCmd.Connection!.CreateCommand();
        cmd.Transaction = dbCmd.Transaction;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
