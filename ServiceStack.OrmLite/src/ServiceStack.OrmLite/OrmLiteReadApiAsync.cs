// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.OrmLite;

public static class OrmLiteReadApiAsync
{
    /// <summary>
    /// Returns results from the active connection, E.g:
    /// <para>db.SelectAsync&lt;Person&gt;()</para>
    /// </summary>
    public static Task<List<T>> SelectAsync<T>(this IDbConnection dbConn, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SelectAsync<T>(token));
    }

    /// <summary>
    /// Returns results from using sql. E.g:
    /// <para>db.SelectAsync&lt;Person&gt;("Age &gt; 40")</para>
    /// <para>db.SelectAsync&lt;Person&gt;("SELECT * FROM Person WHERE Age &gt; 40")</para>
    /// </summary>
    public static Task<List<T>> SelectAsync<T>(this IDbConnection dbConn, string sql, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SelectAsync<T>(sql, (object)null, token));
    }

    /// <summary>
    /// Returns results from using a parameterized query. E.g:
    /// <para>db.SelectAsync&lt;Person&gt;("Age &gt; @age", new { age = 40})</para>
    /// <para>db.SelectAsync&lt;Person&gt;("SELECT * FROM Person WHERE Age &gt; @age", new[] { db.CreateParam("age",40) })</para>
    /// </summary>
    public static Task<List<T>> SelectAsync<T>(this IDbConnection dbConn, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SelectAsync<T>(sql, sqlParams, token));
    }

    /// <summary>
    /// Returns results from using a parameterized query. E.g:
    /// <para>db.SelectAsync&lt;Person&gt;("Age &gt; @age", new { age = 40})</para>
    /// <para>db.SelectAsync&lt;Person&gt;("SELECT * FROM Person WHERE Age &gt; @age", new { age = 40})</para>
    /// </summary>
    public static Task<List<T>> SelectAsync<T>(this IDbConnection dbConn, string sql, object anonType, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SelectAsync<T>(sql, anonType, token));
    }

    /// <summary>
    /// Returns results from using a parameterized query. E.g:
    /// <para>db.SelectAsync&lt;Person&gt;("Age &gt; @age", new Dictionary&lt;string, object&gt; { { "age", 40 } })</para>
    /// <para>db.SelectAsync&lt;Person&gt;("SELECT * FROM Person WHERE Age &gt; @age", new Dictionary&lt;string, object&gt; { { "age", 40 } })</para>
    /// </summary>
    public static Task<List<T>> SelectAsync<T>(this IDbConnection dbConn, string sql, Dictionary<string, object> dict, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SelectAsync<T>(sql, dict, token));
    }

    /// <summary>
    /// Returns a partial subset of results from the specified tableType. E.g:
    /// <para>db.SelectAsync&lt;EntityWithId&gt;(typeof(Person))</para>
    /// <para></para>
    /// </summary>
    public static Task<List<TModel>> SelectAsync<TModel>(this IDbConnection dbConn, Type fromTableType, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SelectAsync<TModel>(fromTableType, token));
    }

    /// <summary>
    /// Returns a partial subset of results from the specified tableType. E.g:
    /// <para>db.SelectAsync&lt;EntityWithId&gt;(typeof(Person), "Age = @age", new { age = 27 })</para>
    /// <para></para>
    /// </summary>
    public static Task<List<TModel>> SelectAsync<TModel>(this IDbConnection dbConn, Type fromTableType, string sqlFilter, object anonType = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SelectAsync<TModel>(fromTableType, sqlFilter, anonType, token));
    }

    /// <summary>
    /// Returns results from using a single name, value filter. E.g:
    /// <para>db.WhereAsync&lt;Person&gt;("Age", 27)</para>
    /// </summary>
    public static Task<List<T>> WhereAsync<T>(this IDbConnection dbConn, string name, object value, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.WhereAsync<T>(name, value, token));
    }

    /// <summary>
    /// Returns results from using an anonymous type filter. E.g:
    /// <para>db.WhereAsync&lt;Person&gt;(new { Age = 27 })</para>
    /// </summary>
    public static Task<List<T>> WhereAsync<T>(this IDbConnection dbConn, object anonType, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.WhereAsync<T>(anonType, token));
    }

    /// <summary>
    /// Returns results using the supplied primary key ids. E.g:
    /// <para>db.SelectByIdsAsync&lt;Person&gt;(new[] { 1, 2, 3 })</para>
    /// </summary>
    public static Task<List<T>> SelectByIdsAsync<T>(this IDbConnection dbConn, IEnumerable idValues, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SelectByIdsAsync<T>(idValues, token));
    }

    /// <summary>
    /// Query results using the non-default values in the supplied partially populated POCO example. E.g:
    /// <para>db.SelectNonDefaultsAsync(new Person { Id = 1 })</para>
    /// </summary>
    public static Task<List<T>> SelectNonDefaultsAsync<T>(this IDbConnection dbConn, T filter, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SelectNonDefaultsAsync<T>(filter, token));
    }

    /// <summary>
    /// Query results using the non-default values in the supplied partially populated POCO example. E.g:
    /// <para>db.SelectNonDefaultsAsync("Age &gt; @Age", new Person { Age = 42 })</para>
    /// </summary>
    public static Task<List<T>> SelectNonDefaultsAsync<T>(this IDbConnection dbConn, string sql, T filter, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SelectNonDefaultsAsync<T>(sql, filter, token));
    }

    /// <summary>
    /// Returns the first result using a parameterized query. E.g:
    /// <para>db.SingleAsync&lt;Person&gt;(new { Age = 42 })</para>
    /// </summary>
    public static Task<T> SingleAsync<T>(this IDbConnection dbConn, object anonType, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SingleAsync<T>(anonType, token));
    }

    /// <summary>
    /// Returns results from using a single name, value filter. E.g:
    /// <para>db.SingleAsync&lt;Person&gt;("Age = @age", new[] { db.CreateParam("age",42) })</para>
    /// </summary>
    public static Task<T> SingleAsync<T>(this IDbConnection dbConn, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SingleAsync<T>(sql, sqlParams, token));
    }

    /// <summary>
    /// Returns results from using a single name, value filter. E.g:
    /// <para>db.SingleAsync&lt;Person&gt;("Age = @age", new { age = 42 })</para>
    /// </summary>
    public static Task<T> SingleAsync<T>(this IDbConnection dbConn, string sql, object anonType = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SingleAsync<T>(sql, anonType, token));
    }

    /// <summary>
    /// Returns the first result using a primary key id. E.g:
    /// <para>db.SingleByIdAsync&lt;Person&gt;(1)</para>
    /// </summary>
    public static Task<T> SingleByIdAsync<T>(this IDbConnection dbConn, object idValue, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SingleByIdAsync<T>(idValue, token));
    }

    /// <summary>
    /// Returns the first result using a name, value filter. E.g:
    /// <para>db.SingleWhereAsync&lt;Person&gt;("Age", 42)</para>
    /// </summary>
    public static Task<T> SingleWhereAsync<T>(this IDbConnection dbConn, string name, object value, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SingleWhereAsync<T>(name, value, token));
    }

    public static T ScalarAsync<T>(this IDbCommand dbCmd, string sql, IEnumerable<IDbDataParameter> sqlParams)
    {
        if (sqlParams != null)
            dbCmd.SetParameters(sqlParams);

        return dbCmd.Scalar<T>(sql);
    }

    /// <summary>
    /// Returns a single scalar value using an SqlExpression. E.g:
    /// <para>db.ScalarAsync&lt;int&gt;(db.From&lt;Person&gt;().Select(x => Sql.Count("*")).Where(q => q.Age > 40))</para>
    /// </summary>
    public static Task<T> ScalarAsync<T>(this IDbConnection dbConn, ISqlExpression sqlExpression, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ScalarAsync<T>(sqlExpression.ToSelectStatement(QueryType.Scalar), sqlExpression.Params, token));
    }

    /// <summary>
    /// Returns a single scalar value using a parameterized query. E.g:
    /// <para>db.ScalarAsync&lt;int&gt;("SELECT COUNT(*) FROM Person WHERE Age &gt; @age", new[] { db.CreateParam("age",40) })</para>
    /// </summary>
    public static Task<T> ScalarAsync<T>(this IDbConnection dbConn, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ScalarAsync<T>(sql, sqlParams, token));
    }

    /// <summary>
    /// Returns a single scalar value using a parameterized query. E.g:
    /// <para>db.ScalarAsync&lt;int&gt;("SELECT COUNT(*) FROM Person WHERE Age &gt; @age", new { age = 40 })</para>
    /// </summary>
    public static Task<T> ScalarAsync<T>(this IDbConnection dbConn, string sql, object anonType = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ScalarAsync<T>(sql, anonType, token));
    }

    /// <summary>
    /// Returns the distinct first column values in a HashSet using an SqlExpression. E.g:
    /// <para>db.ColumnAsync&lt;int&gt;(db.From&lt;Person&gt;().Select(x => x.LastName).Where(q => q.Age == 27))</para>
    /// </summary>
    public static Task<List<T>> ColumnAsync<T>(this IDbConnection dbConn, ISqlExpression query, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ColumnAsync<T>(query.ToSelectStatement(QueryType.Select), query.Params, token));
    }

    /// <summary>
    /// Returns the first column in a List using a SqlFormat query. E.g:
    /// <para>db.ColumnAsync&lt;string&gt;("SELECT LastName FROM Person WHERE Age = @age", new[] { db.CreateParam("age",27) })</para>
    /// </summary>
    public static Task<List<T>> ColumnAsync<T>(this IDbConnection dbConn, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ColumnAsync<T>(sql, sqlParams, token));
    }

    /// <summary>
    /// Returns the first column in a List using a SqlFormat query. E.g:
    /// <para>db.ColumnAsync&lt;string&gt;("SELECT LastName FROM Person WHERE Age = @age", new { age = 27 })</para>
    /// </summary>
    public static Task<List<T>> ColumnAsync<T>(this IDbConnection dbConn, string sql, object anonType = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ColumnAsync<T>(sql, anonType, token));
    }

    /// <summary>
    /// Returns the distinct first column values in a HashSet using an SqlExpression. E.g:
    /// <para>db.ColumnDistinctAsync&lt;int&gt;(db.From&lt;Person&gt;().Select(x => x.Age).Where(q => q.Age &lt; 50))</para>
    /// </summary>
    public static Task<HashSet<T>> ColumnDistinctAsync<T>(this IDbConnection dbConn, ISqlExpression query, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ColumnDistinctAsync<T>(query.ToSelectStatement(QueryType.Select), query.Params, token));
    }

    /// <summary>
    /// Returns the distinct first column values in a HashSet using an SqlFormat query. E.g:
    /// <para>db.ColumnDistinctAsync&lt;int&gt;("SELECT Age FROM Person WHERE Age &lt; @age", new[] { db.CreateParam("age",50) })</para>
    /// </summary>
    public static Task<HashSet<T>> ColumnDistinctAsync<T>(this IDbConnection dbConn, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ColumnDistinctAsync<T>(sql, sqlParams, token));
    }

    /// <summary>
    /// Returns the distinct first column values in a HashSet using an SqlFormat query. E.g:
    /// <para>db.ColumnDistinctAsync&lt;int&gt;("SELECT Age FROM Person WHERE Age &lt; @age", new { age = 50 })</para>
    /// </summary>
    public static Task<HashSet<T>> ColumnDistinctAsync<T>(this IDbConnection dbConn, string sql, object anonType = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ColumnDistinctAsync<T>(sql, anonType, token));
    }

    /// <summary>
    /// Returns an Dictionary&lt;K, List&lt;V&gt;&gt; grouping made from the first two columns using an Sql Expression. E.g:
    /// <para>db.LookupAsync&lt;int, string&gt;(db.From&lt;Person&gt;().Select(x => new { x.Age, x.LastName }).Where(q => q.Age &lt; 50))</para>
    /// </summary>
    public static Task<Dictionary<K, List<V>>> LookupAsync<K, V>(this IDbConnection dbConn, ISqlExpression sqlExpression, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.LookupAsync<K, V>(sqlExpression.ToSelectStatement(QueryType.Select), sqlExpression.Params, token));
    }

    /// <summary>
    /// Returns an Dictionary&lt;K, List&lt;V&gt;&gt; grouping made from the first two columns using an parameterized query. E.g:
    /// <para>db.LookupAsync&lt;int, string&gt;("SELECT Age, LastName FROM Person WHERE Age &lt; @age", new[] { db.CreateParam("age",50) })</para>
    /// </summary>
    public static Task<Dictionary<K, List<V>>> LookupAsync<K, V>(this IDbConnection dbConn, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.LookupAsync<K, V>(sql, sqlParams, token));
    }

    /// <summary>
    /// Returns an Dictionary&lt;K, List&lt;V&gt;&gt; grouping made from the first two columns using an parameterized query. E.g:
    /// <para>db.LookupAsync&lt;int, string&gt;("SELECT Age, LastName FROM Person WHERE Age &lt; @age", new { age = 50 })</para>
    /// </summary>
    public static Task<Dictionary<K, List<V>>> LookupAsync<K, V>(this IDbConnection dbConn, string sql, object anonType = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.LookupAsync<K, V>(sql, anonType, token));
    }

    /// <summary>
    /// Returns a Dictionary from the first 2 columns: Column 1 (Keys), Column 2 (Values) using an SqlExpression. E.g:
    /// <para>db.DictionaryAsync&lt;int, string&gt;(db.From&lt;Person&gt;().Select(x => new { x.Id, x.LastName }).Where(x => x.Age &lt; 50))</para>
    /// </summary>
    public static Task<Dictionary<K, V>> DictionaryAsync<K, V>(this IDbConnection dbConn, ISqlExpression query, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.DictionaryAsync<K, V>(query.ToSelectStatement(QueryType.Select), query.Params, token));
    }

    /// <summary>
    /// Returns a Dictionary from the first 2 columns: Column 1 (Keys), Column 2 (Values) using sql. E.g:
    /// <para>db.DictionaryAsync&lt;int, string&gt;("SELECT Id, LastName FROM Person WHERE Age &lt; @age", new[] { db.CreateParam("age",50) })</para>
    /// </summary>
    public static Task<Dictionary<K, V>> DictionaryAsync<K, V>(this IDbConnection dbConn, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.DictionaryAsync<K, V>(sql, sqlParams, token));
    }

    /// <summary>
    /// Returns a Dictionary from the first 2 columns: Column 1 (Keys), Column 2 (Values) using sql. E.g:
    /// <para>db.DictionaryAsync&lt;int, string&gt;("SELECT Id, LastName FROM Person WHERE Age &lt; @age", new { age = 50 })</para>
    /// </summary>
    public static Task<Dictionary<K, V>> DictionaryAsync<K, V>(this IDbConnection dbConn, string sql, object anonType = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.DictionaryAsync<K, V>(sql, anonType, token));
    }

    /// <summary>
    /// Returns a list of KeyValuePairs from the first 2 columns: Column 1 (Keys), Column 2 (Values) using an SqlExpression. E.g:
    /// <para>db.KeyValuePairsAsync&lt;int, string&gt;(db.From&lt;Person&gt;().Select(x => new { x.Id, x.LastName }).Where(x => x.Age &lt; 50))</para>
    /// </summary>
    public static Task<List<KeyValuePair<K, V>>> KeyValuePairsAsync<K, V>(this IDbConnection dbConn, ISqlExpression query, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.KeyValuePairsAsync<K, V>(query.ToSelectStatement(QueryType.Select), query.Params, token));
    }

    /// <summary>
    /// Returns a list of KeyValuePairs from the first 2 columns: Column 1 (Keys), Column 2 (Values) using sql. E.g:
    /// <para>db.KeyValuePairsAsync&lt;int, string&gt;("SELECT Id, LastName FROM Person WHERE Age &lt; @age", new[] { db.CreateParam("age",50) })</para>
    /// </summary>
    public static Task<List<KeyValuePair<K, V>>> KeyValuePairsAsync<K, V>(this IDbConnection dbConn, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.KeyValuePairsAsync<K, V>(sql, sqlParams, token));
    }

    /// <summary>
    /// Returns a list of KeyValuePairs from the first 2 columns: Column 1 (Keys), Column 2 (Values) using sql. E.g:
    /// <para>db.KeyValuePairsAsync&lt;int, string&gt;("SELECT Id, LastName FROM Person WHERE Age &lt; @age", new { age = 50 })</para>
    /// </summary>
    public static Task<List<KeyValuePair<K, V>>> KeyValuePairsAsync<K, V>(this IDbConnection dbConn, string sql, object anonType = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.KeyValuePairsAsync<K, V>(sql, anonType, token));
    }

    /// <summary>
    /// Returns true if the Query returns any records that match the LINQ expression, E.g:
    /// <para>db.ExistsAsync&lt;Person&gt;(x =&gt; x.Age &lt; 50)</para>
    /// </summary>
    public static Task<bool> ExistsAsync<T>(this IDbConnection dbConn, Expression<Func<T, bool>> expression, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ScalarAsync(dbConn.From<T>().Where(expression).Limit(1).Select("'exists'"), token).Then(x => x != null));
    }

    /// <summary>
    /// Returns true if the Query returns any records that match the supplied SqlExpression, E.g:
    /// <para>db.ExistsAsync(db.From&lt;Person&gt;().Where(x =&gt; x.Age &lt; 50))</para>
    /// </summary>
    public static Task<bool> ExistsAsync<T>(this IDbConnection dbConn, SqlExpression<T> expression, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ScalarAsync(expression.Limit(1).Select("'exists'"), token).Then(x => x != null));
    }
    /// <summary>
    /// Returns true if the Query returns any records, using an SqlFormat query. E.g:
    /// <para>db.ExistsAsync&lt;Person&gt;(new { Age = 42 })</para>
    /// </summary>
    public static Task<bool> ExistsAsync<T>(this IDbConnection dbConn, object anonType, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ExistsAsync<T>(anonType, token));
    }

    /// <summary>
    /// Returns true if the Query returns any records, using a parameterized query. E.g:
    /// <para>db.ExistsAsync&lt;Person&gt;("Age = @age", new { age = 42 })</para>
    /// <para>db.ExistsAsync&lt;Person&gt;("SELECT * FROM Person WHERE Age = @age", new { age = 42 })</para>
    /// </summary>
    public static Task<bool> ExistsAsync<T>(this IDbConnection dbConn, string sql, object anonType = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ExistsAsync<T>(sql, anonType, token));
    }

    /// <summary>
    /// Returns results from an arbitrary SqlExpression. E.g:
    /// <para>db.SqlListAsync&lt;Person&gt;(db.From&lt;Person&gt;().Select("*").Where(q => q.Age &lt; 50))</para>
    /// </summary>
    public static Task<List<T>> SqlListAsync<T>(this IDbConnection dbConn, ISqlExpression sqlExpression, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SqlListAsync<T>(sqlExpression.ToSelectStatement(QueryType.Select), sqlExpression.Params, token));
    }

    /// <summary>
    /// Returns results from an arbitrary parameterized raw sql query. E.g:
    /// <para>db.SqlListAsync&lt;Person&gt;("EXEC GetRockstarsAged @age", new { age = 50 })</para>
    /// </summary>
    public static Task<List<T>> SqlListAsync<T>(this IDbConnection dbConn, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SqlListAsync<T>(sql, sqlParams, token));
    }

    /// <summary>
    /// Returns results from an arbitrary parameterized raw sql query. E.g:
    /// <para>db.SqlListAsync&lt;Person&gt;("EXEC GetRockstarsAged @age", new { age = 50 })</para>
    /// </summary>
    public static Task<List<T>> SqlListAsync<T>(this IDbConnection dbConn, string sql, object anonType = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SqlListAsync<T>(sql, anonType, token));
    }

    /// <summary>
    /// Returns results from an arbitrary parameterized raw sql query. E.g:
    /// <para>db.SqlListAsync&lt;Person&gt;("EXEC GetRockstarsAged @age", new Dictionary&lt;string, object&gt; { { "age", 42 } })</para>
    /// </summary>
    public static Task<List<T>> SqlListAsync<T>(this IDbConnection dbConn, string sql, Dictionary<string, object> dict, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SqlListAsync<T>(sql, dict, token));
    }

    /// <summary>
    /// Returns results from an arbitrary parameterized raw sql query with a dbCmd filter. E.g:
    /// <para>db.SqlListAsync&lt;Person&gt;("EXEC GetRockstarsAged @age", dbCmd => ...)</para>
    /// </summary>
    public static Task<List<T>> SqlListAsync<T>(this IDbConnection dbConn, string sql, Action<IDbCommand> dbCmdFilter, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SqlListAsync<T>(sql, dbCmdFilter, token));
    }

    /// <summary>
    /// Returns the first column in a List using an SqlExpression. E.g:
    /// <para>db.SqlColumnAsync&lt;string&gt;(db.From&lt;Person&gt;().Select(x => x.LastName).Where(q => q.Age &lt; 50))</para>
    /// </summary>
    public static Task<List<T>> SqlColumnAsync<T>(this IDbConnection dbConn, ISqlExpression sqlExpression, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SqlColumnAsync<T>(sqlExpression.ToSelectStatement(QueryType.Select), sqlExpression.Params, token));
    }

    /// <summary>
    /// Returns the first column in a List using a parameterized query. E.g:
    /// <para>db.SqlColumnAsync&lt;string&gt;("SELECT LastName FROM Person WHERE Age &lt; @age", new[] { db.CreateParam("age",50) })</para>
    /// </summary>
    public static Task<List<T>> SqlColumnAsync<T>(this IDbConnection dbConn, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SqlColumnAsync<T>(sql, sqlParams, token));
    }

    /// <summary>
    /// Returns the first column in a List using a parameterized query. E.g:
    /// <para>db.SqlColumnAsync&lt;string&gt;("SELECT LastName FROM Person WHERE Age &lt; @age", new { age = 50 })</para>
    /// </summary>
    public static Task<List<T>> SqlColumnAsync<T>(this IDbConnection dbConn, string sql, object anonType = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SqlColumnAsync<T>(sql, anonType, token));
    }

    /// <summary>
    /// Returns the first column in a List using a parameterized query. E.g:
    /// <para>db.SqlColumnAsync&lt;string&gt;("SELECT LastName FROM Person WHERE Age &lt; @age", new Dictionary&lt;string, object&gt; { { "age", 50 } })</para>
    /// </summary>
    public static Task<List<T>> SqlColumnAsync<T>(this IDbConnection dbConn, string sql, Dictionary<string, object> dict, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SqlColumnAsync<T>(sql, dict, token));
    }

    /// <summary>
    /// Returns a single Scalar value using an SqlExpression. E.g:
    /// <para>db.SqlScalarAsync&lt;int&gt;(db.From&lt;Person&gt;().Select(Sql.Count("*")).Where(q => q.Age &lt; 50))</para>
    /// </summary>
    public static Task<T> SqlScalarAsync<T>(this IDbConnection dbConn, ISqlExpression sqlExpression, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SqlScalarAsync<T>(sqlExpression.ToSelectStatement(QueryType.Scalar), sqlExpression.Params, token));
    }

    /// <summary>
    /// Returns a single Scalar value using a parameterized query. E.g:
    /// <para>db.SqlScalarAsync&lt;int&gt;("SELECT COUNT(*) FROM Person WHERE Age &lt; @age", new[] { db.CreateParam("age",50) })</para>
    /// </summary>
    public static Task<T> SqlScalarAsync<T>(this IDbConnection dbConn, string sql, IEnumerable<IDbDataParameter> sqlParams, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SqlScalarAsync<T>(sql, sqlParams, token));
    }

    /// <summary>
    /// Returns a single Scalar value using a parameterized query. E.g:
    /// <para>db.SqlScalarAsync&lt;int&gt;("SELECT COUNT(*) FROM Person WHERE Age &lt; @age", new { age = 50 })</para>
    /// </summary>
    public static Task<T> SqlScalarAsync<T>(this IDbConnection dbConn, string sql, object anonType = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SqlScalarAsync<T>(sql, anonType, token));
    }

    /// <summary>
    /// Returns a single Scalar value using a parameterized query. E.g:
    /// <para>db.SqlScalarAsync&lt;int&gt;("SELECT COUNT(*) FROM Person WHERE Age &lt; @age", new Dictionary&lt;string, object&gt; { { "age", 50 } })</para>
    /// </summary>
    public static Task<T> SqlScalarAsync<T>(this IDbConnection dbConn, string sql, Dictionary<string, object> dict, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SqlScalarAsync<T>(sql, dict, token));
    }

    /// <summary>
    /// Executes a raw sql non-query using sql. E.g:
    /// <para>var rowsAffected = db.ExecuteNonQueryAsync("UPDATE Person SET LastName={0} WHERE Id={1}".SqlFormat("WaterHouse", 7))</para>
    /// </summary>
    /// <returns>number of rows affected</returns>
    public static Task<int> ExecuteNonQueryAsync(this IDbConnection dbConn, string sql, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ExecNonQueryAsync(sql, null, token));
    }

    /// <summary>
    /// Executes a raw sql non-query using a parameterized query. E.g:
    /// <para>var rowsAffected = db.ExecuteNonQueryAsync("UPDATE Person SET LastName=@name WHERE Id=@id", new { name = "WaterHouse", id = 7 })</para>
    /// </summary>
    /// <returns>number of rows affected</returns>
    public static Task<int> ExecuteNonQueryAsync(this IDbConnection dbConn, string sql, object anonType, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ExecNonQueryAsync(sql, anonType, token));
    }

    /// <summary>
    /// Executes a raw sql non-query using a parameterized query.
    /// </summary>
    /// <returns>number of rows affected</returns>
    public static Task<int> ExecuteNonQueryAsync(this IDbConnection dbConn, string sql, Dictionary<string, object> dict, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ExecNonQueryAsync(sql, dict, token));
    }

    /// <summary>
    /// Returns results from a Stored Procedure, using a parameterized query.
    /// </summary>
    public static Task<List<TOutputModel>> SqlProcedureAsync<TOutputModel>(this IDbConnection dbConn, object anonType, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.SqlProcedureAsync<TOutputModel>(anonType, token));
    }

    /// <summary>
    /// Returns the scalar result as a long.
    /// </summary>
    public static Task<long> LongScalarAsync(this IDbConnection dbConn, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.ExecLongScalarAsync(null, token));
    }

    /// <summary>
    /// Returns the first result with all its references loaded, using a primary key id. E.g:
    /// <para>db.LoadSingleByIdAsync&lt;Person&gt;(1)</para>
    /// </summary>
    public static Task<T> LoadSingleByIdAsync<T>(this IDbConnection dbConn, object idValue, string[] include = null, CancellationToken token=default)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSingleByIdAsync<T>(idValue, include, token));
    }

    /// <summary>
    /// Returns the first result with all its references loaded, using a primary key id. E.g:
    /// <para>db.LoadSingleByIdAsync&lt;Person&gt;(1, include = x => new { x.Address })</para>
    /// </summary>
    public static Task<T> LoadSingleByIdAsync<T>(this IDbConnection dbConn, object idValue, Expression<Func<T, object>> include, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadSingleByIdAsync<T>(idValue, include.GetFieldNames(), token));
    }

    /// <summary>
    /// Loads all the related references onto the instance. E.g:
    /// <para>db.LoadReferencesAsync(customer)</para> 
    /// </summary>
    public static Task LoadReferencesAsync<T>(this IDbConnection dbConn, T instance, string[] include = null, CancellationToken token = default)
    {
        return dbConn.Exec(dbCmd => dbCmd.LoadReferencesAsync(instance, include, token));
    }
}