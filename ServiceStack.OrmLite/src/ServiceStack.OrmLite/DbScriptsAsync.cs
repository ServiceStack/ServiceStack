using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Data;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.OrmLite;

public partial class DbScriptsAsync : ScriptMethods
{
    private const string DbInfo = "__dbinfo"; // Keywords.DbInfo
    private const string DbConnection = "__dbconnection"; // useDb global
    static void ConfigureDb(IDbConnection db) => db.WithTag(nameof(DbScriptsAsync));

    private IDbConnectionFactory dbFactory;
    public IDbConnectionFactory DbFactory
    {
        get => dbFactory ??= Context.Container.Resolve<IDbConnectionFactory>();
        set => dbFactory = value;
    }

    public async Task<IDbConnection> OpenDbConnectionAsync(ScriptScopeContext scope, Dictionary<string, object> options)
    {
        var dbConn = await OpenDbConnectionFromOptionsAsync(options).ConfigAwait();
        if (dbConn != null)
            return dbConn;

        if (scope.PageResult != null)
        {
            if (scope.PageResult.Args.TryGetValue(DbInfo, out var oDbInfo) && oDbInfo is ConnectionInfo dbInfo)
                return await DbFactory.OpenDbConnectionAsync(dbInfo,ConfigureDb);

            if (scope.PageResult.Args.TryGetValue(DbConnection, out var oDbConn) && oDbConn is Dictionary<string, object> globalDbConn)
                return await OpenDbConnectionFromOptionsAsync(globalDbConn);
        }

        return await DbFactory.OpenAsync(ConfigureDb);
    }

    T dialect<T>(ScriptScopeContext scope, Func<IOrmLiteDialectProvider, T> fn)
    {
        if (scope.PageResult != null)
        {
            if (scope.PageResult.Args.TryGetValue(DbInfo, out var oDbInfo) && oDbInfo is ConnectionInfo dbInfo)
                return fn(DbFactory.GetDialectProvider(dbInfo));

            if (scope.PageResult.Args.TryGetValue(DbConnection, out var oDbConn) && oDbConn is Dictionary<string, object> useDb)
                return fn(DbFactory.GetDialectProvider(
                    providerName:useDb.GetValueOrDefault("providerName")?.ToString(),
                    namedConnection:useDb.GetValueOrDefault("namedConnection")?.ToString()));
        }
        return fn(OrmLiteConfig.DialectProvider);
    }

    public IgnoreResult useDb(ScriptScopeContext scope, Dictionary<string, object> dbConnOptions)
    {
        if (dbConnOptions == null)
        {
            scope.PageResult.Args.Remove(DbConnection);
        }
        else
        {
            if (!dbConnOptions.ContainsKey("connectionString") && !dbConnOptions.ContainsKey("namedConnection"))
                throw new NotSupportedException(nameof(useDb) + " requires either 'connectionString' or 'namedConnection' property");

            scope.PageResult.Args[DbConnection] = dbConnOptions;
        }
        return IgnoreResult.Value;
    }

    private async Task<IDbConnection> OpenDbConnectionFromOptionsAsync(Dictionary<string, object> options)
    {
        if (options != null)
        {
            if (options.TryGetValue("connectionString", out var connectionString))
            {
                return options.TryGetValue("providerName", out var providerName)
                    ? await DbFactory.OpenDbConnectionStringAsync((string) connectionString, (string) providerName, ConfigureDb).ConfigAwait()
                    : await DbFactory.OpenDbConnectionStringAsync((string) connectionString, ConfigureDb).ConfigAwait();
            }

            if (options.TryGetValue("namedConnection", out var namedConnection))
            {
                return await DbFactory.OpenDbConnectionAsync((string) namedConnection, ConfigureDb).ConfigAwait();
            }
        }

        return null;
    }

    async Task<object> exec<T>(Func<IDbConnection, Task<T>> fn, ScriptScopeContext scope, object options)
    {
        try
        {
            using var db = await OpenDbConnectionAsync(scope, options as Dictionary<string, object>).ConfigAwait();
            var result = await fn(db).ConfigAwait();
            return result;
        }
        catch (Exception ex)
        {
            throw new StopFilterExecutionException(scope, options, ex);
        }
    }

    public Task<object> dbSelect(ScriptScopeContext scope, string sql) => 
        exec(db => db.SqlListAsync<Dictionary<string, object>>(sql), scope, null);

    public Task<object> dbSelect(ScriptScopeContext scope, string sql, Dictionary<string, object> args) => 
        exec(db => db.SqlListAsync<Dictionary<string, object>>(sql, args), scope, null);

    public Task<object> dbSelect(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) => 
        exec(db => db.SqlListAsync<Dictionary<string, object>>(sql, args), scope, options);


    public Task<object> dbSingle(ScriptScopeContext scope, string sql) => 
        exec(db => db.SingleAsync<Dictionary<string, object>>(sql), scope, null);

    public Task<object> dbSingle(ScriptScopeContext scope, string sql, Dictionary<string, object> args) =>
        exec(db => db.SingleAsync<Dictionary<string, object>>(sql, args), scope, null);

    public Task<object> dbSingle(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) =>
        exec(db => db.SingleAsync<Dictionary<string, object>>(sql, args), scope, options);


    public Task<object> dbScalar(ScriptScopeContext scope, string sql) => 
        exec(db => db.ScalarAsync<object>(sql), scope, null);

    public Task<object> dbScalar(ScriptScopeContext scope, string sql, Dictionary<string, object> args) => 
        exec(db => db.ScalarAsync<object>(sql, args), scope, null);

    public Task<object> dbScalar(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) => 
        exec(db => db.ScalarAsync<object>(sql, args), scope, options);


    public Task<object> dbCount(ScriptScopeContext scope, string sql) => 
        exec(db => db.RowCountAsync(sql), scope, null);

    public Task<object> dbCount(ScriptScopeContext scope, string sql, Dictionary<string, object> args) => 
        exec(db => db.RowCountAsync(sql, args), scope, null);

    public Task<object> dbCount(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) => 
        exec(db => db.RowCountAsync(sql, args), scope, options);


    public async Task<object> dbExists(ScriptScopeContext scope, string sql) => 
        await dbScalar(scope, sql).ConfigAwait() != null;

    public async Task<object> dbExists(ScriptScopeContext scope, string sql, Dictionary<string, object> args) => 
        await dbScalar(scope, sql, args).ConfigAwait() != null;

    public async Task<object> dbExists(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) => 
        await dbScalar(scope, sql, args, options).ConfigAwait() != null;


    public Task<object> dbExec(ScriptScopeContext scope, string sql) => 
        exec(db => db.ExecuteSqlAsync(sql), scope, null);

    public Task<object> dbExec(ScriptScopeContext scope, string sql, Dictionary<string, object> args) => 
        exec(db => db.ExecuteSqlAsync(sql, args), scope, null);

    public Task<object> dbExec(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) => 
        exec(db => db.ExecuteSqlAsync(sql, args), scope, options);

    public List<string> dbNamedConnections() => OrmLiteConnectionFactory.NamedConnections.Keys.ToList();
    public Task<object> dbTableNames(ScriptScopeContext scope) => dbTableNames(scope, null, null);
    public Task<object> dbTableNames(ScriptScopeContext scope, Dictionary<string, object> args) => dbTableNames(scope, args, null);
    public Task<object> dbTableNames(ScriptScopeContext scope, Dictionary<string, object> args, object options) => 
        exec(db => db.GetTableNamesAsync(args != null && args.TryGetValue("schema", out var oSchema) ? oSchema as string : null), scope, options);

    public Task<object> dbTableNamesWithRowCounts(ScriptScopeContext scope) => 
        dbTableNamesWithRowCounts(scope, null, null);
    public Task<object> dbTableNamesWithRowCounts(ScriptScopeContext scope, Dictionary<string, object> args) => 
        dbTableNamesWithRowCounts(scope, args, null);
    public Task<object> dbTableNamesWithRowCounts(ScriptScopeContext scope, Dictionary<string, object> args, object options) => 
        exec(db => args == null 
                ? db.GetTableNamesWithRowCountsAsync() 
                : db.GetTableNamesWithRowCountsAsync(
                    live: args.TryGetValue("live", out var oLive) && oLive is bool b && b,
                    schema: args.TryGetValue("schema", out var oSchema) ? oSchema as string : null), 
            scope, options);

    public Task<object> dbColumnNames(ScriptScopeContext scope, string tableName) => dbColumnNames(scope, tableName, null);
    public Task<object> dbColumnNames(ScriptScopeContext scope, string tableName, object options) => 
        exec(async db => (await db.GetTableColumnsAsync($"SELECT * FROM {sqlQuote(scope,tableName)}").ConfigAwait())
            .Select(x => x.ColumnName).ToArray(), scope, options);

    public Task<object> dbColumns(ScriptScopeContext scope, string tableName) => dbColumns(scope, tableName, null);
    public Task<object> dbColumns(ScriptScopeContext scope, string tableName, object options) => 
        exec(db => db.GetTableColumnsAsync($"SELECT * FROM {sqlQuote(scope,tableName)}"), scope, options);

    public Task<object> dbDesc(ScriptScopeContext scope, string sql) => dbDesc(scope, sql, null);
    public Task<object> dbDesc(ScriptScopeContext scope, string sql, object options) => exec(db => db.GetTableColumnsAsync(sql), scope, options);

    public string sqlQuote(ScriptScopeContext scope, string name) => dialect(scope, d => d.GetQuotedName(name));
    public string sqlConcat(ScriptScopeContext scope, IEnumerable<object> values) => dialect(scope, d => d.SqlConcat(values));
    public string sqlCurrency(ScriptScopeContext scope, string fieldOrValue) => dialect(scope, d => d.SqlCurrency(fieldOrValue));
    public string sqlCurrency(ScriptScopeContext scope, string fieldOrValue, string symbol) => 
        dialect(scope, d => d.SqlCurrency(fieldOrValue, symbol));

    public string sqlCast(ScriptScopeContext scope, object fieldOrValue, string castAs) => 
        dialect(scope, d => d.SqlCast(fieldOrValue, castAs));
    public string sqlBool(ScriptScopeContext scope, bool value) => dialect(scope, d => d.SqlBool(value));
    public string sqlTrue(ScriptScopeContext scope) => dialect(scope, d => d.SqlBool(true));
    public string sqlFalse(ScriptScopeContext scope) => dialect(scope, d => d.SqlBool(false));
    public string sqlLimit(ScriptScopeContext scope, int? offset, int? limit) => 
        dialect(scope, d => padCondition(d.SqlLimit(offset, limit)));
    public string sqlLimit(ScriptScopeContext scope, int? limit) => 
        dialect(scope, d => padCondition(d.SqlLimit(null, limit)));
    public string sqlSkip(ScriptScopeContext scope, int? offset) => 
        dialect(scope, d => padCondition(d.SqlLimit(offset, null)));
    public string sqlTake(ScriptScopeContext scope, int? limit) => 
        dialect(scope, d => padCondition(d.SqlLimit(null, limit)));
    public string sqlOrderByFields(ScriptScopeContext scope, string orderBy) => 
        dialect(scope, d => OrmLiteUtils.OrderByFields(d, orderBy));
    public string ormliteVar(ScriptScopeContext scope, string name) => 
        dialect(scope, d => d.Variables.TryGetValue(name, out var value) ? value : null);

    public string sqlVerifyFragment(string sql) => sql.SqlVerifyFragment();
    public bool isUnsafeSql(string sql) => OrmLiteUtils.isUnsafeSql(sql, OrmLiteUtils.VerifySqlRegEx);
    public bool isUnsafeSqlFragment(string sql) => OrmLiteUtils.isUnsafeSql(sql, OrmLiteUtils.VerifyFragmentRegEx);

    private string padCondition(string text) => string.IsNullOrEmpty(text) ? "" : " " + text;
}