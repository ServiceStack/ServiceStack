using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
using ServiceStack.Logging;

namespace ServiceStack
{
    public interface IDataQuery
    {
        IQueryData Request { get; }
        Dictionary<string, string> DynamicParams { get; }

        int? Offset { get; }
        IDataQuery Select(string[] fields);
    }

    public delegate IDataQuery QueryDataFilterDelegate(IQueryData request, IDataQuery q);

    public class QueryDataFilterContext
    {
        public IQueryDataSource Db { get; set; }
        public List<Command> Commands { get; set; }
        public IQuery Request { get; set; }
        public IDataQuery Query { get; set; }
        public IQueryResponse Response { get; set; }
    }

    public class AutoQueryDataFeature : IPlugin, IPostInitPlugin
    {
        public HashSet<string> IgnoreProperties { get; set; }
        public HashSet<Assembly> LoadFromAssemblies { get; set; } 
        public int? MaxLimit { get; set; }
        public bool EnableUntypedQueries { get; set; }
        public bool EnableAutoQueryViewer { get; set; }
        public bool OrderByPrimaryKeyOnPagedQuery { get; set; }
        public Type AutoQueryServiceBaseType { get; set; }
        public Dictionary<Type, QueryDataFilterDelegate> QueryFilters { get; set; }
        public List<Action<QueryDataFilterContext>> ResponseFilters { get; set; }

        public ConcurrentDictionary<Type, Func<QueryDataContext, IQueryDataSource>> DataSources { get; private set; }

        public static QueryCondition GreaterThanOrEqualCondition = new GreaterEqualCondition();
        public static QueryCondition GreaterThanCondition =        new GreaterCondition();
        public static QueryCondition LessThanCondition =           new LessCondition();
        public static QueryCondition LessThanOrEqualCondition =    new LessEqualCondition();
        public static QueryCondition NotEqualCondition =           new NotEqualCondition();

        public Dictionary<string, QueryCondition> ImplicitConventions = new Dictionary<string, QueryCondition> 
        {
            {"%Above%",         GreaterThanCondition},
            {"Begin%",          GreaterThanCondition},
            {"%Beyond%",        GreaterThanCondition},
            {"%Over%",          GreaterThanCondition},
            {"%OlderThan",      GreaterThanCondition},
            {"%After%",         GreaterThanCondition},
            {"OnOrAfter%",      GreaterThanOrEqualCondition},
            {"%From%",          GreaterThanOrEqualCondition},
            {"Since%",          GreaterThanOrEqualCondition},
            {"Start%",          GreaterThanOrEqualCondition},
            {"%Higher%",        GreaterThanOrEqualCondition},
            {">%",              GreaterThanOrEqualCondition},
            {"%>",              GreaterThanCondition},
            {"%!",              NotEqualCondition},

            {"%GreaterThanOrEqualTo%", GreaterThanOrEqualCondition},
            {"%GreaterThan%",          GreaterThanCondition},
            {"%LessThan%",             LessThanCondition},
            {"%LessThanOrEqualTo%",    LessThanOrEqualCondition},
            {"%NotEqualTo",            NotEqualCondition},

            {"Behind%",         LessThanCondition},
            {"%Below%",         LessThanCondition},
            {"%Under%",         LessThanCondition},
            {"%Lower%",         LessThanCondition},
            {"%Before%",        LessThanCondition},
            {"%YoungerThan",    LessThanCondition},
            {"OnOrBefore%",     LessThanOrEqualCondition},
            {"End%",            LessThanOrEqualCondition},
            {"Stop%",           LessThanOrEqualCondition},
            {"To%",             LessThanOrEqualCondition},
            {"Until%",          LessThanOrEqualCondition},
            {"%<",              LessThanOrEqualCondition},
            {"<%",              LessThanCondition},

            {"%Like%",          new CaseInsensitiveEqualCondition()},
            {"%In",             new InCollectionCondition()},
            {"%Ids",            new InCollectionCondition()},
            {"%Between%",       new InBetweenCondition()},

            {"%StartsWith",     new StartsWithCondition()},
            {"%Contains",       new ContainsCondition()},
            {"%EndsWith",       new EndsWithCondition()},
        };

        public Dictionary<string, QueryCondition> StartsWithConventions = new Dictionary<string, QueryCondition>();
        public Dictionary<string, QueryCondition> EndsWithConventions = new Dictionary<string, QueryCondition>();

        public AutoQueryDataFeature()
        {
            IgnoreProperties = new HashSet<string>(new[] { "Skip", "Take", "OrderBy", "OrderByDesc" }, 
                StringComparer.OrdinalIgnoreCase);
            AutoQueryServiceBaseType = typeof(AutoQueryDataServiceBase);
            QueryFilters = new Dictionary<Type, QueryDataFilterDelegate>();
            ResponseFilters = new List<Action<QueryDataFilterContext>>();
            EnableUntypedQueries = true;
            EnableAutoQueryViewer = true;
            OrderByPrimaryKeyOnPagedQuery = true;
            LoadFromAssemblies = new HashSet<Assembly>();
            DataSources = new ConcurrentDictionary<Type, Func<QueryDataContext, IQueryDataSource>>();
        }

        public void Register(IAppHost appHost)
        {
            foreach (var entry in ImplicitConventions)
            {
                var key = entry.Key.Trim('%');
                var query = entry.Value;
                if (entry.Key.EndsWith("%"))
                    StartsWithConventions[key] = query;
                if (entry.Key.StartsWith("%"))
                    EndsWithConventions[key] = query;
            }

            appHost.GetContainer().Register<IAutoQueryData>(c =>
                new AutoQueryData
                {
                    IgnoreProperties = IgnoreProperties,                    
                    MaxLimit = MaxLimit,
                    EnableUntypedQueries = EnableUntypedQueries,
                    OrderByPrimaryKeyOnLimitQuery = OrderByPrimaryKeyOnPagedQuery,
                    QueryFilters = QueryFilters,
                    ResponseFilters = ResponseFilters,
                    StartsWithConventions = StartsWithConventions,
                    EndsWithConventions = EndsWithConventions,
                })
                .ReusedWithin(ReuseScope.None);

            appHost.Metadata.GetOperationAssemblies()
                .Each(x => LoadFromAssemblies.Add(x));

            ((ServiceStackHost)appHost).ServiceAssemblies.Each(x => {
                if (!LoadFromAssemblies.Contains(x))
                    LoadFromAssemblies.Add(x);
            });

            //if (EnableAutoQueryViewer)
            //    appHost.RegisterService<AutoQueryMetadataService>();
        }

        public void AfterPluginsLoaded(IAppHost appHost)
        {
            var scannedTypes = LoadFromAssemblies.SelectMany(x => x.GetTypes());

            var misingRequestTypes = scannedTypes
                .Where(x => x.HasInterface(typeof(IQueryData)))
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
                Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
                .DefineDynamicModule("tmpModule")
                .DefineType("__AutoQueryDataServices",
                    TypeAttributes.Public | TypeAttributes.Class,
                    AutoQueryServiceBaseType);

            foreach (var requestType in misingRequestTypes)
            {
                var genericDef = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryData<,>));
                var hasExplicitInto = genericDef != null;
                if (genericDef == null)
                    genericDef = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryData<>));
                if (genericDef == null)
                    continue;

                var method = typeBuilder.DefineMethod("Any", MethodAttributes.Public | MethodAttributes.Virtual,
                    CallingConventions.Standard,
                    returnType: typeof(object),
                    parameterTypes: new[] { requestType });

                var il = method.GetILGenerator();

                var genericArgs = genericDef.GetGenericArguments();
                var mi = AutoQueryServiceBaseType.GetMethods()
                    .First(x => x.GetGenericArguments().Length == genericArgs.Length);
                var genericMi = mi.MakeGenericMethod(genericArgs);

                var queryType = hasExplicitInto
                    ? typeof(IQueryData<,>).MakeGenericType(genericArgs)
                    : typeof(IQueryData<>).MakeGenericType(genericArgs);

                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Box, queryType);
                il.Emit(OpCodes.Callvirt, genericMi);
                il.Emit(OpCodes.Ret);
            }

            var servicesType = typeBuilder.CreateType();
            return servicesType;
        }

        public AutoQueryDataFeature RegisterQueryFilter<Request, From>(Func<Request, DataQuery<From>, DataQuery<From>> filterFn)
        {
            QueryFilters[typeof(Request)] = (request, q) =>
                filterFn((Request)request, (DataQuery<From>)q);

            return this;
        }

        public AutoQueryDataFeature AddDataSource<T>(Func<QueryDataContext, QueryDataSource<T>> dataSourceFactory)
        {
            DataSources[typeof(T)] = dataSourceFactory;
            return this;
        }

        public AutoQueryDataFeature AddDataSource<T>(Func<QueryDataContext, IQueryDataSource> dataSourceFactory)
        {
            DataSources[typeof(T)] = dataSourceFactory;
            return this;
        }

        public AutoQueryDataFeature AddDataSource(Type type, Func<QueryDataContext, IQueryDataSource> dataSourceFactory)
        {
            DataSources[type] = dataSourceFactory;
            return this;
        }

        public Func<QueryDataContext, IQueryDataSource> GetDataSource(Type type)
        {
            Func<QueryDataContext, IQueryDataSource> source;
            DataSources.TryGetValue(type, out source);
            return source;
        }
    }

    public class ConditionExpression
    {
        public QueryTerm Term { get; set; }
        public QueryCondition Condition { get; set; }
        public PropertyInfo Field { get; set; }
        public object Value { get; set; }
    }

    public class DataQuery<T> : IDataQuery
    {
        private QueryDataContext context;

        public IQueryData Request { get; private set; }
        public Dictionary<string, string> DynamicParams { get; private set; }
        public List<ConditionExpression> Conditions { get; set; }
        public int? Offset { get; set; }
        public int? Rows { get; set; }

        public DataQuery(QueryDataContext context)
        {
            this.context = context;
            this.Request = context.Request;
            this.DynamicParams = context.DynamicParams;
            this.Conditions = new List<ConditionExpression>();
        }

        public virtual bool HasConditions
        {
            get { return Conditions.Count > 0; }
        }

        public virtual DataQuery<T> Limit(int? skip, int? take)
        {
            this.Offset = skip;
            this.Rows = take;
            return this;
        }

        public DataQuery<T> Take(int take)
        {
            this.Rows = take;
            return this;
        } 

        public virtual IDataQuery Select(string[] fields)
        {
            return this;
        }

        public virtual Tuple<Type, PropertyInfo> FirstMatchingField(string field)
        {
            var pi = typeof(T).GetProperty(field);
            return pi != null
                ? Tuple.Create(typeof(T), pi)
                : null;
        }

        public virtual DataQuery<T> OrderByFields(params string[] fieldNames)
        {
            return this;
        }

        public virtual DataQuery<T> OrderByFieldsDescending(params string[] fieldNames)
        {
            return this;
        }

        public virtual DataQuery<T> OrderByPrimaryKey()
        {
            return this;
        }

        public virtual DataQuery<T> Join(Type joinType, Type type)
        {
            return this;
        }

        public virtual DataQuery<T> LeftJoin(Type joinType, Type type)
        {
            return this;
        }

        public virtual DataQuery<T> AddCondition(QueryTerm term, QueryCondition condition, PropertyInfo field, object value)
        {
            this.Conditions.Add(new ConditionExpression
            {
                Term = term,
                Condition = condition,
                Field = field,
                Value = value,
            });
            return this;
        }

        public virtual DataQuery<T> From(string @from)
        {
            return this;
        }
    }

    public interface IAutoQueryData
    {
        DataQuery<From> CreateQuery<From>(IQueryData<From> request, Dictionary<string, string> dynamicParams, IRequest req = null, IQueryDataSource db = null);

        QueryResponse<From> Execute<From>(IQueryData<From> request, DataQuery<From> q);

        DataQuery<From> CreateQuery<From, Into>(IQueryData<From, Into> request, Dictionary<string, string> dynamicParams, IRequest req = null, IQueryDataSource db = null);

        QueryResponse<Into> Execute<From, Into>(IQueryData<From, Into> request, DataQuery<From> q);
    }

    public abstract class AutoQueryDataServiceBase : Service
    {
        public IAutoQueryData AutoQuery { get; set; }

        public virtual object Exec<From>(IQueryData<From> dto)
        {
            DataQuery<From> q;
            using (Profiler.Current.Step("AutoQueryData.CreateQuery"))
            {
                q = AutoQuery.CreateQuery(dto, Request.GetRequestParams(), Request);
            }
            using (Profiler.Current.Step("AutoQueryData.Execute"))
            {
                return AutoQuery.Execute(dto, q);
            }
        }

        public virtual object Exec<From, Into>(IQueryData<From, Into> dto)
        {
            DataQuery<From> q;
            using (Profiler.Current.Step("AutoQueryData.CreateQuery"))
            {
                q = AutoQuery.CreateQuery(dto, Request.GetRequestParams(), Request);
            }
            using (Profiler.Current.Step("AutoQueryData.Execute"))
            {
                return AutoQuery.Execute(dto, q);
            }
        }
    }

    public interface IAutoQueryDataOptions
    {
        int? MaxLimit { get; set; }
        bool EnableUntypedQueries { get; set; }
        bool OrderByPrimaryKeyOnLimitQuery { get; set; }
        HashSet<string> IgnoreProperties { get; set; }
        Dictionary<string, QueryCondition> StartsWithConventions { get; set; }
        Dictionary<string, QueryCondition> EndsWithConventions { get; set; }
    }

    public class AutoQueryData : IAutoQueryData, IAutoQueryDataOptions, IDisposable
    {
        public int? MaxLimit { get; set; }
        public bool EnableUntypedQueries { get; set; }
        public bool OrderByPrimaryKeyOnLimitQuery { get; set; }
        public string RequiredRoleForRawSqlFilters { get; set; }
        public HashSet<string> IgnoreProperties { get; set; }
        public Dictionary<string, QueryCondition> StartsWithConventions { get; set; }
        public Dictionary<string, QueryCondition> EndsWithConventions { get; set; }

        public virtual IQueryDataSource Db { get; set; }
        public Dictionary<Type, QueryDataFilterDelegate> QueryFilters { get; set; }
        public List<Action<QueryDataFilterContext>> ResponseFilters { get; set; }

        public virtual void Dispose()
        {
            if (Db != null)
                Db.Dispose();
        }

        private static Dictionary<Type, ITypedQueryData> TypedQueries = new Dictionary<Type, ITypedQueryData>();

        public static ITypedQueryData GetTypedQuery(Type dtoType, Type fromType)
        {
            ITypedQueryData defaultValue;
            if (TypedQueries.TryGetValue(dtoType, out defaultValue)) return defaultValue;

            var genericType = typeof(TypedQueryData<,>).MakeGenericType(dtoType, fromType);
            defaultValue = genericType.CreateInstance<ITypedQueryData>();

            Dictionary<Type, ITypedQueryData> snapshot, newCache;
            do
            {
                snapshot = TypedQueries;
                newCache = new Dictionary<Type, ITypedQueryData>(TypedQueries);
                newCache[dtoType] = defaultValue;

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref TypedQueries, newCache, snapshot), snapshot));

            return defaultValue;
        }

        public DataQuery<From> Filter<From>(IQueryData request, IDataQuery q)
        {
            if (QueryFilters == null)
                return (DataQuery<From>)q;

            QueryDataFilterDelegate filterFn = null;
            if (!QueryFilters.TryGetValue(request.GetType(), out filterFn))
            {
                foreach (var type in request.GetType().GetInterfaces())
                {
                    if (QueryFilters.TryGetValue(type, out filterFn))
                        break;
                }
            }

            if (filterFn != null)
                return (DataQuery<From>)(filterFn(request, q) ?? q);

            return (DataQuery<From>)q;
        }

        public QueryResponse<Into> ResponseFilter<From, Into>(QueryResponse<Into> response, DataQuery<From> expr, IQuery model)
        {
            response.Meta = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            var commands = model.Include.ParseCommands();

            var totalCountRequested = commands.Any(x =>
                "COUNT".EqualsIgnoreCase(x.Name) && 
                (x.Args.Count == 0 || (x.Args.Count == 1 && x.Args[0] == "*"))); 

            if (!totalCountRequested)
                commands.Add(new Command { Name = "COUNT", Args = { "*" }});

            var ctx = new QueryDataFilterContext
            {
                Db = Db,
                Commands = commands,
                Request = model,
                Query = expr,
                Response = response,
            };

            foreach (var responseFilter in ResponseFilters)
            {
                responseFilter(ctx);
            }

            string total;
            response.Total = response.Meta.TryGetValue("COUNT(*)", out total) 
                ? total.ToInt() 
                : (int) Db.Count(expr); //fallback if it's not populated (i.e. if stripped by custom ResponseFilter)

            //reduce payload on wire
            if (!totalCountRequested)
            {
                response.Meta.Remove("COUNT(*)");
                if (response.Meta.Count == 0)
                    response.Meta = null;
            }

            return response;
        }

        public IQueryDataSource GetDb<From>(QueryDataContext ctx)
        {
            if (Db != null)
                return Db;

            var dataSourceFactory = HostContext.GetPlugin<AutoQueryDataFeature>().GetDataSource(typeof(From));
            if (dataSourceFactory == null)
                throw new NotSupportedException("No datasource was registered on AutoQueryDataFeature for Type '{0}'".Fmt(typeof(From).Name));

            return Db = dataSourceFactory(ctx);
        }

        public DataQuery<From> CreateQuery<From>(IQueryData<From> request, Dictionary<string, string> dynamicParams, IRequest req = null, IQueryDataSource db = null)
        {
            if (db != null)
                this.Db = db;

            var ctx = new QueryDataContext(request, dynamicParams);
            var typedQuery = GetTypedQuery(request.GetType(), typeof(From));
            return Filter<From>(request, typedQuery.CreateQuery(GetDb<From>(ctx), request, dynamicParams, this));
        }

        public QueryResponse<From> Execute<From>(IQueryData<From> request, DataQuery<From> q)
        {
            var typedQuery = GetTypedQuery(request.GetType(), typeof(From));
            return ResponseFilter(typedQuery.Execute<From>(Db, q), q, request);
        }

        public DataQuery<From> CreateQuery<From, Into>(IQueryData<From, Into> request, Dictionary<string, string> dynamicParams, IRequest req = null, IQueryDataSource db = null)
        {
            if (db != null)
                this.Db = db;

            var ctx = new QueryDataContext(request, dynamicParams);
            var typedQuery = GetTypedQuery(request.GetType(), typeof(From));
            return Filter<From>(request, typedQuery.CreateQuery(GetDb<From>(ctx), request, dynamicParams, this));
        }

        public QueryResponse<Into> Execute<From, Into>(IQueryData<From, Into> request, DataQuery<From> q)
        {
            var typedQuery = GetTypedQuery(request.GetType(), typeof(From));
            return ResponseFilter(typedQuery.Execute<Into>(Db, q), q, request);
        }
    }

    public interface IQueryDataSource : IDisposable
    {
        DataQuery<T> From<T>();
        List<Into> LoadSelect<Into, From>(DataQuery<From> q);
        int Count<T>(DataQuery<T> q);
    }

    public class QueryDataContext
    {
        public QueryDataContext(IQueryData request, Dictionary<string, string> dynamicParams)
        {
            this.Request = request;
            this.DynamicParams = dynamicParams;
        }

        public IQueryData Request { get; private set; }
        public Dictionary<string, string> DynamicParams { get; private set; }
    }

    public class QueryDataSource<T> : IQueryDataSource
    {
        private readonly QueryDataContext context;
        private readonly IEnumerable<T> data;

        public QueryDataSource(QueryDataContext context, IEnumerable<T> data)
        {
            this.context = context;
            this.data = data;
        }

        public DataQuery<T> From<T>()
        {
            return new DataQuery<T>(context);
        }

        public List<Into> LoadSelect<Into, From>(DataQuery<From> q)
        {
            var source = data;
            if (q.Offset != null)
                source = source.Skip(q.Offset.Value);
            if (q.Rows != null)
                source = source.Take(q.Rows.Value);

            var rows = typeof(From) == typeof(Into)
                ? source.Map(x => (Into)x)
                : source.Map(x => x.ConvertTo<Into>());

            return rows;
        }

        public int Count<T>(DataQuery<T> q)
        {
            return data.Count();
        }

        public void Dispose() {}
    }

    public interface ITypedQueryData
    {
        IDataQuery CreateQuery(
            IQueryDataSource db,
            IQueryData request,
            Dictionary<string, string> dynamicParams,
            IAutoQueryDataOptions options=null);

        QueryResponse<Into> Execute<Into>(
            IQueryDataSource db,
            IDataQuery query);
    }

    public class TypedQueryData<QueryModel, From> : ITypedQueryData
    {
        private static ILog log = LogManager.GetLogger(typeof(AutoQueryDataFeature));

        static readonly Dictionary<string, Func<object, object>> PropertyGetters =
            new Dictionary<string, Func<object, object>>();

        static readonly Dictionary<string, QueryCondition> QueryFieldMap =
            new Dictionary<string, QueryCondition>();

        static TypedQueryData()
        {
            foreach (var pi in typeof(QueryModel).GetPublicProperties())
            {
                var fn = pi.GetValueGetter(typeof(QueryModel));
                PropertyGetters[pi.Name] = fn;

                //var queryAttr = pi.FirstAttribute<QueryFieldAttribute>();
                //if (queryAttr != null)
                //    QueryFieldMap[pi.Name] = queryAttr.Init();
            }
        }

        public IDataQuery CreateQuery(
            IQueryDataSource db,
            IQueryData request,
            Dictionary<string, string> dynamicParams,
            IAutoQueryDataOptions options = null)
        {
            dynamicParams = new Dictionary<string, string>(dynamicParams, StringComparer.OrdinalIgnoreCase);
            var q = db.From<From>();

            AppendJoins(q, request);

            AppendLimits(q, request, options);

            var dtoAttr = request.GetType().FirstAttribute<QueryAttribute>();
            var defaultTerm = dtoAttr != null && dtoAttr.DefaultTerm == QueryTerm.Or ? QueryTerm.Or : QueryTerm.And;

            var aliases = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            var props = typeof(From).GetProperties();
            foreach (var pi in props)
            {
                var attr = pi.FirstAttribute<DataMemberAttribute>();
                if (attr == null || attr.Name == null) continue;
                aliases[attr.Name] = pi.Name;
            }

            AppendTypedQueries(q, request, dynamicParams, defaultTerm, options, aliases);

            if (options != null && options.EnableUntypedQueries)
            {
                AppendUntypedQueries(q, dynamicParams, defaultTerm, options, aliases);
            }

            if (defaultTerm == QueryTerm.Or && !q.HasConditions)
            {
                q.AddCondition(defaultTerm, new FalseCondition(), null, null); //Empty OR queries should be empty
            }

            if (!string.IsNullOrEmpty(request.Fields))
            {
                var fields = request.Fields.Split(',')
                    .Where(x => x.Trim().Length > 0)
                    .Map(x => x.Trim());

                q.Select(fields.ToArray());
            }

            return q;
        }

        private static readonly char[] FieldSeperators = new[] {',', ';'};

        private static void AppendLimits(DataQuery<From> q, IQuery model, IAutoQueryDataOptions options)
        {
            var maxLimit = options != null ? options.MaxLimit : null;
            var take = model.Take ?? maxLimit;
            if (take > maxLimit)
                take = maxLimit;
            q.Limit(model.Skip, take);

            if (model.OrderBy != null)
            {
                var fieldNames = model.OrderBy.Split(FieldSeperators, StringSplitOptions.RemoveEmptyEntries);
                q.OrderByFields(fieldNames);
            }
            else if (model.OrderByDesc != null)
            {
                var fieldNames = model.OrderByDesc.Split(FieldSeperators, StringSplitOptions.RemoveEmptyEntries);
                q.OrderByFieldsDescending(fieldNames);
            }
            else if ((model.Skip != null || model.Take != null)
                && (options != null && options.OrderByPrimaryKeyOnLimitQuery))
            {
                q.OrderByPrimaryKey();
            }
        }

        private static void AppendJoins(DataQuery<From> q, IQuery model)
        {
            if (model is IJoin)
            {
                var dtoInterfaces = model.GetType().GetInterfaces();
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

        private static void AppendTypedQueries(DataQuery<From> q, IQuery model, Dictionary<string, string> dynamicParams, QueryTerm defaultTerm, IAutoQueryDataOptions options, Dictionary<string, string> aliases)
        {
            foreach (var entry in PropertyGetters)
            {
                var name = entry.Key.SplitOnFirst('#')[0];

                QueryCondition condition;
                QueryFieldMap.TryGetValue(name, out condition);

                //if (condition != null && condition.Field != null)
                //    name = condition.Field;

                var match = GetQueryMatch(q, name, options, aliases);
                if (match == null)
                    continue;

                if (condition == null)
                    condition = match.Condition;

                var value = entry.Value(model);
                if (value == null)
                    continue;

                dynamicParams.Remove(entry.Key);

                AddCondition(q, defaultTerm, match.FieldDef, value, condition);
            }
        }

        private static void AddCondition(DataQuery<From> q, QueryTerm defaultTerm, PropertyInfo property, object value, QueryCondition condition)
        {
            if (condition != null)
            {
                if (condition.Term == QueryTerm.Or)
                    defaultTerm = QueryTerm.Or;
                else if (condition.Term == QueryTerm.And)
                    defaultTerm = QueryTerm.And;
            }

            q.AddCondition(defaultTerm, condition, property, value);
        }

        private static void AppendUntypedQueries(DataQuery<From> q, Dictionary<string, string> dynamicParams, QueryTerm defaultTerm, IAutoQueryDataOptions options, Dictionary<string, string> aliases)
        {
            foreach (var entry in dynamicParams)
            {
                var name = entry.Key.SplitOnFirst('#')[0];

                var match = GetQueryMatch(q, name, options, aliases);
                if (match == null)
                    continue;

                var condition = match.Condition;

                var strValue = !string.IsNullOrEmpty(entry.Value)
                    ? entry.Value
                    : null;
                var fieldType = match.FieldDef.PropertyType;
                var isMultiple = condition is IQueryMultipleValues
                    || string.Compare(name, match.FieldDef.Name + Pluralized, StringComparison.OrdinalIgnoreCase) == 0;
                
                var value = strValue == null ? 
                      null 
                    : isMultiple ? 
                      TypeSerializer.DeserializeFromString(strValue, Array.CreateInstance(fieldType, 0).GetType())
                    : fieldType == typeof(string) ? 
                      strValue
                    : fieldType.IsValueType && !fieldType.IsEnum ? 
                      Convert.ChangeType(strValue, fieldType) :
                      TypeSerializer.DeserializeFromString(strValue, fieldType);

                AddCondition(q, defaultTerm, match.FieldDef, value, condition);
            }
        }

        class MatchQuery
        {
            public MatchQuery(Tuple<Type, PropertyInfo> match, QueryCondition condition)
            {
                ModelDef = match.Item1;
                FieldDef = match.Item2;
                Condition = condition;
            }
            public readonly Type ModelDef;
            public readonly PropertyInfo FieldDef;
            public readonly QueryCondition Condition;
        }

        private const string Pluralized = "s";

        private static MatchQuery GetQueryMatch(DataQuery<From> q, string name, IAutoQueryDataOptions options, Dictionary<string,string> aliases)
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

        private static MatchQuery GetQueryMatch(DataQuery<From> q, string name, IAutoQueryDataOptions options)
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

        public QueryResponse<Into> Execute<Into>(IQueryDataSource db, IDataQuery query)
        {
            try
            {
                var q = (DataQuery<From>)query;

                var response = new QueryResponse<Into>
                {
                    Offset = q.Offset.GetValueOrDefault(0),
                    Results = db.LoadSelect<Into, From>(q),
                };

                return response;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message, ex);
            }
        }
    }

    public static class AutoQueryDataExtensions
    {
        //public static QueryFieldAttribute Init(this QueryFieldAttribute query)
        //{
        //    query.ValueStyle = ValueStyle.Single;
        //    if (query.Template == null || query.ValueFormat != null) return query;

        //    var i = 0;
        //    while (query.Template.Contains("{Value" + (i + 1) + "}")) i++;
        //    if (i > 0)
        //    {
        //        query.ValueStyle = ValueStyle.Multiple;
        //        query.ValueArity = i;
        //    }
        //    else
        //    {
        //        query.ValueStyle = !query.Template.Contains("{Values}")
        //            ? ValueStyle.Single
        //            : ValueStyle.List;
        //    }
        //    return query;
        //}

        public static DataQuery<From> CreateQuery<From>(this IAutoQueryData autoQuery, IQueryData<From> model, IRequest request)
        {
            return autoQuery.CreateQuery(model, request.GetRequestParams(), request);
        }

        public static DataQuery<From> CreateQuery<From, Into>(this IAutoQueryData autoQuery, IQueryData<From, Into> model, IRequest request)
        {
            return autoQuery.CreateQuery(model, request.GetRequestParams(), request);
        }
    }
}