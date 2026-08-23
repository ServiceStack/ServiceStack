using System.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.AI;

/// <summary>
/// OrmLite data access for the gemini extension's file stores + documents (port of gemini/db.py's
/// GeminiDB), sharing the host's chat database instead of Python's per-user gemini.sqlite file.
/// Rows are partitioned by the same "user" column convention as the rest of the chat tables.
/// </summary>
public partial class GeminiDb(ChatDb db)
{
    public IDbConnection OpenDb() => db.OpenDb();

    public void InitSchema()
    {
        using var conn = OpenDb();
        conn.CreateTableIfNotExists<ChatFilestore>();
        conn.CreateTableIfNotExists<ChatDocument>();
        conn.CreateTableIfNotExists<ChatSource>();
        conn.CreateTableIfNotExists<ChatSourceRun>();
        conn.CreateTableIfNotExists<ChatAssistant>();
        conn.CreateTableIfNotExists<ChatAssistantConversation>();
        conn.CreateTableIfNotExists<ChatAssistantMessage>();
        ChatDb.AddMissingColumns<ChatFilestore>(conn);
        ChatDb.AddMissingColumns<ChatDocument>(conn);
        ChatDb.AddMissingColumns<ChatSource>(conn);
        ChatDb.AddMissingColumns<ChatSourceRun>(conn);
        ChatDb.AddMissingColumns<ChatAssistant>(conn);
        ChatDb.AddMissingColumns<ChatAssistantConversation>(conn);
        ChatDb.AddMissingColumns<ChatAssistantMessage>(conn);
    }

    public static readonly Dictionary<string, string> FilestoreColumns = ChatDb.ColumnsOf<ChatFilestore>();
    public static readonly Dictionary<string, string> DocumentColumns = ChatDb.ColumnsOf<ChatDocument>();

    /// <summary>Document states the "issues" sort surfaces first (port of the sort=issues CASE)</summary>
    public static readonly string[] IssueStates =
    [
        "STATE_UNSPECIFIED", "STATE_PENDING", "MISSING_METADATA",
        "DUPLICATE_FILE", "MISSING_FROM_REMOTE", "METADATA_MISMATCH",
    ];

    static string Col<T>(SqlExpression<T> q, string name) => q.DialectProvider.GetQuotedColumnName(name);

    // ── Filestores ──

    /// <summary>A null user matches any partition (used by the upload worker, which runs outside a request)</summary>
    public ChatFilestore? GetFilestore(long id, string? user)
    {
        using var conn = OpenDb();
        return GetFilestore(conn, id, user);
    }

    static ChatFilestore? GetFilestore(IDbConnection conn, long id, string? user)
    {
        var q = conn.From<ChatFilestore>().Where(x => x.Id == id);
        if (user != null)
            ChatDb.ApplyUserFilter(q, user);
        return conn.Single(q);
    }

    public List<ChatFilestore> QueryFilestores(JsonObject query, string? user)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatFilestore>();
        ChatDb.ApplyUserFilter(q, user);
        ChatDb.ApplyFilters(q, query, FilestoreColumns);

        if (query.GetString("q") is { Length: > 0 } search)
            q.And(x => x.DisplayName!.Contains(search));

        var sort = query.GetString("sort") ?? "-id";
        if (sort == "failed")
        {
            var error = Col(q, nameof(ChatFilestore.Error));
            var failed = Col(q, nameof(ChatFilestore.FailedDocumentsCount));
            var createdAt = Col(q, nameof(ChatFilestore.CreatedAt));
            q.UnsafeOrderBy($"CASE WHEN {error} IS NOT NULL OR {failed} > 0 THEN 0 ELSE 1 END, {failed} DESC, {createdAt} DESC");
        }
        else
        {
            ChatDb.ApplySort(q, sort, FilestoreColumns);
        }

        q.Limit(query.GetInt("skip") ?? 0, Math.Min(query.GetInt("take") ?? 50, 1000));
        return conn.Select(q);
    }

    public long InsertFilestore(ChatFilestore filestore)
    {
        using var conn = OpenDb();
        return conn.Insert(filestore, selectIdentity: true);
    }

    public void UpdateFilestore(ChatFilestore filestore)
    {
        filestore.UpdatedAt = DateTime.Now;
        using var conn = OpenDb();
        conn.Update(filestore);
    }

    /// <summary>Describe every local record affected by permanently deleting a File Store.</summary>
    public JsonObject? FilestoreDeleteSummary(long id, string? user)
    {
        using var conn = OpenDb();
        return FilestoreDeleteSummary(conn, id, user);
    }

    static JsonObject? FilestoreDeleteSummary(IDbConnection conn, long id, string? user)
    {
        var store = GetFilestore(conn, id, user);
        if (store == null) return null;

        var sourceIds = conn.Column<long>(conn.From<ChatSource>()
            .Where(x => x.FilestoreId == id).Select(x => x.Id));
        var documents = conn.Select<ChatDocument>(x => x.FilestoreId == id)
            .ToDictionary(x => x.Id);
        foreach (var sourceIdsBatch in sourceIds.Chunk(500))
        {
            foreach (var document in conn.Select<ChatDocument>(x => x.SourceId != null
                         && sourceIdsBatch.Contains(x.SourceId.Value)))
                documents.TryAdd(document.Id, document);
        }
        var assistants = conn.Select<ChatAssistant>(x => x.FilestoreId == id);
        var conversationIds = new List<long>();
        foreach (var assistantIdsBatch in assistants.Select(x => x.Id).Chunk(500))
        {
            conversationIds.AddRange(conn.Column<long>(conn.From<ChatAssistantConversation>()
                .Where(x => assistantIdsBatch.Contains(x.AssistantId)).Select(x => x.Id)));
        }
        long messages = 0;
        foreach (var conversationIdsBatch in conversationIds.Chunk(500))
            messages += conn.Count<ChatAssistantMessage>(x => conversationIdsBatch.Contains(x.ConversationId));

        var remoteDocuments = (store.ActiveDocumentsCount ?? 0)
            + (store.PendingDocumentsCount ?? 0) + (store.FailedDocumentsCount ?? 0);
        return new JsonObject
        {
            ["id"] = store.Id,
            ["name"] = store.Name,
            ["displayName"] = store.DisplayName,
            ["remoteStoreExists"] = !string.IsNullOrEmpty(store.Name),
            ["remoteDocuments"] = remoteDocuments,
            ["remoteDocumentBytes"] = store.SizeBytes ?? 0,
            ["documents"] = documents.Count,
            ["documentBytes"] = documents.Values.Sum(x => x.SizeBytes ?? x.Size ?? 0),
            ["savedImports"] = sourceIds.Count,
            ["importRuns"] = sourceIds.Count == 0 ? 0 : conn.Count<ChatSourceRun>(x => sourceIds.Contains(x.SourceId)),
            ["assistants"] = assistants.Count,
            ["publishedAssistants"] = assistants.Count(x => x.Enabled && x.PublishedAt != null),
            ["conversations"] = conversationIds.Count,
            ["messages"] = messages,
        };
    }

    /// <summary>
    /// Transactionally deletes the File Store and every dependent record identified by its
    /// relationships. The user filter is used to authorize the store; dependent rows are then
    /// removed by store identity so stale ownership metadata cannot leave conflicting orphans.
    /// </summary>
    public JsonObject? DeleteFilestore(long id, string? user, string? confirmation = null)
    {
        using var conn = OpenDb();
        using var tx = conn.OpenTransaction();
        var impact = FilestoreDeleteSummary(conn, id, user);
        if (impact == null) return null;
        if (confirmation != null && confirmation != impact.GetString("displayName"))
            throw new ArgumentException($"Type \"{impact.GetString("displayName")}\" to confirm permanent deletion");

        var sourceIds = conn.Column<long>(conn.From<ChatSource>()
            .Where(x => x.FilestoreId == id).Select(x => x.Id));
        var assistantIds = conn.Column<long>(conn.From<ChatAssistant>()
            .Where(x => x.FilestoreId == id).Select(x => x.Id));
        var conversationIds = new List<long>();
        foreach (var assistantIdsBatch in assistantIds.Chunk(500))
        {
            conversationIds.AddRange(conn.Column<long>(conn.From<ChatAssistantConversation>()
                .Where(x => assistantIdsBatch.Contains(x.AssistantId)).Select(x => x.Id)));
        }

        foreach (var conversationIdsBatch in conversationIds.Chunk(500))
            conn.Delete<ChatAssistantMessage>(x => conversationIdsBatch.Contains(x.ConversationId));
        foreach (var assistantIdsBatch in assistantIds.Chunk(500))
            conn.Delete<ChatAssistantConversation>(x => assistantIdsBatch.Contains(x.AssistantId));
        conn.Delete<ChatAssistant>(x => x.FilestoreId == id);
        foreach (var sourceIdsBatch in sourceIds.Chunk(500))
        {
            conn.Delete<ChatSourceRun>(x => sourceIdsBatch.Contains(x.SourceId));
            conn.Delete<ChatDocument>(x => x.SourceId != null && sourceIdsBatch.Contains(x.SourceId.Value));
        }
        conn.Delete<ChatDocument>(x => x.FilestoreId == id);
        conn.Delete<ChatSource>(x => x.FilestoreId == id);
        conn.Delete<ChatFilestore>(x => x.Id == id);
        tx.Commit();
        return impact;
    }

    // ── Documents ──

    public ChatDocument? GetDocument(long id, string? user)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatDocument>().Where(x => x.Id == id);
        if (user != null)
            ChatDb.ApplyUserFilter(q, user);
        return conn.Single(q);
    }

    public ChatDocument? FindDocumentByHash(string hash, string? user)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatDocument>().Where(x => x.Hash == hash);
        if (user != null)
            ChatDb.ApplyUserFilter(q, user);
        q.Limit(1);
        return conn.Select(q).FirstOrDefault();
    }

    public List<ChatDocument> QueryDocuments(JsonObject query, string? user)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatDocument>();
        ChatDb.ApplyUserFilter(q, user);
        var sqlQuery = query.Clone();
        sqlQuery.Remove("versions");
        sqlQuery.Remove("tags");
        sqlQuery.Remove("categoryUnder");
        var uncategorized = sqlQuery.GetString("category") == "";
        if (uncategorized)
            sqlQuery.Remove("category");
        var nullColumns = sqlQuery.GetString("null")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? [];
        uncategorized |= nullColumns.RemoveAll(x => string.Equals(x, "category", StringComparison.OrdinalIgnoreCase)) > 0;
        if (uncategorized)
        {
            if (nullColumns.Count > 0) sqlQuery["null"] = string.Join(',', nullColumns);
            else sqlQuery.Remove("null");
        }
        ChatDb.ApplyFilters(q, sqlQuery, DocumentColumns);
        if (uncategorized)
            q.And(x => x.Category == null || x.Category == "");
        if (!query.GetBool("includeTombstoned"))
            q.And(x => x.TombstonedAt == null);

        if (query.GetString("ids_in") is { Length: > 0 } idsIn)
        {
            var ids = idsIn.Split(',')
                .Select(x => long.TryParse(x.Trim(), out var id) ? id : (long?)null)
                .Where(x => x != null).Select(x => x!.Value).ToList();
            if (ids.Count > 0)
                q.And(x => ids.Contains(x.Id));
        }
        if (query.GetString("displayNames") is { Length: > 0 } displayNames)
        {
            var names = displayNames.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
            if (names.Count > 0)
                q.And(x => names.Contains(x.DisplayName!));
        }
        if (query.GetString("q") is { Length: > 0 } search)
            q.And(x => x.DisplayName!.Contains(search));

        var sort = query.GetString("sort") ?? "-id";
        var uploadedAt = Col(q, nameof(ChatDocument.UploadedAt));
        var createdAt = Col(q, nameof(ChatDocument.CreatedAt));
        switch (sort)
        {
            // pending uploads first (oldest first), then everything else newest first
            case "uploading":
                q.UnsafeOrderBy($"CASE WHEN {uploadedAt} IS NULL AND {Col(q, nameof(ChatDocument.Error))} IS NULL "
                    + $"THEN 0 ELSE 1 END, {uploadedAt} DESC, {createdAt}");
                break;
            case "failed":
                q.UnsafeOrderBy($"CASE WHEN {Col(q, nameof(ChatDocument.Error))} IS NOT NULL THEN 0 ELSE 1 END, {createdAt} DESC");
                break;
            case "issues":
                var states = string.Join(",", IssueStates.Select(x => $"'{x}'"));
                q.UnsafeOrderBy($"CASE WHEN {Col(q, nameof(ChatDocument.State))} IN ({states}) THEN 0 ELSE 1 END, {uploadedAt} DESC");
                break;
            default:
                ChatDb.ApplySort(q, sort, DocumentColumns);
                break;
        }

        var postFilter = query.GetString("versions") != null || query.GetString("tags") != null
            || query.GetString("categoryUnder") != null;
        var skip = query.GetInt("skip") ?? 0;
        var take = Math.Min(query.GetInt("take") ?? 50, 1000);
        if (!postFilter)
        {
            q.Limit(skip, take);
            return conn.Select(q);
        }
        var rows = conn.Select(q).Where(x => MatchesListFilters(x, query)).Skip(skip).Take(take).ToList();
        return rows;
    }

    static bool MatchesListFilters(ChatDocument doc, JsonObject query)
    {
        if (query.GetString("versions") is { } version
            && !GeminiMetadata.AsList(doc.Versions).Contains(version, StringComparer.Ordinal))
            return false;
        if (query.GetString("tags") is { } tag
            && !GeminiMetadata.AsList(doc.Tags).Contains(tag, StringComparer.Ordinal))
            return false;
        if (query.GetString("categoryUnder") is { } category
            && !GeminiMetadata.AsList(doc.CategoryPath).Contains(category, StringComparer.Ordinal))
            return false;
        return true;
    }

    public long CountDocuments(JsonObject query, string? user)
    {
        var all = query.Clone();
        all["skip"] = 0;
        all["take"] = 1000;
        long count = 0;
        while (true)
        {
            var page = QueryDocuments(all, user);
            count += page.Count;
            if (page.Count < 1000) break;
            all["skip"] = all.GetInt("skip")!.Value + 1000;
        }
        return count;
    }

    /// <summary>Every document in a file store, paged 1000 at a time (port of query_documents_all)</summary>
    public IEnumerable<ChatDocument> QueryAllDocuments(long filestoreId, string? user)
    {
        const int pageSize = 1000;
        var skip = 0;
        while (true)
        {
            var page = QueryDocuments(new JsonObject
            {
                ["filestoreId"] = filestoreId,
                ["take"] = pageSize,
                ["skip"] = skip,
            }, user);

            foreach (var doc in page)
                yield return doc;

            if (page.Count < pageSize)
                yield break;
            skip += pageSize;
        }
    }

    public long InsertDocument(ChatDocument document)
    {
        document.SourceKey ??= document.DisplayName;
        GeminiMetadata.NormalizeDocument(document);
        using var conn = OpenDb();
        return conn.Insert(document, selectIdentity: true);
    }

    public void UpdateDocument(ChatDocument document)
    {
        document.UpdatedAt = DateTime.Now;
        GeminiMetadata.NormalizeDocument(document);
        using var conn = OpenDb();
        conn.Update(document);
    }

    public void UpdateDocumentState(long id, string state)
    {
        using var conn = OpenDb();
        conn.UpdateOnly(() => new ChatDocument { State = state, UpdatedAt = DateTime.Now },
            where: x => x.Id == id);
    }

    public void UpdateDocumentError(long id, string error)
    {
        using var conn = OpenDb();
        conn.UpdateOnly(() => new ChatDocument { Error = error, UpdatedAt = DateTime.Now },
            where: x => x.Id == id);
    }

    /// <summary>Requeue a document for the upload worker (port of the {error:None, uploadedAt:None} update)</summary>
    public void ResetDocumentUpload(long id)
    {
        using var conn = OpenDb();
        conn.UpdateOnly(() => new ChatDocument
        {
            Error = null,
            UploadedAt = null,
            StartedAt = null,
            UpdatedAt = DateTime.Now,
        }, where: x => x.Id == id);
    }

    public void DeleteDocument(long id, string? user)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatDocument>().Where(x => x.Id == id);
        if (user != null)
            ChatDb.ApplyUserFilter(q, user);
        conn.Delete(q);
    }

    public ChatDocument? FindDocumentBySourceKey(long filestoreId, long? sourceId, string sourceKey, string? user)
    {
        using var conn = OpenDb();
        var scope = sourceId ?? 0;
        var q = conn.From<ChatDocument>().Where(x => x.FilestoreId == filestoreId
            && x.SourceScopeId == scope && x.SourceKey == sourceKey);
        if (user != null) ChatDb.ApplyUserFilter(q, user);
        return conn.Single(q);
    }

    /// <summary>Documents queued for upload across all users (the worker runs outside a request)</summary>
    public List<ChatDocument> GetPendingDocuments(int limit = 10)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatDocument>()
            .Where(x => x.UploadedAt == null && x.Error == null && x.TombstonedAt == null)
            .OrderBy(x => x.Id)
            .Limit(limit);
        return conn.Select(q);
    }

    /// <summary>Documents recorded locally for a file store (port of get_filestore_stats)</summary>
    public AiChatFilestoreStats FilestoreStats(long filestoreId, string? user)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatDocument>();
        ChatDb.ApplyUserFilter(q, user);
        q.And(x => x.FilestoreId == filestoreId)
            .Select(x => new { Count = Sql.Count("*"), Size = Sql.Sum(x.Size) });
        return conn.SqlList<AiChatFilestoreStats>(q).FirstOrDefault() ?? new AiChatFilestoreStats();
    }

    public List<AiChatDocumentCategory> DocumentCategories(long filestoreId, string? user)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatDocument>();
        ChatDb.ApplyUserFilter(q, user);
        q.And(x => x.FilestoreId == filestoreId)
            .GroupBy(x => x.Category)
            .OrderBy(x => x.Category)
            .Select(x => new { x.Category, Count = Sql.Count("*"), Size = Sql.Sum(x.Size) });
        return conn.SqlList<AiChatDocumentCategory>(q);
    }

    // ── Sources and runs ──

    public ChatSource? GetSource(long id, string? user)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatSource>().Where(x => x.Id == id);
        if (user != null) ChatDb.ApplyUserFilter(q, user);
        return conn.Single(q);
    }

    public List<ChatSource> QuerySources(long filestoreId, string? user, bool savedOnly = true)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatSource>().Where(x => x.FilestoreId == filestoreId);
        if (user != null) ChatDb.ApplyUserFilter(q, user);
        if (savedOnly) q.And(x => x.LastRunId != null);
        q.OrderByDescending(x => x.UpdatedAt);
        return conn.Select(q);
    }

    public bool SavedSourceNameExists(long filestoreId, string? user, string name, long? exceptId = null)
    {
        return QuerySources(filestoreId, user).Any(x => x.Id != exceptId
            && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public long InsertSource(ChatSource source)
    {
        using var conn = OpenDb();
        return conn.Insert(source, selectIdentity: true);
    }

    public void UpdateSource(ChatSource source)
    {
        source.UpdatedAt = DateTime.Now;
        using var conn = OpenDb();
        conn.Update(source);
    }

    public void DeleteSource(long id, string? user, bool detachDocuments = true)
    {
        using var conn = OpenDb();
        var source = GetSource(id, user);
        if (source == null) return;
        if (detachDocuments)
            conn.UpdateOnly(() => new ChatDocument { SourceId = null, SourceScopeId = 0, UpdatedAt = DateTime.Now },
                x => x.SourceId == id);
        conn.Delete<ChatSourceRun>(x => x.SourceId == id);
        conn.Delete<ChatSource>(x => x.Id == id);
    }

    public long InsertSourceRun(ChatSourceRun run)
    {
        using var conn = OpenDb();
        return conn.Insert(run, selectIdentity: true);
    }

    public void UpdateSourceRun(ChatSourceRun run)
    {
        using var conn = OpenDb();
        conn.Update(run);
    }

    public List<ChatSourceRun> QuerySourceRuns(long sourceId, string? user, int take = 50)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatSourceRun>().Where(x => x.SourceId == sourceId);
        if (user != null) ChatDb.ApplyUserFilter(q, user);
        q.OrderByDescending(x => x.Id).Limit(Math.Min(take, 200));
        return conn.Select(q);
    }
}
