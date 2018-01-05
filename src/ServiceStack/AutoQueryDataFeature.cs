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
using ServiceStack.Caching;
using ServiceStack.DataAnnotations;
using ServiceStack.MiniProfiler;
using ServiceStack.Reflection;
using ServiceStack.Web;
using ServiceStack.Logging;
using ServiceStack.Text;

#if NETSTANDARD2_0
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack
{
    public interface IDataQuery
    {
        IQueryData Dto { get; }
        Dictionary<string, string> DynamicParams { get; }
        List<DataConditionExpression> Conditions { get; }
        OrderByExpression OrderBy { get; }
        HashSet<string> OnlyFields { get; }
        int? Offset { get; }
        int? Rows { get; }
        bool HasConditions { get; }

        Tuple<Type, PropertyInfo> FirstMatchingField(string name);

        void Select(string[] fields);
        void Join(Type joinType, Type type);
        void LeftJoin(Type joinType, Type type);
        void And(string field, QueryCondition condition, string value);
        void Or(string field, QueryCondition condition, string value);
        void AddCondition(QueryTerm defaultTerm, PropertyInfo field, QueryCondition condition, object value);
        void OrderByFields(string[] fieldNames);
        void OrderByFieldsDescending(string[] fieldNames);
        void OrderByPrimaryKey();
        void Limit(int? skip, int? take);
    }

    public interface IQueryDataSource<T> : IQueryDataSource { }
    public interface IQueryDataSource : IDisposable
    {
        IDataQuery From<T>();
        List<Into> LoadSelect<Into, From>(IDataQuery q);
        int Count(IDataQuery q);

        object SelectAggregate(IDataQuery q, string name, IEnumerable<string> args);
    }

    public class QueryDataContext
    {
        public IQueryData Dto { get; set; }
        public Dictionary<string, string> DynamicParams { get; set; }
        public IRequest Request { get; set; }
    }

    public delegate void QueryDataFilterDelegate(IDataQuery q, IQueryData dto, IRequest req);

    public class QueryDataFilterContext
    {
        public IQueryDataSource Db { get; set; }
        public List<Command> Commands { get; set; }
        public IQueryData Dto { get; set; }
        public IDataQuery Query { get; set; }
        public IQueryResponse Response { get; set; }
    }

    public class AutoQueryDataFeature : IPlugin, IPostInitPlugin
    {
        public HashSet<string> IgnoreProperties { get; set; }
        public HashSet<Assembly> LoadFromAssemblies { get; set; }
        public int? MaxLimit { get; set; }
        public bool IncludeTotal { get; set; }
        public bool EnableUntypedQueries { get; set; }
        public bool EnableAutoQueryViewer { get; set; }
        public bool OrderByPrimaryKeyOnPagedQuery { get; set; }
        public Type AutoQueryServiceBaseType { get; set; }
        public Dictionary<Type, QueryDataFilterDelegate> QueryFilters { get; set; }
        public List<Action<QueryDataFilterContext>> ResponseFilters { get; set; }
        public Action<TypeBuilder, MethodBuilder, Type> GenerateServiceFilter { get; set; }

        public ConcurrentDictionary<Type, Func<QueryDataContext, IQueryDataSource>> DataSources { get; private set; }

        public List<QueryCondition> Conditions = new List<QueryCondition>
        {
            new EqualsCondition(),
            new NotEqualCondition(),
            new LessEqualCondition(),
            new LessCondition(),
            new GreaterCondition(),
            new GreaterEqualCondition(),

            new StartsWithCondition(),
            new ContainsCondition(),
            new EndsWithCondition(),

            new InCollectionCondition(),
            new InBetweenCondition(),
            new CaseInsensitiveEqualCondition(),
        };

        public Dictionary<string, QueryCondition> ConditionsAliases =
            new Dictionary<string, QueryCondition>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> ImplicitConventions = new Dictionary<string, string>
        {
            {"%Above%",         ConditionAlias.Greater},
            {"Begin%",          ConditionAlias.Greater},
            {"%Beyond%",        ConditionAlias.Greater},
            {"%Over%",          ConditionAlias.Greater},
            {"%OlderThan",      ConditionAlias.Greater},
            {"%After%",         ConditionAlias.Greater},
            {"OnOrAfter%",      ConditionAlias.GreaterEqual},
            {"%From%",          ConditionAlias.GreaterEqual},
            {"Since%",          ConditionAlias.GreaterEqual},
            {"Start%",          ConditionAlias.GreaterEqual},
            {"%Higher%",        ConditionAlias.GreaterEqual},
            {">%",              ConditionAlias.GreaterEqual},
            {"%>",              ConditionAlias.Greater},
            {"%!",              ConditionAlias.NotEqual},
            {"%<>",             ConditionAlias.NotEqual},

            {"%GreaterThanOrEqualTo%", ConditionAlias.GreaterEqual},
            {"%GreaterThan%",          ConditionAlias.Greater},
            {"%LessThan%",             ConditionAlias.Less},
            {"%LessThanOrEqualTo%",    ConditionAlias.LessEqual},
            {"%NotEqualTo",            ConditionAlias.NotEqual},

            {"Behind%",         ConditionAlias.Less},
            {"%Below%",         ConditionAlias.Less},
            {"%Under%",         ConditionAlias.Less},
            {"%Lower%",         ConditionAlias.Less},
            {"%Before%",        ConditionAlias.Less},
            {"%YoungerThan",    ConditionAlias.Less},
            {"OnOrBefore%",     ConditionAlias.LessEqual},
            {"End%",            ConditionAlias.LessEqual},
            {"Stop%",           ConditionAlias.LessEqual},
            {"To%",             ConditionAlias.LessEqual},
            {"Until%",          ConditionAlias.LessEqual},
            {"%<",              ConditionAlias.LessEqual},
            {"<%",              ConditionAlias.Less},

            {"%Like%",          ConditionAlias.Like },
            {"%In",             ConditionAlias.In },
            {"%Ids",            ConditionAlias.In},
            {"%Between%",       ConditionAlias.Between },

            {"%StartsWith",     ConditionAlias.StartsWith },
            {"%Contains",       ConditionAlias.Contains },
            {"%EndsWith",       ConditionAlias.EndsWith },
        };

        public Dictionary<string, QueryDataField> StartsWithConventions = new Dictionary<string, QueryDataField>();
        public Dictionary<string, QueryDataField> EndsWithConventions = new Dictionary<string, QueryDataField>();

        public AutoQueryDataFeature()
        {
            IgnoreProperties = new HashSet<string>(new[] { "Skip", "Take", "OrderBy", "OrderByDesc", "Fields" },
                StringComparer.OrdinalIgnoreCase);
            AutoQueryServiceBaseType = typeof(AutoQueryDataServiceBase);
            QueryFilters = new Dictionary<Type, QueryDataFilterDelegate>();
            ResponseFilters = new List<Action<QueryDataFilterContext>> { IncludeAggregates };
            IncludeTotal = false;
            EnableUntypedQueries = true;
            EnableAutoQueryViewer = true;
            OrderByPrimaryKeyOnPagedQuery = true;
            LoadFromAssemblies = new HashSet<Assembly>();
            DataSources = new ConcurrentDictionary<Type, Func<QueryDataContext, IQueryDataSource>>();
        }

        public void Register(IAppHost appHost)
        {
            foreach (var condition in Conditions)
            {
                ConditionsAliases[condition.Alias] = condition;
            }

            foreach (var entry in ImplicitConventions)
            {
                var key = entry.Key.Trim('%');
                var conditioAlias = entry.Value;
                QueryCondition query;
                if (!ConditionsAliases.TryGetValue(conditioAlias, out query))
                    throw new NotSupportedException($"No Condition registered with name '{conditioAlias}'");

                if (entry.Key.EndsWith("%"))
                {
                    StartsWithConventions[key] = new QueryDataField
                    {
                        Term = QueryTerm.And,
                        Condition = conditioAlias,
                        QueryCondition = query,
                        Field = key,
                    };
                }
                if (entry.Key.StartsWith("%"))
                {
                    EndsWithConventions[key] = new QueryDataField
                    {
                        Term = QueryTerm.And,
                        Condition = conditioAlias,
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
                    IncludeTotal = IncludeTotal,
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

            ((ServiceStackHost)appHost).ServiceAssemblies.Each(x =>
            {
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
                AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
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

                GenerateServiceFilter?.Invoke(typeBuilder, method, requestType);

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

            var servicesType = typeBuilder.CreateTypeInfo().AsType();
            return servicesType;
        }

        public AutoQueryDataFeature RegisterQueryFilter<Request>(Action<IDataQuery, Request, IRequest> filterFn)
        {
            QueryFilters[typeof(Request)] = (q, dto, req) =>
                filterFn(q, (Request)dto, req);

            return this;
        }

        public AutoQueryDataFeature AddDataSource<T>(Func<QueryDataContext, IQueryDataSource<T>> dataSourceFactory)
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

        public void IncludeAggregates(QueryDataFilterContext ctx)
        {
            var commands = ctx.Commands;
            if (commands.Count == 0)
                return;

            var aggregateCommands = new List<Command>();
            foreach (var cmd in commands)
            {
                if (cmd.Args.Count == 0)
                    cmd.Args.Add(new StringSegment("*"));

                var result = ctx.Db.SelectAggregate(ctx.Query, cmd.Name.ToString(), cmd.Args.ToStringList());
                if (result == null)
                    continue;

                var hasAlias = !cmd.Suffix.IsNullOrWhiteSpace();
                var alias = cmd.ToString();
                if (hasAlias)
                {
                    alias = cmd.Suffix.TrimStart().ToString();
                    if (alias.StartsWithIgnoreCase("as "))
                        alias = alias.Substring("as ".Length);
                }

                ctx.Response.Meta[alias] = result.ToString();
            }

            ctx.Commands.RemoveAll(aggregateCommands.Contains);
        }
    }

    public class DataConditionExpression
    {
        public QueryTerm Term { get; set; }
        public QueryCondition QueryCondition { get; set; }
        public PropertyInfo Field { get; set; }
        public GetMemberDelegate FieldGetter { get; set; }
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
                    if (QueryCondition.Match(fieldValue, Value))
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
                    if (QueryCondition.Match(fieldValue, Value) && !to.Contains(item))
                        to.Add(item);
                }
                return to;
            }
        }
    }

    public abstract class FilterExpression
    {
        public abstract IEnumerable<T> Apply<T>(IEnumerable<T> source);
    }

    public class OrderByExpression : FilterExpression
    {
        public string[] FieldNames { get; private set; }
        public GetMemberDelegate[] FieldGetters { get; private set; }
        public bool[] OrderAsc { get; private set; }

        public OrderByExpression(string fieldName, GetMemberDelegate fieldGetter, bool orderAsc = true)
            : this(new[] { fieldName }, new[] { fieldGetter }, new[] { orderAsc }) { }

        public OrderByExpression(string[] fieldNames, GetMemberDelegate[] fieldGetters, bool[] orderAsc)
        {
            this.FieldNames = fieldNames;
            this.FieldGetters = fieldGetters;
            this.OrderAsc = orderAsc;
        }

        class OrderByComparator<T> : IComparer<T>
        {
            readonly GetMemberDelegate[] getters;
            readonly bool[] orderAsc;

            public OrderByComparator(GetMemberDelegate[] getters, bool[] orderAsc)
            {
                this.getters = getters;
                this.orderAsc = orderAsc;
            }

            public int Compare(T x, T y)
            {
                for (int i = 0; i < getters.Length; i++)
                {
                    var getter = getters[i];
                    var xVal = getter(x);
                    var yVal = getter(y);
                    var cmp = CompareTypeUtils.CompareTo(xVal, yVal);
                    if (cmp != 0)
                        return orderAsc[i] ? cmp : cmp * -1;
                }

                return 0;
            }
        }

        public override IEnumerable<T> Apply<T>(IEnumerable<T> source)
        {
            var to = source.ToList();
            to.Sort(new OrderByComparator<T>(FieldGetters, OrderAsc));
            return to;
        }
    }

    public class DataQuery<T> : IDataQuery
    {
        private static PropertyInfo PrimaryKey;

        private QueryDataContext context;

        public IQueryData Dto { get; private set; }
        public Dictionary<string, string> DynamicParams { get; private set; }
        public List<DataConditionExpression> Conditions { get; set; }
        public OrderByExpression OrderBy { get; set; }
        public HashSet<string> OnlyFields { get; set; }
        public int? Offset { get; set; }
        public int? Rows { get; set; }

        static DataQuery()
        {
            var pis = TypeProperties<T>.Instance.PublicPropertyInfos;
            PrimaryKey = pis.FirstOrDefault(x => x.HasAttribute<PrimaryKeyAttribute>())
                ?? pis.FirstOrDefault(x => x.HasAttribute<AutoIncrementAttribute>())
                ?? pis.FirstOrDefault(x => x.Name == IdUtils.IdField)
                ?? pis.FirstOrDefault();
        }

        public DataQuery(QueryDataContext context)
        {
            this.context = context;
            this.Dto = context.Dto;
            this.DynamicParams = context.DynamicParams;
            this.Conditions = new List<DataConditionExpression>();
        }

        public virtual bool HasConditions => Conditions.Count > 0;

        public virtual void Limit(int? skip, int? take)
        {
            this.Offset = skip;
            this.Rows = take;
        }

        public void Take(int take)
        {
            this.Rows = take;
        }

        public virtual void Select(string[] fields)
        {
            this.OnlyFields = fields == null || fields.Length == 0
                ? null //All Fields
                : new HashSet<string>(fields, StringComparer.OrdinalIgnoreCase);
        }

        public virtual Tuple<Type, PropertyInfo> FirstMatchingField(string field)
        {
            var pi = typeof(T).GetProperties()
                .FirstOrDefault(x => string.Equals(x.Name, field, StringComparison.OrdinalIgnoreCase));
            return pi != null
                ? Tuple.Create(typeof(T), pi)
                : null;
        }

        public virtual void OrderByFields(params string[] fieldNames)
        {
            OrderByFieldsImpl(fieldNames, x => x[0] != '-');
        }

        public virtual void OrderByFieldsDescending(params string[] fieldNames)
        {
            OrderByFieldsImpl(fieldNames, x => x[0] == '-');
        }

        void OrderByFieldsImpl(string[] fieldNames, Func<string, bool> orderFn)
        {
            var getters = new List<GetMemberDelegate>();
            var orderAscs = new List<bool>();
            var fields = new List<string>();

            foreach (var fieldName in fieldNames)
            {
                if (string.IsNullOrEmpty(fieldName))
                    continue;

                var getter = TypeProperties<T>.Instance.GetPublicGetter(fieldName.TrimStart('-'));
                if (getter == null)
                    continue;

                var orderAsc = orderFn(fieldName);

                fields.Add(fieldName);
                getters.Add(getter);
                orderAscs.Add(orderAsc);
            }

            if (getters.Count > 0)
            {
                OrderBy = new OrderByExpression(fields.ToArray(), getters.ToArray(), orderAscs.ToArray());
            }
        }

        public virtual void OrderByPrimaryKey()
        {
            OrderBy = new OrderByExpression(PrimaryKey.Name, TypeProperties<T>.Instance.GetPublicGetter(PrimaryKey));
        }

        public virtual void Join(Type joinType, Type type)
        {
        }

        public virtual void LeftJoin(Type joinType, Type type)
        {
        }

        public virtual void AddCondition(QueryTerm term, PropertyInfo field, QueryCondition condition, object value)
        {
            this.Conditions.Add(new DataConditionExpression
            {
                Term = term,
                Field = field,
                FieldGetter = TypeProperties<T>.Instance.GetPublicGetter(field),
                QueryCondition = condition,
                Value = value,
            });
        }

        public virtual void And(string field, QueryCondition condition, string value)
        {
            AddCondition(QueryTerm.And, TypeProperties<T>.Instance.GetPublicProperty(field), condition, value);
        }

        public virtual void Or(string field, QueryCondition condition, string value)
        {
            AddCondition(QueryTerm.Or, TypeProperties<T>.Instance.GetPublicProperty(field), condition, value);
        }
    }

    public interface IAutoQueryData
    {
        ITypedQueryData GetTypedQuery(Type requestDtoType, Type fromType);

        DataQuery<From> CreateQuery<From>(IQueryData<From> dto, Dictionary<string, string> dynamicParams, IRequest req = null, IQueryDataSource db = null);

        QueryResponse<From> Execute<From>(IQueryData<From> request, DataQuery<From> q);

        DataQuery<From> CreateQuery<From, Into>(IQueryData<From, Into> dto, Dictionary<string, string> dynamicParams, IRequest req = null, IQueryDataSource db = null);

        QueryResponse<Into> Execute<From, Into>(IQueryData<From, Into> request, DataQuery<From> q);
        
        IDataQuery CreateQuery(IQueryData dto, Dictionary<string, string> dynamicParams, IRequest req = null, IQueryDataSource db = null);

        IQueryResponse Execute(IQueryData request, IDataQuery q);
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
        bool IncludeTotal { get; set; }
        bool EnableUntypedQueries { get; set; }
        bool OrderByPrimaryKeyOnLimitQuery { get; set; }
        HashSet<string> IgnoreProperties { get; set; }
        Dictionary<string, QueryDataField> StartsWithConventions { get; set; }
        Dictionary<string, QueryDataField> EndsWithConventions { get; set; }
    }

    public class AutoQueryData : IAutoQueryData, IAutoQueryDataOptions, IDisposable
    {
        public int? MaxLimit { get; set; }
        public bool IncludeTotal { get; set; }
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
            Db?.Dispose();
        }

        private static Dictionary<Type, ITypedQueryData> TypedQueries = new Dictionary<Type, ITypedQueryData>();

        public Type GetFromType(Type requestDtoType)
        {
            var intoTypeDef = requestDtoType.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryData<,>));
            if (intoTypeDef != null)
            {
                var args = intoTypeDef.GetGenericArguments();
                return args[1];
            }
            
            var typeDef = requestDtoType.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryData<>));
            if (typeDef != null)
            {
                var args = typeDef.GetGenericArguments();
                return args[0];
            }

            throw new NotSupportedException("Request DTO is not an AutoQuery Data DTO: " + requestDtoType.Name);
        }

        public ITypedQueryData GetTypedQuery(Type requestDtoType, Type fromType)
        {
            if (TypedQueries.TryGetValue(requestDtoType, out var defaultValue)) return defaultValue;

            var genericType = typeof(TypedQueryData<,>).MakeGenericType(requestDtoType, fromType);
            defaultValue = genericType.CreateInstance<ITypedQueryData>();

            Dictionary<Type, ITypedQueryData> snapshot, newCache;
            do
            {
                snapshot = TypedQueries;
                newCache = new Dictionary<Type, ITypedQueryData>(TypedQueries) { [requestDtoType] = defaultValue };

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref TypedQueries, newCache, snapshot), snapshot));

            return defaultValue;
        }

        public DataQuery<From> Filter<From>(IDataQuery q, IQueryData dto, IRequest req)
        {
            if (QueryFilters == null)
                return (DataQuery<From>)q;

            if (!QueryFilters.TryGetValue(dto.GetType(), out var filterFn))
            {
                foreach (var type in dto.GetType().GetInterfaces())
                {
                    if (QueryFilters.TryGetValue(type, out filterFn))
                        break;
                }
            }

            filterFn?.Invoke(q, dto, req);

            return (DataQuery<From>)q;
        }

        public IDataQuery Filter(IDataQuery q, IQueryData dto, IRequest req)
        {
            if (QueryFilters == null)
                return q;

            if (!QueryFilters.TryGetValue(dto.GetType(), out var filterFn))
            {
                foreach (var type in dto.GetType().GetInterfaces())
                {
                    if (QueryFilters.TryGetValue(type, out filterFn))
                        break;
                }
            }

            filterFn?.Invoke(q, dto, req);

            return q;
        }

        public QueryResponse<Into> ResponseFilter<From, Into>(QueryResponse<Into> response, DataQuery<From> expr, IQueryData dto)
        {
            response.Meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var commands = dto.Include.ParseCommands();

            var ctx = new QueryDataFilterContext
            {
                Db = Db,
                Commands = commands,
                Dto = dto,
                Query = expr,
                Response = response,
            };

            var totalCommand = commands.FirstOrDefault(x => x.Name.EqualsIgnoreCase("Total"));
            if (totalCommand != null)
            {
                totalCommand.Name = "COUNT".ToStringSegment();
            }

            var totalRequested = commands.Any(x =>
                x.Name.EqualsIgnoreCase("COUNT") &&
                (x.Args.Count == 0 || (x.Args.Count == 1 && x.Args[0].Equals("*"))));

            if (IncludeTotal || totalRequested)
            {
                if (!totalRequested)
                    commands.Add(new Command { Name = "COUNT".ToStringSegment(), Args = { "*".ToStringSegment() } });

                foreach (var responseFilter in ResponseFilters)
                {
                    responseFilter(ctx);
                }

                response.Total = response.Meta.TryGetValue("COUNT(*)", out var total)
                    ? total.ToInt()
                    : (int)Db.Count(expr); //fallback if it's not populated (i.e. if stripped by custom ResponseFilter)

                //reduce payload on wire
                if (totalCommand != null || !totalRequested)
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

        public IQueryDataSource GetDb<From>(QueryDataContext ctx) => GetDb(ctx, typeof(From));
        public IQueryDataSource GetDb(QueryDataContext ctx, Type type)
        {
            if (Db != null)
                return Db;

            var dataSourceFactory = HostContext.GetPlugin<AutoQueryDataFeature>().GetDataSource(type);
            if (dataSourceFactory == null)
                throw new NotSupportedException($"No datasource was registered on AutoQueryDataFeature for Type '{type.Name}'");

            return Db = dataSourceFactory(ctx);
        }

        public DataQuery<From> CreateQuery<From>(IQueryData<From> dto, Dictionary<string, string> dynamicParams, IRequest req = null, IQueryDataSource db = null)
        {
            if (db != null)
                this.Db = db;

            var ctx = new QueryDataContext { Dto = dto, DynamicParams = dynamicParams, Request = req };
            var typedQuery = GetTypedQuery(dto.GetType(), typeof(From));
            var q = typedQuery.CreateQuery(GetDb<From>(ctx));
            return Filter<From>(typedQuery.AddToQuery(q, dto, dynamicParams, this), dto, req);
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

            var ctx = new QueryDataContext { Dto = dto, DynamicParams = dynamicParams, Request = req };
            var typedQuery = GetTypedQuery(dto.GetType(), typeof(From));
            var q = typedQuery.CreateQuery(GetDb<From>(ctx));
            return Filter<From>(typedQuery.AddToQuery(q, dto, dynamicParams, this), dto, req);
        }

        public QueryResponse<Into> Execute<From, Into>(IQueryData<From, Into> request, DataQuery<From> q)
        {
            var typedQuery = GetTypedQuery(request.GetType(), typeof(From));
            return ResponseFilter(typedQuery.Execute<Into>(Db, q), q, request);
        }

        public IDataQuery CreateQuery(IQueryData requestDto, Dictionary<string, string> dynamicParams, IRequest req = null, IQueryDataSource db = null)
        {
            if (db != null)
                this.Db = db;

            var ctx = new QueryDataContext { Dto = requestDto, DynamicParams = dynamicParams, Request = req };
            var requestDtoType = requestDto.GetType();
            var fromType = GetFromType(requestDtoType);
            var typedQuery = GetTypedQuery(requestDtoType, fromType);
            var q = typedQuery.CreateQuery(GetDb(ctx, fromType));
            return Filter(typedQuery.AddToQuery(q, requestDto, dynamicParams, this), requestDto, req);
        }

        private Dictionary<Type, GenericAutoQueryData> genericAutoQueryCache = new Dictionary<Type, GenericAutoQueryData>();

        public IQueryResponse Execute(IQueryData request, IDataQuery q)
        {
            var requestDtoType = request.GetType();
            
            Type fromType;
            Type intoType;
            var intoTypeDef = requestDtoType.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryData<,>));
            if (intoTypeDef != null)
            {
                var args = intoTypeDef.GetGenericArguments();
                fromType = args[0];
                intoType = args[1];
            }
            else
            {
                var typeDef = requestDtoType.GetTypeWithGenericTypeDefinitionOf(typeof(IQueryData<>));
                var args = typeDef.GetGenericArguments();
                fromType = args[0];
                intoType = args[0];
            }

            if (genericAutoQueryCache.TryGetValue(fromType, out GenericAutoQueryData typedApi))
                return typedApi.ExecuteObject(this, request, q);

            var genericType = typeof(GenericAutoQueryData<,>).MakeGenericType(fromType, intoType);
            var instance = genericType.CreateInstance<GenericAutoQueryData>();
            
            Dictionary<Type, GenericAutoQueryData> snapshot, newCache;
            do
            {
                snapshot = genericAutoQueryCache;
                newCache = new Dictionary<Type, GenericAutoQueryData>(genericAutoQueryCache)
                {
                    [requestDtoType] = instance
                };

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref genericAutoQueryCache, newCache, snapshot), snapshot));

            return instance.ExecuteObject(this, request, q);
        }
    }

    internal abstract class GenericAutoQueryData
    {
        public abstract IQueryResponse ExecuteObject(AutoQueryData autoQuery, IQueryData request, IDataQuery query);
    }
    
    internal class GenericAutoQueryData<From, Into> : GenericAutoQueryData
    {
        public override IQueryResponse ExecuteObject(AutoQueryData autoQuery, IQueryData request, IDataQuery query)
        {
            var typedQuery = autoQuery.GetTypedQuery(request.GetType(), typeof(From));
            var q = (DataQuery<From>)query;
            return autoQuery.ResponseFilter(typedQuery.Execute<Into>(autoQuery.Db, q), q, request);
        }
    }

    public class MemoryDataSource<T> : QueryDataSource<T>
    {
        public IEnumerable<T> Data { get; }

        public MemoryDataSource(QueryDataContext context, IEnumerable<T> data) : base(context)
        {
            this.Data = data;
        }

        public MemoryDataSource(IEnumerable<T> data, IQueryData dto, IRequest req)
            : this(new QueryDataContext
            {
                Dto = dto,
                Request = req,
                DynamicParams = req.GetRequestParams(),
            }, data)
        { }

        public override IEnumerable<T> GetDataSource(IDataQuery q)
        {
            return Data;
        }
    }

    public abstract class QueryDataSource<T> : IQueryDataSource<T>
    {
        private readonly QueryDataContext context;

        protected QueryDataSource(QueryDataContext context)
        {
            this.context = context;
        }

        public virtual IDataQuery From<TSource>()
        {
            return new DataQuery<TSource>(context);
        }

        public abstract IEnumerable<T> GetDataSource(IDataQuery q);

        public virtual IEnumerable<T> ApplyConditions(IEnumerable<T> data, IEnumerable<DataConditionExpression> conditions)
        {
            var source = data;
            var i = 0;
            foreach (var condition in conditions)
            {
                if (i++ == 0)
                    condition.Term = QueryTerm.And; //First condition always filters

                source = condition.Apply(source, data);
            }

            return source;
        }

        public virtual List<Into> LoadSelect<Into, From>(IDataQuery q)
        {
            var data = GetDataSource(q);
            var source = ApplyConditions(data, q.Conditions);
            source = ApplySorting(source, q.OrderBy);
            source = ApplyLimits(source, q.Offset, q.Rows);

            var to = new List<Into>();

            foreach (var item in source)
            {
                var into = typeof(From) == typeof(Into)
                    ? (Into)(object)item
                    : item.ConvertTo<Into>();

                //ConvertTo<T> short-circuits to instance cast when types match, we to mutate a copy instead
                if (typeof(From) == typeof(Into) && q.OnlyFields != null)
                {
                    into = typeof(Into).CreateInstance<Into>();
                    into.PopulateWith(item);
                }

                to.Add(into);

                if (q.OnlyFields == null)
                    continue;

                foreach (var entry in TypeProperties<Into>.Instance.PropertyMap)
                {
                    var propType = entry.Value;
                    if (q.OnlyFields.Contains(entry.Key))
                        continue;

                    var setter = propType.PublicSetter;
                    if (setter == null)
                        continue;

                    var defaultValue = propType.PropertyInfo.PropertyType.GetDefaultValue();
                    setter(into, defaultValue);
                }
            }

            return to;
        }

        public virtual IEnumerable<T> ApplySorting(IEnumerable<T> source, OrderByExpression orderBy)
        {
            return orderBy != null ? orderBy.Apply(source) : source;
        }

        public virtual IEnumerable<T> ApplyLimits(IEnumerable<T> source, int? skip, int? take)
        {
            if (skip != null)
                source = source.Skip(skip.Value);
            if (take != null)
                source = source.Take(take.Value);
            return source;
        }

        public virtual int Count(IDataQuery q)
        {
            var source = ApplyConditions(GetDataSource(q), q.Conditions);
            return source.Count();
        }

        public virtual object SelectAggregate(IDataQuery q, string name, IEnumerable<string> args)
        {
            name = name?.ToUpper() ?? throw new ArgumentNullException(nameof(name));

            if (name != "COUNT" && name != "MIN" && name != "MAX" && name != "AVG" && name != "SUM"
                && name != "FIRST" && name != "LAST")
                return null;

            var query = (DataQuery<T>)q;

            var argsArray = args.ToArray();
            var firstArg = argsArray.FirstOrDefault();
            string modifier = null;
            if (firstArg != null && firstArg.StartsWithIgnoreCase("DISTINCT "))
            {
                modifier = "DISTINCT";
                firstArg = firstArg.Substring(modifier.Length + 1);
            }

            if (name == "COUNT" && (firstArg == null || firstArg == "*"))
                return Count(q);

            var firstGetter = TypeProperties<T>.Instance.GetPublicGetter(firstArg);
            if (firstGetter == null)
                return null;

            var data = ApplyConditions(GetDataSource(q), query.Conditions);
            if (name == "FIRST" || name == "LAST")
                data = ApplySorting(data, q.OrderBy);

            var source = data.ToArray();

            switch (name)
            {
                case "COUNT":
                    if (modifier == "DISTINCT")
                    {
                        var results = new HashSet<object>();
                        foreach (var item in source)
                        {
                            results.Add(firstGetter(item));
                        }
                        return results.Count;
                    }

                    return CompareTypeUtils.Aggregate(source,
                        (acc, next) => CompareTypeUtils.Add(acc, firstGetter(next)), 0);

                case "MIN":
                    return CompareTypeUtils.Aggregate(source,
                        (acc, next) => CompareTypeUtils.Min(acc, firstGetter(next)));

                case "MAX":
                    return CompareTypeUtils.Aggregate(source,
                        (acc, next) => CompareTypeUtils.Max(acc, firstGetter(next)));

                case "SUM":
                    return CompareTypeUtils.Sum(source.Map(x => firstGetter(x)));

                case "AVG":
                    object sum = CompareTypeUtils.Sum(source.Map(x => firstGetter(x)));
                    var sumDouble = (double)Convert.ChangeType(sum, typeof(double));
                    return sumDouble / source.Length;

                case "FIRST":
                    return source.Length > 0 ? firstGetter(source[0]) : null;

                case "LAST":
                    return source.Length > 0 ? firstGetter(source[source.Length - 1]) : null;
            }

            return null;
        }

        public virtual void Dispose() { }
    }

    public interface ITypedQueryData
    {
        IDataQuery CreateQuery(IQueryDataSource db);

        IDataQuery AddToQuery(
            IDataQuery q,
            IQueryData request,
            Dictionary<string, string> dynamicParams,
            IAutoQueryDataOptions options = null);

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
        static readonly Dictionary<string, GetMemberDelegate> RequestPropertyGetters =
            new Dictionary<string, GetMemberDelegate>();

        static readonly Dictionary<string, QueryDataField> QueryFieldMap =
            new Dictionary<string, QueryDataField>();

        static TypedQueryData()
        {
            var feature = HostContext.GetPlugin<AutoQueryDataFeature>();
            foreach (var pi in typeof(QueryModel).GetPublicProperties())
            {
                var fn = pi.CreateGetter();
                RequestPropertyGetters[pi.Name] = fn;

                var queryAttr = pi.FirstAttribute<QueryDataFieldAttribute>();
                if (queryAttr != null)
                    QueryFieldMap[pi.Name] = queryAttr.ToField(pi, feature);
            }
        }

        public Type QueryType { get; } = typeof(QueryModel);
        public Type FromType { get; } = typeof(From);
        
        public IDataQuery CreateQuery(IQueryDataSource db) => db.From<From>();

        public IDataQuery AddToQuery(
            IDataQuery q,
            IQueryData request,
            Dictionary<string, string> dynamicParams,
            IAutoQueryDataOptions options = null)
        {
            dynamicParams = new Dictionary<string, string>(dynamicParams, StringComparer.OrdinalIgnoreCase);

            AppendJoins(q, request);

            AppendLimits(q, request, options);

            var dtoAttr = request.GetType().FirstAttribute<QueryDataAttribute>();
            var defaultTerm = dtoAttr != null && dtoAttr.DefaultTerm == QueryTerm.Or ? QueryTerm.Or : QueryTerm.And;

            var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var props = typeof(From).GetProperties();
            foreach (var pi in props)
            {
                var attr = pi.FirstAttribute<DataMemberAttribute>();
                if (attr?.Name == null) continue;
                aliases[attr.Name] = pi.Name;
            }

            AppendTypedQueries(q, request, dynamicParams, defaultTerm, options, aliases);

            if (options != null && options.EnableUntypedQueries)
            {
                AppendUntypedQueries(q, dynamicParams, defaultTerm, options, aliases);
            }

            if (defaultTerm == QueryTerm.Or && !q.HasConditions)
            {
                q.AddCondition(defaultTerm, null, AlwaysFalseCondition.Instance, null); //Empty OR queries should be empty
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

        private static readonly char[] FieldSeperators = new[] { ',', ';' };

        private static void AppendLimits(IDataQuery q, IQueryData dto, IAutoQueryDataOptions options)
        {
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
                q.OrderByPrimaryKey();
            }

            var maxLimit = options?.MaxLimit;
            var take = dto.Take ?? maxLimit;
            if (take > maxLimit)
                take = maxLimit;
            q.Limit(dto.Skip, take);
        }

        private static void AppendJoins(IDataQuery q, IQueryData dto)
        {
            if (dto is IJoin)
            {
                var dtoInterfaces = dto.GetType().GetInterfaces();
                foreach (var innerJoin in dtoInterfaces.Where(x => x.Name.StartsWith("IJoin`")))
                {
                    var joinTypes = innerJoin.GetGenericArguments();
                    for (var i = 1; i < joinTypes.Length; i++)
                    {
                        q.Join(joinTypes[i - 1], joinTypes[i]);
                    }
                }

                foreach (var leftJoin in dtoInterfaces.Where(x => x.Name.StartsWith("ILeftJoin`")))
                {
                    var joinTypes = leftJoin.GetGenericArguments();
                    for (var i = 1; i < joinTypes.Length; i++)
                    {
                        q.LeftJoin(joinTypes[i - 1], joinTypes[i]);
                    }
                }
            }
        }

        private static void AppendTypedQueries(IDataQuery q, IQueryData dto, Dictionary<string, string> dynamicParams, QueryTerm defaultTerm, IAutoQueryDataOptions options, Dictionary<string, string> aliases)
        {
            foreach (var entry in RequestPropertyGetters)
            {
                var name = entry.Key.LeftPart('#');

                if (QueryFieldMap.TryGetValue(name, out var attr))
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

                var value = entry.Value(dto);
                if (value == null)
                    continue;

                dynamicParams.Remove(entry.Key);

                AddCondition(q, defaultTerm, match.FieldDef, value, attr?.QueryCondition);
            }
        }

        private static void AddCondition(IDataQuery q, QueryTerm defaultTerm, PropertyInfo property, object value, QueryCondition condition)
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
                    ? (QueryCondition)EqualsCondition.Instance
                    : InCollectionCondition.Instance;
            }

            q.AddCondition(defaultTerm, property, condition, value);
        }

        private static void AppendUntypedQueries(IDataQuery q, Dictionary<string, string> dynamicParams, QueryTerm defaultTerm, IAutoQueryDataOptions options, Dictionary<string, string> aliases)
        {
            foreach (var entry in dynamicParams)
            {
                var name = entry.Key.LeftPart('#');

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
                    : strValue.ChangeTo(fieldType);

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
                Condition = queryField?.QueryCondition;
            }
            public readonly Type ModelDef;
            public readonly PropertyInfo FieldDef;
            public readonly QueryDataField QueryField;
            public readonly QueryCondition Condition;
        }

        private const string Pluralized = "s";

        private static MatchQuery GetQueryMatch(IDataQuery q, string name, IAutoQueryDataOptions options, Dictionary<string, string> aliases)
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

        private static MatchQuery GetQueryMatch(IDataQuery q, string name, IAutoQueryDataOptions options)
        {
            if (options == null) return null;

            var match = options.IgnoreProperties == null || !options.IgnoreProperties.Contains(name)
                ? q.FirstMatchingField(name) ?? (name.EndsWith(Pluralized) ? q.FirstMatchingField(name.Substring(0, name.Length - 1)) : null)
                : null;

            if (match == null)
            {
                foreach (var startsWith in options.StartsWithConventions)
                {
                    if (name.Length <= startsWith.Key.Length || !name.StartsWith(startsWith.Key)) continue;

                    var field = name.Substring(startsWith.Key.Length);
                    match = q.FirstMatchingField(field) ?? (field.EndsWith(Pluralized) ? q.FirstMatchingField(field.Substring(0, field.Length - 1)) : null);
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
                    match = q.FirstMatchingField(field) ?? (field.EndsWith(Pluralized) ? q.FirstMatchingField(field.Substring(0, field.Length - 1)) : null);
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
                if (!feature.ConditionsAliases.TryGetValue(attr.Condition, out queryCondition))
                    throw new NotSupportedException($"No Condition registered with name '{attr.Condition}' on [QueryDataField({attr.Field ?? pi.Name})]");

                to.QueryCondition = queryCondition;
            }

            return to;
        }

        public static DataQuery<From> CreateQuery<From>(this IAutoQueryData autoQuery, IQueryData<From> model, IRequest request, IQueryDataSource db = null)
        {
            return autoQuery.CreateQuery(model, request.GetRequestParams(), request, db);
        }

        public static DataQuery<From> CreateQuery<From, Into>(this IAutoQueryData autoQuery, IQueryData<From, Into> model, IRequest request, IQueryDataSource db = null)
        {
            return autoQuery.CreateQuery(model, request.GetRequestParams(), request, db);
        }

        public static IQueryDataSource<T> MemorySource<T>(this QueryDataContext ctx, IEnumerable<T> soruce)
        {
            return new MemoryDataSource<T>(ctx, soruce);
        }

        public static IQueryDataSource<T> MemorySource<T>(this QueryDataContext ctx, Func<IEnumerable<T>> soruceFn, ICacheClient cache, TimeSpan? expiresIn = null, string cacheKey = null)
        {
            if (cacheKey == null)
                cacheKey = "aqd:" + typeof(T).Name;

            var cachedResults = cache.Get<List<T>>(cacheKey);
            if (cachedResults != null)
                return new MemoryDataSource<T>(ctx, cachedResults);

            var results = soruceFn();
            var source = new MemoryDataSource<T>(ctx, results);
            return source.CacheMemorySource(cache, cacheKey, expiresIn);
        }

        public static void And<T>(this IDataQuery q, Expression<Func<T, object>> fieldExpr, QueryCondition condition, object value)
        {
            var pi = fieldExpr.ToPropertyInfo();
            q.AddCondition(QueryTerm.And, pi, condition, value);
        }

        public static void Or<T>(this IDataQuery q, Expression<Func<T, object>> fieldExpr, QueryCondition condition, object value)
        {
            var pi = fieldExpr.ToPropertyInfo();
            q.AddCondition(QueryTerm.Or, pi, condition, value);
        }
    }
}