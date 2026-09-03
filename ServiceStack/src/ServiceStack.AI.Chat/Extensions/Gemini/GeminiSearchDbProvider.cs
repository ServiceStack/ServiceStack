using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;
using ServiceStack.OrmLite;

namespace ServiceStack.AI;

internal enum GeminiSearchDbKind
{
    Unknown,
    Sqlite,
    PostgreSql,
    MySql,
    SqlServer,
}

internal sealed record GeminiSearchDbQuery(string Sql, Dictionary<string, object> Args);

/// <summary>
/// Owns the RDBMS-specific full-text schema and SQL. Everything above this boundary works with
/// durable document hashes and <see cref="ChatSearchSection"/> rows, independently of the database.
/// </summary>
internal sealed class GeminiSearchDbProvider
{
    public GeminiSearchDbKind Kind { get; }
    public bool NativeEnabled { get; private set; }
    public bool UsesManualFullTextRows => Kind == GeminiSearchDbKind.Sqlite && NativeEnabled;

    GeminiSearchDbProvider(GeminiSearchDbKind kind) => Kind = kind;

    public string StatusName => (Kind, NativeEnabled) switch
    {
        (GeminiSearchDbKind.Sqlite, true) => "sqlite-fts5",
        (GeminiSearchDbKind.PostgreSql, true) => "postgresql-fts",
        (GeminiSearchDbKind.MySql, true) => "mysql-fulltext",
        (GeminiSearchDbKind.SqlServer, true) => "sqlserver-fulltext",
        (GeminiSearchDbKind.Sqlite, false) => "sqlite-like",
        (GeminiSearchDbKind.PostgreSql, false) => "postgresql-like",
        (GeminiSearchDbKind.MySql, false) => "mysql-like",
        (GeminiSearchDbKind.SqlServer, false) => "sqlserver-like",
        _ => "like",
    };

    public static GeminiSearchDbProvider Detect(IDbConnection conn)
    {
        var name = conn.GetDialectProvider().GetType().Name;
        var kind = name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ? GeminiSearchDbKind.Sqlite
            : name.Contains("Postgre", StringComparison.OrdinalIgnoreCase) ? GeminiSearchDbKind.PostgreSql
            : name.Contains("MySql", StringComparison.OrdinalIgnoreCase) ? GeminiSearchDbKind.MySql
            : name.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) ? GeminiSearchDbKind.SqlServer
            : GeminiSearchDbKind.Unknown;
        return new GeminiSearchDbProvider(kind);
    }

    public void Initialize(IDbConnection conn)
    {
        NativeEnabled = false;
        var dialect = conn.GetDialectProvider();
        var table = dialect.GetQuotedTableName(typeof(ChatSearchSection));
        var model = typeof(ChatSearchSection).GetModelMetadata();
        string Col(string name) => dialect.GetQuotedColumnName(model.GetFieldDefinition(name));

        switch (Kind)
        {
            case GeminiSearchDbKind.Sqlite:
                conn.ExecuteSql("CREATE VIRTUAL TABLE IF NOT EXISTS ChatSearchSectionFts USING fts5(sectionId UNINDEXED, documentTitle, heading, content)");
                break;
            case GeminiSearchDbKind.PostgreSql:
                conn.ExecuteSql($"CREATE INDEX IF NOT EXISTS ix_chat_search_section_fts ON {table} USING GIN (to_tsvector('simple', coalesce({Col(nameof(ChatSearchSection.DocumentTitle))},'') || ' ' || coalesce({Col(nameof(ChatSearchSection.Heading))},'') || ' ' || coalesce({Col(nameof(ChatSearchSection.Content))},'')))");
                break;
            case GeminiSearchDbKind.MySql:
                var exists = conn.Scalar<long>("SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name=@table AND index_name='ix_chat_search_section_fts'", new { table = model.ModelName });
                if (exists == 0)
                    conn.ExecuteSql($"ALTER TABLE {table} ADD FULLTEXT INDEX ix_chat_search_section_fts ({Col(nameof(ChatSearchSection.DocumentTitle))}, {Col(nameof(ChatSearchSection.Heading))}, {Col(nameof(ChatSearchSection.Content))})");
                break;
            case GeminiSearchDbKind.SqlServer:
                // Full-Text Search is an optional SQL Server component (mssql-server-fts on Linux).
                // Detect it before issuing any FTS DDL so installations without it use LIKE cleanly.
                var isFullTextInstalled = conn.Scalar<int?>("SELECT CONVERT(int, FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))");
                if (isFullTextInstalled != 1)
                    return;

                var objectName = dialect.GetTableName(model).Replace("'", "''");
                conn.ExecuteSql($"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_ChatSearchSection_FtsId' AND object_id=OBJECT_ID(N'{objectName}')) CREATE UNIQUE INDEX UX_ChatSearchSection_FtsId ON {table} ({Col(nameof(ChatSearchSection.Id))})");
                conn.ExecuteSql("IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name='ChatSearchCatalog') CREATE FULLTEXT CATALOG ChatSearchCatalog");
                conn.ExecuteSql($"IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id=OBJECT_ID(N'{objectName}')) CREATE FULLTEXT INDEX ON {table} ({Col(nameof(ChatSearchSection.DocumentTitle))} LANGUAGE 0, {Col(nameof(ChatSearchSection.Heading))} LANGUAGE 0, {Col(nameof(ChatSearchSection.Content))} LANGUAGE 0) KEY INDEX UX_ChatSearchSection_FtsId ON ChatSearchCatalog WITH (CHANGE_TRACKING = AUTO, STOPLIST = OFF)");
                conn.ExecuteSql($"IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id=OBJECT_ID(N'{objectName}') AND is_enabled=0) ALTER FULLTEXT INDEX ON {table} ENABLE");
                break;
            default:
                return;
        }
        NativeEnabled = true;
    }

    public void DisableNative() => NativeEnabled = false;

    public GeminiSearchDbQuery? BuildNativeQuery(IDbConnection conn, long storeId, string query,
        IReadOnlyList<string> tokens, string? user, JsonObject? scope, int take)
    {
        if (!NativeEnabled) return null;
        var dialect = conn.GetDialectProvider();
        var table = dialect.GetQuotedTableName(typeof(ChatSearchSection));
        var model = typeof(ChatSearchSection).GetModelMetadata();
        string Col(string name, string alias = "s") => alias + "." + dialect.GetQuotedColumnName(model.GetFieldDefinition(name));
        var args = new Dictionary<string, object>
        {
            ["storeId"] = storeId,
            ["user"] = user ?? ChatDb.DefaultUser,
            ["query"] = query,
            ["take"] = take,
        };
        var userClause = ChatDb.IsAllUsers(user) ? "" : $" AND {Col(nameof(ChatSearchSection.User))}=@user";
        var scopeClause = ScopeClause(scope, Col, args);
        string sql;

        switch (Kind)
        {
            case GeminiSearchDbKind.Sqlite:
                args["query"] = string.Join(" AND ", tokens.Select(x => $"\"{x.Replace("\"", "\"\"")}\"*"));
                sql = $"SELECT s.*, bm25(ChatSearchSectionFts) AS Score, s.{dialect.GetQuotedColumnName(model.GetFieldDefinition(nameof(ChatSearchSection.Content)))} AS Snippet FROM ChatSearchSectionFts f JOIN {table} s ON {Col(nameof(ChatSearchSection.Id))}=f.sectionId WHERE ChatSearchSectionFts MATCH @query AND {Col(nameof(ChatSearchSection.FilestoreId))}=@storeId{userClause}{scopeClause} ORDER BY Score LIMIT @take";
                break;
            case GeminiSearchDbKind.PostgreSql:
                var vector = $"to_tsvector('simple',coalesce({Col(nameof(ChatSearchSection.DocumentTitle))},'')||' '||coalesce({Col(nameof(ChatSearchSection.Heading))},'')||' '||coalesce({Col(nameof(ChatSearchSection.Content))},''))";
                args["query"] = string.Join(" & ", tokens.Select(x => x + ":*"));
                sql = $"SELECT s.*, ts_rank_cd({vector},to_tsquery('simple',@query)) AS \"Score\", {Col(nameof(ChatSearchSection.Content))} AS \"Snippet\" FROM {table} s WHERE {Col(nameof(ChatSearchSection.FilestoreId))}=@storeId{userClause}{scopeClause} AND {vector} @@ to_tsquery('simple',@query) ORDER BY \"Score\" DESC LIMIT @take";
                break;
            case GeminiSearchDbKind.MySql:
                var cols = string.Join(',', new[] { nameof(ChatSearchSection.DocumentTitle), nameof(ChatSearchSection.Heading), nameof(ChatSearchSection.Content) }.Select(x => Col(x)));
                args["query"] = string.Join(' ', tokens.Select(x => "+" + x + "*"));
                sql = $"SELECT s.*, MATCH({cols}) AGAINST(@query IN BOOLEAN MODE) AS Score, {Col(nameof(ChatSearchSection.Content))} AS Snippet FROM {table} s WHERE {Col(nameof(ChatSearchSection.FilestoreId))}=@storeId{userClause}{scopeClause} AND MATCH({cols}) AGAINST(@query IN BOOLEAN MODE) ORDER BY Score DESC LIMIT @take";
                break;
            case GeminiSearchDbKind.SqlServer:
                args["query"] = string.Join(" AND ", tokens.Select(x => $"\"{x}*\""));
                sql = $"SELECT TOP (@take) s.*, ft.RANK AS Score, {Col(nameof(ChatSearchSection.Content))} AS Snippet FROM {table} s JOIN CONTAINSTABLE({table}, *, @query) ft ON ft.[KEY]={Col(nameof(ChatSearchSection.Id))} WHERE {Col(nameof(ChatSearchSection.FilestoreId))}=@storeId{userClause}{scopeClause} ORDER BY ft.RANK DESC";
                break;
            default:
                return null;
        }
        return new GeminiSearchDbQuery(sql, args);
    }

    string ScopeClause(JsonObject? scope, Func<string, string, string> col,
        Dictionary<string, object> args)
    {
        if (scope == null) return "";
        var clauses = new List<string>();
        foreach (var field in GeminiSearch.ScopeFields)
        {
            var value = scope.GetString(field);
            if (string.IsNullOrEmpty(value)) continue;
            var parameter = "scope" + char.ToUpperInvariant(field[0]) + field[1..];
            args[parameter] = value;
            var column = col(field switch
            {
                "docType" => nameof(ChatSearchSection.DocType),
                "category" => nameof(ChatSearchSection.Category),
                "status" => nameof(ChatSearchSection.Status),
                "locale" => nameof(ChatSearchSection.Locale),
                "product" => nameof(ChatSearchSection.Product),
                "versions" => nameof(ChatSearchSection.Versions),
                "tags" => nameof(ChatSearchSection.Tags),
                _ => throw new ArgumentOutOfRangeException(nameof(field)),
            }, "s");
            if (field is not ("versions" or "tags"))
            {
                clauses.Add($"{column}=@{parameter}");
                continue;
            }
            clauses.Add(Kind switch
            {
                GeminiSearchDbKind.Sqlite => $"EXISTS (SELECT 1 FROM json_each(COALESCE({column},'[]')) WHERE value=@{parameter})",
                GeminiSearchDbKind.PostgreSql => $"EXISTS (SELECT 1 FROM jsonb_array_elements_text(COALESCE({column},'[]')::jsonb) AS scope_value(value) WHERE value=@{parameter})",
                GeminiSearchDbKind.MySql => $"JSON_CONTAINS(COALESCE({column},JSON_ARRAY()),JSON_QUOTE(@{parameter}))",
                GeminiSearchDbKind.SqlServer => $"EXISTS (SELECT 1 FROM OPENJSON(COALESCE({column},'[]')) WHERE [value]=@{parameter})",
                _ => "1=1",
            });
        }
        return clauses.Count == 0 ? "" : " AND " + string.Join(" AND ", clauses);
    }

    public static void ApplyFallbackScope(SqlExpression<ChatSearchSection> q, JsonObject? scope)
    {
        if (scope == null) return;
        foreach (var field in GeminiSearch.ScopeFields)
        {
            var value = scope.GetString(field);
            if (string.IsNullOrEmpty(value)) continue;
            switch (field)
            {
                case "category": q.And(x => x.Category == value); break;
                case "docType": q.And(x => x.DocType == value); break;
                case "status": q.And(x => x.Status == value); break;
                case "locale": q.And(x => x.Locale == value); break;
                case "product": q.And(x => x.Product == value); break;
                case "versions":
                    var version = JsonSerializer.Serialize(value);
                    q.And(x => x.Versions!.Contains(version));
                    break;
                case "tags":
                    var tag = JsonSerializer.Serialize(value);
                    q.And(x => x.Tags!.Contains(tag));
                    break;
            }
        }
    }
}
