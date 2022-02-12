#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ServiceStack.Data;
using ServiceStack.Script;

namespace ServiceStack.OrmLite
{
    [Obsolete("Use DbScriptsAsync")]
    public class DbScripts : ScriptMethods
    {
        private const string DbInfo = "__dbinfo"; // Keywords.DbInfo
        private const string DbConnection = "__dbconnection"; // useDb global
        
        private IDbConnectionFactory dbFactory;
        public IDbConnectionFactory DbFactory
        {
            get => dbFactory ??= Context.Container.Resolve<IDbConnectionFactory>();
            set => dbFactory = value;
        }

        public IDbConnection OpenDbConnection(ScriptScopeContext scope, Dictionary<string, object> options)
        {
            var dbConn = OpenDbConnectionFromOptions(options);
            if (dbConn != null)
                return dbConn;

            if (scope.PageResult != null)
            {
                if (scope.PageResult.Args.TryGetValue(DbInfo, out var oDbInfo) && oDbInfo is ConnectionInfo dbInfo)
                    return DbFactory.OpenDbConnection(dbInfo);

                if (scope.PageResult.Args.TryGetValue(DbConnection, out var oDbConn) && oDbConn is Dictionary<string, object> globalDbConn)
                    return OpenDbConnectionFromOptions(globalDbConn);
            }

            return DbFactory.OpenDbConnection();
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

        private IDbConnection OpenDbConnectionFromOptions(Dictionary<string, object> options)
        {
            if (options != null)
            {
                if (options.TryGetValue("connectionString", out var connectionString))
                {
                    return options.TryGetValue("providerName", out var providerName)
                        ? DbFactory.OpenDbConnectionString((string) connectionString, (string) providerName)
                        : DbFactory.OpenDbConnectionString((string) connectionString);
                }

                if (options.TryGetValue("namedConnection", out var namedConnection))
                {
                    return DbFactory.OpenDbConnection((string) namedConnection);
                }
            }

            return null;
        }

        T exec<T>(Func<IDbConnection, T> fn, ScriptScopeContext scope, object options)
        {
            try
            {
                using var db = OpenDbConnection(scope, options as Dictionary<string, object>);
                return fn(db);
            }
            catch (Exception ex)
            {
                throw new StopFilterExecutionException(scope, options, ex);
            }
        }

        public object dbSelect(ScriptScopeContext scope, string sql) => 
            exec(db => db.SqlList<Dictionary<string, object>>(sql), scope, null);

        public object dbSelect(ScriptScopeContext scope, string sql, Dictionary<string, object> args) => 
            exec(db => db.SqlList<Dictionary<string, object>>(sql, args), scope, null);

        public object dbSelect(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) => 
            exec(db => db.SqlList<Dictionary<string, object>>(sql, args), scope, options);


        public object dbSingle(ScriptScopeContext scope, string sql) => 
            exec(db => db.Single<Dictionary<string, object>>(sql), scope, null);

        public object dbSingle(ScriptScopeContext scope, string sql, Dictionary<string, object> args) =>
            exec(db => db.Single<Dictionary<string, object>>(sql, args), scope, null);

        public object dbSingle(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) =>
            exec(db => db.Single<Dictionary<string, object>>(sql, args), scope, options);


        public object dbScalar(ScriptScopeContext scope, string sql) => 
            exec(db => db.Scalar<object>(sql), scope, null);

        public object dbScalar(ScriptScopeContext scope, string sql, Dictionary<string, object> args) => 
            exec(db => db.Scalar<object>(sql, args), scope, null);

        public object dbScalar(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) => 
            exec(db => db.Scalar<object>(sql, args), scope, options);


        public long dbCount(ScriptScopeContext scope, string sql) => 
            exec(db => db.RowCount(sql), scope, null);

        public long dbCount(ScriptScopeContext scope, string sql, Dictionary<string, object> args) => 
            exec(db => db.RowCount(sql, args), scope, null);

        public long dbCount(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) => 
            exec(db => db.RowCount(sql, args), scope, options);


        public bool dbExists(ScriptScopeContext scope, string sql) => 
            dbScalar(scope, sql) != null;

        public bool dbExists(ScriptScopeContext scope, string sql, Dictionary<string, object> args) => 
            dbScalar(scope, sql, args) != null;

        public bool dbExists(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) => 
            dbScalar(scope, sql, args, options) != null;


        public int dbExec(ScriptScopeContext scope, string sql) => 
            exec(db => db.ExecuteSql(sql), scope, null);

        public int dbExec(ScriptScopeContext scope, string sql, Dictionary<string, object> args) => 
            exec(db => db.ExecuteSql(sql, args), scope, null);

        public int dbExec(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) => 
            exec(db => db.ExecuteSql(sql, args), scope, options);

        public List<string> dbNamedConnections() => OrmLiteConnectionFactory.NamedConnections.Keys.ToList();
        public List<string> dbTableNames(ScriptScopeContext scope) => dbTableNames(scope, null, null);
        public List<string> dbTableNames(ScriptScopeContext scope, Dictionary<string, object> args) => dbTableNames(scope, args, null);
        public List<string> dbTableNames(ScriptScopeContext scope, Dictionary<string, object> args, object options) => 
            exec(db => db.GetTableNames(args != null && args.TryGetValue("schema", out var oSchema) ? oSchema as string : null), scope, options);

        public List<KeyValuePair<string, long>> dbTableNamesWithRowCounts(ScriptScopeContext scope) => 
            dbTableNamesWithRowCounts(scope, null, null);
        public List<KeyValuePair<string, long>> dbTableNamesWithRowCounts(ScriptScopeContext scope, Dictionary<string, object> args) => 
            dbTableNamesWithRowCounts(scope, args, null);
        public List<KeyValuePair<string, long>> dbTableNamesWithRowCounts(ScriptScopeContext scope, Dictionary<string, object> args, object options) => 
            exec(db => args == null 
                    ? db.GetTableNamesWithRowCounts() 
                    : db.GetTableNamesWithRowCounts(
                        live: args.TryGetValue("live", out var oLive) && oLive is bool b && b,
                        schema: args.TryGetValue("schema", out var oSchema) ? oSchema as string : null), 
                scope, options);

        public string[] dbColumnNames(ScriptScopeContext scope, string tableName) => dbColumnNames(scope, tableName, null);
        public string[] dbColumnNames(ScriptScopeContext scope, string tableName, object options) => 
            dbColumns(scope, tableName, options).Select(x => x.ColumnName).ToArray();

        public ColumnSchema[] dbColumns(ScriptScopeContext scope, string tableName) => dbColumns(scope, tableName, null);
        public ColumnSchema[] dbColumns(ScriptScopeContext scope, string tableName, object options) => 
            exec(db => db.GetTableColumns($"SELECT * FROM {sqlQuote(scope,tableName)}"), scope, options);

        public ColumnSchema[] dbDesc(ScriptScopeContext scope, string sql) => dbDesc(scope, sql, null);
        public ColumnSchema[] dbDesc(ScriptScopeContext scope, string sql, object options) => exec(db => db.GetTableColumns(sql), scope, options);


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
    
    public partial class DbScriptsAsync
    {
        private DbScripts sync;
        private DbScripts Sync => sync ??= new DbScripts { Context = Context, Pages = Pages };
        
        public object dbSelectSync(ScriptScopeContext scope, string sql) => Sync.dbSelect(scope, sql);

        public object dbSelectSync(ScriptScopeContext scope, string sql, Dictionary<string, object> args) => 
            Sync.dbSelect(scope, sql, args);

        public object dbSelectSync(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) => 
            Sync.dbSelect(scope, sql, args, options);


        public object dbSingleSync(ScriptScopeContext scope, string sql) => 
            Sync.dbSingle(scope, sql);

        public object dbSingleSync(ScriptScopeContext scope, string sql, Dictionary<string, object> args) =>
            Sync.dbSingle(scope, sql, args);

        public object dbSingleSync(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) =>
            Sync.dbSingle(scope, sql, args, options);


        public object dbScalarSync(ScriptScopeContext scope, string sql) => 
            Sync.dbScalar(scope, sql);

        public object dbScalarSync(ScriptScopeContext scope, string sql, Dictionary<string, object> args) => 
            Sync.dbScalar(scope, sql, args);

        public object dbScalarSync(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) => 
            Sync.dbScalar(scope, sql, args, options);


        public long dbCountSync(ScriptScopeContext scope, string sql) => 
            Sync.dbCount(scope, sql);

        public long dbCountSync(ScriptScopeContext scope, string sql, Dictionary<string, object> args) => 
            Sync.dbCount(scope, sql, args);

        public long dbCountSync(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) => 
            Sync.dbCount(scope, sql, args, options);


        public bool dbExistsSync(ScriptScopeContext scope, string sql) => 
            Sync.dbExists(scope, sql);

        public bool dbExistsSync(ScriptScopeContext scope, string sql, Dictionary<string, object> args) => 
            Sync.dbExists(scope, sql, args);

        public bool dbExistsSync(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) => 
            Sync.dbExists(scope, sql, args, options);


        public int dbExecSync(ScriptScopeContext scope, string sql) => 
            Sync.dbExec(scope, sql);

        public int dbExecSync(ScriptScopeContext scope, string sql, Dictionary<string, object> args) => 
            Sync.dbExec(scope, sql, args);

        public int dbExecSync(ScriptScopeContext scope, string sql, Dictionary<string, object> args, object options) => 
            Sync.dbExec(scope, sql, args, options);

        public List<string> dbTableNamesSync(ScriptScopeContext scope) => dbTableNamesSync(scope, null, null);
        public List<string> dbTableNamesSync(ScriptScopeContext scope, Dictionary<string, object> args) => dbTableNamesSync(scope, args, null);
        public List<string> dbTableNamesSync(ScriptScopeContext scope, Dictionary<string, object> args, object options) => 
            Sync.dbTableNames(scope, args, options);

        public List<KeyValuePair<string, long>> dbTableNamesWithRowCountsSync(ScriptScopeContext scope) => 
            dbTableNamesWithRowCountsSync(scope, null, null);
        public List<KeyValuePair<string, long>> dbTableNamesWithRowCountsSync(ScriptScopeContext scope, Dictionary<string, object> args) => 
            dbTableNamesWithRowCountsSync(scope, args, null);
        public List<KeyValuePair<string, long>> dbTableNamesWithRowCountsSync(ScriptScopeContext scope, Dictionary<string, object> args, object options) => 
            Sync.dbTableNamesWithRowCounts(scope, args, options);

        public string[] dbColumnNamesSync(ScriptScopeContext scope, string tableName) => dbColumnNamesSync(scope, tableName, null);
        public string[] dbColumnNamesSync(ScriptScopeContext scope, string tableName, object options) => 
            dbColumnsSync(scope, tableName, options).Select(x => x.ColumnName).ToArray();

        public ColumnSchema[] dbColumnsSync(ScriptScopeContext scope, string tableName) => dbColumnsSync(scope, tableName, null);
        public ColumnSchema[] dbColumnsSync(ScriptScopeContext scope, string tableName, object options) => 
            Sync.dbColumns(scope, tableName, options);

        public ColumnSchema[] dbDescSync(ScriptScopeContext scope, string sql) => dbDescSync(scope, sql, null);
        public ColumnSchema[] dbDescSync(ScriptScopeContext scope, string sql, object options) =>
            Sync.dbDesc(scope, sql, options);
    }
}