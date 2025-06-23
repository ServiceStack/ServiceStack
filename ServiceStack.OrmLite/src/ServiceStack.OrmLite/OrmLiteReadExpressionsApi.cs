using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

public static class OrmLiteReadExpressionsApi
{
    public static T Exec<T>(this IDbConnection dbConn, Func<IDbCommand, T> filter)
    {
        return dbConn.GetExecFilter().Exec(dbConn, filter);
    }

    public static void Exec(this IDbConnection dbConn, Action<IDbCommand> filter)
    {
        dbConn.GetExecFilter().Exec(dbConn, filter);
    }

    public static Task<T> Exec<T>(this IDbConnection dbConn, Func<IDbCommand, Task<T>> filter)
    {
        return dbConn.GetExecFilter().Exec(dbConn, filter);
    }

    public static Task Exec(this IDbConnection dbConn, Func<IDbCommand, Task> filter)
    {
        return dbConn.GetExecFilter().Exec(dbConn, filter);
    }

    public static IEnumerable<T> ExecLazy<T>(this IDbConnection dbConn, Func<IDbCommand, IEnumerable<T>> filter)
    {
        return dbConn.GetExecFilter().ExecLazy(dbConn, filter);
    }

    public static IDbCommand Exec(this IDbConnection dbConn, Func<IDbCommand, IDbCommand> filter)
    {
        return dbConn.GetExecFilter().Exec(dbConn, filter);
    }

    public static Task<IDbCommand> Exec(this IDbConnection dbConn, Func<IDbCommand, Task<IDbCommand>> filter)
    {
        return dbConn.GetExecFilter().Exec(dbConn, filter);
    }

    /// <summary>
    /// Creates a new SqlExpression builder allowing typed LINQ-like queries.
    /// Alias for SqlExpression.
    /// </summary>
    public static SqlExpression<T> From<T>(this IDbConnection dbConn)
    {
        return dbConn.GetExecFilter().SqlExpression<T>(dbConn);
    }

    public static SqlExpression<T> From<T>(this IDbConnection dbConn, Action<SqlExpression<T>> options)
    {
        var q = dbConn.GetExecFilter().SqlExpression<T>(dbConn);
        options(q);
        return q;
    }

    public static SqlExpression<T> From<T, JoinWith>(this IDbConnection dbConn, Expression<Func<T, JoinWith, bool>> joinExpr=null)
    {
        var sql = dbConn.GetExecFilter().SqlExpression<T>(dbConn);
        sql.Join<T,JoinWith>(joinExpr);
        return sql;
    }

    /// <summary>
    /// Creates a new SqlExpression builder for the specified type using a user-defined FROM sql expression.
    /// </summary>
    public static SqlExpression<T> From<T>(this IDbConnection dbConn, string fromExpression)
    {
        var expr = dbConn.GetExecFilter().SqlExpression<T>(dbConn);
        expr.From(fromExpression);
        return expr;
    }
                
    public static SqlExpression<T> From<T>(this IDbConnection dbConn, TableOptions tableOptions)
    {
        var expr = dbConn.GetExecFilter().SqlExpression<T>(dbConn);
        if (!string.IsNullOrEmpty(tableOptions.Expression))
            expr.From(tableOptions.Expression);
        if (!string.IsNullOrEmpty(tableOptions.Alias))
            expr.SetTableAlias(tableOptions.Alias);
        return expr;
    }

    public static SqlExpression<T> TagWith<T>(this SqlExpression<T> expression,string tag)
    {
        expression.AddTag(tag);
        return expression;
    }

    public static SqlExpression<T> TagWithCallSite<T>(this SqlExpression<T> expression,
        [CallerFilePath] string filePath = null,
        [CallerLineNumber] int lineNumber = 0)
    {
        expression.AddTag($"File: {filePath}:{lineNumber.ToString()}");
        return expression;
    }

    public static SqlExpression<T> From<T>(this IDbConnection dbConn, TableOptions tableOptions,
        Action<SqlExpression<T>> options)
    {
        var q = dbConn.From<T>(tableOptions);
        options(q);
        return q;
    }

    [Obsolete("Use TableAlias")]
    public static JoinFormatDelegate JoinAlias(this IDbConnection db, string alias) => OrmLiteUtils.JoinAlias(alias);

    public static TableOptions TableAlias(this IDbConnection db, string alias) => new TableOptions { Alias = alias };

    public static string GetTableName<T>(this IDbConnection db) => db.GetDialectProvider().GetTableName(ModelDefinition<T>.Definition);

    public static List<string> GetTableNames(this IDbConnection db) => GetTableNames(db, null);
    public static List<string> GetTableNames(this IDbConnection db, string schema) => db.Column<string>(db.GetDialectProvider().ToTableNamesStatement(schema));

    public static Task<List<string>> GetTableNamesAsync(this IDbConnection db) => GetTableNamesAsync(db, null);
    public static Task<List<string>> GetTableNamesAsync(this IDbConnection db, string schema) => db.ColumnAsync<string>(db.GetDialectProvider().ToTableNamesStatement(schema));

    public static List<KeyValuePair<string,long>> GetTableNamesWithRowCounts(this IDbConnection db, bool live=false, string schema=null)
    {
        List<KeyValuePair<string, long>> GetResults()
        {
            var sql = db.GetDialectProvider().ToTableNamesWithRowCountsStatement(live, schema);
            if (sql != null)
                return db.KeyValuePairs<string, long>(sql);

            sql = CreateTableRowCountUnionSql(db, schema);
            return db.KeyValuePairs<string, long>(sql);
        }

        var results = GetResults();
        results.Sort((x,y) => y.Value.CompareTo(x.Value)); //sort desc
        return results;
    }

    public static async Task<List<KeyValuePair<string,long>>> GetTableNamesWithRowCountsAsync(this IDbConnection db, bool live = false, string schema = null)
    {
        Task<List<KeyValuePair<string, long>>> GetResultsAsync()
        {
            var sql = db.GetDialectProvider().ToTableNamesWithRowCountsStatement(live, schema);
            if (sql != null)
                return db.KeyValuePairsAsync<string, long>(sql);

            sql = CreateTableRowCountUnionSql(db, schema);
            return db.KeyValuePairsAsync<string, long>(sql);
        }

        var results = await GetResultsAsync().ConfigAwait();
        results.Sort((x,y) => y.Value.CompareTo(x.Value)); //sort desc
        return results;
    }

    private static string CreateTableRowCountUnionSql(IDbConnection db, string schema)
    {
        var sb = StringBuilderCache.Allocate();

        var dialect = db.GetDialectProvider();

        var tableNames = GetTableNames(db, schema);
        var schemaName = dialect.NamingStrategy.GetSchemaName(schema);
        foreach (var tableName in tableNames)
        {
            if (sb.Length > 0)
                sb.Append(" UNION ");
                
            // retain *real* table names and skip using naming strategy
            sb.AppendLine($"SELECT {OrmLiteUtils.QuotedLiteral(tableName)}, COUNT(*) FROM {dialect.GetQuotedTableName(tableName, schemaName, useStrategy:false)}");
        }

        var sql = StringBuilderCache.ReturnAndFree(sb);
        return sql;
    }

    public static string GetQuotedTableName<T>(this IDbConnection db)
    {
        return db.GetDialectProvider().GetQuotedTableName(ModelDefinition<T>.Definition);
    }

    /// <summary>
    /// Open a Transaction in OrmLite
    /// </summary>
    public static IDbTransaction OpenTransaction(this IDbConnection dbConn)
    {
        return OrmLiteTransaction.Create(dbConn);
    }

    /// <summary>
    /// Returns a new transaction if not yet exists, otherwise null
    /// </summary>
    public static IDbTransaction OpenTransactionIfNotExists(this IDbConnection dbConn)
    {
        return !dbConn.InTransaction()
            ? OrmLiteTransaction.Create(dbConn)
            : null;
    }

    /// <summary>
    /// Open a Transaction in OrmLite
    /// </summary>
    public static IDbTransaction OpenTransaction(this IDbConnection dbConn, IsolationLevel isolationLevel)
    {
        return OrmLiteTransaction.Create(dbConn, isolationLevel);
    }

    /// <summary>
    /// Returns a new transaction if not yet exists, otherwise null
    /// </summary>
    public static IDbTransaction OpenTransactionIfNotExists(this IDbConnection dbConn, IsolationLevel isolationLevel)
    {
        return !dbConn.InTransaction()
            ? OrmLiteTransaction.Create(dbConn, isolationLevel)
            : null;
    }

    public static SavePoint SavePoint(this IDbTransaction trans, string name)
    {
        if (trans is not OrmLiteTransaction dbTrans)
            throw new ArgumentException($"{trans.GetType().Name} is not an OrmLiteTransaction. Use db.OpenTransaction() to Create OrmLite Transactions");
        var savePoint = new SavePoint(dbTrans, name);
        savePoint.Save();
        return savePoint;
    }

    public static async Task<SavePoint> SavePointAsync(this IDbTransaction trans, string name)
    {
        if (trans is not OrmLiteTransaction dbTrans)
            throw new ArgumentException($"{trans.GetType().Name} is not an OrmLiteTransaction. Use db.OpenTransaction() to Create OrmLite Transactions");
        var savePoint = new SavePoint(dbTrans, name);
        await savePoint.SaveAsync().ConfigAwait();
        return savePoint;
    }

    /// <summary>
    /// Create a managed OrmLite IDbCommand
    /// </summary>
    public static IDbCommand OpenCommand(this IDbConnection dbConn)
    {
        return dbConn.GetExecFilter().CreateCommand(dbConn);
    }

    /// <summary>
    /// Returns results from using a LINQ Expression. E.g:
    /// <para>db.Select&lt;Person&gt;(x =&gt; x.Age &gt; 40)</para>
    /// </summary>
    public static List<T> Select<T>(this IDbConnection dbConn, Expression<Func<T, bool>> predicate)
    {
        return dbConn.Exec(dbCmd => dbCmd.Select(predicate));
    }

    /// <summary>
    /// Returns results from using an SqlExpression lambda. E.g:
    /// <para>db.Select(db.From&lt;Person&gt;().Where(x =&gt; x.Age &gt; 40))</para>
    /// </summary>
    public static List<T> Select<T>(this IDbConnection dbConn, SqlExpression<T> expression)
    {
        return dbConn.Exec(dbCmd => dbCmd.Select(expression));
    }

    /// <summary>
    /// Returns results from using an SqlExpression lambda. E.g:
    /// <para>db.Select(db.From&lt;Person&gt;().Where(x =&gt; x.Age &gt; 40))</para>
    /// </summary>
    public static List<T> Select<T>(this IDbConnection dbConn, ISqlExpression expression, object anonType = null)
    {
        if (anonType != null)
            return dbConn.Exec(dbCmd => dbCmd.SqlList<T>(expression.SelectInto<T>(QueryType.Select), anonType));

        if (expression.Params != null && expression.Params.Any())
            return dbConn.Exec(dbCmd => dbCmd.SqlList<T>(expression.SelectInto<T>(QueryType.Select), expression.Params.ToDictionary(param => param.ParameterName, param => param.Value)));

        return dbConn.Exec(dbCmd => dbCmd.SqlList<T>(expression.SelectInto<T>(QueryType.Select), expression.Params));
    }

    public static List<Tuple<T, T2>> SelectMulti<T, T2>(this IDbConnection dbConn, SqlExpression<T> expression) => dbConn.Exec(dbCmd => dbCmd.SelectMulti<T, T2>(expression));
    public static List<Tuple<T, T2, T3>> SelectMulti<T, T2, T3>(this IDbConnection dbConn, SqlExpression<T> expression) => dbConn.Exec(dbCmd => dbCmd.SelectMulti<T, T2, T3>(expression));
    public static List<Tuple<T, T2, T3, T4>> SelectMulti<T, T2, T3, T4>(this IDbConnection dbConn, SqlExpression<T> expression) => dbConn.Exec(dbCmd => dbCmd.SelectMulti<T, T2, T3, T4>(expression));
    public static List<Tuple<T, T2, T3, T4, T5>> SelectMulti<T, T2, T3, T4, T5>(this IDbConnection dbConn, SqlExpression<T> expression) => dbConn.Exec(dbCmd => dbCmd.SelectMulti<T, T2, T3, T4, T5>(expression));
    public static List<Tuple<T, T2, T3, T4, T5, T6>> SelectMulti<T, T2, T3, T4, T5, T6>(this IDbConnection dbConn, SqlExpression<T> expression) => dbConn.Exec(dbCmd => dbCmd.SelectMulti<T, T2, T3, T4, T5, T6>(expression));
    public static List<Tuple<T, T2, T3, T4, T5, T6, T7>> SelectMulti<T, T2, T3, T4, T5, T6, T7>(this IDbConnection dbConn, SqlExpression<T> expression) => dbConn.Exec(dbCmd => dbCmd.SelectMulti<T, T2, T3, T4, T5, T6, T7>(expression));
    public static List<Tuple<T, T2, T3, T4, T5, T6, T7, T8>> SelectMulti<T, T2, T3, T4, T5, T6, T7, T8>(this IDbConnection dbConn, SqlExpression<T> expression) => dbConn.Exec(dbCmd => dbCmd.SelectMulti<T, T2, T3, T4, T5, T6, T7, T8>(expression));


    public static List<Tuple<T, T2>> SelectMulti<T, T2>(this IDbConnection dbConn, SqlExpression<T> expression, string[] tableSelects) => dbConn.Exec(dbCmd => dbCmd.SelectMulti<T, T2>(expression, tableSelects));
    public static List<Tuple<T, T2, T3>> SelectMulti<T, T2, T3>(this IDbConnection dbConn, SqlExpression<T> expression, string[] tableSelects) => dbConn.Exec(dbCmd => dbCmd.SelectMulti<T, T2, T3>(expression, tableSelects));
    public static List<Tuple<T, T2, T3, T4>> SelectMulti<T, T2, T3, T4>(this IDbConnection dbConn, SqlExpression<T> expression, string[] tableSelects) => dbConn.Exec(dbCmd => dbCmd.SelectMulti<T, T2, T3, T4>(expression, tableSelects));
    public static List<Tuple<T, T2, T3, T4, T5>> SelectMulti<T, T2, T3, T4, T5>(this IDbConnection dbConn, SqlExpression<T> expression, string[] tableSelects) => dbConn.Exec(dbCmd => dbCmd.SelectMulti<T, T2, T3, T4, T5>(expression, tableSelects));
    public static List<Tuple<T, T2, T3, T4, T5, T6>> SelectMulti<T, T2, T3, T4, T5, T6>(this IDbConnection dbConn, SqlExpression<T> expression, string[] tableSelects) => dbConn.Exec(dbCmd => dbCmd.SelectMulti<T, T2, T3, T4, T5, T6>(expression, tableSelects));
    public static List<Tuple<T, T2, T3, T4, T5, T6, T7>> SelectMulti<T, T2, T3, T4, T5, T6, T7>(this IDbConnection dbConn, SqlExpression<T> expression, string[] tableSelects) => dbConn.Exec(dbCmd => dbCmd.SelectMulti<T, T2, T3, T4, T5, T6, T7>(expression, tableSelects));
    public static List<Tuple<T, T2, T3, T4, T5, T6, T7, T8>> SelectMulti<T, T2, T3, T4, T5, T6, T7, T8>(this IDbConnection dbConn, SqlExpression<T> expression, string[] tableSelects) => dbConn.Exec(dbCmd => dbCmd.SelectMulti<T, T2, T3, T4, T5, T6, T7, T8>(expression, tableSelects));

    /// <summary>
    /// Returns a single result from using a LINQ Expression. E.g:
    /// <para>db.Single&lt;Person&gt;(x =&gt; x.Age == 42)</para>
    /// </summary>
    public static T Single<T>(this IDbConnection dbConn, Expression<Func<T, bool>> predicate)
    {
        return dbConn.Exec(dbCmd => dbCmd.Single(predicate));
    }

    /// <summary>
    /// Returns results from using an SqlExpression lambda. E.g:
    /// <para>db.Select&lt;Person&gt;(x =&gt; x.Age &gt; 40)</para>
    /// </summary>
    public static T Single<T>(this IDbConnection dbConn, SqlExpression<T> expression)
    {
        return dbConn.Exec(dbCmd => dbCmd.Single(expression));
    }

    /// <summary>
    /// Returns results from using an SqlExpression lambda. E.g:
    /// <para>db.Single(db.From&lt;Person&gt;().Where(x =&gt; x.Age &gt; 40))</para>
    /// </summary>
    public static T Single<T>(this IDbConnection dbConn, ISqlExpression expression)
    {
        return dbConn.Exec(dbCmd => dbCmd.Single<T>(expression.SelectInto<T>(QueryType.Single), expression.Params));
    }

    /// <summary>
    /// Returns a scalar result from using an SqlExpression lambda. E.g:
    /// <para>db.Scalar&lt;Person, int&gt;(x =&gt; Sql.Max(x.Age))</para>
    /// </summary>
    public static TKey Scalar<T, TKey>(this IDbConnection dbConn, Expression<Func<T, object>> field)
    {
        return dbConn.Exec(dbCmd => dbCmd.Scalar<T, TKey>(field));
    }

    /// <summary>
    /// Returns a scalar result from using an SqlExpression lambda. E.g:
    /// <para>db.Scalar&lt;Person, int&gt;(x =&gt; Sql.Max(x.Age), , x =&gt; x.Age &lt; 50)</para>
    /// </summary>        
    public static TKey Scalar<T, TKey>(this IDbConnection dbConn,
        Expression<Func<T, object>> field, Expression<Func<T, bool>> predicate)
    {
        return dbConn.Exec(dbCmd => dbCmd.Scalar<T, TKey>(field, predicate));
    }

    /// <summary>
    /// Returns the count of rows that match the LINQ expression, E.g:
    /// <para>db.Count&lt;Person&gt;(x =&gt; x.Age &lt; 50)</para>
    /// </summary>
    public static long Count<T>(this IDbConnection dbConn, Expression<Func<T, bool>> expression)
    {
        return dbConn.Exec(dbCmd => dbCmd.Count(expression));
    }

    /// <summary>
    /// Returns the count of rows that match the supplied SqlExpression, E.g:
    /// <para>db.Count(db.From&lt;Person&gt;().Where(x =&gt; x.Age &lt; 50))</para>
    /// </summary>
    public static long Count<T>(this IDbConnection dbConn, SqlExpression<T> expression)
    {
        return dbConn.Exec(dbCmd => dbCmd.Count(expression));
    }

    public static long Count<T>(this IDbConnection dbConn)
    {
        var expression = dbConn.GetDialectProvider().SqlExpression<T>();
        return dbConn.Exec(dbCmd => dbCmd.Count(expression));
    }

    /// <summary>
    /// Return the number of rows returned by the supplied expression
    /// </summary>
    public static long RowCount<T>(this IDbConnection dbConn, SqlExpression<T> expression)
    {
        return dbConn.Exec(dbCmd => dbCmd.RowCount(expression));
    }

    /// <summary>
    /// Return the number of rows returned by the supplied expression
    /// </summary>
    public static long RowCount<T>(this IDbConnection dbConn)
    {
        return dbConn.Exec(dbCmd => dbCmd.RowCount<T>());
    }

    /// <summary>
    /// Return the number of rows returned by the supplied sql
    /// </summary>
    public static long RowCount(this IDbConnection dbConn, string sql, object anonType = null)
    {
        return dbConn.Exec(dbCmd => dbCmd.RowCount(sql, anonType));
    }

    /// <summary>
    /// Return the number of rows returned by the supplied sql and db params
    /// </summary>
    public static long RowCount(this IDbConnection dbConn, string sql, IEnumerable<IDbDataParameter> sqlParams)
    {
        return dbConn.Exec(dbCmd => dbCmd.RowCount(sql, sqlParams));
    }

    /// <summary>
    /// Returns results with references from using a LINQ Expression. E.g:
    /// <para>db.LoadSelect&lt;Person&gt;(x =&gt; x.Age &gt; 40)</para>
    /// </summary>
    public static List<T> LoadSelect<T>(this IDbConnection dbConn, Expression<Func<T, bool>> predicate, string[] include = null)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSelect(predicate, include));
    }

    /// <summary>
    /// Returns results with references from using a LINQ Expression. E.g:
    /// <para>db.LoadSelect&lt;Person&gt;(x =&gt; x.Age &gt; 40, include: x => new { x.PrimaryAddress })</para>
    /// </summary>
    public static List<T> LoadSelect<T>(this IDbConnection dbConn, Expression<Func<T, bool>> predicate, Expression<Func<T, object>> include)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSelect(predicate, include.GetFieldNames()));
    }

    /// <summary>
    /// Returns results with references from using an SqlExpression lambda. E.g:
    /// <para>db.LoadSelect(db.From&lt;Person&gt;().Where(x =&gt; x.Age &gt; 40))</para>
    /// </summary>
    public static List<T> LoadSelect<T>(this IDbConnection dbConn, SqlExpression<T> expression = null, string[] include = null)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSelect(expression, include));
    }

    /// <summary>
    /// Returns results with references from using an SqlExpression lambda. E.g:
    /// <para>db.LoadSelect(db.From&lt;Person&gt;().Where(x =&gt; x.Age &gt; 40), include:q.OnlyFields)</para>
    /// </summary>
    public static List<T> LoadSelect<T>(this IDbConnection dbConn, SqlExpression<T> expression, IEnumerable<string> include)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSelect(expression, include));
    }

    /// <summary>
    /// Returns results with references from using an SqlExpression lambda. E.g:
    /// <para>db.LoadSelect(db.From&lt;Person&gt;().Where(x =&gt; x.Age &gt; 40), include: x => new { x.PrimaryAddress })</para>
    /// </summary>
    public static List<T> LoadSelect<T>(this IDbConnection dbConn, SqlExpression<T> expression, Expression<Func<T, object>> include)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSelect(expression, include.GetFieldNames()));
    }

    /// <summary>
    /// Project results with references from a number of joined tables into a different model
    /// </summary>
    public static List<Into> LoadSelect<Into, From>(this IDbConnection dbConn, SqlExpression<From> expression, string[] include = null)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSelect<Into, From>(expression, include));
    }

    /// <summary>
    /// Project results with references from a number of joined tables into a different model
    /// </summary>
    public static List<Into> LoadSelect<Into, From>(this IDbConnection dbConn, SqlExpression<From> expression, IEnumerable<string> include)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSelect<Into, From>(expression, include));
    }

    /// <summary>
    /// Project results with references from a number of joined tables into a different model
    /// </summary>
    public static List<Into> LoadSelect<Into, From>(this IDbConnection dbConn, SqlExpression<From> expression, Expression<Func<Into, object>> include)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSelect<Into, From>(expression, include.GetFieldNames()));
    }

    /// <summary>
    /// Return ADO.NET reader.GetSchemaTable() in a DataTable
    /// </summary>
    /// <param name="dbConn"></param>
    /// <param name="sql"></param>
    /// <returns></returns>
    public static DataTable GetSchemaTable(this IDbConnection dbConn, string sql) => dbConn.Exec(dbCmd => dbCmd.GetSchemaTable(sql));
        
    /// <summary>
    /// Get Table Column Schemas for specified table
    /// </summary>
    public static ColumnSchema[] GetTableColumns<T>(this IDbConnection dbConn) => dbConn.Exec(dbCmd => dbCmd.GetTableColumns(typeof(T)));
    /// <summary>
    /// Get Table Column Schemas for specified table
    /// </summary>
    public static ColumnSchema[] GetTableColumns(this IDbConnection dbConn, Type type) => dbConn.Exec(dbCmd => dbCmd.GetTableColumns(type));
    /// <summary>
    /// Get Table Column Schemas for result-set return from specified sql
    /// </summary>
    public static ColumnSchema[] GetTableColumns(this IDbConnection dbConn, string sql) => dbConn.Exec(dbCmd => dbCmd.GetTableColumns(sql));
        
    public static void EnableForeignKeysCheck(this IDbConnection dbConn) => dbConn.Exec(dbConn.GetDialectProvider().EnableForeignKeysCheck);
    public static void DisableForeignKeysCheck(this IDbConnection dbConn) => dbConn.Exec(dbConn.GetDialectProvider().DisableForeignKeysCheck);
}