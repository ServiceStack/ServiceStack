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

class ChatSearchQueryAggregate
{
    public string? GroupKey { get; set; }
    public string? NormalizedQuery { get; set; }
    public string? Query { get; set; }
    public long Frequency { get; set; }
    public long NoResultCount { get; set; }
    public long ResultTotal { get; set; }
    public DateTime LastSearchedAt { get; set; }
}

class ChatSearchClickSummary
{
    public long TotalClicks { get; set; }
    public long ClickedSearches { get; set; }
}

class ChatSearchGroupClickAggregate
{
    public string? GroupKey { get; set; }
    public long ClickCount { get; set; }
    public long ClickedSearches { get; set; }
}

class ChatSearchDocumentClickAggregate
{
    public long DocumentId { get; set; }
    public string? Title { get; set; }
    public string? SourceUrl { get; set; }
    public long ClickCount { get; set; }
    public long UniqueSearches { get; set; }
    public double AveragePosition { get; set; }
    public DateTime LastClickedAt { get; set; }
}

public partial class GeminiDb
{
    GeminiSearchDbProvider searchProvider = null!;

    void InitSearchSchema(IDbConnection conn)
    {
        searchProvider = GeminiSearchDbProvider.Detect(conn);
        try
        {
            searchProvider.Initialize(conn);
        }
        catch
        {
            searchProvider.DisableNative();
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

    public Dictionary<long, long> SearchQueryCounts(IEnumerable<long> searchWidgetIds)
    {
        var ids = searchWidgetIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        using var conn = OpenDb();
        var q = conn.From<ChatSearchQuery>().Where(x => ids.Contains(x.SearchWidgetId))
            .GroupBy(x => x.SearchWidgetId)
            .Select(x => new { x.SearchWidgetId, Count = Sql.Count("*") });
        return conn.Dictionary<long, long>(q);
    }

    public long SearchQueryCount(long searchWidgetId)
    {
        using var conn = OpenDb();
        return conn.Count<ChatSearchQuery>(x => x.SearchWidgetId == searchWidgetId);
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
        using var conn = OpenDb(); using var tx = conn.OpenTransaction();
        conn.Delete<ChatSearchClick>(x => x.SearchWidgetId == widget.Id);
        conn.Delete<ChatSearchQuery>(x => x.SearchWidgetId == widget.Id);
        var deleted = conn.DeleteById<ChatSearchWidget>(widget.Id) > 0;
        tx.Commit();
        return deleted;
    }

    public long RecordSearchQuery(long searchWidgetId, string query, string? origin, string? pageUrl,
        string? userAgent, int resultCount, int documentCount, int durationMs)
    {
        query = query.Trim().SafeSubstring(0, 200);
        var normalized = GeminiSearch.NormalizeSearchQuery(query);
        using var conn = OpenDb();
        return conn.Insert(new ChatSearchQuery
        {
            SearchWidgetId = searchWidgetId, CreatedAt = DateTime.Now, Query = query,
            NormalizedQuery = normalized, GroupKey = GeminiSearch.SearchQueryGroupKey(normalized),
            Origin = origin?.SafeSubstring(0, 500), PageUrl = pageUrl?.SafeSubstring(0, 2000),
            UserAgent = userAgent?.SafeSubstring(0, 1000), ResultCount = Math.Max(resultCount, 0),
            DocumentCount = Math.Max(documentCount, 0), DurationMs = Math.Max(durationMs, 0),
        }, selectIdentity: true);
    }

    public long RecordSearchClick(long searchWidgetId, long filestoreId, string? user,
        long searchQueryId, long documentId, long? sectionId, int position,
        string? documentTitle = null, string? sourceUrl = null, string? resultType = null)
    {
        using var conn = OpenDb();
        if (!conn.Exists<ChatSearchQuery>(x => x.Id == searchQueryId && x.SearchWidgetId == searchWidgetId))
            return 0;
        var documentQuery = conn.From<ChatDocument>()
            .Where(x => x.Id == documentId && x.FilestoreId == filestoreId);
        if (user != null) ChatDb.ApplyUserFilter(documentQuery, user);
        var document = conn.Single(documentQuery);
        if (document == null) return 0;
        ChatSearchSection? section = null;
        if (sectionId is > 0)
            section = conn.Single<ChatSearchSection>(x => x.Id == sectionId.Value
                && x.DocumentId == documentId && x.FilestoreId == filestoreId);
        static string? Limit(string? value, int max)
        {
            value = value?.Trim();
            return string.IsNullOrEmpty(value) ? null : value.Length <= max ? value : value[..max];
        }
        var url = Limit(section?.Url ?? document.SourceUrl ?? sourceUrl, 2000);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) url = null;
        return conn.Insert(new ChatSearchClick
        {
            SearchQueryId = searchQueryId, SearchWidgetId = searchWidgetId,
            DocumentId = documentId, SectionId = sectionId, CreatedAt = DateTime.Now,
            Position = Math.Clamp(position, 1, 1000),
            DocumentTitle = Limit(section?.DocumentTitle ?? documentTitle
                ?? document.DisplayName ?? document.SourceKey ?? "Document", 500),
            SourceUrl = url, ResultType = Limit(section?.Kind ?? resultType ?? "content", 50),
        }, selectIdentity: true);
    }

    public JsonObject? SearchAnalytics(long searchWidgetId, string? user, int groupTake = 50, int recentTake = 100)
    {
        if (GetSearchWidget(searchWidgetId, user) == null) return null;
        groupTake = Math.Clamp(groupTake, 1, 200); recentTake = Math.Clamp(recentTake, 1, 500);
        using var conn = OpenDb();
        var dialect = conn.GetDialectProvider();
        var model = typeof(ChatSearchQuery).GetModelMetadata();
        var table = dialect.GetQuotedTableName(typeof(ChatSearchQuery));
        string Col(string name) => dialect.GetQuotedColumnName(model.GetFieldDefinition(name));
        var clickModel = typeof(ChatSearchClick).GetModelMetadata();
        var clickTable = dialect.GetQuotedTableName(typeof(ChatSearchClick));
        string ClickCol(string name) => dialect.GetQuotedColumnName(clickModel.GetFieldDefinition(name));
        var sql = $"SELECT {Col(nameof(ChatSearchQuery.GroupKey))} AS GroupKey," +
                  $"{Col(nameof(ChatSearchQuery.NormalizedQuery))} AS NormalizedQuery," +
                  $"MIN({Col(nameof(ChatSearchQuery.Query))}) AS Query,COUNT(*) AS Frequency," +
                  $"SUM(CASE WHEN {Col(nameof(ChatSearchQuery.ResultCount))}=0 THEN 1 ELSE 0 END) AS NoResultCount," +
                  $"COALESCE(SUM({Col(nameof(ChatSearchQuery.ResultCount))}),0) AS ResultTotal," +
                  $"MAX({Col(nameof(ChatSearchQuery.CreatedAt))}) AS LastSearchedAt FROM {table} " +
                  $"WHERE {Col(nameof(ChatSearchQuery.SearchWidgetId))}=@id GROUP BY " +
                  $"{Col(nameof(ChatSearchQuery.GroupKey))},{Col(nameof(ChatSearchQuery.NormalizedQuery))} " +
                  "ORDER BY Frequency DESC,LastSearchedAt DESC";
        var variants = conn.SqlList<ChatSearchQueryAggregate>(sql, new { id = searchWidgetId });
        var groupClickSql = $"SELECT q.{Col(nameof(ChatSearchQuery.GroupKey))} AS GroupKey," +
            $"COUNT(c.{ClickCol(nameof(ChatSearchClick.Id))}) AS ClickCount," +
            $"COUNT(DISTINCT c.{ClickCol(nameof(ChatSearchClick.SearchQueryId))}) AS ClickedSearches " +
            $"FROM {clickTable} c INNER JOIN {table} q ON q.{Col(nameof(ChatSearchQuery.Id))}=" +
            $"c.{ClickCol(nameof(ChatSearchClick.SearchQueryId))} WHERE " +
            $"c.{ClickCol(nameof(ChatSearchClick.SearchWidgetId))}=@id GROUP BY " +
            $"q.{Col(nameof(ChatSearchQuery.GroupKey))}";
        var groupClicks = conn.SqlList<ChatSearchGroupClickAggregate>(groupClickSql, new { id = searchWidgetId })
            .ToDictionary(x => x.GroupKey ?? "", x => x);
        var related = variants.GroupBy(x => x.GroupKey ?? x.NormalizedQuery ?? x.Query ?? "")
            .Select(group =>
            {
                var ordered = group.OrderByDescending(x => x.Frequency).ThenBy(x => x.Query).ToList();
                var count = ordered.Sum(x => x.Frequency);
                groupClicks.TryGetValue(group.Key, out var clicks);
                return new JsonObject
                {
                    ["key"] = group.Key, ["query"] = ordered.FirstOrDefault()?.Query,
                    ["count"] = count, ["noResultCount"] = ordered.Sum(x => x.NoResultCount),
                    ["averageResults"] = count == 0 ? 0 : Math.Round(ordered.Sum(x => x.ResultTotal) / (double)count, 1),
                    ["lastSearchedAt"] = ChatDb.ToDateNode(ordered.Max(x => x.LastSearchedAt)),
                    ["clickCount"] = clicks?.ClickCount ?? 0,
                    ["clickedSearches"] = clicks?.ClickedSearches ?? 0,
                    ["clickThroughRate"] = count == 0 ? 0 : Math.Round((clicks?.ClickedSearches ?? 0) * 100d / count, 1),
                    ["variants"] = new JsonArray(ordered.Select(x => (JsonNode)new JsonObject
                    {
                        ["query"] = x.Query, ["count"] = x.Frequency,
                    }).ToArray()),
                };
            }).OrderByDescending(x => x.GetLong("count")).ThenByDescending(x => x.GetString("lastSearchedAt"))
            .Take(groupTake).ToArray();
        var recent = conn.Select(conn.From<ChatSearchQuery>()
            .Where(x => x.SearchWidgetId == searchWidgetId)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Limit(recentTake));
        var total = variants.Sum(x => x.Frequency);
        var resultTotal = variants.Sum(x => x.ResultTotal);
        var clickSummarySql = $"SELECT COUNT(*) AS TotalClicks," +
            $"COUNT(DISTINCT {ClickCol(nameof(ChatSearchClick.SearchQueryId))}) AS ClickedSearches " +
            $"FROM {clickTable} WHERE {ClickCol(nameof(ChatSearchClick.SearchWidgetId))}=@id";
        var clickSummary = conn.SqlList<ChatSearchClickSummary>(clickSummarySql, new { id = searchWidgetId })
            .FirstOrDefault() ?? new ChatSearchClickSummary();
        var popularSql = $"SELECT {ClickCol(nameof(ChatSearchClick.DocumentId))} AS DocumentId," +
            $"MAX({ClickCol(nameof(ChatSearchClick.DocumentTitle))}) AS Title," +
            $"MAX({ClickCol(nameof(ChatSearchClick.SourceUrl))}) AS SourceUrl," +
            $"COUNT(*) AS ClickCount," +
            $"COUNT(DISTINCT {ClickCol(nameof(ChatSearchClick.SearchQueryId))}) AS UniqueSearches," +
            $"AVG(1.0 * {ClickCol(nameof(ChatSearchClick.Position))}) AS AveragePosition," +
            $"MAX({ClickCol(nameof(ChatSearchClick.CreatedAt))}) AS LastClickedAt " +
            $"FROM {clickTable} WHERE {ClickCol(nameof(ChatSearchClick.SearchWidgetId))}=@id " +
            $"GROUP BY {ClickCol(nameof(ChatSearchClick.DocumentId))} ORDER BY ClickCount DESC,LastClickedAt DESC";
        var popularDocuments = conn.SqlList<ChatSearchDocumentClickAggregate>(popularSql, new { id = searchWidgetId })
            .Take(50).ToArray();
        return new JsonObject
        {
            ["total"] = total, ["uniqueQueries"] = variants.Count, ["relatedGroups"] = variants.Select(x => x.GroupKey).Distinct().Count(),
            ["noResults"] = variants.Sum(x => x.NoResultCount),
            ["averageResults"] = total == 0 ? 0 : Math.Round(resultTotal / (double)total, 1),
            ["totalClicks"] = clickSummary.TotalClicks,
            ["clickedSearches"] = clickSummary.ClickedSearches,
            ["clickThroughRate"] = total == 0 ? 0 : Math.Round(clickSummary.ClickedSearches * 100d / total, 1),
            ["popularDocuments"] = new JsonArray(popularDocuments.Select(x => (JsonNode)new JsonObject
            {
                ["documentId"] = x.DocumentId, ["title"] = x.Title, ["sourceUrl"] = x.SourceUrl,
                ["clickCount"] = x.ClickCount, ["uniqueSearches"] = x.UniqueSearches,
                ["averagePosition"] = Math.Round(x.AveragePosition, 1),
                ["lastClickedAt"] = ChatDb.ToDateNode(x.LastClickedAt),
            }).ToArray()),
            ["groups"] = new JsonArray(related),
            ["recent"] = new JsonArray(recent.Select(x => (JsonNode)new JsonObject
            {
                ["id"] = x.Id, ["query"] = x.Query, ["createdAt"] = ChatDb.ToDateNode(x.CreatedAt),
                ["resultCount"] = x.ResultCount, ["documentCount"] = x.DocumentCount,
                ["durationMs"] = x.DurationMs, ["origin"] = x.Origin, ["pageUrl"] = x.PageUrl,
            }).ToArray()),
        };
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
        var sqlite = searchProvider.UsesManualFullTextRows;
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
        if (searchProvider.UsesManualFullTextRows)
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
            if (searchProvider.UsesManualFullTextRows)
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
            Failed = rows.Count(x => !string.IsNullOrEmpty(x.SearchError)), Sections = conn.Count(sections), Provider = searchProvider.StatusName,
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

    public List<ChatSearchResult> SearchSections(long filestoreId, string query, string? user, JsonObject? scope = null,
        int take = 30, JsonObject? ranking = null, int skip = 0)
    {
        query = query.Trim().SafeSubstring(0, 200); if (query.Length == 0) return [];
        take = Math.Clamp(take, 1, 101);
        skip = Math.Clamp(skip, 0, 1000);
        var tokens = Regex.Matches(query, @"[\p{L}\p{N}_]+\*?").Select(x => x.Value.TrimEnd('*')).Where(x => x.Length > 0).Take(10).ToList();
        if (tokens.Count == 0) return [];
        using var conn = OpenDb();
        List<ChatSearchResult> rows = [];
        try { rows = NativeSearch(conn, filestoreId, query, tokens, user, scope, 1000); }
        catch
        {
            searchProvider.DisableNative();
            rows = [];
        }
        if (rows.Count == 0)
        {
            var q = conn.From<ChatSearchSection>().Where(x => x.FilestoreId == filestoreId);
            ChatDb.ApplyUserFilter(q, user);
            GeminiSearchDbProvider.ApplyFallbackScope(q, scope);
            foreach (var token in tokens) q.And(x => x.DocumentTitle!.Contains(token) || x.Heading!.Contains(token) || x.Content!.Contains(token));
            q.OrderBy(x => x.Id);
            rows = conn.Select(q.Limit(1000)).Select(x => new ChatSearchResult
            {
                Id=x.Id, DocumentId=x.DocumentId, FilestoreId=x.FilestoreId, User=x.User, Ordinal=x.Ordinal,
                DocumentTitle=x.DocumentTitle, Heading=x.Heading, HeadingLevel=x.HeadingLevel, Hierarchy=x.Hierarchy,
                Anchor=x.Anchor, Url=x.Url, Kind=x.Kind, Content=x.Content, Category=x.Category, DocType=x.DocType,
                Status=x.Status, Locale=x.Locale, Product=x.Product, Versions=x.Versions, Tags=x.Tags,
                Snippet=x.Content, Score=0,
            }).ToList();
        }
        rows = rows.Where(x => ScopeMatch(x, scope)).ToList();
        if (rows.Count == 0) return [];
        var documentIds = rows.Select(x => x.DocumentId).Distinct().ToArray();
        var documents = conn.Select(conn.From<ChatDocument>().Where(x => documentIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);
        return GeminiSearch.RankResults(rows, query, documents, ranking).Skip(skip).Take(take).ToList();
    }

    List<ChatSearchResult> NativeSearch(IDbConnection conn, long storeId, string query, List<string> tokens,
        string? user, JsonObject? scope, int take)
    {
        var native = searchProvider.BuildNativeQuery(conn, storeId, query, tokens, user, scope, take);
        return native == null ? [] : conn.SqlList<ChatSearchResult>(native.Sql, native.Args);
    }
}
