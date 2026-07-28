using System.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.AI;

/// <summary>
/// OrmLite data access for the gemini extension's file stores + documents (port of gemini/db.py's
/// GeminiDB), sharing the host's chat database instead of Python's per-user gemini.sqlite file.
/// Rows are partitioned by the same "user" column convention as the rest of the chat tables.
/// </summary>
public class GeminiDb(ChatDb db)
{
    public IDbConnection OpenDb() => db.OpenDb();

    public void InitSchema()
    {
        using var conn = OpenDb();
        conn.CreateTableIfNotExists<ChatFilestore>();
        conn.CreateTableIfNotExists<ChatDocument>();
        ChatDb.AddMissingColumns<ChatFilestore>(conn);
        ChatDb.AddMissingColumns<ChatDocument>(conn);
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

    /// <summary>Deletes the file store and all of its documents (port of delete_filestore)</summary>
    public void DeleteFilestore(long id, string? user)
    {
        using var conn = OpenDb();
        var docs = conn.From<ChatDocument>().Where(x => x.FilestoreId == id);
        var filestores = conn.From<ChatFilestore>().Where(x => x.Id == id);
        if (user != null)
        {
            ChatDb.ApplyUserFilter(docs, user);
            ChatDb.ApplyUserFilter(filestores, user);
        }
        conn.Delete(docs);
        conn.Delete(filestores);
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
        ChatDb.ApplyFilters(q, query, DocumentColumns);

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

        q.Limit(query.GetInt("skip") ?? 0, Math.Min(query.GetInt("take") ?? 50, 1000));
        return conn.Select(q);
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
        using var conn = OpenDb();
        return conn.Insert(document, selectIdentity: true);
    }

    public void UpdateDocument(ChatDocument document)
    {
        document.UpdatedAt = DateTime.Now;
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

    /// <summary>Documents queued for upload across all users (the worker runs outside a request)</summary>
    public List<ChatDocument> GetPendingDocuments(int limit = 10)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatDocument>()
            .Where(x => x.UploadedAt == null && x.Error == null)
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
}
