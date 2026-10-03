#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Logging;
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

    /// <summary>
    /// Also compare the App's models of the tables its migrations create or change, which are matched by table name
    /// to the copies of the models the migrations declare. Enabled by default.
    /// </summary>
    public bool IncludeMigrationModels { get; set; } = true;

    /// <summary>
    /// The folder that the migrate.new App Task writes migrations to, relative to the App's content root
    /// </summary>
    public string MigrationsPath { get; set; } = "Migrations";

    /// <summary>
    /// Log the Schema Diff of each database when the App starts, a warning with the differences when its tables
    /// aren't the same as their models. It's compared in the background, and not when running App Tasks.
    /// </summary>
    public bool LogSchemaDiff { get; set; }

    public void Configure(IServiceCollection services)
    {
        services.RegisterService(typeof(AdminDatabaseService));
        services.RegisterService(typeof(AdminQueryService));
        services.RegisterService(typeof(AdminSchemaDiffService));

        // Write the migration of a database's Schema Diff to the App's migrations, e.g:
        // dotnet run --AppTasks=migrate.new           (the main database)
        // dotnet run --AppTasks=migrate.new:reports   (a named connection)
        AppTasks.Register("migrate.new", args => WriteMigration(HostContext.AppHost, args.FirstOrDefault()));
    }

    /// <summary>
    /// Write the migration of a database's Schema Diff to the App's migrations, see MigrationsPath, named after the
    /// App's last migration. Returns the path of the file, or null when the database is the same as its models.
    /// The migration is a guide to review, which is compiled with a warning until it's reviewed.
    /// </summary>
    public string? WriteMigration(IAppHost appHost, string? namedConnection = null)
    {
        var log = LogManager.GetLogger(typeof(AdminDatabaseFeature));
        var dbName = namedConnection is null or "" or "main" ? null : namedConnection;
        var dbFactory = appHost.Resolve<IDbConnectionFactory>();
        if (dbName != null && !dbFactory.GetNamedConnections().ContainsKey(dbName))
            throw new ArgumentException($"There's no '{dbName}' named connection", nameof(namedConnection));

        // App Tasks run before AutoQuery has registered its APIs, so their data models are read from the App's DTOs
        var modelTypes = GetModelTypes(appHost, dbName, scanRequestTypes: true);
        using var db = dbName != null
            ? dbFactory.Open(dbName, ConfigureDb)
            : dbFactory.Open(ConfigureDb);
        var diff = db.GetSchemaDiff(modelTypes.ToArray());
        if (!diff.HasChanges)
        {
            log.Info($"Schema Diff: the tables of the {dbName ?? "main"} database are the same as their models, no migration was written");
            return null;
        }

        var name = GetNextMigrationName(appHost);
        var source = diff.ToMigration(name, GetMigrationNamespace(appHost));
        if (dbName != null)
        {
            source = source.Replace($"public class {name} : MigrationBase",
                $"[NamedConnection(\"{dbName}\")]\npublic class {name} : MigrationBase");
        }

        var dir = Path.IsPathRooted(MigrationsPath) ? MigrationsPath : Path.Combine(appHost.MapProjectPath("~/"), MigrationsPath);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name + ".cs");
        if (File.Exists(path))
            throw new InvalidOperationException($"{path} already exists");
        File.WriteAllText(path, source);
        log.Info($"Schema Diff: wrote {path}, a guide to review before running it\n{diff}");
        return path;
    }

    /// <summary>
    /// The models of a database: the data models of the App's AutoQuery APIs and ModelTypes that use its named
    /// connection. Models that AutoGen generates from the tables of a database, and the models of ServiceStack's
    /// plugins which create their own tables, are only included when they're in ModelTypes.
    /// </summary>
    public List<Type> GetModelTypes(IAppHost appHost, string? namedConnection) =>
        GetModelTypes(appHost, namedConnection, scanRequestTypes: false);

    // When scanRequestTypes, the data models of AutoQuery APIs are read from the App's DTOs instead of its APIs
    internal List<Type> GetModelTypes(IAppHost appHost, string? namedConnection, bool scanRequestTypes)
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
        var requestTypes = scanRequestTypes
            ? GetAppAssemblies(appHost).SelectMany(GetLoadableTypes)
                .Where(x => x.IsClass && !x.IsAbstract && !x.IsGenericTypeDefinition)
            : appHost.Metadata.Operations.Select(x => x.RequestType);
        foreach (var requestType in requestTypes)
        {
            var modelType = AutoCrudOperation.GetModelType(requestType);
            if (modelType?.Assembly.GetName().Name?.StartsWith("ServiceStack") == true
                && modelType.Assembly != appHost.GetType().Assembly)
                continue;
            Add(modelType, requestType.FirstAttribute<NamedConnectionAttribute>()?.Name);
        }
        if (IncludeMigrationModels)
        {
            foreach (var table in GetMigrationTables(appHost))
            {
                // When more than one App model has the name of the table, it's only compared when it's already one
                // of the models, e.g. the data model of an AutoQuery API
                if (table.ModelType != null)
                    Add(table.ModelType, table.NamedConnection);
            }
        }
        return to.OrderBy(x => x.Name).ToList();
    }

    private List<MigrationTable>? migrationTables;

    /// <summary>
    /// The tables of the App's migrations, with the App's model of each table
    /// </summary>
    public List<MigrationTable> GetMigrationTables(IAppHost appHost)
    {
        if (migrationTables != null)
            return migrationTables;

        var migrationAssemblies = GetMigrationTypes(appHost).Select(x => x.Assembly).Distinct().ToArray();
        if (migrationAssemblies.Length == 0)
            return migrationTables = [];

        return migrationTables = Migrator.GetMigrationTables(migrationAssemblies, GetAppAssemblies(appHost));
    }

    // The App's assemblies and the assemblies of its APIs, e.g. its ServiceModel project
    private static System.Reflection.Assembly[] GetAppAssemblies(IAppHost appHost)
    {
        var assemblies = new List<System.Reflection.Assembly> { appHost.GetType().Assembly };
        assemblies.AddRange(appHost.ServiceAssemblies);
        foreach (var op in appHost.Metadata.Operations)
        {
            assemblies.Add(op.RequestType.Assembly);
            if (op.ResponseType != null)
                assemblies.Add(op.ResponseType.Assembly);
        }
        return assemblies.Distinct()
            .Where(x => !x.IsDynamic && x.GetName().Name?.StartsWith("ServiceStack") != true
                || x == appHost.GetType().Assembly)
            .ToArray();
    }

    // The types of an assembly that can be loaded, without those whose dependencies are missing
    private static IEnumerable<Type> GetLoadableTypes(System.Reflection.Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (System.Reflection.ReflectionTypeLoadException e)
        {
            return e.Types.Where(x => x != null)!;
        }
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
        // Not before App Tasks like migrations, when the differences would be out of date
        if (LogSchemaDiff && !AppTasks.IsRunAsAppTask())
            appHost.AfterInitCallbacks.Add(host => Task.Run(() => LogSchemaDiffs(host)));
    }

    /// <summary>
    /// Log the differences between the models and tables of each database, which are the models the Schema Diff
    /// compares
    /// </summary>
    public void LogSchemaDiffs(IAppHost appHost, ILog? log = null)
    {
        log ??= LogManager.GetLogger(typeof(AdminDatabaseFeature));
        var dbFactory = appHost.TryResolve<IDbConnectionFactory>();
        if (dbFactory == null)
            return;

        var namedConnections = new List<string?> { null };
        namedConnections.AddRange(dbFactory.GetNamedConnections().Keys);
        foreach (var namedConnection in namedConnections)
        {
            var dbName = namedConnection ?? "main";
            try
            {
                var modelTypes = GetModelTypes(appHost, namedConnection);
                if (modelTypes.Count == 0)
                    continue;

                using var db = namedConnection != null
                    ? dbFactory.Open(namedConnection, ConfigureDb)
                    : dbFactory.Open(ConfigureDb);
                var diff = db.GetSchemaDiff(modelTypes.ToArray());
                if (diff.HasChanges)
                {
                    log.Warn($"Schema Diff: the tables of the {dbName} database aren't the same as their models\n{diff}");
                    continue;
                }

                var compared = modelTypes.Count - diff.Ignored.Count;
                var message = compared == 0
                    ? $"Schema Diff: no models of the {dbName} database were compared"
                    : $"Schema Diff: the tables of the {dbName} database are the same as their models " +
                      $"({compared} {(compared == 1 ? "model" : "models")})";
                if (diff.Ignored.Count > 0)
                    message += $", ignored {string.Join(", ", diff.Ignored)}";
                log.Info(message);
                foreach (var warning in diff.Warnings)
                    log.Warn($"Schema Diff of the {dbName} database: {warning}");
            }
            catch (Exception e)
            {
                log.Error($"Schema Diff: couldn't compare the models of the {dbName} database with their tables", e);
            }
        }
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