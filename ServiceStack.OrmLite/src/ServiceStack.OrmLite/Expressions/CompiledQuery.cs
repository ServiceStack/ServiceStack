using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading;

namespace ServiceStack.OrmLite
{
    /// <summary>
    /// Compiled queries generate their SQL once, then only bind the values of their arguments each time they're run
    /// </summary>
    public static class OrmLiteQuery
    {
        /// <summary>
        /// The most SQL statements a compiled query keeps for each dialect. A query has a statement for each
        /// combination of null arguments, the sizes of its collection arguments and the values of the arguments
        /// that are used outside its lambda expressions, e.g. q.Take(take). Queries that need more generate
        /// their SQL each time they're run.
        /// </summary>
        public static int MaxCachedStatements { get; set; } = 256;

        /// <summary>
        /// Compile a query that has no arguments, e.g:
        /// <para>OrmLiteQuery.Compile&lt;Order&gt;(q => q.Where(x => x.Status == OrderStatus.Open))</para>
        /// </summary>
        public static CompiledQuery<T> Compile<T>(Expression<Func<SqlExpression<T>, SqlExpression<T>>> query) =>
            new(query);

        /// <summary>
        /// Compile a query to generate its SQL once, e.g:
        /// <para>OrmLiteQuery.Compile&lt;Order, int&gt;((q, customerId) => q.Where(x => x.CustomerId == customerId))</para>
        /// </summary>
        public static CompiledQuery<T, T1> Compile<T, T1>(
            Expression<Func<SqlExpression<T>, T1, SqlExpression<T>>> query) => new(query);

        /// <summary>
        /// Compile a query to generate its SQL once
        /// </summary>
        public static CompiledQuery<T, T1, T2> Compile<T, T1, T2>(
            Expression<Func<SqlExpression<T>, T1, T2, SqlExpression<T>>> query) => new(query);

        /// <summary>
        /// Compile a query to generate its SQL once
        /// </summary>
        public static CompiledQuery<T, T1, T2, T3> Compile<T, T1, T2, T3>(
            Expression<Func<SqlExpression<T>, T1, T2, T3, SqlExpression<T>>> query) => new(query);

        /// <summary>
        /// Compile a query to generate its SQL once
        /// </summary>
        public static CompiledQuery<T, T1, T2, T3, T4> Compile<T, T1, T2, T3, T4>(
            Expression<Func<SqlExpression<T>, T1, T2, T3, T4, SqlExpression<T>>> query) => new(query);
    }

    /// <summary>
    /// A query with no arguments that generates its SQL once
    /// </summary>
    public sealed class CompiledQuery<T> : CompiledQueryBase<T>
    {
        internal CompiledQuery(LambdaExpression query) : base(query) {}

        /// <summary>
        /// The query to run with the connection, for use in other OrmLite APIs, e.g. db.Column&lt;int&gt;(query.Bind(db))
        /// </summary>
        public BoundQuery<T> Bind(IDbConnection db) => new(this, db, []);
    }

    /// <summary>
    /// A query that generates its SQL once, then only binds its argument each time it's run
    /// </summary>
    public sealed class CompiledQuery<T, T1> : CompiledQueryBase<T>
    {
        internal CompiledQuery(LambdaExpression query) : base(query) {}

        /// <summary>
        /// The query to run with the connection and argument, for use in other OrmLite APIs, e.g.
        /// db.Select&lt;OrderSummary&gt;(query.Bind(db, customerId))
        /// </summary>
        public BoundQuery<T> Bind(IDbConnection db, T1 arg1) => new(this, db, [arg1]);
    }

    /// <summary>
    /// A query that generates its SQL once, then only binds its arguments each time it's run
    /// </summary>
    public sealed class CompiledQuery<T, T1, T2> : CompiledQueryBase<T>
    {
        internal CompiledQuery(LambdaExpression query) : base(query) {}

        /// <summary>
        /// The query to run with the connection and arguments, for use in other OrmLite APIs
        /// </summary>
        public BoundQuery<T> Bind(IDbConnection db, T1 arg1, T2 arg2) => new(this, db, [arg1, arg2]);
    }

    /// <summary>
    /// A query that generates its SQL once, then only binds its arguments each time it's run
    /// </summary>
    public sealed class CompiledQuery<T, T1, T2, T3> : CompiledQueryBase<T>
    {
        internal CompiledQuery(LambdaExpression query) : base(query) {}

        /// <summary>
        /// The query to run with the connection and arguments, for use in other OrmLite APIs
        /// </summary>
        public BoundQuery<T> Bind(IDbConnection db, T1 arg1, T2 arg2, T3 arg3) => new(this, db, [arg1, arg2, arg3]);
    }

    /// <summary>
    /// A query that generates its SQL once, then only binds its arguments each time it's run
    /// </summary>
    public sealed class CompiledQuery<T, T1, T2, T3, T4> : CompiledQueryBase<T>
    {
        internal CompiledQuery(LambdaExpression query) : base(query) {}

        /// <summary>
        /// The query to run with the connection and arguments, for use in other OrmLite APIs
        /// </summary>
        public BoundQuery<T> Bind(IDbConnection db, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
            new(this, db, [arg1, arg2, arg3, arg4]);
    }

    /// <summary>
    /// A compiled query with the connection and arguments it's run with
    /// </summary>
    public sealed class BoundQuery<T> : ISqlExpression
    {
        private readonly CompiledQueryBase<T> query;
        private readonly IDbConnection db;
        private readonly object[] args;
        private List<IDbDataParameter> dbParams;

        internal BoundQuery(CompiledQueryBase<T> query, IDbConnection db, object[] args)
        {
            this.query = query ?? throw new ArgumentNullException(nameof(query));
            this.db = db ?? throw new ArgumentNullException(nameof(db));
            this.args = args;
        }

        /// <summary>
        /// The db params of the last SQL statement that was requested, or of the SELECT statement
        /// </summary>
        public List<IDbDataParameter> Params
        {
            get
            {
                if (dbParams == null)
                    ToSelectStatement();
                return dbParams;
            }
        }

        internal HashSet<string> OnlyFields { get; private set; }

        public string ToSelectStatement() => ToSelectStatement(QueryType.Select);

        public string ToSelectStatement(QueryType forType) =>
            GetSql(CompiledQueryBase<T>.StatementKind + (int)forType, null, CompiledQueryBase<T>.ToStatement(forType));

        public string SelectInto<TModel>() => SelectInto<TModel>(QueryType.Select);

        public string SelectInto<TModel>(QueryType forType) =>
            GetSql((int)forType, typeof(TModel), CompiledQueryBase<T>.Into<TModel>.Get(forType));

        public string ToCountStatement() =>
            GetSql(CompiledQueryBase<T>.CountKind, null, CompiledQueryBase<T>.ToCount);

        internal string ToExistsStatement() =>
            GetSql(CompiledQueryBase<T>.ExistsKind, null, CompiledQueryBase<T>.ToExists);

        private string GetSql(int kind, Type into, Func<SqlExpression<T>, string> toSql)
        {
            var sql = query.GetSql(db, args, kind, into, toSql, out var sqlParams, out var onlyFields);
            dbParams = sqlParams;
            OnlyFields = onlyFields;
            return sql;
        }

        /// <summary>
        /// The typed query with the values of the arguments, which generates its SQL each time, e.g. to change it
        /// or use it in APIs that need an SqlExpression
        /// </summary>
        public SqlExpression<T> ToQuery() => query.ToQuery(db, args);

        public string ToSetOperandStatement(string alias) => ((ISqlExpression)ToQuery()).ToSetOperandStatement(alias);

        public string Dump(bool includeParams) => ToQuery().Dump(includeParams);
    }

    /// <summary>
    /// Generates the SQL of a compiled query once for each dialect, then creates its db params from the arguments
    /// </summary>
    public abstract class CompiledQueryBase<T>
    {
        // SelectInto<TModel>(QueryType) statements use the QueryType, i.e. 0-2
        internal const int StatementKind = 3; // ToSelectStatement(QueryType)
        internal const int CountKind = 6;
        internal const int ExistsKind = 7;
        private const int Kinds = 8;

        internal static readonly Func<SqlExpression<T>, string> ToCount = q => q.ToCountStatement();
        internal static readonly Func<SqlExpression<T>, string> ToExists =
            q => q.CloneForExists().ToSelectStatement(QueryType.Scalar);

        private static readonly Func<SqlExpression<T>, string>[] ToStatements = [
            q => q.ToSelectStatement(QueryType.Select),
            q => q.ToSelectStatement(QueryType.Single),
            q => q.ToSelectStatement(QueryType.Scalar),
        ];

        internal static Func<SqlExpression<T>, string> ToStatement(QueryType forType) => ToStatements[(int)forType];

        internal static class Into<TModel>
        {
            private static readonly Func<SqlExpression<T>, string>[] ToSql = [
                q => q.SelectInto<TModel>(QueryType.Select),
                q => q.SelectInto<TModel>(QueryType.Single),
                q => q.SelectInto<TModel>(QueryType.Scalar),
            ];

            internal static Func<SqlExpression<T>, string> Get(QueryType forType) => ToSql[(int)forType];
        }

        private readonly LambdaExpression query;
        private readonly int argsCount;
        private Func<SqlExpression<T>, SqlExpression<T>> buildFn;
        private Delegate queryFn;
        private readonly ConcurrentDictionary<IOrmLiteDialectProvider, DialectState> states = new();
        private DialectState lastState;

        internal CompiledQueryBase(LambdaExpression query)
        {
            this.query = query ?? throw new ArgumentNullException(nameof(query));
            argsCount = query.Parameters.Count - 1;
        }

        /// <summary>
        /// The number of SQL statements that have been generated and are reused
        /// </summary>
        public int CachedStatements
        {
            get
            {
                var count = 0;
                foreach (var state in states.Values)
                    count += state.Cached;
                return count;
            }
        }

        /// <summary>
        /// Why the query last had to generate its SQL instead of reusing it, or null if it never had to
        /// </summary>
        public string NotCachedReason { get; private set; }

        // The query's lambda with its arguments replaced by values that are read from the build that's running
        private Func<SqlExpression<T>, SqlExpression<T>> BuildFn
        {
            get
            {
                if (buildFn != null)
                    return buildFn;

                var replace = new Dictionary<ParameterExpression, Expression>();
                for (var i = 0; i < argsCount; i++)
                {
                    var param = query.Parameters[i + 1];
                    var argType = typeof(CompiledQueryArg<>).MakeGenericType(param.Type);
                    var arg = Activator.CreateInstance(argType, i);
                    replace[param] = Expression.Property(Expression.Constant(arg, argType), nameof(CompiledQueryArg<int>.Value));
                }
                var body = new CompiledQueryBuild.ReplaceParameters(replace).Visit(query.Body);
                var fn = Expression.Lambda<Func<SqlExpression<T>, SqlExpression<T>>>(body, query.Parameters[0]).Compile();
                return buildFn = fn;
            }
        }

        private DialectState GetState(IOrmLiteDialectProvider dialect)
        {
            var state = lastState;
            if (state != null && ReferenceEquals(state.Dialect, dialect))
                return state;
            return lastState = states.GetOrAdd(dialect, x => new DialectState(x, argsCount));
        }

        /// <summary>
        /// The typed query with the values of the arguments, which generates its SQL each time
        /// </summary>
        internal SqlExpression<T> ToQuery(IDbConnection db, object[] args)
        {
            // Not built with BuildFn, as the query can be used after it's built, e.g. to generate its SQL
            var fn = queryFn ??= query.Compile();
            var fnArgs = new object[args.Length + 1];
            fnArgs[0] = db.From<T>();
            args.CopyTo(fnArgs, 1);
            try
            {
                return (SqlExpression<T>)fn.DynamicInvoke(fnArgs)
                    ?? throw new InvalidOperationException("The compiled query didn't return a query");
            }
            catch (System.Reflection.TargetInvocationException e) when (e.InnerException != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                throw;
            }
        }

        private SqlExpression<T> Run(CompiledQueryBuild build, IDbConnection db, Func<SqlExpression<T>, string> toSql,
            out string sql)
        {
            var hold = CompiledQueryBuild.Current;
            CompiledQueryBuild.Current = build;
            try
            {
                var q = BuildFn(db.From<T>())
                    ?? throw new InvalidOperationException("The compiled query didn't return a query");
                // Some SQL is generated with the statement, e.g. recursive CTEs
                sql = toSql?.Invoke(q);
                return q;
            }
            finally
            {
                CompiledQueryBuild.Current = hold;
            }
        }

        internal string GetSql(IDbConnection db, object[] args, int kind, Type into,
            Func<SqlExpression<T>, string> toSql, out List<IDbDataParameter> sqlParams, out HashSet<string> onlyFields)
        {
            // Filters have values of their own that aren't arguments, e.g. the tenant of the request, so a query is
            // only reused when none of the connection's filters apply to its tables. Write rules don't change queries.
            var filters = db.GetFilters();
            if (!filters.MayFilter(typeof(T))
                && OrmLiteConfig.SqlExpressionSelectFilter == null && OrmLiteConfig.SqlExpressionInitFilter == null)
            {
                var state = GetState(db.GetDialectProvider());
                List<object>[] lists = null;
                var key = MakeKey(state.Modes, args, ref lists);
                var isDefault = key == null && (into == null || into == typeof(T));
                Statement statement;
                if (isDefault)
                    statement = state.Defaults[kind];
                else
                    state.Statements.TryGetValue(new StatementKey(key, kind, into), out statement);

                if (statement == null && !state.IsFull)
                    return Compile(state, db, filters, args, kind, into, toSql, out sqlParams, out onlyFields);

                if (statement?.Sql != null && !IsFiltered(statement.FilterTables, filters))
                {
                    sqlParams = statement.CreateParams(state, args, lists);
                    if (sqlParams != null)
                    {
                        onlyFields = statement.OnlyFields;
                        return statement.Sql;
                    }
                }
            }

            var q = Run(new CompiledQueryBuild(args, null, bind: false), db, toSql, out var sql);
            sqlParams = q.Params;
            onlyFields = q.OnlyFields;
            return sql;
        }

        // Whether the connection has filters for any of the tables
        private static bool IsFiltered(IEnumerable<Type> tables, OrmLiteConnectionFilters filters)
        {
            if (tables == null)
                return false;
            foreach (var table in tables)
            {
                if (filters.MayFilter(table))
                    return true;
            }
            return false;
        }

        private static readonly object NullArg = new();
        private static readonly object NotAList = new();

        // What the SQL of a query depends on: which arguments are null, the sizes of its collections and the values
        // of the arguments that are used in its SQL. Returns null when it doesn't depend on its arguments.
        private static VariantKey MakeKey(ArgMode[] modes, object[] args, ref List<object>[] lists)
        {
            object[] parts = null;
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                object part = null;
                if (arg == null)
                {
                    part = NullArg;
                }
                else if (modes[i] == ArgMode.Value)
                {
                    part = arg;
                }
                else if (modes[i] == ArgMode.Count)
                {
                    var list = CompiledQueryBuild.ToList(arg);
                    (lists ??= new List<object>[args.Length])[i] = list;
                    part = list != null ? list.Count : NotAList;
                }
                if (part != null)
                    (parts ??= new object[args.Length])[i] = part;
            }
            return parts != null ? new VariantKey(parts) : null;
        }

        // Generates the SQL as it's normally generated, to run the query with, and again without the values of
        // its arguments. The second is kept for the next time it's run if it's the same as the first.
        private string Compile(DialectState state, IDbConnection db, OrmLiteConnectionFilters filters, object[] args,
            int kind, Type into, Func<SqlExpression<T>, string> toSql, out List<IDbDataParameter> sqlParams,
            out HashSet<string> onlyFields)
        {
            lock (state)
            {
                var normal = new CompiledQueryBuild(args, null, bind: false);
                var q = Run(normal, db, toSql, out var sql);
                sqlParams = q.Params;
                onlyFields = q.OnlyFields;

                // A joined or sub query table is filtered on this connection, so its SQL has the filter's values
                if (IsFiltered(normal.FilterTables, filters))
                    return sql;

                Statement statement = null;
                string reason = null;
                var bound = new CompiledQueryBuild(args, state.Modes, bind: true);
                try
                {
                    var boundQuery = Run(bound, db, toSql, out var boundSql);
                    if (bound.HasNullValue)
                        return sql; // SQL for a null isn't SQL for a value, generate it again next time
                    statement = Statement.Create(state, bound, boundQuery, boundSql, sql, q.Params, out reason);
                    if (statement != null && (normal.FilterTables != null || bound.FilterTables != null))
                    {
                        // Kept so the statement isn't reused on connections that filter any of its tables
                        var tables = new HashSet<Type>(normal.FilterTables ?? []);
                        tables.UnionWith(bound.FilterTables ?? []);
                        statement.FilterTables = tables.ToArray();
                    }
                }
                catch (Exception e)
                {
                    reason = $"The SQL can't be generated without the values of its arguments: {e.Message}";
                }

                // Arguments that were found to be used in the SQL are part of what the statement is kept for
                var modes = state.Modes;
                for (var i = 0; i < args.Length; i++)
                {
                    var mode = normal.UsedInSql[i] || bound.UsedInSql[i]
                        ? ArgMode.Value
                        : bound.UsedAsList[i] ? ArgMode.Count : ArgMode.None;
                    if (mode > modes[i])
                    {
                        if (ReferenceEquals(modes, state.Modes))
                            modes = (ArgMode[])modes.Clone();
                        modes[i] = mode;
                    }
                }
                state.Modes = modes;

                if (statement == null)
                {
                    NotCachedReason = reason;
                    statement = Statement.NotCached;
                }

                List<object>[] lists = null;
                var key = MakeKey(modes, args, ref lists);
                if (key == null && (into == null || into == typeof(T)))
                {
                    if (state.Defaults[kind] == null && statement.Sql != null)
                        state.Cached++;
                    state.Defaults[kind] = statement;
                }
                else if (state.Statements.Count < OrmLiteQuery.MaxCachedStatements)
                {
                    var statementKey = new StatementKey(key, kind, into);
                    if (!state.Statements.ContainsKey(statementKey) && statement.Sql != null)
                        state.Cached++;
                    state.Statements[statementKey] = statement;
                }
                else
                {
                    state.IsFull = true;
                    NotCachedReason = $"The query has more than {OrmLiteQuery.MaxCachedStatements} SQL statements. " +
                        "Its SQL changes with the values of arguments that are used outside its lambda expressions, " +
                        "which arguments are null and the sizes of its collections.";
                }
                return sql;
            }
        }

        internal sealed class DialectState(IOrmLiteDialectProvider dialect, int argsCount)
        {
            internal readonly IOrmLiteDialectProvider Dialect = dialect;
            internal readonly bool IsMySqlConnector = dialect.IsMySqlConnector();
            internal volatile ArgMode[] Modes = new ArgMode[argsCount];
            internal readonly Statement[] Defaults = new Statement[Kinds];
            internal readonly ConcurrentDictionary<StatementKey, Statement> Statements = new();
            internal volatile bool IsFull;
            internal int Cached;

            // As SqlExpression.AddParam() creates them
            internal IDbDataParameter CreateParam(string name, object value)
            {
                var p = Dialect.CreateParam();
                p.ParameterName = name;
                p.Direction = ParameterDirection.Input;
                if (!IsMySqlConnector)
                    p.SourceVersion = DataRowVersion.Default;
                Dialect.ConfigureParam(p, value, null);
                Dialect.InitQueryParam(p);
                return p;
            }
        }

        internal sealed class VariantKey : IEquatable<VariantKey>
        {
            private readonly object[] parts;
            private readonly int hash;

            internal VariantKey(object[] parts)
            {
                this.parts = parts;
                var h = 17;
                foreach (var part in parts)
                    h = h * 31 + (part?.GetHashCode() ?? 0);
                hash = h;
            }

            public bool Equals(VariantKey other)
            {
                if (other == null || other.hash != hash || other.parts.Length != parts.Length)
                    return false;
                for (var i = 0; i < parts.Length; i++)
                {
                    if (!Equals(parts[i], other.parts[i]))
                        return false;
                }
                return true;
            }

            public override bool Equals(object obj) => Equals(obj as VariantKey);
            public override int GetHashCode() => hash;
        }

        internal readonly struct StatementKey(VariantKey key, int kind, Type into) : IEquatable<StatementKey>
        {
            private readonly VariantKey key = key;
            private readonly int kind = kind;
            private readonly Type into = into;

            public bool Equals(StatementKey other) =>
                kind == other.kind && into == other.into && Equals(key, other.key);

            public override bool Equals(object obj) => obj is StatementKey other && Equals(other);

            public override int GetHashCode() =>
                ((key?.GetHashCode() ?? 0) * 31 + kind) * 31 + (into?.GetHashCode() ?? 0);
        }

        internal sealed class ParamTemplate
        {
            internal IDbDataParameter Param;       // a param that's the same each time
            internal string Name;                  // or a param for the value of an argument
            internal Func<object[], List<object>[], object> GetValue;
            internal Type ValueType;
            internal int? Size;
            internal DbType? DbType;
            internal byte? Precision;
            internal byte? Scale;
        }

        internal sealed class Statement
        {
            // Kept so SQL that can't be reused isn't generated twice each time
            internal static readonly Statement NotCached = new();

            internal string Sql;
            internal HashSet<string> OnlyFields;
            internal Type[] FilterTables; // the tables filters are applied to, without filters on this connection
            private ParamTemplate[] templates;
            private int[] listSizes; // of the collection arguments the SQL has db params for

            // Returns null if the values can't be used with the SQL, e.g. a null needs IS NULL
            internal List<IDbDataParameter> CreateParams(DialectState state, object[] args, List<object>[] lists)
            {
                if (listSizes != null)
                {
                    lists ??= new List<object>[args.Length];
                    for (var i = 0; i < listSizes.Length; i++)
                    {
                        if (listSizes[i] < 0)
                            continue;
                        var list = lists[i] ??= CompiledQueryBuild.ToList(args[i]);
                        if (list == null || list.Count != listSizes[i])
                            return null;
                    }
                }

                var to = new List<IDbDataParameter>(templates.Length);
                foreach (var template in templates)
                {
                    if (template.Param != null)
                    {
                        var p = state.Dialect.CreateParam();
                        p.PopulateWith(template.Param);
                        to.Add(p);
                        continue;
                    }

                    var value = template.GetValue(args, lists);
                    if (value == null || value.GetType() != template.ValueType)
                        return null;

                    to.Add(CreateParam(state, template, value));
                }
                return to;
            }

            private static IDbDataParameter CreateParam(DialectState state, ParamTemplate template, object value)
            {
                var p = state.CreateParam(template.Name, value);
                if (template.DbType != null)
                    p.DbType = template.DbType.Value;
                if (template.Size != null)
                    p.Size = template.Size.Value;
                if (template.Precision != null)
                    p.Precision = template.Precision.Value;
                if (template.Scale != null)
                    p.Scale = template.Scale.Value;
                return p;
            }

            // Returns null with the reason if the SQL and db params aren't what's normally generated
            internal static Statement Create(DialectState state, CompiledQueryBuild build, SqlExpression<T> q,
                string sql, string normalSql, List<IDbDataParameter> normalParams, out string reason)
            {
                reason = null;
                if (sql != normalSql)
                {
                    reason = "The SQL changes with the values of its arguments";
                    return null;
                }

                var dbParams = q.Params;
                if (dbParams.Count != normalParams.Count)
                {
                    reason = "The db params change with the values of its arguments";
                    return null;
                }

                var templates = new ParamTemplate[dbParams.Count];
                var bound = 0;
                for (var i = 0; i < dbParams.Count; i++)
                {
                    var p = dbParams[i];
                    var normal = normalParams[i];
                    if (p.ParameterName != normal.ParameterName || p.DbType != normal.DbType
                        || !ValueEquals(p.Value, normal.Value))
                    {
                        reason = $"The db param {p.ParameterName} isn't the same each time, " +
                                 "values that change need to be arguments of the query";
                        return null;
                    }

                    if (!build.Bound.TryGetValue(p, out var value))
                    {
                        templates[i] = new ParamTemplate { Param = p };
                        continue;
                    }

                    bound++;
                    var template = new ParamTemplate {
                        Name = p.ParameterName,
                        GetValue = value.GetValue,
                        ValueType = value.Sample.GetType(),
                    };
                    // Changes made to the db param after it was created are made each time
                    var created = state.CreateParam(p.ParameterName, value.Sample);
                    if (!ValueEquals(created.Value, p.Value))
                    {
                        reason = $"The db param {p.ParameterName} isn't created from the value of its argument";
                        return null;
                    }
                    if (created.DbType != p.DbType)
                        template.DbType = p.DbType;
                    if (created.Size != p.Size)
                        template.Size = p.Size;
                    if (created.Precision != p.Precision)
                        template.Precision = p.Precision;
                    if (created.Scale != p.Scale)
                        template.Scale = p.Scale;
                    templates[i] = template;
                }

                if (bound != build.Bound.Count)
                {
                    reason = "The db params of its arguments are changed by the query";
                    return null;
                }

                int[] listSizes = null;
                for (var i = 0; i < build.UsedAsList.Length; i++)
                {
                    if (!build.UsedAsList[i])
                        continue;
                    if (listSizes == null)
                    {
                        listSizes = new int[build.UsedAsList.Length];
                        for (var j = 0; j < listSizes.Length; j++)
                            listSizes[j] = -1;
                    }
                    listSizes[i] = build.Lists[i].Count;
                }

                return new Statement {
                    Sql = sql,
                    OnlyFields = q.OnlyFields,
                    templates = templates,
                    listSizes = listSizes,
                };
            }

            private static bool ValueEquals(object a, object b)
            {
                if (Equals(a, b))
                    return true;
                if (a is not Array x || b is not Array y || x.Length != y.Length || x.Rank != 1 || y.Rank != 1)
                    return false;
                for (var i = 0; i < x.Length; i++)
                {
                    if (!Equals(x.GetValue(i), y.GetValue(i)))
                        return false;
                }
                return true;
            }
        }
    }

    internal enum ArgMode
    {
        None,
        Count, // a collection, the SQL has a db param for each of its values
        Value, // the SQL was generated with its value
    }

    /// <summary>
    /// An argument of a compiled query, whose value is read from the query that's being built
    /// </summary>
    internal abstract class CompiledQueryArg(int index)
    {
        internal readonly int Index = index;
    }

    internal sealed class CompiledQueryArg<TArg>(int index) : CompiledQueryArg(index)
    {
        public TArg Value
        {
            get
            {
                var value = CompiledQueryBuild.ReadArg(Index);
                return value == null ? default : (TArg)value;
            }
        }
    }

    /// <summary>
    /// A value from the arguments of a compiled query, which becomes a db param that's created each time it's run
    /// </summary>
    internal sealed class CompiledValue
    {
        internal readonly Func<object[], List<object>[], object> GetValue;
        internal readonly object Sample; // its value in the query that's being built, never null
        internal readonly int ArgIndex;  // when it's the argument itself, otherwise -1

        internal CompiledValue(Func<object[], List<object>[], object> getValue, object sample, int argIndex = -1)
        {
            GetValue = getValue;
            Sample = sample;
            ArgIndex = argIndex;
        }

        // The value after it's converted as it normally is, or null if that's null
        internal CompiledValue Map(Func<object, object> convert)
        {
            var sample = convert(Sample);
            if (sample == null)
            {
                CompiledQueryBuild.Current.HasNullValue = true;
                return null;
            }
            var getValue = GetValue;
            return new CompiledValue((args, lists) => getValue(args, lists) is { } value ? convert(value) : null, sample);
        }

        // SQL that uses this instead of a db param isn't the SQL that's normally generated, so it's never kept
        public override string ToString() => "{CompiledQueryArg}";
    }

    /// <summary>
    /// The compiled query that's being built on this thread
    /// </summary>
    internal sealed class CompiledQueryBuild
    {
        [ThreadStatic] internal static CompiledQueryBuild Current;

        // The tables the query's SQL would have the connection's filters for, including joined and sub query tables
        internal HashSet<Type> FilterTables;

        /// <summary>
        /// Record a table the connection's filters are applied to, while a compiled query is built
        /// </summary>
        internal static void FilterTable(Type tableType)
        {
            if (Current is { } build && tableType != null)
                (build.FilterTables ??= []).Add(tableType);
        }

        internal readonly object[] Args;
        internal readonly bool[] UsedInSql;  // arguments whose value was read to generate the SQL
        internal readonly bool[] UsedAsList; // arguments with a db param for each of their values
        internal readonly List<object>[] Lists;
        internal readonly Dictionary<IDbDataParameter, CompiledValue> Bound;
        internal bool HasNullValue; // a value from the arguments was null, which has its own SQL
        private readonly bool[] useValue;
        private readonly bool bind;
        private int evaluating;

        internal CompiledQueryBuild(object[] args, ArgMode[] modes, bool bind)
        {
            Args = args;
            this.bind = bind;
            UsedInSql = new bool[args.Length];
            UsedAsList = new bool[args.Length];
            Lists = new List<object>[args.Length];
            useValue = new bool[args.Length];
            if (bind)
            {
                Bound = new Dictionary<IDbDataParameter, CompiledValue>(ReferenceComparer.Instance);
                for (var i = 0; i < args.Length; i++)
                    useValue[i] = args[i] == null || modes[i] == ArgMode.Value;
            }
        }

        internal static object ReadArg(int index)
        {
            var build = Current ?? throw new InvalidOperationException(
                "The arguments of a compiled query can only be used in its query");
            // Read where its value isn't expected, its value is in the SQL
            if (build.evaluating == 0)
                build.UsedInSql[index] = true;
            return build.Args[index];
        }

        internal static List<object> ToList(object value) =>
            value is IEnumerable list and not string ? Sql.Flatten(list) : null;

        private object EvaluateValue(Expression expression)
        {
            evaluating++;
            try
            {
                return CachedExpressionCompiler.Evaluate(expression);
            }
            finally
            {
                evaluating--;
            }
        }

        // The value of an expression, or how to get it from the arguments if it uses them
        internal object Evaluate(Expression expression)
        {
            if (!bind)
                return EvaluateValue(expression);

            var finder = new FindArgs(useValue);
            finder.Visit(expression);
            if (!finder.UsesArg)
                return EvaluateValue(expression);

            if (expression is MemberExpression { Expression: ConstantExpression { Value: CompiledQueryArg arg } })
            {
                var index = arg.Index;
                return new CompiledValue((args, _) => args[index], Args[index], index);
            }

            var argsParam = Expression.Parameter(typeof(object[]), "args");
            var body = new ReplaceArgs(argsParam).Visit(expression);
            var getValue = Expression.Lambda<Func<object[], object>>(
                Expression.Convert(body, typeof(object)), argsParam).Compile();
            var sample = getValue(Args);
            if (sample == null)
            {
                HasNullValue = true;
                return null;
            }
            return new CompiledValue((args, _) => getValue(args), sample);
        }

        internal void Bind(IDbDataParameter dbParam, CompiledValue value) => Bound[dbParam] = value;

        // A db param for each value of a collection, which has to be an argument to know its size each time
        internal List<object> ToInArgs(CompiledValue value)
        {
            var index = value.ArgIndex;
            var list = index >= 0 ? Lists[index] ??= ToList(Args[index]) : null;
            if (list == null)
                throw new NotSupportedException("A collection has to be an argument of the compiled query");

            UsedAsList[index] = true;
            var to = new List<object>(list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                var item = i;
                to.Add(new CompiledValue((_, lists) => lists[index][item], list[i]));
            }
            return to;
        }

        private sealed class ReferenceComparer : IEqualityComparer<IDbDataParameter>
        {
            internal static readonly ReferenceComparer Instance = new();
            public bool Equals(IDbDataParameter x, IDbDataParameter y) => ReferenceEquals(x, y);
            public int GetHashCode(IDbDataParameter obj) =>
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }

        internal sealed class ReplaceParameters(Dictionary<ParameterExpression, Expression> replace) : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node) =>
                replace.TryGetValue(node, out var to) ? to : node;
        }

        // Whether an expression uses an argument that becomes a db param
        private sealed class FindArgs(bool[] useValue) : ExpressionVisitor
        {
            internal bool UsesArg;

            protected override Expression VisitConstant(ConstantExpression node)
            {
                if (node.Value is CompiledQueryArg arg && !useValue[arg.Index])
                    UsesArg = true;
                return node;
            }
        }

        private sealed class ReplaceArgs(ParameterExpression argsParam) : ExpressionVisitor
        {
            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression is ConstantExpression { Value: CompiledQueryArg arg })
                {
                    return Expression.Convert(
                        Expression.ArrayIndex(argsParam, Expression.Constant(arg.Index)), node.Type);
                }
                return base.VisitMember(node);
            }
        }
    }
}
