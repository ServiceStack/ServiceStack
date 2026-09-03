using System.Data;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ServiceStack.OrmLite;

namespace ServiceStack.AI;

public class ChatSearchResult : ChatSearchSection
{
    public double Score { get; set; }
    public string? Snippet { get; set; }
}

public class ChatSearchStats
{
    public long Documents { get; set; }
    public long Indexed { get; set; }
    public long Pending { get; set; }
    public long Failed { get; set; }
    public long Sections { get; set; }
    public string Provider { get; set; } = "like";
}

public partial class GeminiDb
{
    string searchProvider = "like";

    void InitSearchSchema(IDbConnection conn)
    {
        var dialect = conn.GetDialectProvider();
        var provider = dialect.GetType().Name;
        var table = dialect.GetQuotedTableName(typeof(ChatSearchSection));
        var model = typeof(ChatSearchSection).GetModelMetadata();
        string Col(string name) => dialect.GetQuotedColumnName(model.GetFieldDefinition(name));
        try
        {
            if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                conn.ExecuteSql("CREATE VIRTUAL TABLE IF NOT EXISTS ChatSearchSectionFts USING fts5(sectionId UNINDEXED, documentTitle, heading, content)");
                searchProvider = "sqlite-fts5+like";
            }
            else if (provider.Contains("Postgre", StringComparison.OrdinalIgnoreCase))
            {
                conn.ExecuteSql($"CREATE INDEX IF NOT EXISTS ix_chat_search_section_fts ON {table} USING GIN (to_tsvector('simple', coalesce({Col(nameof(ChatSearchSection.DocumentTitle))},'') || ' ' || coalesce({Col(nameof(ChatSearchSection.Heading))},'') || ' ' || coalesce({Col(nameof(ChatSearchSection.Content))},'')))");
                searchProvider = "postgresql-fts+like";
            }
            else if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            {
                var exists = conn.Scalar<long>("SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema=DATABASE() AND table_name=@table AND index_name='ix_chat_search_section_fts'", new { table = model.ModelName });
                if (exists == 0) conn.ExecuteSql($"ALTER TABLE {table} ADD FULLTEXT INDEX ix_chat_search_section_fts ({Col(nameof(ChatSearchSection.DocumentTitle))}, {Col(nameof(ChatSearchSection.Heading))}, {Col(nameof(ChatSearchSection.Content))})");
                searchProvider = "mysql-fulltext+like";
            }
            else if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                var rawTable = model.ModelName;
                conn.ExecuteSql($"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UX_ChatSearchSection_FtsId' AND object_id=OBJECT_ID('{rawTable}')) CREATE UNIQUE INDEX UX_ChatSearchSection_FtsId ON {table} ({Col(nameof(ChatSearchSection.Id))})");
                conn.ExecuteSql("IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name='ChatSearchCatalog') CREATE FULLTEXT CATALOG ChatSearchCatalog");
                conn.ExecuteSql($"IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id=OBJECT_ID('{rawTable}')) CREATE FULLTEXT INDEX ON {table} ({Col(nameof(ChatSearchSection.DocumentTitle))}, {Col(nameof(ChatSearchSection.Heading))}, {Col(nameof(ChatSearchSection.Content))}) KEY INDEX UX_ChatSearchSection_FtsId ON ChatSearchCatalog WITH CHANGE_TRACKING AUTO");
                searchProvider = "sqlserver-fulltext+like";
            }
        }
        catch
        {
            // Full-text extensions/permissions are optional. SearchSections transparently uses LIKE.
        }
    }

    public ChatSearchWidget? GetSearchWidget(long id, string? user)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatSearchWidget>().Where(x => x.Id == id);
        if (user != null) ChatDb.ApplyUserFilter(q, user);
        return conn.Single(q);
    }

    public ChatSearchWidget? GetPublicSearchWidget(string publicId)
    {
        using var conn = OpenDb();
        return conn.Single<ChatSearchWidget>(x => x.PublicId == publicId && x.Enabled && x.PublishedAt != null);
    }

    public List<ChatSearchWidget> QuerySearchWidgets(long filestoreId, string? user, bool includeArchived = false)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatSearchWidget>().Where(x => x.FilestoreId == filestoreId);
        ChatDb.ApplyUserFilter(q, user);
        if (!includeArchived) q.And(x => x.Enabled);
        return conn.Select(q.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.Id));
    }

    public bool SearchWidgetNameExists(long filestoreId, string name, string? user, long? excludeId = null)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatSearchWidget>().Where(x => x.FilestoreId == filestoreId && x.Name == name && x.Enabled);
        ChatDb.ApplyUserFilter(q, user);
        if (excludeId != null) q.And(x => x.Id != excludeId.Value);
        return conn.Exists(q);
    }

    public long InsertSearchWidget(ChatSearchWidget widget)
    {
        using var conn = OpenDb();
        return conn.Insert(widget, selectIdentity: true);
    }

    public void UpdateSearchWidget(ChatSearchWidget widget)
    {
        widget.UpdatedAt = DateTime.Now;
        using var conn = OpenDb(); conn.Update(widget);
    }

    public bool ArchiveSearchWidget(long id, string? user)
    {
        var widget = GetSearchWidget(id, user); if (widget == null) return false;
        widget.Enabled = false; widget.PublishedAt = null; UpdateSearchWidget(widget); return true;
    }

    public ChatSearchWidget? RestoreSearchWidget(long id, string? user)
    {
        var widget = GetSearchWidget(id, user); if (widget == null) return null;
        if (SearchWidgetNameExists(widget.FilestoreId, widget.Name ?? "", user, widget.Id))
            throw new InvalidOperationException($"An active Search widget named '{widget.Name}' already exists");
        widget.Enabled = true; widget.PublishedAt = null; UpdateSearchWidget(widget); return widget;
    }

    public bool DeleteSearchWidget(long id, string? user, string? confirmation)
    {
        var widget = GetSearchWidget(id, user); if (widget == null) return false;
        if (confirmation != widget.Name) throw new ArgumentException($"Type \"{widget.Name}\" to confirm permanent deletion");
        using var conn = OpenDb(); return conn.DeleteById<ChatSearchWidget>(widget.Id) > 0;
    }

    public List<ChatDocument> GetSearchCandidates(int limit = 100)
    {
        using var conn = OpenDb();
        var q = conn.From<ChatDocument>().Where(x => x.TombstonedAt == null
            && x.SearchHash != null && (x.SearchIndexedHash == null || x.SearchIndexedHash != x.SearchHash))
            .OrderBy(x => x.Id).Limit(limit);
        return conn.Select(q);
    }

    public void SetSearchDesired(ChatDocument doc, bool force = false)
    {
        doc.SearchHash = GeminiSearch.DesiredHash(doc);
        if (force) doc.SearchIndexedHash = null;
        doc.SearchError = null;
    }

    public int EnsureSearchDesiredHashes()
    {
        using var conn = OpenDb(); var rows = conn.Select(conn.From<ChatDocument>().Where(x => x.TombstonedAt == null));
        var changed = 0;
        foreach (var doc in rows)
        {
            var desired = GeminiSearch.DesiredHash(doc);
            if (doc.SearchHash == desired) continue;
            doc.SearchHash = desired; doc.SearchError = null; doc.UpdatedAt = DateTime.Now; conn.Update(doc); changed++;
        }
        return changed;
    }

    public void UpdateSearchError(long id, string error)
    {
        using var conn = OpenDb();
        conn.UpdateOnly(() => new ChatDocument { SearchError = error, SearchStartedAt = null, UpdatedAt = DateTime.Now }, x => x.Id == id);
    }

    public void MarkSearchStarted(long id)
    {
        using var conn = OpenDb();
        conn.UpdateOnly(() => new ChatDocument { SearchStartedAt = DateTime.Now, SearchError = null, UpdatedAt = DateTime.Now }, x => x.Id == id);
    }

    public void ReplaceSearchSections(ChatDocument doc, List<ChatSearchSection> sections, string desiredHash)
    {
        using var conn = OpenDb(); using var tx = conn.OpenTransaction();
        var sqlite = conn.GetDialectProvider().GetType().Name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);
        if (sqlite)
        {
            var oldIds = conn.Column<long>(conn.From<ChatSearchSection>().Where(x => x.DocumentId == doc.Id).Select(x => x.Id));
            foreach (var ids in oldIds.Chunk(200))
                conn.ExecuteSql($"DELETE FROM ChatSearchSectionFts WHERE sectionId IN ({string.Join(',', ids)})");
        }
        conn.Delete<ChatSearchSection>(x => x.DocumentId == doc.Id);
        foreach (var section in sections)
        {
            section.Id = conn.Insert(section, selectIdentity: true);
            if (sqlite) conn.ExecuteSql("INSERT INTO ChatSearchSectionFts(sectionId,documentTitle,heading,content) VALUES (@id,@title,@heading,@content)",
                new { id = section.Id, title = section.DocumentTitle, heading = section.Heading, content = section.Content });
        }
        conn.UpdateOnly(() => new ChatDocument
        {
            SearchHash = desiredHash, SearchIndexedHash = desiredHash, SearchIndexedAt = DateTime.Now,
            SearchStartedAt = null, SearchError = null, UpdatedAt = DateTime.Now,
        }, x => x.Id == doc.Id);
        tx.Commit();
    }

    public void RemoveSearchDocument(long documentId)
    {
        using var conn = OpenDb(); using var tx = conn.OpenTransaction();
        if (conn.GetDialectProvider().GetType().Name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var ids = conn.Column<long>(conn.From<ChatSearchSection>().Where(x => x.DocumentId == documentId).Select(x => x.Id));
            foreach (var batch in ids.Chunk(200)) conn.ExecuteSql($"DELETE FROM ChatSearchSectionFts WHERE sectionId IN ({string.Join(',', batch)})");
        }
        conn.Delete<ChatSearchSection>(x => x.DocumentId == documentId); tx.Commit();
    }

    internal void DeleteSearchSections(IDbConnection conn, IEnumerable<long> documentIds)
    {
        foreach (var documentBatch in documentIds.Distinct().Chunk(200))
        {
            if (conn.GetDialectProvider().GetType().Name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                var sectionIds = conn.Column<long>(conn.From<ChatSearchSection>()
                    .Where(x => documentBatch.Contains(x.DocumentId)).Select(x => x.Id));
                foreach (var sectionBatch in sectionIds.Chunk(200))
                    conn.ExecuteSql($"DELETE FROM ChatSearchSectionFts WHERE sectionId IN ({string.Join(',', sectionBatch)})");
            }
            conn.Delete<ChatSearchSection>(x => documentBatch.Contains(x.DocumentId));
        }
    }

    public ChatSearchStats SearchStats(long filestoreId, string? user)
    {
        using var conn = OpenDb();
        var docs = conn.From<ChatDocument>().Where(x => x.FilestoreId == filestoreId && x.TombstonedAt == null);
        ChatDb.ApplyUserFilter(docs, user); var rows = conn.Select(docs);
        var sections = conn.From<ChatSearchSection>().Where(x => x.FilestoreId == filestoreId); ChatDb.ApplyUserFilter(sections, user);
        return new ChatSearchStats
        {
            Documents = rows.Count, Indexed = rows.Count(x => x.SearchHash != null && x.SearchIndexedHash == x.SearchHash),
            Pending = rows.Count(x => x.SearchHash != null && x.SearchIndexedHash != x.SearchHash),
            Failed = rows.Count(x => !string.IsNullOrEmpty(x.SearchError)), Sections = conn.Count(sections), Provider = searchProvider,
        };
    }

    static bool ScopeMatch(ChatSearchSection row, JsonObject? scope)
    {
        if (scope == null) return true;
        foreach (var field in GeminiSearch.ScopeFields)
        {
            var wanted = scope.GetString(field); if (string.IsNullOrEmpty(wanted)) continue;
            var actual = field switch
            {
                "category" => row.Category, "docType" => row.DocType, "status" => row.Status,
                "locale" => row.Locale, "product" => row.Product, "versions" => row.Versions, "tags" => row.Tags, _ => null,
            };
            if (field is "versions" or "tags")
            {
                if (!GeminiMetadata.AsList(actual).Contains(wanted, StringComparer.Ordinal)) return false;
            }
            else if (!string.Equals(actual, wanted, StringComparison.Ordinal)) return false;
        }
        return true;
    }

    public List<ChatSearchResult> SearchSections(long filestoreId, string query, string? user, JsonObject? scope = null, int take = 30)
    {
        query = query.Trim(); if (query.Length == 0) return [];
        take = Math.Clamp(take, 1, 100);
        var tokens = Regex.Matches(query, @"[\p{L}\p{N}_]+\*?").Select(x => x.Value.TrimEnd('*')).Where(x => x.Length > 0).Take(10).ToList();
        if (tokens.Count == 0) return [];
        using var conn = OpenDb();
        List<ChatSearchResult> rows = [];
        try { rows = NativeSearch(conn, filestoreId, query, tokens, user, Math.Min(1000, Math.Max(take * 10, 100))); }
        catch { rows = []; }
        if (rows.Count == 0)
        {
            var q = conn.From<ChatSearchSection>().Where(x => x.FilestoreId == filestoreId);
            ChatDb.ApplyUserFilter(q, user);
            foreach (var token in tokens) q.And(x => x.DocumentTitle!.Contains(token) || x.Heading!.Contains(token) || x.Content!.Contains(token));
            rows = conn.Select(q.Limit(1000)).Select(x => new ChatSearchResult
            {
                Id=x.Id, DocumentId=x.DocumentId, FilestoreId=x.FilestoreId, User=x.User, Ordinal=x.Ordinal,
                DocumentTitle=x.DocumentTitle, Heading=x.Heading, HeadingLevel=x.HeadingLevel, Hierarchy=x.Hierarchy,
                Anchor=x.Anchor, Url=x.Url, Kind=x.Kind, Content=x.Content, Category=x.Category, DocType=x.DocType,
                Status=x.Status, Locale=x.Locale, Product=x.Product, Versions=x.Versions, Tags=x.Tags,
                Snippet=x.Content, Score=0,
            }).ToList();
        }
        return rows.Where(x => ScopeMatch(x, scope)).Take(take).ToList();
    }

    static List<ChatSearchResult> NativeSearch(IDbConnection conn, long storeId, string query, List<string> tokens, string? user, int take)
    {
        var dialect = conn.GetDialectProvider(); var provider = dialect.GetType().Name;
        var table = dialect.GetQuotedTableName(typeof(ChatSearchSection));
        var model = typeof(ChatSearchSection).GetModelMetadata();
        string Col(string name, string alias="s") => alias + "." + dialect.GetQuotedColumnName(model.GetFieldDefinition(name));
        var userClause = ChatDb.IsAllUsers(user) ? "" : $" AND {Col(nameof(ChatSearchSection.User))}=@user";
        var args = new { storeId, user = user ?? ChatDb.DefaultUser, query, take };
        string sql;
        if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var match = string.Join(" AND ", tokens.Select(x => $"\"{x.Replace("\"", "\"\"")}\""));
            args = new { storeId, user = user ?? ChatDb.DefaultUser, query = match, take };
            sql = $"SELECT s.*, bm25(ChatSearchSectionFts) AS Score, s.{dialect.GetQuotedColumnName(model.GetFieldDefinition(nameof(ChatSearchSection.Content)))} AS Snippet FROM ChatSearchSectionFts f JOIN {table} s ON {Col(nameof(ChatSearchSection.Id))}=f.sectionId WHERE ChatSearchSectionFts MATCH @query AND {Col(nameof(ChatSearchSection.FilestoreId))}=@storeId{userClause} ORDER BY Score LIMIT @take";
        }
        else if (provider.Contains("Postgre", StringComparison.OrdinalIgnoreCase))
        {
            var vector = $"to_tsvector('simple',coalesce({Col(nameof(ChatSearchSection.DocumentTitle))},'')||' '||coalesce({Col(nameof(ChatSearchSection.Heading))},'')||' '||coalesce({Col(nameof(ChatSearchSection.Content))},''))";
            sql = $"SELECT s.*, ts_rank_cd({vector},websearch_to_tsquery('simple',@query)) AS \"Score\", {Col(nameof(ChatSearchSection.Content))} AS \"Snippet\" FROM {table} s WHERE {Col(nameof(ChatSearchSection.FilestoreId))}=@storeId{userClause} AND {vector} @@ websearch_to_tsquery('simple',@query) ORDER BY \"Score\" DESC LIMIT @take";
        }
        else if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
        {
            var cols = string.Join(',', new[] { nameof(ChatSearchSection.DocumentTitle), nameof(ChatSearchSection.Heading), nameof(ChatSearchSection.Content) }.Select(x => Col(x)));
            args = new { storeId, user = user ?? ChatDb.DefaultUser, query = string.Join(' ', tokens.Select(x => "+" + x + "*")), take };
            sql = $"SELECT s.*, MATCH({cols}) AGAINST(@query IN BOOLEAN MODE) AS Score, {Col(nameof(ChatSearchSection.Content))} AS Snippet FROM {table} s WHERE {Col(nameof(ChatSearchSection.FilestoreId))}=@storeId{userClause} AND MATCH({cols}) AGAINST(@query IN BOOLEAN MODE) ORDER BY Score DESC LIMIT @take";
        }
        else if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            args = new { storeId, user = user ?? ChatDb.DefaultUser, query = string.Join(" AND ", tokens.Select(x => $"\"{x}*\"")), take };
            sql = $"SELECT TOP (@take) s.*, ft.RANK AS Score, {Col(nameof(ChatSearchSection.Content))} AS Snippet FROM {table} s JOIN CONTAINSTABLE({table}, *, @query) ft ON ft.[KEY]={Col(nameof(ChatSearchSection.Id))} WHERE {Col(nameof(ChatSearchSection.FilestoreId))}=@storeId{userClause} ORDER BY ft.RANK DESC";
        }
        else return [];
        return conn.SqlList<ChatSearchResult>(sql, args);
    }
}
