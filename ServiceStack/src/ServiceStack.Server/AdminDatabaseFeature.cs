#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack;

public class AdminDatabaseFeature : IPlugin, Model.IHasStringId, IPreInitPlugin
{
    public string Id { get; set; } = Plugins.AdminDatabase;
    public string AdminRole { get; set; } = RoleNames.Admin;

    public Action<List<DatabaseInfo>>? DatabasesFilter { get; set; }
    public Action<List<SchemaInfo>>? SchemasFilter { get; set; }

    public int QueryLimit { get; set; } = 100;

    public void Register(IAppHost appHost)
    {
        appHost.RegisterService(typeof(AdminDatabaseService));

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


[ExcludeMetadata, Tag("admin")]
public class AdminDatabase : IGet, IReturn<AdminDatabaseResponse>
{
    public string? Db { get; set; }
    public string? Schema { get; set; }
    public string? Table { get; set; }
    public List<string>? Fields { get; set; }
    public int? Take { get; set; }
    public int? Skip { get; set; }
    public string? OrderBy { get; set; }
}

public class AdminDatabaseResponse : IHasResponseStatus
{
    public List<Dictionary<string, object?>> Results { get; set; }

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
            };
        }
    }

    private static char[] Delims = { '=', '!', '<', '>', '[', ']' }; 
    
    public object Any(AdminDatabase request)
    {
        using var db = HostContext.AppHost.GetDbConnection(request.Db is null or "main" ? null : request.Db);
        var dialect = db.GetDialectProvider();

        var schema = request.Schema == "default" ? null : request.Schema;
        var table = dialect.GetQuotedTableName(request.Table, schema);
        var sb = StringBuilderCache.Allocate();
        var fields = request.Fields.IsEmpty()
            ? "*"
            : string.Join(",", request.Fields.Map(x => dialect.GetQuotedName(x.SafeVarName())));
        
        sb.AppendLine($"SELECT {fields} FROM {table}");
        var qs = Request.GetRequestParams(exclude:IgnoreFields);
        
        var filters = new List<string>();
        var dbParams = new Dictionary<string, object>();
        
        foreach (var entry in qs)
        {
            string? op = null;
            var name = entry.Key;
            if (Array.IndexOf(Delims, entry.Key[0]) >= 0)
            {
                var pos = entry.Key.LastIndexOfAny(Delims);
                op = entry.Key.Substring(0, pos + 1);
                name = entry.Key.Substring(pos + 1);
            }
            else
            {
                var pos = entry.Key.IndexOfAny(Delims);
                if (pos >= 0)
                {
                    name = entry.Key.Substring(0, pos);
                    op = entry.Key.Substring(pos);
                    if (op is ">" or "<" or "!")
                        op += "=";
                }
            }
            
            var quotedName = dialect.GetQuotedColumnName(name);
            var paramName = "@" + name;
            var value = entry.Value.SqlVerifyFragment();
            if (value == "null")
            {
                filters.Add($"{quotedName} IS NULL");
                filters.Add($"{quotedName} IS NOT NULL");
            }
            if (op != null)
            {
                if (op == "[]")
                {
                    var inValues = value.Split(',')
                        .Map(x => DynamicNumber.TryParse(x, out _) ? x : dialect.GetQuotedValue(x));
                    filters.Add($"{quotedName} IN ({string.Join(",", inValues)})");
                }
                else
                {
                    dbParams[name] = value;
                    filters.Add($"{dialect.SqlCast(quotedName, "VARCHAR")} {op} {paramName}");
                }
            }
            else
            {
                dbParams[name] = value;
                filters.Add(value?.IndexOf('%') >= 0
                    ? $"{dialect.SqlCast(quotedName, "VARCHAR")} LIKE {paramName}"
                    : $"{quotedName} = {paramName}");
            }
        }

        if (filters.Count > 0)
        {
            sb.AppendLine($"WHERE {string.Join(" AND ", filters)}");
        }
        if (!string.IsNullOrEmpty(request.OrderBy))
        {
            sb.AppendLine($"ORDER BY {OrmLiteUtils.OrderByFields(dialect, request.OrderBy)}");
        }

        var feature = AssertPlugin<AdminDatabaseFeature>();
        var take = Math.Min(request.Take.GetValueOrDefault(feature.QueryLimit), feature.QueryLimit);
        sb.AppendLine(dialect.SqlLimit(request.Skip, take));

        var sql = StringBuilderCache.ReturnAndFree(sb);
        var results = db.SqlList<Dictionary<string, object?>>(sql, dbParams);
        
        return new AdminDatabaseResponse
        {
            Results = results,
        };
    }
}