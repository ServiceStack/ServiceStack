using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        IQueryData Dto { get; }
        Dictionary<string, string> DynamicParams { get; }

        int? Offset { get; }
        IDataQuery Select(string[] fields);
    }

    public delegate IDataQuery QueryDataFilterDelegate(IDataQuery q, IQueryData dto, IRequest req);

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

        public static string GreaterThanOrEqualCondition = typeof(GreaterEqualCondition).Name;
        public static string GreaterThanCondition        = typeof(GreaterCondition).Name;
        public static string LessThanCondition           = typeof(LessCondition).Name;
        public static string LessThanOrEqualCondition    = typeof(LessEqualCondition).Name;
        public static string NotEqualCondition           = typeof(NotEqualCondition).Name;

        public Dictionary<string, QueryCondition> Conditions = new Dictionary<string, QueryCondition>
        {
            { typeof(GreaterEqualCondition).Name, new GreaterEqualCondition() },
            { typeof(GreaterCondition).Name, new GreaterCondition() },
            { typeof(LessCondition).Name, new LessCondition() },
            { typeof(LessEqualCondition).Name, new LessEqualCondition() },
            { typeof(NotEqualCondition).Name, new NotEqualCondition() },

            { typeof(CaseInsensitiveEqualCondition).Name, new CaseInsensitiveEqualCondition() },
            { typeof(InCollectionCondition).Name, new InCollectionCondition() },
            { typeof(InBetweenCondition).Name, new InBetweenCondition() },
            { typeof(StartsWithCondition).Name, new StartsWithCondition() },
            { typeof(ContainsCondition).Name, new ContainsCondition() },
            { typeof(EndsWithCondition).Name, new EndsWithCondition() },
        }; 

        public Dictionary<string, string> ImplicitConventions = new Dictionary<string, string> 
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

            {"%Like%",          typeof(CaseInsensitiveEqualCondition).Name },
            {"%In",             typeof(InCollectionCondition).Name },
            {"%Ids",            typeof(InCollectionCondition).Name },
            {"%Between%",       typeof(InBetweenCondition).Name },

            {"%StartsWith",     typeof(StartsWithCondition).Name },
            {"%Contains",       typeof(ContainsCondition).Name },
            {"%EndsWith",       typeof(EndsWithCondition).Name },
        };

        public Dictionary<string, QueryDataField> StartsWithConventions = new Dictionary<string, QueryDataField>();
        public Dictionary<string, QueryDataField> EndsWithConventions = new Dictionary<string, QueryDataField>();

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
                var conditionName = entry.Value;
                QueryCondition query;
                if (!Conditions.TryGetValue(conditionName, out query))
                    throw new NotSupportedException("No Condition registered with name '{0}'".Fmt(conditionName));

                if (entry.Key.EndsWith("%"))
                {
                    StartsWithConventions[key] = new QueryDataField
                    {
                        Term = QueryTerm.And,
                        Condition = conditionName,
                        QueryCondition = query,
                        Field = key,
                    };
                }
                if (entry.Key.StartsWith("%"))
                {
                    EndsWithConventions[key] = new QueryDataField
                    {
                        Term = QueryTerm.And,
                        Condition = conditionName,
                        QueryCondition = query,
                        Field = key,
                    };
                }
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

            if (EnableAutoQueryViewer && appHost.GetPlugin<AutoQueryMetadataFeature>() == null)
                appHost.LoadPlugin(new AutoQueryMetadataFeature());
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

        public AutoQueryDataFeature RegisterQueryFilter<Request, From>(Func<DataQuery<From>, Request, IRequest, DataQuery<From>> filterFn)
        {
            QueryFilters[typeof(Request)] = (q, dto, req) =>
                filterFn((DataQuery<From>)q, (Request)dto, req);

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
        public Func<object, object> FieldGetter { get; set; }
        public object Value { get; set; }

        public object GetFieldValue(object instance)
        {
            if (Field == null || FieldGetter == null)
                return null;

            return FieldGetter(instance);
        }

        public IEnumerable<T> Apply<T>(IEnumerable<T> source, IEnumerable<T> original)
        {
            if (Term != QueryTerm.Or)
            {
                var to = new List<T>();
                foreach (var item in source)
                {
                    var fieldValue = GetFieldValue(item);
                    if (Condition.Match(fieldValue, Value))
                        to.Add(item);
                }
                return to;
            }
            else
            {
                var to = new List<T>(source);
                foreach (var item in original)
                {
                    var fieldValue = GetFieldValue(item);
                    if (Condition.Match(fieldValue, Value) && !to.Contains(item))
                        to.Add(item);
                }
                return to;
            }
        }
    }

    public class DataQuery<T> : IDataQuery
    {
        private QueryDataContext context;

        public IQueryData Dto { get; private set; }
        public Dictionary<string, string> DynamicParams { get; private set; }
        public List<ConditionExpression> Conditions { get; set; }
        public int? Offset { get; set; }
        public int? Rows { get; set; }

        public DataQuery(QueryDataContext context)
        {
            this.context = context;
            this.Dto = context.Dto;
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
            var pi = typeof(T).GetProperties()
                .FirstOrDefault(x => string.Equals(x.Name, field, StringComparison.InvariantCultureIgnoreCase));
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

        public virtual DataQuery<T> AddCondition(QueryTerm term, QueryCondition condition, PropertyInfo field, Func<object, object> fieldGetter, object value)
        {
            this.Conditions.Add(new ConditionExpression
            {
                Term = term,
                Condition = condition,
                Field = field,
                FieldGetter = fieldGetter,
                Value = value,
            });
            return this;
        }

        public virtual DataQuery<T> And(Expression<Func<T, object>> fieldExpr, QueryCondition condition, object value)
        {
            var pi = fieldExpr.ToPropertyInfo();
            return AddCondition(QueryTerm.And, condition, pi, pi.GetValueGetter(), value);
        }

        public virtual DataQuery<T> Or(Expression<Func<T, object>> fieldExpr, QueryCondition condition, object value)
        {
            var pi = fieldExpr.ToPropertyInfo();
            return AddCondition(QueryTerm.Or, condition, pi, pi.GetValueGetter(), value);
        }

        public virtual DataQuery<T> From(string @from)
        {
            return this;
        }
    }

    public interface IAutoQueryData
    {
        DataQuery<From> CreateQuery<From>(IQueryData<From> dto, Dictionary<string, string> dynamicParams, IRequest req = null, IQueryDataSource db = null);

        QueryResponse<From> Execute<From>(IQueryData<From> request, DataQuery<From> q);

        DataQuery<From> CreateQuery<From, Into>(IQueryData<From, Into> dto, Dictionary<string, string> dynamicParams, IRequest req = null, IQueryDataSource db = null);

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
        Dictionary<string, QueryDataField> StartsWithConventions { get; set; }
        Dictionary<string, QueryDataField> EndsWithConventions { get; set; }
    }

    public class AutoQueryData : IAutoQueryData, IAutoQueryDataOptions, IDisposable
    {
        public int? MaxLimit { get; set; }
        public bool EnableUntypedQueries { get; set; }
        public bool OrderByPrimaryKeyOnLimitQuery { get; set; }
        public string RequiredRoleForRawSqlFilters { get; set; }
        public HashSet<string> IgnoreProperties { get; set; }
        public Dictionary<string, QueryDataField> StartsWithConventions { get; set; }
        public Dictionary<string, QueryDataField> EndsWithConventions { get; set; }

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

        public DataQuery<From> Filter<From>(IDataQuery q, IQueryData dto, IRequest req)
        {
            if (QueryFilters == null)
                return (DataQuery<From>)q;

            QueryDataFilterDelegate filterFn = null;
            if (!QueryFilters.TryGetValue(dto.GetType(), out filterFn))
            {
                foreach (var type in dto.GetType().GetInterfaces())
                {
                    if (QueryFilters.TryGetValue(type, out filterFn))
                        break;
                }
            }

            if (filterFn != null)
                return (DataQuery<From>)(filterFn(q, dto, req) ?? q);

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

        public DataQuery<From> CreateQuery<From>(IQueryData<From> dto, Dictionary<string, string> dynamicParams, IRequest req = null, IQueryDataSource db = null)
        {
            if (db != null)
                this.Db = db;

            var ctx = new QueryDataContext(dto, dynamicParams);
            var typedQuery = GetTypedQuery(dto.GetType(), typeof(From));
            return Filter<From>(typedQuery.CreateQuery(GetDb<From>(ctx), dto, dynamicParams, this), dto, req);
        }

        public QueryResponse<From> Execute<From>(IQueryData<From> request, DataQuery<From> q)
        {
            var typedQuery = GetTypedQuery(request.GetType(), typeof(From));
            return ResponseFilter(typedQuery.Execute<From>(Db, q), q, request);
        }

        public DataQuery<From> CreateQuery<From, Into>(IQueryData<From, Into> dto, Dictionary<string, string> dynamicParams, IRequest req = null, IQueryDataSource db = null)
        {
            if (db != null)
                this.Db = db;

            var ctx = new QueryDataContext(dto, dynamicParams);
            var typedQuery = GetTypedQuery(dto.GetType(), typeof(From));
            return Filter<From>(typedQuery.CreateQuery(GetDb<From>(ctx), dto, dynamicParams, this), dto, req);
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
        public QueryDataContext(IQueryData dto, Dictionary<string, string> dynamicParams)
        {
            this.Dto = dto;
            this.DynamicParams = dynamicParams;
        }

        public IQueryData Dto { get; private set; }
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

        public IEnumerable<T> GetFilteredData<From>(IEnumerable<T> data, DataQuery<From> q)
        {
            var source = data;
            for (var i = 0; i < q.Conditions.Count; i++)
            {
                var condition = q.Conditions[i];
                if (i == 0)
                    condition.Term = QueryTerm.And; //First condition always filters

                source = condition.Apply(source, data);
            }
            return source;
        } 

        public List<Into> LoadSelect<Into, From>(DataQuery<From> q)
        {
            var source = GetFilteredData(data, q);
            source = ApplyLimits(source, q.Offset, q.Rows);

            var rows = typeof(From) == typeof(Into)
                ? source.Map(x => (Into)x)
                : source.Map(x => x.ConvertTo<Into>());

            return rows;
        }

        private static IEnumerable<T> ApplyLimits(IEnumerable<T> source, int? skip, int? take)
        {
            if (skip != null)
                source = source.Skip(skip.Value);
            if (take != null)
                source = source.Take(take.Value);
            return source;
        }

        public int Count<T>(DataQuery<T> q)
        {
            var source = GetFilteredData(data, q);
            return source.Count();
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

    public class QueryDataField
    {
        public QueryTerm Term { get; set; }
        public string Condition { get; set; }
        public string Field { get; set; }
        public QueryCondition QueryCondition { get; set; }
    }

    public class TypedQueryData<QueryModel, From> : ITypedQueryData
    {
        private static ILog log = LogManager.GetLogger(typeof(AutoQueryDataFeature));

        static readonly Dictionary<string, Func<object, object>> RequestPropertyGetters =
            new Dictionary<string, Func<object, object>>();

        static readonly Dictionary<string, Func<object, object>> FromPropertyGetters =
            new Dictionary<string, Func<object, object>>();

        static readonly Dictionary<string, QueryDataField> QueryFieldMap =
            new Dictionary<string, QueryDataField>();

        static TypedQueryData()
        {
            var feature = HostContext.GetPlugin<AutoQueryDataFeature>();
            foreach (var pi in typeof(QueryModel).GetPublicProperties())
            {
                var fn = pi.GetValueGetter(typeof(QueryModel));
                RequestPropertyGetters[pi.Name] = fn;

                var queryAttr = pi.FirstAttribute<QueryDataFieldAttribute>();
                if (queryAttr != null)
                    QueryFieldMap[pi.Name] = queryAttr.ToField(pi, feature);
            }

            foreach (var pi in typeof(From).GetPublicProperties())
            {
                var fn = pi.GetValueGetter(typeof(From));
                FromPropertyGetters[pi.Name] = fn;
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

            var dtoAttr = request.GetType().FirstAttribute<QueryDataAttribute>();
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
                q.AddCondition(defaultTerm, AlwaysFalseCondition.Instance, null, null, null); //Empty OR queries should be empty
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
            foreach (var entry in RequestPropertyGetters)
            {
                var name = entry.Key.SplitOnFirst('#')[0];

                QueryDataField attr;
                if (QueryFieldMap.TryGetValue(name, out attr))
                {
                    if (attr.Field != null)
                        name = attr.Field;
                }

                var match = GetQueryMatch(q, name, options, aliases);
                if (match == null)
                    continue;

                if (attr == null)
                    attr = match.QueryField;

                if (attr != null)
                {
                    if (attr.Term == QueryTerm.Or)
                        defaultTerm = QueryTerm.Or;
                    else if (attr.Term == QueryTerm.And)
                        defaultTerm = QueryTerm.And;
                }

                var value = entry.Value(model);
                if (value == null)
                    continue;

                dynamicParams.Remove(entry.Key);

                AddCondition(q, defaultTerm, match.FieldDef, value, attr != null ? attr.QueryCondition : null);
            }
        }

        private static void AddCondition(DataQuery<From> q, QueryTerm defaultTerm, PropertyInfo property, object value, QueryCondition condition)
        {
            var isMultiple = condition is IQueryMultiple
                || (value is IEnumerable && !(value is string));

            if (condition != null)
            {
                if (condition.Term == QueryTerm.Or)
                    defaultTerm = QueryTerm.Or;
                else if (condition.Term == QueryTerm.And)
                    defaultTerm = QueryTerm.And;
            }
            else
            {
                condition = !isMultiple
                    ? (QueryCondition) EqualsCondition.Instance
                    : InCollectionCondition.Instance;
            }

            q.AddCondition(defaultTerm, condition, property, FromPropertyGetters[property.Name], value);
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
                var fieldType = Nullable.GetUnderlyingType(match.FieldDef.PropertyType)
                    ?? match.FieldDef.PropertyType;

                var isMultiple = condition is IQueryMultiple
                    || (fieldType.HasInterface(typeof(IEnumerable)) && fieldType != typeof(string))
                    || string.Compare(name, match.FieldDef.Name + Pluralized, StringComparison.OrdinalIgnoreCase) == 0;

                if (condition == null)
                {
                    condition = !isMultiple
                        ? (QueryCondition)EqualsCondition.Instance
                        : InCollectionCondition.Instance;
                }

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
            public MatchQuery(Tuple<Type, PropertyInfo> match, QueryDataField queryField)
            {
                ModelDef = match.Item1;
                FieldDef = match.Item2;
                QueryField = queryField;
                Condition = queryField != null ? queryField.QueryCondition : null;
            }
            public readonly Type ModelDef;
            public readonly PropertyInfo FieldDef;
            public readonly QueryDataField QueryField;
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
        public static QueryDataField ToField(this QueryDataFieldAttribute attr, PropertyInfo pi, AutoQueryDataFeature feature)
        {
            var to = new QueryDataField
            {
                Term = attr.Term,
                Field = attr.Field,
                Condition = attr.Condition,
            };

            if (attr.Condition != null)
            {
                QueryCondition queryCondition;
                if (!feature.Conditions.TryGetValue(attr.Condition, out queryCondition))
                    throw new NotSupportedException("No Condition registered with name '{0}' on [QueryDataField({1})]".Fmt(attr.Condition, attr.Field ?? pi.Name));

                to.QueryCondition = queryCondition;
            }

            return to;
        }

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