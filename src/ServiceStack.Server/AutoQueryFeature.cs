using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

using Funq;
using ServiceStack.Host;
using ServiceStack.MiniProfiler;
using ServiceStack.Reflection;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack
{
    public delegate ISqlExpression QueryFilterDelegate(IRequest request, ISqlExpression sqlExpression, IQuery model);

    public class AutoQueryFeature : IPlugin, IPostInitPlugin
    {
        public HashSet<string> IgnoreProperties { get; set; }
        public HashSet<string> IllegalSqlFragmentTokens { get; set; }
        public HashSet<Assembly> LoadFromAssemblies { get; set; } 
        public int? MaxLimit { get; set; }
        public string UseNamedConnection { get; set; }
        public bool EnableUntypedQueries { get; set; }
        public bool EnableRawSqlFilters { get; set; }
        public bool OrderByPrimaryKeyOnPagedQuery { get; set; }
        public Type AutoQueryServiceBaseType { get; set; }
        public Dictionary<Type, QueryFilterDelegate> QueryFilters { get; set; }

        public const string GreaterThanOrEqualFormat = "{Field} >= {Value}";
        public const string GreaterThanFormat =        "{Field} > {Value}";
        public const string LessThanFormat =           "{Field} < {Value}";
        public const string LessThanOrEqualFormat =    "{Field} <= {Value}";
        
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

            {"%GreaterThanOrEqualTo%", GreaterThanOrEqualFormat},
            {"%GreaterThan%",          GreaterThanFormat},
            {"%LessThan%",             LessThanFormat},
            {"%LessThanOrEqualTo%",    LessThanOrEqualFormat},

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

            {"Like%",           "UPPER({Field}) LIKE UPPER({Value})"},
            {"%In",             "{Field} IN ({Values})"},
            {"%Ids",            "{Field} IN ({Values})"},
            {"%Between%",       "{Field} BETWEEN {Value1} AND {Value2}"},
        };

        public Dictionary<string, QueryFieldAttribute> StartsWithConventions =
            new Dictionary<string, QueryFieldAttribute>();

        public Dictionary<string, QueryFieldAttribute> EndsWithConventions = new Dictionary<string, QueryFieldAttribute>
        {
            { "StartsWith", new QueryFieldAttribute { Template = "UPPER({Field}) LIKE UPPER({Value})", ValueFormat = "{0}%" }},
            { "Contains", new QueryFieldAttribute { Template = "UPPER({Field}) LIKE UPPER({Value})", ValueFormat = "%{0}%" }},
            { "EndsWith", new QueryFieldAttribute { Template = "UPPER({Field}) LIKE UPPER({Value})", ValueFormat = "%{0}" }},
        };

        public AutoQueryFeature()
        {
            IgnoreProperties = new HashSet<string>(new[] { "Skip", "Take", "OrderBy", "OrderByDesc", "_select", "_from", "_join", "_where" }, 
                StringComparer.OrdinalIgnoreCase);
            IllegalSqlFragmentTokens = new HashSet<string>();
            AutoQueryServiceBaseType = typeof(AutoQueryServiceBase);
            QueryFilters = new Dictionary<Type, QueryFilterDelegate>();
            EnableUntypedQueries = true;
            OrderByPrimaryKeyOnPagedQuery = true;
            LoadFromAssemblies = new HashSet<Assembly>();
        }

        public void Register(IAppHost appHost)
        {
            foreach (var entry in ImplicitConventions)
            {
                var key = entry.Key.Trim('%');
                var fmt = entry.Value;
                var query = new QueryFieldAttribute { Template = fmt }.Init();
                if (entry.Key.EndsWith("%"))
                    StartsWithConventions[key] = query;
                if (entry.Key.StartsWith("%"))
                    EndsWithConventions[key] = query;
            }

            appHost.GetContainer().Register<IAutoQuery>(c =>
                new AutoQuery
                {
                    IgnoreProperties = IgnoreProperties,                    
                    IllegalSqlFragmentTokens = IllegalSqlFragmentTokens,
                    MaxLimit = MaxLimit,
                    EnableUntypedQueries = EnableUntypedQueries,
                    EnableSqlFilters = EnableRawSqlFilters,
                    OrderByPrimaryKeyOnLimitQuery = OrderByPrimaryKeyOnPagedQuery,
                    QueryFilters = QueryFilters,
                    StartsWithConventions = StartsWithConventions,
                    EndsWithConventions = EndsWithConventions,
                    Db = UseNamedConnection != null
                        ? c.Resolve<IDbConnectionFactory>().OpenDbConnection(UseNamedConnection)
                        : c.Resolve<IDbConnectionFactory>().OpenDbConnection(),
                })
                .ReusedWithin(ReuseScope.None);

            appHost.Metadata.GetOperationAssemblies()
                .Each(x => LoadFromAssemblies.Add(x));
        }

        public void AfterPluginsLoaded(IAppHost appHost)
        {
            var scannedTypes = LoadFromAssemblies.SelectMany(x => x.GetTypes());

            var misingRequestTypes = scannedTypes
                .Where(x => x.HasInterface(typeof(IQuery)))
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
                .DefineType("__AutoQueryServices",
                    TypeAttributes.Public | TypeAttributes.Class,
                    AutoQueryServiceBaseType);

            var emptyCtor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            var baseType = AutoQueryServiceBaseType.BaseType;
            if (baseType != null)
            {
                var ctorIl = emptyCtor.GetILGenerator();
                ctorIl.Emit(OpCodes.Ldarg_0);
                ctorIl.Emit(OpCodes.Call, baseType.GetEmptyConstructor());
                ctorIl.Emit(OpCodes.Ret);
            }

            foreach (var requestType in misingRequestTypes)
            {
                var method = typeBuilder.DefineMethod("Any", MethodAttributes.Public | MethodAttributes.Virtual,
                    CallingConventions.Standard,
                    returnType: typeof(object),
                    parameterTypes: new[] { requestType });

                var genericDef = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IQuery<,>));
                var hasExplicitInto = genericDef != null;
                if (genericDef == null)
                    genericDef = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IQuery<>));

                var il = method.GetILGenerator();

                var genericArgs = genericDef.GetGenericArguments();
                var mi = AutoQueryServiceBaseType.GetMethods()
                    .First(x => x.GetGenericArguments().Length == genericArgs.Length);
                var genericMi = mi.MakeGenericMethod(genericArgs);

                var queryType = hasExplicitInto
                    ? typeof(IQuery<,>).MakeGenericType(genericArgs)
                    : typeof(IQuery<>).MakeGenericType(genericArgs);

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

        public AutoQueryFeature RegisterQueryFilter<Request, From>(Func<IRequest, SqlExpression<From>, Request, SqlExpression<From>> filterFn)
        {
            QueryFilters[typeof(Request)] = (req, expression, model) =>
                filterFn(req, (SqlExpression<From>)expression, (Request)model);

            return this;
        }
    }

    public interface IAutoQuery
    {
        SqlExpression<From> CreateQuery<From>(IQuery<From> model, Dictionary<string, string> dynamicParams, IRequest request = null);

        QueryResponse<From> Execute<From>(IQuery<From> model, SqlExpression<From> query);

        SqlExpression<From> CreateQuery<From, Into>(IQuery<From, Into> model, Dictionary<string, string> dynamicParams, IRequest request = null);

        QueryResponse<Into> Execute<From, Into>(IQuery<From, Into> model, SqlExpression<From> query);
    }

    public abstract class AutoQueryServiceBase : Service
    {
        public IAutoQuery AutoQuery { get; set; }

        public virtual object Exec<From>(IQuery<From> dto)
        {
            SqlExpression<From> q;
            using (Profiler.Current.Step("AutoQuery.CreateQuery"))
            {
                q = AutoQuery.CreateQuery(dto, Request.GetRequestParams());
            }
            using (Profiler.Current.Step("AutoQuery.Execute"))
            {
                return AutoQuery.Execute(dto, q);
            }
        }

        public virtual object Exec<From, Into>(IQuery<From, Into> dto)
        {
            SqlExpression<From> q;
            using (Profiler.Current.Step("AutoQuery.CreateQuery"))
            {
                q = AutoQuery.CreateQuery(dto, Request.GetRequestParams());
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
        bool EnableUntypedQueries { get; set; }
        bool EnableSqlFilters { get; set; }
        bool OrderByPrimaryKeyOnLimitQuery { get; set; }
        HashSet<string> IgnoreProperties { get; set; }
        HashSet<string> IllegalSqlFragmentTokens { get; set; }
        Dictionary<string, QueryFieldAttribute> StartsWithConventions { get; set; }
        Dictionary<string, QueryFieldAttribute> EndsWithConventions { get; set; }
    }

    public class AutoQuery : IAutoQuery, IAutoQueryOptions, IDisposable
    {
        public int? MaxLimit { get; set; }
        public bool EnableUntypedQueries { get; set; }
        public bool EnableSqlFilters { get; set; }
        public bool OrderByPrimaryKeyOnLimitQuery { get; set; }
        public string RequiredRoleForRawSqlFilters { get; set; }
        public HashSet<string> IgnoreProperties { get; set; }
        public HashSet<string> IllegalSqlFragmentTokens { get; set; }
        public Dictionary<string, QueryFieldAttribute> StartsWithConventions { get; set; }
        public Dictionary<string, QueryFieldAttribute> EndsWithConventions { get; set; }

        public virtual IDbConnection Db { get; set; }
        public Dictionary<Type, QueryFilterDelegate> QueryFilters { get; set; }

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

        public SqlExpression<From> Filter<From>(IRequest request, ISqlExpression expr, IQuery model)
        {
            if (QueryFilters == null)
                return (SqlExpression<From>)expr;

            QueryFilterDelegate filterFn = null;
            if (!QueryFilters.TryGetValue(model.GetType(), out filterFn))
            {
                foreach (var type in model.GetType().GetInterfaces())
                {
                    if (QueryFilters.TryGetValue(type, out filterFn))
                        break;
                }
            }

            if (filterFn != null)
                return (SqlExpression<From>)(filterFn(request, expr, model) ?? expr);

            return (SqlExpression<From>)expr;
        }

        public SqlExpression<From> CreateQuery<From>(IQuery<From> model, Dictionary<string, string> dynamicParams, IRequest request = null)
        {
            var typedQuery = GetTypedQuery(model.GetType(), typeof(From));
            return Filter<From>(request, typedQuery.CreateQuery(Db, model, dynamicParams, this), model);
        }

        public QueryResponse<From> Execute<From>(IQuery<From> model, SqlExpression<From> query)
        {
            var typedQuery = GetTypedQuery(model.GetType(), typeof(From));
            return typedQuery.Execute<From>(Db, query);
        }

        public SqlExpression<From> CreateQuery<From, Into>(IQuery<From, Into> model, Dictionary<string, string> dynamicParams, IRequest request = null)
        {
            var typedQuery = GetTypedQuery(model.GetType(), typeof(From));
            return Filter<From>(request, typedQuery.CreateQuery(Db, model, dynamicParams, this), model);
        }

        public QueryResponse<Into> Execute<From, Into>(IQuery<From, Into> model, SqlExpression<From> query)
        {
            var typedQuery = GetTypedQuery(model.GetType(), typeof(From));
            return typedQuery.Execute<Into>(Db, query);
        }
    }

    public interface ITypedQuery
    {
        ISqlExpression CreateQuery(
            IDbConnection db,
            IQuery model,
            Dictionary<string, string> dynamicParams,
            IAutoQueryOptions options=null);

        QueryResponse<Into> Execute<Into>(
            IDbConnection db,
            ISqlExpression query);
    }

    public class TypedQuery<QueryModel, From> : ITypedQuery
    {
        static readonly Dictionary<string, Func<object, object>> PropertyGetters =
            new Dictionary<string, Func<object, object>>();

        static readonly Dictionary<string, QueryFieldAttribute> QueryFieldMap =
            new Dictionary<string, QueryFieldAttribute>();

        static TypedQuery()
        {
            foreach (var pi in typeof(QueryModel).GetPublicProperties())
            {
                var fn = pi.GetValueGetter(typeof(QueryModel));
                PropertyGetters[pi.Name] = fn;

                var queryAttr = pi.FirstAttribute<QueryFieldAttribute>();
                if (queryAttr != null)
                    QueryFieldMap[pi.Name] = queryAttr.Init();
            }
        }

        public ISqlExpression CreateQuery(
            IDbConnection db,
            IQuery model,
            Dictionary<string, string> dynamicParams,
            IAutoQueryOptions options=null)
        {
            dynamicParams = new Dictionary<string, string>(dynamicParams, StringComparer.OrdinalIgnoreCase);
            var q = db.From<From>();

            if (options != null && options.EnableSqlFilters)
            {
                AppendSqlFilters(q, model, dynamicParams, options);
            }

            AppendJoins(q, model);

            AppendLimits(q, model, options);

            var dtoAttr = model.GetType().FirstAttribute<QueryAttribute>();
            var defaultTerm = dtoAttr != null && dtoAttr.DefaultTerm == QueryTerm.Or ? "OR" : "AND";

            AppendTypedQueries(q, model, dynamicParams, defaultTerm, options);

            if (options != null && options.EnableUntypedQueries)
            {
                AppendUntypedQueries(q, dynamicParams, defaultTerm, options);
            }

            if (defaultTerm == "OR" && q.WhereExpression == null)
            {
                q.Where("1=0"); //Empty OR queries should be empty
            }

            return q;
        }

        private void AppendSqlFilters(SqlExpression<From> q, IQuery model, Dictionary<string, string> dynamicParams, IAutoQueryOptions options)
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

        private static void AppendLimits(SqlExpression<From> q, IQuery model, IAutoQueryOptions options)
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
                q.OrderByFields(typeof(From).GetModelMetadata().PrimaryKey);
            }
        }

        private static void AppendJoins(SqlExpression<From> q, IQuery model)
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

        private static void AppendTypedQueries(SqlExpression<From> q, IQuery model,
            Dictionary<string, string> dynamicParams, string defaultTerm, IAutoQueryOptions options)
        {
            foreach (var entry in PropertyGetters)
            {
                var name = entry.Key;

                QueryFieldAttribute implicitQuery;
                QueryFieldMap.TryGetValue(name, out implicitQuery);

                if (implicitQuery != null && implicitQuery.Field != null)
                    name = implicitQuery.Field;

                var match = GetQueryMatch(q, name, options);
                if (match == null)
                    continue;

                if (implicitQuery == null)
                    implicitQuery = match.ImplicitQuery;
                var quotedColumn = q.DialectProvider.GetQuotedColumnName(match.ModelDef, match.FieldDef);

                var value = entry.Value(model);
                if (value == null)
                    continue;

                dynamicParams.Remove(entry.Key);

                AddCondition(q, defaultTerm, quotedColumn, value, implicitQuery);
            }
        }

        private static void AddCondition(SqlExpression<From> q, string defaultTerm, string quotedColumn, object value, QueryFieldAttribute implicitQuery)
        {
            var seq = value as IEnumerable;
            if (value is string)
                seq = null;
            var format = seq == null 
                ? quotedColumn + " = {0}"
                : quotedColumn + " IN ({0})";
            if (implicitQuery != null)
            {
                var operand = implicitQuery.Operand ?? "=";
                if (implicitQuery.Term == QueryTerm.Or)
                    defaultTerm = "OR";
                else if (implicitQuery.Term == QueryTerm.And)
                    defaultTerm = "AND";

                format = quotedColumn + " " + operand + " {0}";
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

        private static void AppendUntypedQueries(SqlExpression<From> q, Dictionary<string, string> dynamicParams, string defaultTerm, IAutoQueryOptions options)
        {
            foreach (var entry in dynamicParams)
            {
                var name = entry.Key;

                var match = GetQueryMatch(q, name, options);
                if (match == null)
                    continue;

                var implicitQuery = match.ImplicitQuery;
                var quotedColumn = q.DialectProvider.GetQuotedColumnName(match.ModelDef, match.FieldDef);

                var strValue = entry.Value;
                var fieldType = match.FieldDef.FieldType;
                var isMultiple = (implicitQuery != null && (implicitQuery.ValueStyle > ValueStyle.Single))
                    || string.Compare(name, match.FieldDef.Name + Pluralized, StringComparison.OrdinalIgnoreCase) == 0;
                
                var value = isMultiple ? 
                    TypeSerializer.DeserializeFromString(strValue, Array.CreateInstance(fieldType, 0).GetType())
                    : fieldType == typeof(string) ? 
                      strValue
                    : fieldType.IsValueType && !fieldType.IsEnum ? 
                      Convert.ChangeType(strValue, fieldType) :
                      TypeSerializer.DeserializeFromString(strValue, fieldType);

                AddCondition(q, defaultTerm, quotedColumn, value, implicitQuery);
            }
        }

        class MatchQuery
        {
            public MatchQuery(Tuple<ModelDefinition,FieldDefinition> match, QueryFieldAttribute implicitQuery)
            {
                ModelDef = match.Item1;
                FieldDef = match.Item2;
                ImplicitQuery = implicitQuery;
            }
            public readonly ModelDefinition ModelDef;
            public readonly FieldDefinition FieldDef;
            public readonly QueryFieldAttribute ImplicitQuery;
        }

        private const string Pluralized = "s";

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
                    Total = (int)db.Count(q),
                    Results = db.LoadSelect<Into, From>(q),
                };

                return response;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }
        }
    }

    public static class AutoQueryExtensions
    {
        public static QueryFieldAttribute Init(this QueryFieldAttribute query)
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
    }
}