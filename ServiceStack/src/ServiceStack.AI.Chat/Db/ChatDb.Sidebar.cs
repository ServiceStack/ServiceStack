using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Project sidebar and focused thread-metadata writes (port of llms-py extensions/app/db.py
/// sidebar_page/rename_thread/move_thread/reconcile_projects). None of these read or rewrite the
/// compatibility `Messages` blob or canonical ChatMessage rows. Compare-and-set updates are single
/// parameterized UPDATE statements built from dialect-quoted identifiers, so they stay portable
/// across the supported OrmLite databases.
/// </summary>
public partial class ChatDb
{
    public static class TitleSources
    {
        public const string Placeholder = "placeholder";
        public const string Fallback = "fallback";
        public const string Generated = "generated";
        public const string Manual = "manual";
        public const string Legacy = "legacy";
    }

    public static class TitleStatuses
    {
        public const string Idle = "idle";
        public const string Pending = "pending";
        public const string Complete = "complete";
        public const string Failed = "failed";
        public const string Skipped = "skipped";
    }

    /// <summary>Backfill metadata for rows created before the project sidebar (idempotent)</summary>
    void MigrateThreadMetadata(IDbConnection db)
    {
        db.UpdateOnly(() => new ChatThread { TitleSource = TitleSources.Legacy }, x => x.TitleSource == null);
        db.UpdateOnly(() => new ChatThread { TitleStatus = TitleStatuses.Idle }, x => x.TitleStatus == null);
        db.UpdateOnly(() => new ChatThread { MetadataVersion = 0 }, x => x.MetadataVersion == null);
        db.UpdateOnly(() => new ChatThread { MembershipVersion = 0 }, x => x.MembershipVersion == null);
        db.UpdateOnly(() => new ChatThread { TitleVersion = 0 }, x => x.TitleVersion == null);

        var dialect = db.GetDialectProvider();
        string C(string name) => dialect.GetQuotedColumnName(name);
        var table = dialect.GetQuotedTableName(typeof(ChatThread).GetModelMetadata());
        db.ExecuteSql($"UPDATE {table} SET {C(nameof(ChatThread.LastActivityAt))} = {C(nameof(ChatThread.UpdatedAt))} " +
                      $"WHERE {C(nameof(ChatThread.LastActivityAt))} IS NULL");
        try
        {
            // Keyset index for the sidebar. Plain CREATE INDEX is portable; it throws once it exists.
            db.ExecuteSql($"CREATE INDEX {dialect.GetQuotedName("idx_chatthread_sidebar")} ON {table} " +
                          $"({C("user")}, {C(nameof(ChatThread.ProjectId))}, {C(nameof(ChatThread.LastActivityAt))}, {C(nameof(ChatThread.Id))})");
        }
        catch (Exception) { /* already exists */ }
    }

    /// <summary>Defaults for a new row: sidebar activity, versions and automatic-title ownership</summary>
    static void InitNewThread(ChatThread thread)
    {
        if (thread.CreatedAt == default) thread.CreatedAt = DateTime.Now;
        if (thread.UpdatedAt == default) thread.UpdatedAt = thread.CreatedAt;
        thread.LastActivityAt ??= thread.CreatedAt;
        thread.MetadataVersion ??= 0;
        thread.MembershipVersion ??= 0;
        thread.TitleVersion ??= 0;
        thread.TitleStatus ??= TitleStatuses.Idle;
        thread.TitleSource ??= string.IsNullOrEmpty(thread.Title) || thread.Title == "New Chat"
            ? TitleSources.Placeholder
            : TitleSources.Manual;
    }

    string Q(IDbConnection db, string column) => db.GetDialectProvider().GetQuotedColumnName(column);

    /// <summary>
    /// SQL fragment (for UnsafeAnd: built only from dialect-quoted identifiers and parameters)
    /// matching threads with a persisted conversation, without hydrating history:
    /// an active canonical message, or a legacy compatibility blob that isn't empty.
    /// </summary>
    string HasMessagesSql(SqlExpression<ChatThread> q)
    {
        var d = q.DialectProvider;
        string C(string name) => d.GetQuotedColumnName(name);
        var threadTable = d.GetQuotedTableName(typeof(ChatThread).GetModelMetadata());
        var messageTable = d.GetQuotedTableName(typeof(ChatMessage).GetModelMetadata());
        var active = q.ConvertToParam(true);
        var messages = C(nameof(ChatThread.Messages));
        return $"(EXISTS (SELECT 1 FROM {messageTable} WHERE {messageTable}.{C(nameof(ChatMessage.ThreadId))} = " +
               $"{threadTable}.{C(nameof(ChatThread.Id))} AND {messageTable}.{C(nameof(ChatMessage.Active))} = {active}) " +
               $"OR ({messages} IS NOT NULL AND {messages} <> '' AND {messages} <> '[]'))";
    }

    int ExecuteUpdate(IDbConnection db, string setSql, SqlExpression<ChatThread> where)
    {
        var table = db.GetDialectProvider().GetQuotedTableName(typeof(ChatThread).GetModelMetadata());
        using var cmd = db.CreateCommand();
        cmd.CommandText = $"UPDATE {table} SET {setSql} {where.WhereExpression}";
        foreach (var p in where.Params)
            cmd.Parameters.Add(p);
        return cmd.ExecuteNonQuery();
    }

    string Bump(IDbConnection db, string column) => $"{Q(db, column)} = COALESCE({Q(db, column)}, 0) + 1";

    string ActiveRunSql(SqlExpression<ChatThread> q, long threadId)
    {
        var d = q.DialectProvider;
        var runTable = d.GetQuotedTableName(typeof(AgentRun).GetModelMetadata());
        return $"NOT EXISTS (SELECT 1 FROM {runTable} WHERE {d.GetQuotedColumnName(nameof(AgentRun.ThreadId))} = {q.ConvertToParam(threadId)} " +
               $"AND {d.GetQuotedColumnName(nameof(AgentRun.Status))} IN ({q.ConvertToParam(AgentRunStatus.Queued)}, " +
               $"{q.ConvertToParam(AgentRunStatus.Running)}, {q.ConvertToParam(AgentRunStatus.WaitingApproval)}))";
    }

    SqlExpression<ChatThread> ThreadWhere(IDbConnection db, string? user)
    {
        var q = db.From<ChatThread>();
        if (user != null && !IsAllUsers(user))
            q.Where(x => x.User == user);
        return q;
    }

    /// <summary>True when a thread in any of these projects has a queued/running/approval-waiting run</summary>
    public bool HasActiveRunsInProjects(ICollection<string> projectIds, string? user)
    {
        if (projectIds.Count == 0) return false;
        using var db = OpenDb();
        var threadIds = db.Column<long>(ThreadWhere(db, user)
            .And(x => Sql.In(x.ProjectId, projectIds)).Select(x => x.Id));
        return threadIds.Count > 0 && db.Exists(db.From<AgentRun>().Where(x => Sql.In(x.ThreadId, threadIds)
            && (x.Status == AgentRunStatus.Queued || x.Status == AgentRunStatus.Running
                || x.Status == AgentRunStatus.WaitingApproval)));
    }

    /// <summary>Move associations to deleted projects back to Recents (idempotent)</summary>
    public int ReconcileProjects(ICollection<string> projectIds, string? user)
    {
        using var db = OpenDb();
        var q = ThreadWhere(db, user).And(x => x.ProjectId != null);
        if (projectIds.Count > 0)
            q.And(x => !Sql.In(x.ProjectId, projectIds));
        return ExecuteUpdate(db, $"{Q(db, nameof(ChatThread.ProjectId))} = NULL, " +
            $"{Bump(db, nameof(ChatThread.MembershipVersion))}, {Bump(db, nameof(ChatThread.MetadataVersion))}", q);
    }

    /// <summary>
    /// Compare-and-set project move. Returns false when the membership version changed or the
    /// thread has an active run (a durable run must not change workspace midway).
    /// </summary>
    public bool MoveThread(long id, string? projectId, long membershipVersion, string? user)
    {
        using var db = OpenDb();
        var q = ThreadWhere(db, user).And(x => x.Id == id);
        q.And($"COALESCE({Q(db, nameof(ChatThread.MembershipVersion))}, 0) = {{0}}", membershipVersion);
        q.UnsafeAnd(ActiveRunSql(q, id));
        var project = projectId == null ? "NULL" : q.ConvertToParam(projectId);
        return ExecuteUpdate(db, $"{Q(db, nameof(ChatThread.ProjectId))} = {project}, " +
            $"{Bump(db, nameof(ChatThread.MembershipVersion))}, {Bump(db, nameof(ChatThread.MetadataVersion))}", q) > 0;
    }

    /// <summary>An explicit rename owns the title: it invalidates any pending generated title</summary>
    public bool RenameThread(long id, string title, string? user)
    {
        title = string.Join(' ', title.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (title.Length > 200) title = title[..200];
        if (title.Length == 0)
            throw new ArgumentException("Title is required");
        using var db = OpenDb();
        var q = ThreadWhere(db, user).And(x => x.Id == id);
        return ExecuteUpdate(db, $"{Q(db, nameof(ChatThread.Title))} = {q.ConvertToParam(title)}, " +
            $"{Q(db, nameof(ChatThread.TitleSource))} = {q.ConvertToParam(TitleSources.Manual)}, " +
            $"{Q(db, nameof(ChatThread.TitleStatus))} = {q.ConvertToParam(TitleStatuses.Skipped)}, " +
            $"{Bump(db, nameof(ChatThread.TitleVersion))}, {Bump(db, nameof(ChatThread.MetadataVersion))}", q) > 0;
    }

    /// <summary>Conditional title-status transition, e.g. idle → pending claims generation exactly once</summary>
    public bool SetTitleStatus(long id, string status, params string[] fromStatuses)
    {
        using var db = OpenDb();
        var q = db.From<ChatThread>().Where(x => x.Id == id && Sql.In(x.TitleStatus, fromStatuses));
        return ExecuteUpdate(db, $"{Q(db, nameof(ChatThread.TitleStatus))} = {q.ConvertToParam(status)}", q) > 0;
    }

    /// <summary>
    /// Apply a generated title, or record failure, only while the thread still has the automatic
    /// fallback it was generated for: manual renames and deleted threads discard late results.
    /// Never touches activity ordering or conversation history.
    /// </summary>
    public bool CompleteGeneratedTitle(long id, long titleVersion, string? title)
    {
        using var db = OpenDb();
        var q = db.From<ChatThread>().Where(x => x.Id == id && x.TitleSource == TitleSources.Fallback);
        q.And($"COALESCE({Q(db, nameof(ChatThread.TitleVersion))}, 0) = {{0}}", titleVersion);
        var set = title != null
            ? $"{Q(db, nameof(ChatThread.Title))} = {q.ConvertToParam(title)}, " +
              $"{Q(db, nameof(ChatThread.TitleSource))} = {q.ConvertToParam(TitleSources.Generated)}, " +
              $"{Q(db, nameof(ChatThread.TitleStatus))} = {q.ConvertToParam(TitleStatuses.Complete)}, " +
              Bump(db, nameof(ChatThread.MetadataVersion))
            : $"{Q(db, nameof(ChatThread.TitleStatus))} = {q.ConvertToParam(TitleStatuses.Failed)}";
        return ExecuteUpdate(db, set, q) > 0;
    }

    /// <summary>Threads whose automatic title was interrupted (e.g. by a restart)</summary>
    public List<ChatThread> GetPendingTitleThreads()
    {
        using var db = OpenDb();
        return db.Select(db.From<ChatThread>()
            .Where(x => x.TitleSource == TitleSources.Fallback
                && (x.TitleStatus == TitleStatuses.Idle || x.TitleStatus == TitleStatuses.Pending))
            .Select(x => new { x.Id, x.User, x.TitleVersion }));
    }

    /// <summary>The first active user message of a thread, used as title input</summary>
    public JsonObject? GetFirstUserMessage(long threadId)
    {
        EnsureChatMessages(threadId);
        using var db = OpenDb();
        var row = db.Single(db.From<ChatMessage>()
            .Where(x => x.ThreadId == threadId && x.Role == "user" && x.Active)
            .OrderBy(x => x.Sequence).Limit(1));
        return row == null ? null : ChatDtos.ParseJson(row.Message) as JsonObject;
    }

    /// <summary>Project IDs with a persisted conversation; empty folders stay out of the sidebar</summary>
    public HashSet<string> ProjectIdsWithMessages(string? user)
    {
        using var db = OpenDb();
        var q = db.From<ChatThread>();
        ApplyUserFilter(q, user);
        q.And(x => x.ProjectId != null);
        q.UnsafeAnd(HasMessagesSql(q));
        q.SelectDistinct(x => x.ProjectId);
        return db.Column<string>(q).ToSet();
    }

    /// <summary>
    /// A cheap fingerprint of everything the sidebar shows: grouped thread counts/ids/metadata
    /// versions/activity and run states. Changes whenever a group needs refreshing.
    /// </summary>
    public string SidebarRevision(string? user)
    {
        using var db = OpenDb();
        string C(string name) => Q(db, name);
        var threads = db.From<ChatThread>();
        ApplyUserFilter(threads, user);
        threads.GroupBy(C(nameof(ChatThread.ProjectId)))
            .Select($"{C(nameof(ChatThread.ProjectId))}, COUNT(*), SUM({C(nameof(ChatThread.Id))}), " +
                    $"SUM(COALESCE({C(nameof(ChatThread.MetadataVersion))}, 0)), " +
                    $"MAX({C(nameof(ChatThread.LastActivityAt))}), MAX({C(nameof(ChatThread.CompletedAt))})");
        var runs = db.From<AgentRun>();
        ApplyUserFilter(runs, user);
        runs.GroupBy(C(nameof(AgentRun.Status)))
            .Select($"{C(nameof(AgentRun.Status))}, COUNT(*), SUM({C(nameof(AgentRun.Id))})");
        var sb = new StringBuilder();
        foreach (var row in db.Select<List<object>>(threads).Concat(db.Select<List<object>>(runs)))
            sb.AppendLine(string.Join('|', row.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture))));
        return sb.ToString();
    }

    /// <summary>
    /// Compact, ownership-scoped keyset page ordered by (lastActivityAt DESC, id DESC). Never
    /// hydrates conversation history. Returns {items, nextCursor, hasMore}.
    /// </summary>
    public JsonObject SidebarPage(string? user, string? projectId, int limit = 10, string? cursor = null)
    {
        if (limit is < 1 or > 100)
            throw new ArgumentException("Sidebar limit must be between 1 and 100");
        using var db = OpenDb();
        var q = db.From<ChatThread>();
        ApplyUserFilter(q, user);
        if (projectId == null)
            q.And(x => x.ProjectId == null);
        else
        {
            q.And(x => x.ProjectId == projectId);
            q.UnsafeAnd(HasMessagesSql(q));
        }
        if (!string.IsNullOrEmpty(cursor))
        {
            var (activity, rowId) = DecodeCursor(cursor, projectId);
            q.And(x => x.LastActivityAt < activity || (x.LastActivityAt == activity && x.Id < rowId));
        }
        q.OrderByDescending(x => x.LastActivityAt).ThenByDescending(x => x.Id).Limit(limit + 1);
        q.Select(x => new
        {
            x.Id, x.Title, x.ProjectId, x.LastActivityAt, x.MetadataVersion, x.MembershipVersion,
            x.Status, x.CompletedAt, x.Error, x.Model, x.Stats, x.InputTokens, x.OutputTokens, x.Cost,
        });
        var rows = db.Select(q);
        var hasMore = rows.Count > limit;
        rows = rows.Take(limit).ToList();

        var ids = rows.Select(x => x.Id).ToList();
        var runStatus = new Dictionary<long, string>();
        var counts = new Dictionary<long, long>();
        if (ids.Count > 0)
        {
            foreach (var run in db.Select(db.From<AgentRun>()
                         .Where(x => Sql.In(x.ThreadId, ids) && (x.Status == AgentRunStatus.Queued
                             || x.Status == AgentRunStatus.Running || x.Status == AgentRunStatus.WaitingApproval))
                         .OrderByDescending(x => x.Id).Select(x => new { x.Id, x.ThreadId, x.Status })))
                runStatus.TryAdd(run.ThreadId, run.Status);
            counts = db.Dictionary<long, long>(db.From<ChatMessage>()
                .Where(x => Sql.In(x.ThreadId, ids) && x.Active)
                .GroupBy(x => x.ThreadId)
                .Select(x => new { x.ThreadId, Count = Sql.Count("*") }));
        }

        var items = new JsonArray();
        foreach (var row in rows)
        {
            items.Add(new JsonObject
            {
                ["id"] = row.Id,
                ["title"] = row.Title,
                ["projectId"] = row.ProjectId,
                ["lastActivityAt"] = ToDateNode(row.LastActivityAt),
                ["metadataVersion"] = row.MetadataVersion ?? 0,
                ["membershipVersion"] = row.MembershipVersion ?? 0,
                ["status"] = row.Status,
                ["completedAt"] = ToDateNode(row.CompletedAt),
                ["error"] = row.Error,
                ["model"] = row.Model,
                ["stats"] = ChatDtos.ParseJson(row.Stats),
                ["inputTokens"] = row.InputTokens,
                ["outputTokens"] = row.OutputTokens,
                ["cost"] = row.Cost,
                ["runStatus"] = runStatus.GetValueOrDefault(row.Id),
                // legacy rows without canonical messages leave this unset; the hover card fetches it
                ["messageCount"] = counts.TryGetValue(row.Id, out var count) ? count : null,
            });
        }
        string? nextCursor = null;
        if (hasMore && rows.Count > 0)
        {
            var last = rows[^1];
            nextCursor = Convert.ToBase64String(Encoding.UTF8.GetBytes(new JsonArray(projectId,
                (last.LastActivityAt ?? default).ToString("o", CultureInfo.InvariantCulture), last.Id)
                .ToJsonString())).Replace('+', '-').Replace('/', '_');
        }
        return new JsonObject { ["items"] = items, ["nextCursor"] = nextCursor, ["hasMore"] = hasMore };
    }

    static (DateTime Activity, long Id) DecodeCursor(string cursor, string? projectId)
    {
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor.Replace('-', '+').Replace('_', '/')));
            if (JsonNode.Parse(json) is JsonArray { Count: 3 } parts
                && parts[0]?.GetValue<string>() == projectId
                && DateTime.TryParse(parts[1]?.GetValue<string>(), CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var activity)
                && parts[2] is JsonValue idValue && idValue.TryGetValue<long>(out var id))
                return (activity, id);
        }
        catch (Exception) { /* malformed */ }
        throw new ArgumentException("Invalid sidebar cursor");
    }
}
