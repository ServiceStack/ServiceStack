#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack;

public class AdminDatabaseFeature : IPlugin, IConfigureServices, Model.IHasStringId, IPreInitPlugin, IPostInitPlugin
{
    public string Id { get; set; } = Plugins.AdminDatabase;
    public string AdminRole { get; set; } = RoleNames.Admin;

    public Action<List<DatabaseInfo>>? DatabasesFilter { get; set; }
    public Action<List<SchemaInfo>>? SchemasFilter { get; set; }

    public int QueryLimit { get; set; } = 100;

    /// <summary>
    /// The max number of rows returned when re-running a profiled query from the Profiling Admin UI (default 20)
    /// </summary>
    public int RunQueryLimit { get; set; } = 20;

    /// <summary>
    /// Models that are compared with their tables in the Schema Diff, in addition to the data models of the App's
    /// AutoQuery APIs. Models with a [NamedConnection] are compared with the tables of its database.
    /// </summary>
    public List<Type> ModelTypes { get; set; } = [];

    /// <summary>
    /// Choose the models that are compared with their tables in the Schema Diff
    /// </summary>
    public Func<Type, bool>? ModelTypesFilter { get; set; }

    /// <summary>
    /// The namespace of the migration the Schema Diff writes, defaults to the namespace of the App's migrations
    /// </summary>
    public string? MigrationNamespace { get; set; }

    public void Configure(IServiceCollection services)
    {
        services.RegisterService(typeof(AdminDatabaseService));
        services.RegisterService(typeof(AdminQueryService));
        services.RegisterService(typeof(AdminSchemaDiffService));
    }

    /// <summary>
    /// The models of a database: the data models of the App's AutoQuery APIs and ModelTypes that use its named
    /// connection. Models that AutoGen generates from the tables of a database, and the models of ServiceStack's
    /// plugins which create their own tables, are only included when they're in ModelTypes.
    /// </summary>
    public List<Type> GetModelTypes(IAppHost appHost, string? namedConnection)
    {
        var to = new List<Type>();
        void Add(Type? modelType, string? dtoConnection)
        {
            if (modelType == null || modelType.Assembly.IsDynamic || to.Contains(modelType))
                return;
            var connection = dtoConnection ?? modelType.FirstAttribute<NamedConnectionAttribute>()?.Name;
            if (connection != namedConnection)
                return;
            if (ModelTypesFilter != null && !ModelTypesFilter(modelType))
                return;
            to.Add(modelType);
        }

        foreach (var modelType in ModelTypes)
        {
            Add(modelType, null);
        }
        foreach (var op in appHost.Metadata.Operations)
        {
            var modelType = op.DataModelType;
            if (modelType?.Assembly.GetName().Name?.StartsWith("ServiceStack") == true
                && modelType.Assembly != appHost.GetType().Assembly)
                continue;
            Add(modelType, op.RequestType.FirstAttribute<NamedConnectionAttribute>()?.Name);
        }
        return to.OrderBy(x => x.Name).ToList();
    }

    private static List<Type> GetMigrationTypes(IAppHost appHost)
    {
        var assemblies = new List<System.Reflection.Assembly> { appHost.GetType().Assembly };
        assemblies.AddRange(appHost.ServiceAssemblies);
        return assemblies.Distinct()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.IsClass && !x.IsAbstract && typeof(MigrationBase).IsAssignableFrom(x))
            .ToList();
    }

    /// <summary>
    /// The name of the migration after the App's last migration, e.g. Migration1005 after Migration1004
    /// </summary>
    public string GetNextMigrationName(IAppHost appHost)
    {
        var last = 999;
        foreach (var type in GetMigrationTypes(appHost))
        {
            if (type.Name.StartsWith("Migration") && int.TryParse(type.Name.Substring("Migration".Length), out var number))
                last = Math.Max(last, number);
        }
        return "Migration" + (last + 1);
    }

    public string GetMigrationNamespace(IAppHost appHost) => MigrationNamespace
        ?? GetMigrationTypes(appHost).OrderByDescending(x => x.Name).FirstOrDefault()?.Namespace
        ?? (appHost.GetType().Namespace is { } ns ? ns + ".Migrations" : "Migrations");

    public void Register(IAppHost appHost)
    {
    }

    private static List<SchemaInfo> ToSchemaTables(Dictionary<string, List<string>> schemasMap)
    {
        var schemas = new List<SchemaInfo>(); 
        schemasMap.Keys.OrderBy(x => x).Each(schema =>
        {
            schemas.Add(new SchemaInfo
            {
                Name = schema,
                Tables = schemasMap[schema],
            });
        });
        return schemas;
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        appHost.ConfigurePlugin<UiFeature>(feature => {
            feature.AddAdminLink(AdminUiFeature.Database, new LinkInfo {
                Id = "database",
                Label = "Database",
                Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Database)),
                Show = $"role:{AdminRole}",
            });
        });
        appHost.ConfigurePlugin<RequestLogsFeature>(feature =>
        {
            feature.ExcludeRequestDtoTypes.Add(typeof(AdminDatabase));
            feature.ExcludeRequestDtoTypes.Add(typeof(AdminExplainQuery));
            feature.ExcludeRequestDtoTypes.Add(typeof(AdminRunQuery));
            feature.ExcludeRequestDtoTypes.Add(typeof(AdminSchemaDiff));
        });
        appHost.ConfigurePlugin<ProfilingFeature>(feature =>
        {
            feature.ExcludeRequestDtoTypes.Add(typeof(AdminExplainQuery));
            feature.ExcludeRequestDtoTypes.Add(typeof(AdminRunQuery));
            feature.ExcludeRequestDtoTypes.Add(typeof(AdminSchemaDiff));
            // Explaining or re-running a profiled query isn't profiled
            feature.ExcludeTags.Add(nameof(AdminDatabaseFeature));
        });
    }

    internal static void ConfigureDb(IDbConnection db) => db.WithTag(nameof(AdminDatabaseFeature));

    // e.g. SQLite can explain queries, but not analyze them
    static bool? SupportsAnalyze(IDbConnection db)
    {
        try
        {
            db.GetDialectProvider().ToExplainQuery(db, "SELECT 1", analyze: true);
            return true;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    public void AfterPluginsLoaded(IAppHost appHost)
    {
        var dbFactory = appHost.Resolve<IDbConnectionFactory>();
        using var db = dbFactory.Open(ConfigureDb);

        var databases = new List<DatabaseInfo> {
            new() {
                Name = "main",
                Schemas = ToSchemaTables(db.GetSchemaTables()),
                SupportsAnalyze = SupportsAnalyze(db),
            }
        };

        foreach (var entry in dbFactory.GetNamedConnections())
        {
            using var namedDb = dbFactory.Open(entry.Key, ConfigureDb);
            databases.Add(new () {
                Name = entry.Key,
                Schemas = ToSchemaTables(namedDb.GetSchemaTables()),
                SupportsAnalyze = SupportsAnalyze(namedDb),
            });
        }

        if (SchemasFilter != null)
            databases.Each(x => SchemasFilter(x.Schemas));
        
        DatabasesFilter?.Invoke(databases);

        appHost.AddToAppMetadata(meta => {
            meta.Plugins.AdminDatabase = new AdminDatabaseInfo {
                QueryLimit = QueryLimit,
                RunQueryLimit = RunQueryLimit,
                Databases = databases,
            };
        });
    }
}


[ExcludeMetadata, Tag(TagNames.Admin)]
public class AdminDatabase : IGet, IReturn<AdminDatabaseResponse>
{
    public string? Db { get; set; }
    public string? Schema { get; set; }
    public string? Table { get; set; }
    public List<string>? Fields { get; set; }
    public int? Take { get; set; }
    public int? Skip { get; set; }
    public string? OrderBy { get; set; }
    public string? Include { get; set; }
}

[Csv(CsvBehavior.FirstEnumerable)]
public class AdminDatabaseResponse : IHasResponseStatus
{
    public List<Dictionary<string, object?>> Results { get; set; } = [];
    public long? Total { get; set; }
    public List<MetadataPropertyType>? Columns { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

public class AdminDatabaseService : Service
{
    private static HashSet<string>? ignoreFields;

    public static HashSet<string> IgnoreFields
    {
        get
        {
            if (ignoreFields != null)
                return ignoreFields;
            
            return ignoreFields = new HashSet<string>(HostContext.Config.IgnoreWarningsOnPropertyNames, StringComparer.OrdinalIgnoreCase)
            {
                nameof(AdminDatabase.Db),
                nameof(AdminDatabase.Schema),
                nameof(AdminDatabase.Table),
                nameof(AdminDatabase.Skip),
                nameof(AdminDatabase.Take),
                nameof(AdminDatabase.OrderBy),
                nameof(AdminDatabase.Include),
                nameof(AdminDatabase.Fields)
            };
        }
    }

    private static readonly char[] Delims = ['=', '!', '<', '>', '[', ']'];

    private static readonly ConcurrentDictionary<string, List<MetadataPropertyType>> ColumnCache = new();

    private async Task<AdminDatabaseFeature> AssertRequiredRole()
    {
        var feature = AssertPlugin<AdminDatabaseFeature>();
        await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AdminRole);
        return feature;
    }
    
    public async Task<object> Any(AdminDatabase request)
    {
        var feature = await AssertRequiredRole();

        using var db = await HostContext.AppHost.GetDbConnectionAsync(request.Db is null or "main" ? null : request.Db);
        var dialect = db.GetDialectProvider();
        var schema = request.Schema == "default" ? null : request.Schema;

        var supportsMultiDb = !dialect.GetType().Name.StartsWith("Sqlite");
        var table = dialect.SupportsSchema
            ? dialect.QuoteTable(new(schema, request.Table))
            : supportsMultiDb //schema is db when !SupportsSchema 
                ? dialect.GetQuotedName(schema) + "." + dialect.GetQuotedTableName(request.Table)
                : dialect.GetQuotedTableName(request.Table);
        
        var sb = StringBuilderCache.Allocate().AppendLine();
        var fields = request.Fields.IsEmpty()
            ? "*"
            : string.Join(",", request.Fields.Map(x => dialect.GetQuotedName(x.SafeVarName())));

        var qs = Request.GetRequestParams(exclude:IgnoreFields);
        
        var filters = new List<string>();
        var dbParams = new Dictionary<string, object>();
        var columns = GetColumns(db, request.Db ?? "main", table);

        var i = 0;
        foreach (var entry in qs)
        {
            string? op = null;
            var name = entry.Key.SqlVerifyFragment();
            var value = entry.Value.SqlVerifyFragment();

            if (Array.IndexOf(Delims, entry.Key[0]) >= 0)
            {
                var pos = entry.Key.LastIndexOfAny(Delims);
                op = entry.Key.Substring(0, pos + 1);
                name = entry.Key.Substring(pos + 1);
                if (op == ">")
                    op += "=";
            }
            else
            {
                var pos = entry.Key.IndexOfAny(Delims);
                if (pos >= 0)
                {
                    name = entry.Key.Substring(0, pos);
                    op = entry.Key.Substring(pos);
                    if (op is "<" or "!")
                        op += "=";
                }
            }
            
            // Support AutoQuery conventions as well 
            if (name.EndsWith("StartsWith"))
            {
                name = name.Substring(0, name.Length - "StartsWith".Length);
                value = value + "%";
            }
            else if (name.EndsWith("EndsWith"))
            {
                name = name.Substring(0, name.Length - "EndsWith".Length);
                value = "%" + value;
            }
            else if (name.EndsWith("Contains"))
            {
                name = name.Substring(0, name.Length - "Contains".Length);
                value = "%" + value + "%";
            }
            else if (name.EndsWith("IsNull"))
            {
                name = name.Substring(0, name.Length - "IsNull".Length);
                value = "null";
            }
            else if (name.EndsWith("IsNotNull"))
            {
                name = name.Substring(0, name.Length - "IsNotNull".Length);
                op = "!";
                value = "null";
            }
            else if (name.EndsWith("In"))
            {
                name = name.Substring(0, name.Length - "In".Length);
                op = "[]";
            }

            name = name.SafeVarName();
            var columnType = columns.FirstOrDefault(x => x.Name.EqualsIgnoreCase(name))?.PropertyType ?? typeof(string);
            //var isNumber = DynamicNumber.IsNumber(columnType);
            var quotedName = dialect.GetQuotedColumnName(name);
            var paramName = $"@p{i++}";
            if (value == "null")
            {
                filters.Add(op == "!"
                    ? $"{quotedName} IS NOT NULL"
                    : $"{quotedName} IS NULL");
            }
            else if (op != null)
            {
                if (op == "[]")
                {
                    var inValues = value.Split(',')
                        .Map(x => DynamicNumber.TryParse(x, out _) ? x : dialect.GetQuotedValue(x));
                    filters.Add($"{quotedName} IN ({string.Join(",", inValues)})");
                }
                else
                {
                    dbParams[paramName] = value.ConvertTo(columnType);
                    filters.Add(columnType == typeof(string) 
                        ? $"{dialect.SqlCast(quotedName, "VARCHAR")} {op} {paramName}"
                        : $"{quotedName} {op} {paramName}");
                }
            }
            else
            {
                dbParams[paramName] = value.ConvertTo(columnType);
                filters.Add(value?.IndexOf('%') >= 0
                    ? $"{dialect.SqlCast(quotedName, "VARCHAR")} LIKE {paramName}"
                    : $"{quotedName} = {paramName}");
            }
        }

        if (filters.Count > 0)
        {
            sb.AppendLine($"WHERE {string.Join(" AND ", filters)}");
        }

        var sqlWhere = StringBuilderCache.ReturnAndFree(sb);

        var take = Math.Min(request.Take.GetValueOrDefault(feature.QueryLimit), feature.QueryLimit);

        // OrderBy always required when paging
        var n = Environment.NewLine;
        var orderBy = request.OrderBy ?? (columns.FirstOrDefault(x => x.IsPrimaryKey == true) ?? columns[0]).Name;
        var resultsSql = $"SELECT {fields} FROM {table}" + n 
             + sqlWhere + n
             + "ORDER BY " + OrmLiteUtils.OrderByFields(dialect, orderBy) + n 
             + dialect.SqlLimit(request.Skip, take);
        
        var results = await db.SqlListAsync<Dictionary<string, object?>>(resultsSql, dbParams);
        long? total = null;

        var includes = request.Include?.Split(',') ?? [];
        if (includes.Contains("total"))
        {
            var totalSql = $"SELECT COUNT(*) FROM {table}" + n + sqlWhere;
            total = await db.SqlScalarAsync<long>(totalSql, dbParams);
        }

        // Change CSV download filename
        Request!.Items[Keywords.FileName] = request.Table + ".csv";
        
        return new AdminDatabaseResponse
        {
            Total = total,
            Columns = includes.Contains("columns") ? columns : null,
            Results = results,
        };
    }

    static List<MetadataPropertyType> GetColumns(IDbConnection db, string dbName, string table)
    {
        var key = $"{dbName}.{table}";
        return ColumnCache.GetOrAdd(key, k =>
        {
            var dialect = db.GetDialectProvider();
            var columnSchemas = db.GetTableColumns($"SELECT * FROM {table} ORDER BY 1 {dialect.SqlLimit(0, 1)}");
            var columns = columnSchemas.Map(x =>
            {
                var type = GenerateCrudServices.DefaultResolveColumnType(x, dialect)
                    ?? throw new NotSupportedException($"Unknown Column Type: {x.ColumnName}");
                var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
                return new MetadataPropertyType
                {
                    Name = x.ColumnName,
                    PropertyType = type,
                    Type = type.GetMetadataPropertyType(),
                    IsValueType = underlyingType.IsValueType ? true : null,
                    IsEnum = underlyingType.IsEnum ? true : null,
                    GenericArgs = type.ToGenericArgs(),
                    IsPrimaryKey = x.IsKey ? true : null,
                };
            });
            return columns;
        });
    }
}