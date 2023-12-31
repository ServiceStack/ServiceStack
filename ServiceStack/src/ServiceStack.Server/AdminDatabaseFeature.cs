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

public class AdminDatabaseFeature : IPlugin, IConfigureServices, Model.IHasStringId, IPreInitPlugin
{
    public string Id { get; set; } = Plugins.AdminDatabase;
    public string AdminRole { get; set; } = RoleNames.Admin;

    public Action<List<DatabaseInfo>>? DatabasesFilter { get; set; }
    public Action<List<SchemaInfo>>? SchemasFilter { get; set; }

    public int QueryLimit { get; set; } = 100;

    public void Configure(IServiceCollection services)
    {
        services.RegisterService(typeof(AdminDatabaseService));
    }

    public void Register(IAppHost appHost)
    {
        var dbFactory = appHost.Resolve<IDbConnectionFactory>();
        using var db = dbFactory.Open();

        var databases = new List<DatabaseInfo> {
            new() {
                Name = "main",
                Schemas = ToSchemaTables(db.GetSchemaTables()),
            }
        };

        foreach (var entry in dbFactory.GetNamedConnections())
        {
            using var namedDb = dbFactory.OpenDbConnection(entry.Key);
            databases.Add(new () {
                Name = entry.Key,
                Schemas = ToSchemaTables(namedDb.GetSchemaTables()),
            });
        }

        if (SchemasFilter != null)
            databases.Each(x => SchemasFilter(x.Schemas));
        
        DatabasesFilter?.Invoke(databases);

        appHost.AddToAppMetadata(meta => {
            meta.Plugins.AdminDatabase = new AdminDatabaseInfo {
                QueryLimit = QueryLimit,
                Databases = databases,
            };
        });
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
    public List<Dictionary<string, object?>> Results { get; set; }
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

    private static char[] Delims = ['=', '!', '<', '>', '[', ']'];

    private static ConcurrentDictionary<string, List<MetadataPropertyType>> ColumnCache = new();

    private async Task<AdminDatabaseFeature> AssertRequiredRole()
    {
        var feature = AssertPlugin<AdminDatabaseFeature>();
        await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AdminRole);
        return feature;
    }
    
    public async Task<object> Any(AdminDatabase request)
    {
        var feature = await AssertRequiredRole();

        using var db = HostContext.AppHost.GetDbConnection(request.Db is null or "main" ? null : request.Db);
        var dialect = db.GetDialectProvider();
        var schema = request.Schema == "default" ? null : request.Schema;

        var supportsMultiDb = !dialect.GetType().Name.StartsWith("Sqlite");
        var table = dialect.SupportsSchema
            ? dialect.GetQuotedTableName(request.Table, schema)
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

        var includes = request.Include?.Split(',') ?? Array.Empty<string>();
        if (includes.Contains("total"))
        {
            var totalSql = $"SELECT COUNT(*) FROM {table}" + n + sqlWhere;
            total = await db.SqlScalarAsync<long>(totalSql, dbParams);
        }

        // Change CSV download filename
        Request.Items[Keywords.FileName] = request.Table + ".csv";
        
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
                var type = GenerateCrudServices.DefaultResolveColumnType(x, dialect);
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