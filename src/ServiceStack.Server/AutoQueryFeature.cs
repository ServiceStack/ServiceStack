using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Threading;

using Funq;
using ServiceStack.MiniProfiler;
using ServiceStack.Reflection;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.Data;
using ServiceStack.Logging;
using ServiceStack.OrmLite;

namespace ServiceStack
{
    public delegate void QueryFilterDelegate(ISqlExpression q, IQueryDb dto, IRequest req);

    public class QueryDbFilterContext
    {
        public IDbConnection Db { get; set; }
        public List<Command> Commands { get; set; }
        public IQueryDb Dto { get; set; }
        public ISqlExpression SqlExpression { get; set; }
        public IQueryResponse Response { get; set; }

        [Obsolete("Use Dto")]
        public IQueryDb Request { get { return Dto; } }
    }

    public class AutoQueryFeature : IPlugin, IPostInitPlugin
    {
        public HashSet<string> IgnoreProperties { get; set; }
        public HashSet<string> IllegalSqlFragmentTokens { get; set; }
        public HashSet<Assembly> LoadFromAssemblies { get; set; } 
        public int? MaxLimit { get; set; }
        public string UseNamedConnection { get; set; }
        public bool StripUpperInLike { get; set; }
        public bool IncludeTotal { get; set; }
        public bool EnableUntypedQueries { get; set; }
        public bool EnableRawSqlFilters { get; set; }
        public bool EnableAutoQueryViewer { get; set; }
        public bool OrderByPrimaryKeyOnPagedQuery { get; set; }
        public Type AutoQueryServiceBaseType { get; set; }
        public Dictionary<Type, QueryFilterDelegate> QueryFilters { get; set; }
        public List<Action<QueryDbFilterContext>> ResponseFilters { get; set; }
        public Action<Type, TypeBuilder, MethodBuilder, ILGenerator> GenerateServiceFilter { get; set; }

        public const string GreaterThanOrEqualFormat = "{Field} >= {Value}";
        public const string GreaterThanFormat =        "{Field} > {Value}";
        public const string LessThanFormat =           "{Field} < {Value}";
        public const string LessThanOrEqualFormat =    "{Field} <= {Value}";
        public const string NotEqualFormat =           "{Field} <> {Value}";
        public const string CaseSensitiveLikeFormat =  "{Field} LIKE {Value}";
        public const string CaseInsensitiveLikeFormat = "UPPER({Field}) LIKE UPPER({Value})";

        public Dictionary<string, string> ImplicitConventions = new Dictionary<string, string> 
        {
            {"%Above%",         GreaterThanFormat},
            {"Begin%",          GreaterThanFormat},
            {"%Beyond%",        GreaterThanFormat},
            {"%Over%",          GreaterThanFormat},
            {"%OlderThan",      GreaterThanFormat},
            {"%After%",         GreaterThanFormat},
            {"OnOrAfter%",      GreaterThanOrEqualFormat},
            {"%From%",          GreaterThanOrEqualFormat},
            {"Since%",          GreaterThanOrEqualFormat},
            {"Start%",          GreaterThanOrEqualFormat},
            {"%Higher%",        GreaterThanOrEqualFormat},
            {">%",              GreaterThanOrEqualFormat},
            {"%>",              GreaterThanFormat},
            {"%!",              NotEqualFormat},

            {"%GreaterThanOrEqualTo%", GreaterThanOrEqualFormat},
            {"%GreaterThan%",          GreaterThanFormat},
            {"%LessThan%",             LessThanFormat},
            {"%LessThanOrEqualTo%",    LessThanOrEqualFormat},
            {"%NotEqualTo",            NotEqualFormat},

            {"Behind%",         LessThanFormat},
            {"%Below%",         LessThanFormat},
            {"%Under%",         LessThanFormat},
            {"%Lower%",         LessThanFormat},
            {"%Before%",        LessThanFormat},
            {"%YoungerThan",    LessThanFormat},
            {"OnOrBefore%",     LessThanOrEqualFormat},
            {"End%",            LessThanOrEqualFormat},
            {"Stop%",           LessThanOrEqualFormat},
            {"To%",             LessThanOrEqualFormat},
            {"Until%",          LessThanOrEqualFormat},
            {"%<",              LessThanOrEqualFormat},
            {"<%",              LessThanFormat},

            {"%Like%",          CaseInsensitiveLikeFormat },
            {"%In",             "{Field} IN ({Values})"},
            {"%Ids",            "{Field} IN ({Values})"},
            {"%Between%",       "{Field} BETWEEN {Value1} AND {Value2}"},
        };

        public Dictionary<string, QueryDbFieldAttribute> StartsWithConventions =
            new Dictionary<string, QueryDbFieldAttribute>();

        public Dictionary<string, QueryDbFieldAttribute> EndsWithConventions = new Dictionary<string, QueryDbFieldAttribute>
        {
            { "StartsWith", new QueryDbFieldAttribute { Template = CaseInsensitiveLikeFormat, ValueFormat = "{0}%" }},
            { "Contains", new QueryDbFieldAttribute { Template = CaseInsensitiveLikeFormat, ValueFormat = "%{0}%" }},
            { "EndsWith", new QueryDbFieldAttribute { Template = CaseInsensitiveLikeFormat, ValueFormat = "%{0}" }},
        };

        public AutoQueryFeature()
        {
            IgnoreProperties = new HashSet<string>(new[] { "Skip", "Take", "OrderBy", "OrderByDesc", "Fields", "_select", "_from", "_join", "_where" }, 
                StringComparer.OrdinalIgnoreCase);
            IllegalSqlFragmentTokens = new HashSet<string>();
            AutoQueryServiceBaseType = typeof(AutoQueryServiceBase);
            QueryFilters = new Dictionary<Type, QueryFilterDelegate>();
            ResponseFilters = new List<Action<QueryDbFilterContext>> { IncludeAggregates };
            IncludeTotal = true;
            EnableUntypedQueries = true;
            EnableAutoQueryViewer = true;
            OrderByPrimaryKeyOnPagedQuery = true;
            StripUpperInLike = OrmLiteConfig.StripUpperInLike;
            LoadFromAssemblies = new HashSet<Assembly>();
        }

        public void Register(IAppHost appHost)
        {
            if (StripUpperInLike)
            {
                string convention;
                if (ImplicitConventions.TryGetValue("%Like%", out convention) && convention == CaseInsensitiveLikeFormat)
                    ImplicitConventions["%Like%"] = CaseSensitiveLikeFormat;

                foreach (var attr in EndsWithConventions)
                {
                    if (attr.Value.Template == CaseInsensitiveLikeFormat)
                        attr.Value.Template = CaseSensitiveLikeFormat;
                }
            }

            foreach (var entry in ImplicitConventions)
            {
                var key = entry.Key.Trim('%');
                var fmt = entry.Value;
                var query = new QueryDbFieldAttribute { Template = fmt }.Init();
                if (entry.Key.EndsWith("%"))
                    StartsWithConventions[key] = query;
                if (entry.Key.StartsWith("%"))
                    EndsWithConventions[key] = query;
            }

            appHost.GetContainer().Register<IAutoQueryDb>(c => new AutoQuery
                {
                    IgnoreProperties = IgnoreProperties,
                    IllegalSqlFragmentTokens = IllegalSqlFragmentTokens,
                    MaxLimit = MaxLimit,
                    IncludeTotal = IncludeTotal,
                    EnableUntypedQueries = EnableUntypedQueries,
                    EnableSqlFilters = EnableRawSqlFilters,
                    OrderByPrimaryKeyOnLimitQuery = OrderByPrimaryKeyOnPagedQuery,
                    QueryFilters = QueryFilters,
                    ResponseFilters = ResponseFilters,
                    StartsWithConventions = StartsWithConventions,
                    EndsWithConventions = EndsWithConventions,
                    UseNamedConnection = UseNamedConnection,
                })
                .ReusedWithin(ReuseScope.None);

            appHost.Metadata.GetOperationAssemblies()
                .Each(x => LoadFromAssemblies.Add(x));

            ((ServiceStackHost)appHost).ServiceAssemblies.Each(x => {
                if (!LoadFromAssemblies.Contains(x))
                    LoadFromAssemblies.Add(x);
            });

            if (EnableAutoQueryViewer && appHost.GetPlugin<AutoQueryMetadataFeature>() == null)
                appHost.LoadPlugin(new AutoQueryMetadataFeature { MaxLimit = MaxLimit });
        }

        public void AfterPluginsLoaded(IAppHost appHost)
        {
            var scannedTypes = LoadFromAssemblies.SelectMany(x => x.GetTypes());

            var misingRequestTypes = scannedTypes
                .Where(x => x.HasInterface(typeof(IQueryDb)))
                .Where(x => !appHost.Metadata.OperationsMap.ContainsKey(x))
                .ToList();

            if (misingRequestTypes.Count == 0)
                return;

            var serviceType = GenerateMissingServices(misingRequestTypes);
            appHost.RegisterService(serviceType);
        }

        Type GenerateMissingServices(IEnumerable<Type> misingRequestTypes)
        {
            var assemblyName = new AssemblyName { Name = "tmpAssembly" };
            var typeBuilder =
                AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
                .DefineDynamicModule("tmpModule")
                .DefineType("__AutoQueryServices",
                    TypeAttributes.Public | TypeAttributes.Class,
                    AutoQueryServiceBaseType);

            foreach (var requestType in misingRequestTypes)
            {
                var genericDef = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryDb<,>));
                var hasExplicitInto = genericDef != null;
                if (genericDef == null)
                    genericDef = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryDb<>));
                if (genericDef == null)
                    continue;

                var method = typeBuilder.DefineMethod("Any", MethodAttributes.Public | MethodAttributes.Virtual,
                    CallingConventions.Standard,
                    returnType: typeof(object),
                    parameterTypes: new[] { requestType });

                var il = method.GetILGenerator();
                
                if (GenerateServiceFilter != null)
                    GenerateServiceFilter(requestType, typeBuilder, method, il);

                var genericArgs = genericDef.GetGenericArguments();
                var mi = AutoQueryServiceBaseType.GetMethods()
                    .First(x => x.GetGenericArguments().Length == genericArgs.Length);
                var genericMi = mi.MakeGenericMethod(genericArgs);

                var queryType = hasExplicitInto
                    ? typeof(IQueryDb<,>).MakeGenericType(genericArgs)
                    : typeof(IQueryDb<>).MakeGenericType(genericArgs);

                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Box, queryType);
                il.Emit(OpCodes.Callvirt, genericMi);
                il.Emit(OpCodes.Ret);
            }

            var servicesType = typeBuilder.CreateTypeInfo().AsType();
            return servicesType;
        }

        public AutoQueryFeature RegisterQueryFilter<Request, From>(Action<SqlExpression<From>, Request, IRequest> filterFn)
        {
            QueryFilters[typeof(Request)] = (q, dto, req) =>
                filterFn((SqlExpression<From>)q, (Request)dto, req);

            return this;
        }

        public HashSet<string> SqlAggregateFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AVG", "COUNT", "FIRST", "LAST", "MAX", "MIN", "SUM"
        };

        public void IncludeAggregates(QueryDbFilterContext ctx)
        {
            var commands = ctx.Commands;
            if (commands.Count == 0)
                return;

            var q = ctx.SqlExpression.GetUntypedSqlExpression()
                .Clone()
                .ClearLimits()
                .OrderBy();

            var aggregateCommands = new List<Command>();
            foreach (var cmd in commands)
            {
                if (!SqlAggregateFunctions.Contains(cmd.Name))
                    continue;

                aggregateCommands.Add(cmd);

                if (cmd.Args.Count == 0)
                    cmd.Args.Add("*");

                cmd.Original = cmd.ToString();

                var hasAlias = !string.IsNullOrWhiteSpace(cmd.Suffix);

                for (var i = 0; i < cmd.Args.Count; i++)
                {
                    var arg = cmd.Args[i];

                    string modifier = "";
                    if (arg.StartsWithIgnoreCase("DISTINCT "))
                    {
                        var argParts = arg.SplitOnFirst(' ');
                        modifier = argParts[0] + " ";
                        arg = argParts[1];
                    }

                    var fieldRef = q.FirstMatchingField(arg);
                    if (fieldRef != null)
                    {
                        //To return predictable aliases, if it's primary table don't fully qualify name
                        var fieldName = fieldRef.Item2.FieldName;
                        var needsRewrite = !fieldName.EqualsIgnoreCase(q.DialectProvider.NamingStrategy.GetColumnName(fieldName)); 
                        if (fieldRef.Item1 != q.ModelDef || fieldRef.Item2.Alias != null || needsRewrite || hasAlias)
                        {
                            cmd.Args[i] = modifier + q.DialectProvider.GetQuotedColumnName(fieldRef.Item1, fieldRef.Item2);
                        }
                    }
                    else
                    {
                        double d;
                        if (arg != "*" && !double.TryParse(arg, out d))
                        {
                            cmd.Args[i] = "{0}".SqlFmt(arg);
                        }
                    }
                }

                if (hasAlias)
                {
                    var alias = cmd.Suffix.TrimStart();
                    if (alias.StartsWithIgnoreCase("as "))
                        alias = alias.Substring("as ".Length);

                    cmd.Suffix = " " + alias.SafeVarName();
                }
                else
                {
                    cmd.Suffix = " " + q.DialectProvider.GetQuotedName(cmd.Original);
                }
            }

            var selectSql = string.Join(", ", aggregateCommands.Map(x => x.ToString()));
            q.UnsafeSelect(selectSql);

            var rows = ctx.Db.Select<Dictionary<string, object>>(q);
            var row = rows.FirstOrDefault();

            foreach (var key in row.Keys)
            {
                ctx.Response.Meta[key] = row[key].ToString();
            }

            ctx.Commands.RemoveAll(aggregateCommands.Contains);
        }
    }

    [Obsolete("Use IAutoQueryDb")]
    public interface IAutoQuery { }

    public interface IAutoQueryDb
    {
        SqlExpression<From> CreateQuery<From>(IQueryDb<From> dto, Dictionary<string, string> dynamicParams, IRequest req = null);

        QueryResponse<From> Execute<From>(IQueryDb<From> model, SqlExpression<From> query);

        SqlExpression<From> CreateQuery<From, Into>(IQueryDb<From, Into> dto, Dictionary<string, string> dynamicParams, IRequest req = null);

        QueryResponse<Into> Execute<From, Into>(IQueryDb<From, Into> model, SqlExpression<From> query);
    }

    public abstract class AutoQueryServiceBase : Service
    {
        public IAutoQueryDb AutoQuery { get; set; }

        public virtual object Exec<From>(IQueryDb<From> dto)
        {
            SqlExpression<From> q;
            using (Profiler.Current.Step("AutoQuery.CreateQuery"))
            {
                q = AutoQuery.CreateQuery(dto, Request.GetRequestParams(), Request);
            }
            using (Profiler.Current.Step("AutoQuery.Execute"))
            {
                return AutoQuery.Execute(dto, q);
            }
        }

        public virtual object Exec<From, Into>(IQueryDb<From, Into> dto)
        {
            SqlExpression<From> q;
            using (Profiler.Current.Step("AutoQuery.CreateQuery"))
            {
                q = AutoQuery.CreateQuery(dto, Request.GetRequestParams(), Request);
            }
            using (Profiler.Current.Step("AutoQuery.Execute"))
            {
                return AutoQuery.Execute(dto, q);
            }
        }
    }

    public interface IAutoQueryOptions
    {
        int? MaxLimit { get; set; }
        bool IncludeTotal { get; set; }
        bool EnableUntypedQueries { get; set; }
        bool EnableSqlFilters { get; set; }
        bool OrderByPrimaryKeyOnLimitQuery { get; set; }
        HashSet<string> IgnoreProperties { get; set; }
        HashSet<string> IllegalSqlFragmentTokens { get; set; }
        Dictionary<string, QueryDbFieldAttribute> StartsWithConventions { get; set; }
        Dictionary<string, QueryDbFieldAttribute> EndsWithConventions { get; set; }
    }

    public class AutoQuery : IAutoQueryDb, IAutoQueryOptions, IDisposable
    {
        public int? MaxLimit { get; set; }
        public bool IncludeTotal { get; set; }
        public bool EnableUntypedQueries { get; set; }
        public bool EnableSqlFilters { get; set; }
        public bool OrderByPrimaryKeyOnLimitQuery { get; set; }
        public string RequiredRoleForRawSqlFilters { get; set; }
        public HashSet<string> IgnoreProperties { get; set; }
        public HashSet<string> IllegalSqlFragmentTokens { get; set; }
        public Dictionary<string, QueryDbFieldAttribute> StartsWithConventions { get; set; }
        public Dictionary<string, QueryDbFieldAttribute> EndsWithConventions { get; set; }

        public string UseNamedConnection { get; set; }
        public virtual IDbConnection Db { get; set; }
        public Dictionary<Type, QueryFilterDelegate> QueryFilters { get; set; }
        public List<Action<QueryDbFilterContext>> ResponseFilters { get; set; }

        public virtual void Dispose()
        {
            if (Db != null)
                Db.Dispose();
        }

        private static Dictionary<Type, ITypedQuery> TypedQueries = new Dictionary<Type, ITypedQuery>();

        public static ITypedQuery GetTypedQuery(Type dtoType, Type fromType)
        {
            ITypedQuery defaultValue;
            if (TypedQueries.TryGetValue(dtoType, out defaultValue)) return defaultValue;

            var genericType = typeof(TypedQuery<,>).MakeGenericType(dtoType, fromType);
            defaultValue = genericType.CreateInstance<ITypedQuery>();

            Dictionary<Type, ITypedQuery> snapshot, newCache;
            do
            {
                snapshot = TypedQueries;
                newCache = new Dictionary<Type, ITypedQuery>(TypedQueries);
                newCache[dtoType] = defaultValue;

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref TypedQueries, newCache, snapshot), snapshot));

            return defaultValue;
        }

        public SqlExpression<From> Filter<From>(ISqlExpression q, IQueryDb dto, IRequest req)
        {
            if (QueryFilters == null)
                return (SqlExpression<From>)q;

            QueryFilterDelegate filterFn = null;
            if (!QueryFilters.TryGetValue(dto.GetType(), out filterFn))
            {
                foreach (var type in dto.GetType().GetInterfaces())
                {
                    if (QueryFilters.TryGetValue(type, out filterFn))
                        break;
                }
            }

            if (filterFn != null)
                filterFn(q, dto, req);

            return (SqlExpression<From>)q;
        }

        public QueryResponse<Into> ResponseFilter<From, Into>(QueryResponse<Into> response, SqlExpression<From> sqlExpression, IQueryDb dto)
        {
            response.Meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var commands = dto.Include.ParseCommands();

            var ctx = new QueryDbFilterContext
            {
                Db = Db,
                Commands = commands,
                Dto = dto,
                SqlExpression = sqlExpression,
                Response = response,
            };

            if (IncludeTotal)
            {
                var totalCountRequested = commands.Any(x =>
                    "COUNT".EqualsIgnoreCase(x.Name) &&
                    (x.Args.Count == 0 || (x.Args.Count == 1 && x.Args[0] == "*")));

                if (!totalCountRequested)
                    commands.Add(new Command { Name = "COUNT", Args = { "*" } });

                foreach (var responseFilter in ResponseFilters)
                {
                    responseFilter(ctx);
                }

                string total;
                response.Total = response.Meta.TryGetValue("COUNT(*)", out total)
                    ? total.ToInt()
                    : (int)Db.Count(sqlExpression); //fallback if it's not populated (i.e. if stripped by custom ResponseFilter)

                //reduce payload on wire
                if (!totalCountRequested)
                {
                    response.Meta.Remove("COUNT(*)");
                    if (response.Meta.Count == 0)
                        response.Meta = null;
                }
            }
            else
            {
                foreach (var responseFilter in ResponseFilters)
                {
                    responseFilter(ctx);
                }
            }

            return response;
        }

        public IDbConnection GetDb<From>(IRequest req = null)
        {
            if (Db != null)
                return Db;

            var namedConnection = UseNamedConnection;
            var attr = typeof(From).FirstAttribute<NamedConnectionAttribute>();
            if (attr != null)
                namedConnection = attr.Name;

            Db = namedConnection == null 
                ? HostContext.AppHost.GetDbConnection(req)
                : HostContext.TryResolve<IDbConnectionFactory>().OpenDbConnection(namedConnection);

            return Db;
        }

        public SqlExpression<From> CreateQuery<From>(IQueryDb<From> dto, Dictionary<string, string> dynamicParams, IRequest req = null)
        {
            var typedQuery = GetTypedQuery(dto.GetType(), typeof(From));
            return Filter<From>(typedQuery.CreateQuery(GetDb<From>(req), dto, dynamicParams, this), dto, req);
        }

        public QueryResponse<From> Execute<From>(IQueryDb<From> model, SqlExpression<From> query)
        {
            var typedQuery = GetTypedQuery(model.GetType(), typeof(From));
            return ResponseFilter(typedQuery.Execute<From>(GetDb<From>(), query), query, model);
        }

        public SqlExpression<From> CreateQuery<From, Into>(IQueryDb<From, Into> dto, Dictionary<string, string> dynamicParams, IRequest req = null)
        {
            var typedQuery = GetTypedQuery(dto.GetType(), typeof(From));
            return Filter<From>(typedQuery.CreateQuery(GetDb<From>(req), dto, dynamicParams, this), dto, req);
        }

        public QueryResponse<Into> Execute<From, Into>(IQueryDb<From, Into> model, SqlExpression<From> query)
        {
            var typedQuery = GetTypedQuery(model.GetType(), typeof(From));
            return ResponseFilter(typedQuery.Execute<Into>(GetDb<From>(), query), query, model);
        }
    }

    public interface ITypedQuery
    {
        ISqlExpression CreateQuery(
            IDbConnection db,
            IQueryDb dto,
            Dictionary<string, string> dynamicParams,
            IAutoQueryOptions options=null);

        QueryResponse<Into> Execute<Into>(
            IDbConnection db,
            ISqlExpression query);
    }

    public class TypedQuery<QueryModel, From> : ITypedQuery
    {
        private static ILog log = LogManager.GetLogger(typeof(AutoQueryFeature));

        static readonly Dictionary<string, Func<object, object>> PropertyGetters =
            new Dictionary<string, Func<object, object>>();

        static readonly Dictionary<string, QueryDbFieldAttribute> QueryFieldMap =
            new Dictionary<string, QueryDbFieldAttribute>();

        static TypedQuery()
        {
            foreach (var pi in typeof(QueryModel).GetPublicProperties())
            {
                var fn = pi.GetValueGetter(typeof(QueryModel));
                PropertyGetters[pi.Name] = fn;

                var queryAttr = pi.FirstAttribute<QueryDbFieldAttribute>();
                if (queryAttr != null)
                    QueryFieldMap[pi.Name] = queryAttr.Init();
            }
        }

        public ISqlExpression CreateQuery(
            IDbConnection db,
            IQueryDb dto,
            Dictionary<string, string> dynamicParams,
            IAutoQueryOptions options=null)
        {
            dynamicParams = new Dictionary<string, string>(dynamicParams, StringComparer.OrdinalIgnoreCase);
            var q = db.From<From>();

            if (options != null && options.EnableSqlFilters)
            {
                AppendSqlFilters(q, dto, dynamicParams, options);
            }

            AppendJoins(q, dto);

            AppendLimits(q, dto, options);

            var dtoAttr = dto.GetType().FirstAttribute<QueryDbAttribute>();
            var defaultTerm = dtoAttr != null && dtoAttr.DefaultTerm == QueryTerm.Or ? "OR" : "AND";

            var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var props = typeof(From).GetProperties();
            foreach (var pi in props)
            {
                var attr = pi.FirstAttribute<DataMemberAttribute>();
                if (attr == null || attr.Name == null) continue;
                aliases[attr.Name] = pi.Name;
            }

            AppendTypedQueries(q, dto, dynamicParams, defaultTerm, options, aliases);

            if (options != null && options.EnableUntypedQueries)
            {
                AppendUntypedQueries(q, dynamicParams, defaultTerm, options, aliases);
            }

            if (defaultTerm == "OR" && q.WhereExpression == null)
            {
                q.Where("1=0"); //Empty OR queries should be empty
            }

            if (!string.IsNullOrEmpty(dto.Fields))
            {
                var fields = dto.Fields.Split(',')
                    .Where(x => x.Trim().Length > 0)
                    .Map(x => x.Trim());

                q.Select(fields.ToArray());
            }

            return q;
        }

        private void AppendSqlFilters(SqlExpression<From> q, IQueryDb dto, Dictionary<string, string> dynamicParams, IAutoQueryOptions options)
        {
            string select, from, where;

            dynamicParams.TryGetValue("_select", out select);
            if (select != null)
            {
                dynamicParams.Remove("_select");
                select.SqlVerifyFragment(options.IllegalSqlFragmentTokens);
                q.Select(select);
            }

            dynamicParams.TryGetValue("_from", out from);
            if (from != null)
            {
                dynamicParams.Remove("_from");
                from.SqlVerifyFragment(options.IllegalSqlFragmentTokens);
                q.From(from);
            }

            dynamicParams.TryGetValue("_where", out where);
            if (where != null)
            {
                dynamicParams.Remove("_where");
                where.SqlVerifyFragment(options.IllegalSqlFragmentTokens);
                q.Where(where);
            }
        }

        private static readonly char[] FieldSeperators = new[] {',', ';'};

        private static void AppendLimits(SqlExpression<From> q, IQueryDb dto, IAutoQueryOptions options)
        {
            var maxLimit = options != null ? options.MaxLimit : null;
            var take = dto.Take ?? maxLimit;
            if (take > maxLimit)
                take = maxLimit;
            q.Limit(dto.Skip, take);

            if (dto.OrderBy != null)
            {
                var fieldNames = dto.OrderBy.Split(FieldSeperators, StringSplitOptions.RemoveEmptyEntries);
                q.OrderByFields(fieldNames);
            }
            else if (dto.OrderByDesc != null)
            {
                var fieldNames = dto.OrderByDesc.Split(FieldSeperators, StringSplitOptions.RemoveEmptyEntries);
                q.OrderByFieldsDescending(fieldNames);
            }
            else if ((dto.Skip != null || dto.Take != null)
                && (options != null && options.OrderByPrimaryKeyOnLimitQuery))
            {
                q.OrderByFields(typeof(From).GetModelMetadata().PrimaryKey);
            }
        }

        private static void AppendJoins(SqlExpression<From> q, IQueryDb dto)
        {
            if (dto is IJoin)
            {
                var dtoInterfaces = dto.GetType().GetInterfaces();
                foreach(var innerJoin in dtoInterfaces.Where(x => x.Name.StartsWith("IJoin`")))
                {
                    var joinTypes = innerJoin.GetGenericArguments();
                    for (var i = 1; i < joinTypes.Length; i++)
                    {
                        q.Join(joinTypes[i - 1], joinTypes[i]);
                    }
                }

                foreach(var leftJoin in dtoInterfaces.Where(x => x.Name.StartsWith("ILeftJoin`")))
                {
                    var joinTypes = leftJoin.GetGenericArguments();
                    for (var i = 1; i < joinTypes.Length; i++)
                    {
                        q.LeftJoin(joinTypes[i - 1], joinTypes[i]);
                    } 
                }
            }
        }

        private static void AppendTypedQueries(SqlExpression<From> q, IQueryDb dto, Dictionary<string, string> dynamicParams, string defaultTerm, IAutoQueryOptions options, Dictionary<string, string> aliases)
        {
            foreach (var entry in PropertyGetters)
            {
                var name = entry.Key.LeftPart('#');

                QueryDbFieldAttribute implicitQuery;
                QueryFieldMap.TryGetValue(name, out implicitQuery);

                if (implicitQuery != null && implicitQuery.Field != null)
                    name = implicitQuery.Field;

                var match = GetQueryMatch(q, name, options, aliases);
                if (match == null)
                    continue;

                if (implicitQuery == null)
                    implicitQuery = match.ImplicitQuery;
                var quotedColumn = q.DialectProvider.GetQuotedColumnName(match.ModelDef, match.FieldDef);

                var value = entry.Value(dto);
                if (value == null)
                    continue;

                dynamicParams.Remove(entry.Key);

                AddCondition(q, defaultTerm, quotedColumn, value, implicitQuery);
            }
        }

        private static void AddCondition(SqlExpression<From> q, string defaultTerm, string quotedColumn, object value, QueryDbFieldAttribute implicitQuery)
        {
            var seq = value as IEnumerable;
            if (value is string)
                seq = null;
            var format = seq == null 
                ? (value != null ? quotedColumn + " = {0}" : quotedColumn + " IS NULL")
                : quotedColumn + " IN ({0})";
            if (implicitQuery != null)
            {
                var operand = implicitQuery.Operand ?? "=";
                if (implicitQuery.Term == QueryTerm.Or)
                    defaultTerm = "OR";
                else if (implicitQuery.Term == QueryTerm.And)
                    defaultTerm = "AND";

                format = "(" + quotedColumn + " " + operand + " {0}" + ")";
                if (implicitQuery.Template != null)
                {
                    format = implicitQuery.Template.Replace("{Field}", quotedColumn);

                    if (implicitQuery.ValueStyle == ValueStyle.Multiple)
                    {
                        if (seq == null)
                            throw new ArgumentException("{0} requires {1} values"
                                .Fmt(implicitQuery.Field, implicitQuery.ValueArity));

                        var args = new object[implicitQuery.ValueArity];
                        int i = 0;
                        foreach (var x in seq)
                        {
                            if (i < args.Length)
                            {
                                format = format.Replace("{Value" + (i + 1) + "}", "{" + i + "}");
                                args[i++] = x;
                            }
                        }

                        q.AddCondition(defaultTerm, format, args);
                        return;
                    }
                    if (implicitQuery.ValueStyle == ValueStyle.List)
                    {
                        if (seq == null)
                            throw new ArgumentException("{0} expects a list of values".Fmt(implicitQuery.Field));

                        format = format.Replace("{Values}", "{0}");
                        value = new SqlInValues(seq);
                    }
                    else
                    {
                        format = format.Replace("{Value}", "{0}");
                    }

                    if (implicitQuery.ValueFormat != null)
                        value = string.Format(implicitQuery.ValueFormat, value);
                }
            }
            else
            {
                if (seq != null)
                    value = new SqlInValues(seq);
            }

            q.AddCondition(defaultTerm, format, value);
        }

        private static void AppendUntypedQueries(SqlExpression<From> q, Dictionary<string, string> dynamicParams, string defaultTerm, IAutoQueryOptions options, Dictionary<string, string> aliases)
        {
            foreach (var entry in dynamicParams)
            {
                var name = entry.Key.LeftPart('#');

                var match = GetQueryMatch(q, name, options, aliases);
                if (match == null)
                    continue;

                var implicitQuery = match.ImplicitQuery;
                var quotedColumn = q.DialectProvider.GetQuotedColumnName(match.ModelDef, match.FieldDef);

                var strValue = !string.IsNullOrEmpty(entry.Value)
                    ? entry.Value
                    : null;
                var fieldType = match.FieldDef.FieldType;
                var isMultiple = (implicitQuery != null && (implicitQuery.ValueStyle > ValueStyle.Single))
                    || string.Compare(name, match.FieldDef.Name + Pluralized, StringComparison.OrdinalIgnoreCase) == 0;
                
                var value = strValue == null ? 
                      null 
                    : isMultiple ? 
                      TypeSerializer.DeserializeFromString(strValue, Array.CreateInstance(fieldType, 0).GetType())
                    : fieldType == typeof(string) ? 
                      strValue
                    : strValue.ChangeTo(fieldType);

                AddCondition(q, defaultTerm, quotedColumn, value, implicitQuery);
            }
        }

        class MatchQuery
        {
            public MatchQuery(Tuple<ModelDefinition,FieldDefinition> match, QueryDbFieldAttribute implicitQuery)
            {
                ModelDef = match.Item1;
                FieldDef = match.Item2;
                ImplicitQuery = implicitQuery;
            }
            public readonly ModelDefinition ModelDef;
            public readonly FieldDefinition FieldDef;
            public readonly QueryDbFieldAttribute ImplicitQuery;
        }

        private const string Pluralized = "s";

        private static MatchQuery GetQueryMatch(SqlExpression<From> q, string name, IAutoQueryOptions options, Dictionary<string,string> aliases)
        {
            var match = GetQueryMatch(q, name, options);

            if (match == null)
            {
                string alias;
                if (aliases.TryGetValue(name, out alias))
                    match = GetQueryMatch(q, alias, options);

                if (match == null && JsConfig.EmitLowercaseUnderscoreNames && name.Contains("_"))
                    match = GetQueryMatch(q, name.Replace("_", ""), options);
            }

            return match;
        }

        private static MatchQuery GetQueryMatch(SqlExpression<From> q, string name, IAutoQueryOptions options)
        {
            if (options == null) return null;

            var match = options.IgnoreProperties == null || !options.IgnoreProperties.Contains(name)
                ? q.FirstMatchingField(name) ?? (name.EndsWith(Pluralized) ? q.FirstMatchingField(name.TrimEnd('s')) : null)
                : null;

            if (match == null)
            {
                foreach (var startsWith in options.StartsWithConventions)
                {
                    if (name.Length <= startsWith.Key.Length || !name.StartsWith(startsWith.Key)) continue;

                    var field = name.Substring(startsWith.Key.Length);
                    match = q.FirstMatchingField(field) ?? (field.EndsWith(Pluralized) ? q.FirstMatchingField(field.TrimEnd('s')) : null);
                    if (match != null)
                        return new MatchQuery(match, startsWith.Value);
                }
            }
            if (match == null)
            {
                foreach (var endsWith in options.EndsWithConventions)
                {
                    if (name.Length <= endsWith.Key.Length || !name.EndsWith(endsWith.Key)) continue;

                    var field = name.Substring(0, name.Length - endsWith.Key.Length);
                    match = q.FirstMatchingField(field) ?? (field.EndsWith(Pluralized) ? q.FirstMatchingField(field.TrimEnd('s')) : null);
                    if (match != null)
                        return new MatchQuery(match, endsWith.Value);
                }
            }

            return match != null 
                ? new MatchQuery(match, null) 
                : null;
        }

        public QueryResponse<Into> Execute<Into>(IDbConnection db, ISqlExpression query)
        {
            try
            {
                var q = (SqlExpression<From>)query;

                var response = new QueryResponse<Into>
                {
                    Offset = q.Offset.GetValueOrDefault(0),
                    Results = db.LoadSelect<Into, From>(q, include:q.OnlyFields),
                };

                return response;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message, ex);
            }
        }
    }

    public static class AutoQueryExtensions
    {
        public static QueryDbFieldAttribute Init(this QueryDbFieldAttribute query)
        {
            query.ValueStyle = ValueStyle.Single;
            if (query.Template == null || query.ValueFormat != null) return query;

            var i = 0;
            while (query.Template.Contains("{Value" + (i + 1) + "}")) i++;
            if (i > 0)
            {
                query.ValueStyle = ValueStyle.Multiple;
                query.ValueArity = i;
            }
            else
            {
                query.ValueStyle = !query.Template.Contains("{Values}")
                    ? ValueStyle.Single
                    : ValueStyle.List;
            }
            return query;
        }

        public static SqlExpression<From> CreateQuery<From>(this IAutoQueryDb autoQuery, IQueryDb<From> model, IRequest request)
        {
            return autoQuery.CreateQuery(model, request.GetRequestParams(), request);
        }

        public static SqlExpression<From> CreateQuery<From, Into>(this IAutoQueryDb autoQuery, IQueryDb<From, Into> model, IRequest request)
        {
            return autoQuery.CreateQuery(model, request.GetRequestParams(), request);
        }
    }
}