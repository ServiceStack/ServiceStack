using System.Data;
using System.Text.Json.Nodes;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// OrmLite data access for chat threads/requests/media, replacing llms-py's per-user SQLite files
/// with a single shared RDBMS partitioned by the "user" column (extensions/app/db.py + gallery/db.py).
/// DTOs are emitted in the exact wire shape the synced UI expects: camelCase keys, parsed JSON
/// columns, and sortable string timestamps (the UI string-compares updatedAt when long-polling).
/// </summary>
public class ChatDb(IDbConnectionFactory dbFactory, string? namedConnection = null)
{
    /// <summary>Python datetime repr — sortable and directly string-comparable</summary>
    public const string DateFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

    public static string ToDateString(DateTime date) => date.ToString(DateFormat);
    public static JsonNode? ToDateNode(DateTime? date) => date == null ? null : ToDateString(date.Value);

    public IDbConnection OpenDb() => namedConnection != null
        ? dbFactory.OpenDbConnection(namedConnection)
        : dbFactory.OpenDbConnection();

    public void InitSchema()
    {
        using var db = OpenDb();
        db.CreateTableIfNotExists<ChatThread>();
        db.CreateTableIfNotExists<ChatRequest>();
        db.CreateTableIfNotExists<ChatMedia>();

        // tables created by an earlier version are missing newly added columns
        // (port of Python's add_missing_columns PRAGMA/ALTER TABLE migration)
        AddMissingColumns<ChatThread>(db);
        AddMissingColumns<ChatRequest>(db);
        AddMissingColumns<ChatMedia>(db);
    }

    /// <summary>Add columns missing from a table created by an earlier version (also used by extension tables)</summary>
    public static void AddMissingColumns<T>(IDbConnection db)
    {
        var modelDef = typeof(T).GetModelMetadata();
        foreach (var fieldDef in modelDef.FieldDefinitions)
        {
            if (fieldDef.IsComputed || fieldDef.IsRowVersion)
                continue;
            if (db.ColumnExists(fieldDef.FieldName, new TableRef(modelDef)))
                continue;
            try
            {
                db.AddColumn(typeof(T), fieldDef);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed adding {modelDef.ModelName} column {fieldDef.FieldName}: {e.Message}");
            }
        }
    }

    // ── Threads ──
    public ChatThread? GetThread(long id, string? user)
    {
        using var db = OpenDb();
        var q = db.From<ChatThread>().Where(x => x.Id == id);
        if (user != null && !IsAllUsers(user))
            q.And(x => x.User == user);
        return db.Single(q);
    }

    public T? GetThreadColumn<T>(long id, string columnName, string? user)
    {
        using var db = OpenDb();
        var q = db.From<ChatThread>().Where(x => x.Id == id);
        if (user != null && !IsAllUsers(user))
            q.And(x => x.User == user);
        q.Select(columnName.SqlColumn(db.GetDialectProvider()));
        return db.Scalar<T>(q);
    }

    public long InsertThread(ChatThread thread)
    {
        using var db = OpenDb();
        return db.Insert(thread, selectIdentity: true);
    }

    public void UpdateThread(ChatThread thread, string? user)
    {
        using var db = OpenDb();
        db.Update(thread);
    }

    /// <summary>
    /// Write only the in-flight streaming message. A thread's messages can be megabytes, so a stream
    /// checkpoint updates this one small column instead of rewriting the row.
    /// </summary>
    public void UpdateThreadStreamingMessage(long id, string? json, string? user)
    {
        using var db = OpenDb();
        var q = db.From<ChatThread>().Where(x => x.Id == id);
        if (user != null && !IsAllUsers(user))
            q.And(x => x.User == user);
        db.UpdateOnly(() => new ChatThread { StreamingMessage = json, UpdatedAt = DateTime.Now }, q);
    }

    public bool DeleteThread(long id, string? user)
    {
        using var db = OpenDb();
        var q = db.From<ChatThread>().Where(x => x.Id == id);
        if (user != null && !IsAllUsers(user))
            q.And(x => x.User == user);
        return db.Delete(q) > 0;
    }

    /// <summary>Port of AppDB.query_threads: take/skip/sort/fields/equality/null/not_null/q filters</summary>
    public List<ChatThread> QueryThreads(JsonObject query, string? user)
    {
        using var db = OpenDb();
        var q = db.From<ChatThread>();
        ApplyUserFilter(q, user);
        ApplyFilters(q, query, ThreadColumns);

        if (query.GetString("q") is { Length: > 0 } search)
        {
            var like = $"%{search}%";
            q.And(x => x.Title!.Contains(search) || x.Messages!.Contains(search));
        }

        ApplySort(q, query.GetString("sort") ?? "-id", ThreadColumns);
        q.Limit(query.GetInt("skip") ?? 0, Math.Min(query.GetInt("take") ?? 50, 1000));
        return db.Select(q);
    }

    // ── Requests ──

    public long InsertRequest(ChatRequest request)
    {
        using var db = OpenDb();
        return db.Insert(request, selectIdentity: true);
    }

    public void UpdateRequest(ChatRequest request)
    {
        using var db = OpenDb();
        db.Update(request);
    }

    public ChatRequest? GetRequest(long id, string? user)
    {
        using var db = OpenDb();
        var q = db.From<ChatRequest>().Where(x => x.Id == id);
        if (user != null && !IsAllUsers(user))
            q.And(x => x.User == user);
        return db.Single(q);
    }

    public bool DeleteRequest(long id, string? user)
    {
        using var db = OpenDb();
        var q = db.From<ChatRequest>().Where(x => x.Id == id);
        if (user != null && !IsAllUsers(user))
            q.And(x => x.User == user);
        return db.Delete(q) > 0;
    }

    public List<ChatRequest> QueryRequests(JsonObject query, string? user)
    {
        using var db = OpenDb();
        var q = db.From<ChatRequest>();
        ApplyUserFilter(q, user);
        ApplyFilters(q, query, RequestColumns);

        // ?month=YYYY-MM filters on createdAt (Python: strftime('%Y-%m', createdAt))
        if (query.GetString("month") is { Length: 7 } month
            && DateTime.TryParse($"{month}-01", out var from))
        {
            var to = from.AddMonths(1);
            q.And(x => x.CreatedAt >= from && x.CreatedAt < to);
        }

        ApplySort(q, query.GetString("sort") ?? "-id", RequestColumns);
        q.Limit(query.GetInt("skip") ?? 0, Math.Min(query.GetInt("take") ?? 1000, 10000));
        return db.Select(q);
    }

    /// <summary>Daily cost/token rollups for the analytics dashboards (port of requests/summary)</summary>
    public List<ChatRequest> GetRequestsForSummary(string? user, DateTime? from = null)
    {
        using var db = OpenDb();
        var q = db.From<ChatRequest>();
        ApplyUserFilter(q, user);
        if (from != null)
            q.And(x => x.CreatedAt >= from.Value);
        q.Select(x => new {
            x.CreatedAt, x.Cost, x.InputTokens, x.OutputTokens, x.TotalTokens, x.Model, x.Provider, x.Duration,
        });
        return db.Select(q);
    }

    // ── Media (gallery) ──

    public long InsertMedia(ChatMedia media)
    {
        using var db = OpenDb();
        return db.Insert(media, selectIdentity: true);
    }

    public List<ChatMedia> QueryMedia(JsonObject query, string? user)
    {
        using var db = OpenDb();
        var q = db.From<ChatMedia>();
        ApplyUserFilter(q, user);
        ApplyFilters(q, query, MediaColumns);

        if (query.GetString("q") is { Length: > 0 } search)
        {
            q.And(x => x.Prompt!.Contains(search) || x.Name!.Contains(search)
                || x.Description!.Contains(search) || x.Caption!.Contains(search));
        }
        if (query.GetString("format") is { } format
            && MediaFormats.GetValueOrDefault(format) is { Count: > 0 } ratios)
        {
            q.And(x => ratios.Contains(x.AspectRatio!));
        }

        ApplySort(q, query.GetString("sort") ?? "-id", MediaColumns);
        q.Limit(query.GetInt("skip") ?? 0, Math.Min(query.GetInt("take") ?? 50, 1000));
        return db.Select(q);
    }

    /// <summary>Counts per media type (port of media_totals)</summary>
    public List<(string Type, long Count)> MediaTotals(string? user)
    {
        using var db = OpenDb();
        var q = db.From<ChatMedia>();
        ApplyUserFilter(q, user);
        q.GroupBy(x => x.Type)
            .OrderByDescending("COUNT(*)")
            .Select(x => new { x.Type, Count = Sql.Count("*") });
        return db.Dictionary<string, long>(q).Select(x => (x.Key, x.Value)).ToList();
    }

    public void UpdateMedia(ChatMedia media)
    {
        using var db = OpenDb();
        db.Update(media);
    }

    /// <summary>Ratio buckets for the gallery format filter (portrait/landscape/square)</summary>
    public static readonly Dictionary<string, List<string>> MediaFormats = new()
    {
        ["square"] = ChatFeature.AspectRatios.Keys.Where(r => RatioFormat(r) == 0).ToList(),
        ["landscape"] = ChatFeature.AspectRatios.Keys.Where(r => RatioFormat(r) > 0).ToList(),
        ["portrait"] = ChatFeature.AspectRatios.Keys.Where(r => RatioFormat(r) < 0).ToList(),
    };

    static int RatioFormat(string ratio)
    {
        var w = int.Parse(ratio.LeftPart(':'));
        var h = int.Parse(ratio.RightPart(':'));
        return w.CompareTo(h);
    }

    /// <summary>Nearest configured aspect ratio for the given dimensions (port of closest_aspect_ratio)</summary>
    public static string ClosestAspectRatio(int width, int height)
    {
        var targetRatio = (double)width / height;
        var closest = "1:1";
        var minDiff = double.MaxValue;
        foreach (var ratio in ChatFeature.AspectRatios.Keys)
        {
            var diff = Math.Abs(targetRatio -
                (double)int.Parse(ratio.LeftPart(':')) / int.Parse(ratio.RightPart(':')));
            if (diff < minDiff)
            {
                minDiff = diff;
                closest = ratio;
            }
        }
        return closest;
    }

    public bool DeleteMediaByHash(string hash, string? user)
    {
        using var db = OpenDb();
        var q = db.From<ChatMedia>().Where(x => x.Hash == hash);
        if (user != null && !IsAllUsers(user))
            q.And(x => x.User == user);
        return db.Delete(q) > 0;
    }

    public ChatMedia? GetMediaByHash(string hash, string? user)
    {
        using var db = OpenDb();
        var q = db.From<ChatMedia>().Where(x => x.Hash == hash);
        if (user != null && !IsAllUsers(user))
            q.And(x => x.User == user);
        return db.Single(q);
    }

    // ── Query helpers (shared with the extension tables, e.g. gemini's filestore/document) ──

    public static void ApplyUserFilter<T>(SqlExpression<T> q, string? user)
    {
        // Admin views read across every user by asking for "all"
        if (IsAllUsers(user))
            return;
        // null user (auth disabled) sees the shared "default" partition
        var partition = user ?? DefaultUser;
        q.Where("\"user\" = {0}".Replace("\"user\"", q.DialectProvider.GetQuotedColumnName("user")), partition);
    }

    public const string DefaultUser = "default";

    /// <summary>
    /// The sentinel an authorized Admin view passes to read across every user's rows
    /// (llms-py's user="all"/"*"). Callers must have checked <see cref="IChatAuth.IsAdmin"/> first —
    /// nothing in ChatDb authorizes it.
    /// </summary>
    public static bool IsAllUsers(string? user) => user is AllUsers or "*";

    public const string AllUsers = "all";

    // ── Per-user rollups for the Admin analytics views ──

    /// <summary>One row per user that has made a request, most active first (port of get_users_summary)</summary>
    public List<ChatUserSummary> GetUsersSummary()
    {
        using var db = OpenDb();
        // every identifier is quoted through the dialect: unquoted names fold to lower case on
        // Postgres and wouldn't match OrmLite's PascalCase columns
        string C(string name) => db.GetDialectProvider().GetQuotedColumnName(name);
        return db.Select<ChatUserSummary>(db.From<ChatRequest>()
            .GroupBy(C("user"))
            .OrderBy("COUNT(*) DESC")
            .Select($@"{C("user")} AS {C(nameof(ChatUserSummary.User))},
                COUNT(*) AS {C(nameof(ChatUserSummary.Requests))},
                SUM({C(nameof(ChatRequest.Cost))}) AS {C(nameof(ChatUserSummary.Cost))},
                SUM({C(nameof(ChatRequest.InputTokens))}) AS {C(nameof(ChatUserSummary.InputTokens))},
                SUM({C(nameof(ChatRequest.OutputTokens))}) AS {C(nameof(ChatUserSummary.OutputTokens))},
                MAX({C(nameof(ChatRequest.CreatedAt))}) AS {C(nameof(ChatUserSummary.LastActive))}"));
    }

    /// <summary>Distinct users that have made a request, sorted (port of get_users_list)</summary>
    public List<string> GetUsersList()
    {
        using var db = OpenDb();
        var userColumn = db.GetDialectProvider().GetQuotedColumnName("user");
        return db.Column<string?>(db.From<ChatRequest>()
                .GroupBy(userColumn)
                .OrderBy(userColumn)
                .Select(userColumn))
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x => x!)
            .ToList();
    }

    public static void ApplyFilters<T>(SqlExpression<T> q, JsonObject query, Dictionary<string, string> columns)
    {
        foreach (var entry in query)
        {
            // ?user= selects whose rows to read and is applied by ApplyUserFilter after the caller
            // has authorized it — never as an ordinary column filter on top of it
            if (entry.Key == "user")
                continue;
            if (!columns.TryGetValue(entry.Key, out var column))
                continue;
            if (entry.Value is not JsonValue value)
                continue;
            // convert to the column's CLR type so typed columns (e.g. bigint) aren't compared as text
            var propertyType = typeof(T).GetProperty(column)?.PropertyType;
            var typedValue = ConvertValue(value.ToString(), propertyType);
            q.And($"{q.DialectProvider.GetQuotedColumnName(column)} = {{0}}", typedValue);
        }

        if (query.GetString("null") is { } nullCols)
        {
            foreach (var col in ValidColumns(nullCols, columns))
                q.And($"{q.DialectProvider.GetQuotedColumnName(col)} IS NULL");
        }
        if (query.GetString("not_null") is { } notNullCols)
        {
            foreach (var col in ValidColumns(notNullCols, columns))
                q.And($"{q.DialectProvider.GetQuotedColumnName(col)} IS NOT NULL");
        }
    }

    public static void ApplySort<T>(SqlExpression<T> q, string sort, Dictionary<string, string> columns)
    {
        var desc = sort.StartsWith('-');
        var name = desc ? sort[1..] : sort;
        if (!columns.TryGetValue(name, out var column))
            column = nameof(ChatThread.Id);
        var quoted = q.DialectProvider.GetQuotedColumnName(column);
        q.OrderBy(desc ? $"{quoted} DESC" : quoted);
    }

    static object? ConvertValue(string value, Type? targetType)
    {
        if (targetType == null)
            return value;
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (type == typeof(string))
            return value;
        try
        {
            return Convert.ChangeType(value, type);
        }
        catch (Exception)
        {
            return value;
        }
    }

    static List<string> ValidColumns(string csv, Dictionary<string, string> columns) =>
        csv.Split(',')
            .Select(x => x.Trim())
            .Where(columns.ContainsKey)
            .Select(x => columns[x])
            .ToList();

    /// <summary>Wire (camelCase) name → POCO property, restricting query params to known columns</summary>
    public static Dictionary<string, string> ColumnsOf<T>() =>
        typeof(T).GetProperties().ToDictionary(
            x => x.Name == nameof(ChatThread.User) ? "user" : x.Name.ToCamelCase(),
            x => x.Name);

    public static readonly Dictionary<string, string> ThreadColumns = ColumnsOf<ChatThread>();
    public static readonly Dictionary<string, string> RequestColumns = ColumnsOf<ChatRequest>();
    public static readonly Dictionary<string, string> MediaColumns = ColumnsOf<ChatMedia>();
}
