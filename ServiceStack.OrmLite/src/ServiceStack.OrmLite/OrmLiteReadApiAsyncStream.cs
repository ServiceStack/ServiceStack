using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

/// <summary>
/// Async streaming APIs returning results as they're read, with bounded memory, e.g:
/// <para>await foreach (var row in db.SelectLazyAsync(db.From&lt;Person&gt;().Where(x =&gt; x.Age &gt; 40))) ...</para>
/// </summary>
public static class OrmLiteReadApiAsyncStream
{
    /// <summary>
    /// Returns an async stream of results using an SqlExpression, e.g:
    /// <para>await foreach (var row in db.SelectLazyAsync(db.From&lt;Person&gt;().Where(x =&gt; x.Age &gt; 40))) ...</para>
    /// </summary>
    public static IAsyncEnumerable<T> SelectLazyAsync<T>(this IDbConnection dbConn, SqlExpression<T> expression,
        CancellationToken token = default) =>
        dbConn.ExecLazyAsync((dbCmd, ct) => dbCmd.SetParameters(expression.Params)
            .SelectLazyAsync<T>(expression.ToSelectStatement(QueryType.Select), ct), token);

    /// <summary>
    /// Returns an async stream of results using a parameterized query, e.g:
    /// <para>await foreach (var row in db.SelectLazyAsync&lt;Person&gt;("Age &gt; @age", new { age = 40 })) ...</para>
    /// </summary>
    public static IAsyncEnumerable<T> SelectLazyAsync<T>(this IDbConnection dbConn, string sql, object anonType = null,
        CancellationToken token = default) =>
        dbConn.ExecLazyAsync((dbCmd, ct) => {
            if (anonType != null) dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql);
            return dbCmd.SelectLazyAsync<T>(sql, ct);
        }, token);

    /// <summary>
    /// Returns an async stream of results using interpolated SQL where each value is sent as a db param, e.g:
    /// <para>await foreach (var row in db.SelectLazyAsync&lt;Person&gt;(Sql.Fmt($"Age &gt; {age}"))) ...</para>
    /// </summary>
    public static IAsyncEnumerable<T> SelectLazyAsync<T>(this IDbConnection dbConn, SqlFormattable sql,
        CancellationToken token = default) =>
        dbConn.SelectLazyAsync<T>(sql.ToSql(dbConn.GetDialectProvider(), out var dbParams), dbParams, token);

    /// <summary>
    /// Returns an async stream of the first column's values using an SqlExpression, e.g:
    /// <para>await foreach (var name in db.ColumnLazyAsync&lt;string&gt;(db.From&lt;Person&gt;().Select(x =&gt; x.LastName))) ...</para>
    /// </summary>
    public static IAsyncEnumerable<T> ColumnLazyAsync<T>(this IDbConnection dbConn, ISqlExpression query,
        CancellationToken token = default) =>
        dbConn.ExecLazyAsync((dbCmd, ct) => dbCmd.SetParameters(query.Params)
            .ColumnLazyAsync<T>(query.ToSelectStatement(QueryType.Select), ct), token);

    /// <summary>
    /// Returns an async stream of the first column's values using a parameterized query, e.g:
    /// <para>await foreach (var name in db.ColumnLazyAsync&lt;string&gt;("SELECT LastName FROM Person WHERE Age = @age", new { age = 27 })) ...</para>
    /// </summary>
    public static IAsyncEnumerable<T> ColumnLazyAsync<T>(this IDbConnection dbConn, string sql, object anonType = null,
        CancellationToken token = default) =>
        dbConn.ExecLazyAsync((dbCmd, ct) => {
            if (anonType != null) dbCmd.SetParameters<T>(anonType, excludeDefaults: false, sql: ref sql);
            return dbCmd.ColumnLazyAsync<T>(sql, ct);
        }, token);

    /// <summary>
    /// Executes the async stream with a command created from the connection's exec filter, which is disposed
    /// after the stream completes or is disposed early.
    /// </summary>
    public static async IAsyncEnumerable<T> ExecLazyAsync<T>(this IDbConnection dbConn,
        Func<IDbCommand, CancellationToken, IAsyncEnumerable<T>> filter,
        [EnumeratorCancellation] CancellationToken token = default)
    {
        var execFilter = dbConn.GetExecFilter();
        var dbCmd = execFilter.CreateCommand(dbConn);
        var id = Diagnostics.OrmLite.WriteCommandBefore(dbCmd);
        try
        {
            await foreach (var item in filter(dbCmd, token).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            Diagnostics.OrmLite.WriteCommandAfter(id, dbCmd);
            execFilter.DisposeCommand(dbCmd, dbConn);
        }
    }

    internal static async IAsyncEnumerable<T> SelectLazyAsync<T>(this IDbCommand dbCmd, string sql,
        [EnumeratorCancellation] CancellationToken token)
    {
        var dialectProvider = dbCmd.GetDialectProvider();
        dbCmd.CommandText = dbCmd.ToFilteredSelectStatement(typeof(T), sql);

        var resultsFilter = OrmLiteConfig.ResultsFilter;
        if (resultsFilter != null)
        {
            foreach (var item in resultsFilter.GetList<T>(dbCmd))
                yield return item;
            yield break;
        }

        using var reader = await dbCmd.ExecReaderAsync(dbCmd.CommandText, token).ConfigAwait();
        var indexCache = reader.GetIndexFieldsCache(ModelDefinition<T>.Definition, dialectProvider);
        var values = new object[reader.FieldCount];
        while (await dialectProvider.ReadAsync(reader, token).ConfigAwait())
        {
            token.ThrowIfCancellationRequested(); // not all providers observe the token in ReadAsync
            var row = OrmLiteUtils.CreateInstance<T>();
            row.PopulateWithSqlReader(dialectProvider, reader, indexCache, values);
            yield return row;
        }
    }

    internal static async IAsyncEnumerable<T> ColumnLazyAsync<T>(this IDbCommand dbCmd, string sql,
        [EnumeratorCancellation] CancellationToken token)
    {
        var dialectProvider = dbCmd.GetDialectProvider();
        dbCmd.CommandText = dbCmd.ToFilteredSelectStatement(typeof(T), sql);

        var resultsFilter = OrmLiteConfig.ResultsFilter;
        if (resultsFilter != null)
        {
            foreach (var item in resultsFilter.GetColumn<T>(dbCmd))
                yield return item;
            yield break;
        }

        using var reader = await dbCmd.ExecReaderAsync(dbCmd.CommandText, token).ConfigAwait();
        while (await dialectProvider.ReadAsync(reader, token).ConfigAwait())
        {
            token.ThrowIfCancellationRequested();
            var value = dialectProvider.FromDbValue(reader, 0, typeof(T));
            yield return value == DBNull.Value ? default : (T)value;
        }
    }
}
