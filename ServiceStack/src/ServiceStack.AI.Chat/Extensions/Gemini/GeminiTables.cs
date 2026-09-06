using ServiceStack.DataAnnotations;

namespace ServiceStack.AI;

/// <summary>
/// A Gemini File Search Store (port of llms-py gemini/db.py's "filestore" table).
/// Mirrors the fields of the remote https://ai.google.dev/api/file-search resource so the local
/// record can be compared against (and re-synced with) what Gemini reports.
/// </summary>
[UniqueConstraint(nameof(User), nameof(DisplayName))]
public class ChatFilestore
{
    [AutoIncrement]
    public long Id { get; set; }

    /// <summary>Data partition key: the authenticated username (null/"default" when auth is disabled)</summary>
    [Alias("user"), Index]
    public string? User { get; set; }

    [Index]
    public DateTime CreatedAt { get; set; }
    [Index]
    public DateTime UpdatedAt { get; set; }

    /// <summary>Gemini resource name, e.g. "fileSearchStores/my-docs-3w65kkumaxcd"</summary>
    public string? Name { get; set; }
    public string? DisplayName { get; set; }

    /// <summary>Gemini's create/update timestamps, stored as sortable strings like the rest of the wire DTOs</summary>
    public string? CreateTime { get; set; }
    public string? UpdateTime { get; set; }

    public long? ActiveDocumentsCount { get; set; }
    public long? PendingDocumentsCount { get; set; }
    public long? FailedDocumentsCount { get; set; }
    public long? SizeBytes { get; set; }

    public string? Metadata { get; set; }          // JSON
    [StringLength(StringLengthAttribute.MaxText)]
    public string? Error { get; set; }
    public string? Ref { get; set; }
    /// <summary>public | internal; access is enforced at store scope, never by metadata filters.</summary>
    public string? Visibility { get; set; }
    /// <summary>JSON list of metadata fields displayed as facets in Explorer.</summary>
    public string? Facets { get; set; }
}

/// <summary>
/// A document uploaded to a file search store (port of gemini/db.py's "document" table).
/// The file itself lives in the content-addressed cache; this row tracks both the local copy
/// (filename/url/hash/size) and the remote Gemini document (name/state/customMetadata).
/// </summary>
[UniqueConstraint(nameof(FilestoreId), nameof(SourceScopeId), nameof(SourceKey))]
public class ChatDocument
{
    [AutoIncrement]
    public long Id { get; set; }

    [Index]
    public long FilestoreId { get; set; }

    [Alias("user"), Index]
    public string? User { get; set; }

    [Index]
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>SHA256-based cache filename, e.g. "2388...72ef.pdf"</summary>
    public string? Filename { get; set; }
    /// <summary>Cache url, e.g. "/~cache/23/2388...72ef.pdf"</summary>
    public string? Url { get; set; }
    [Index]
    public string? Hash { get; set; }
    public long? Size { get; set; }

    /// <summary>Original filename (also the Gemini document's displayName)</summary>
    public string? DisplayName { get; set; }
    /// <summary>Gemini resource name, e.g. "fileSearchStores/my-docs-xxx/documents/yyy"</summary>
    public string? Name { get; set; }
    /// <summary>Gemini CustomMetadata[] as stored by the sync: [{"key":"id","numeric_value":1},...]</summary>
    public string? CustomMetadata { get; set; }    // JSON

    public string? CreateTime { get; set; }
    public string? UpdateTime { get; set; }
    public long? SizeBytes { get; set; }
    public string? MimeType { get; set; }

    /// <summary>
    /// STATE_UNSPECIFIED | STATE_PENDING | STATE_ACTIVE | STATE_FAILED as reported by Gemini, plus the
    /// local-only sync verdicts MISSING_FROM_REMOTE, MISSING_METADATA, METADATA_MISMATCH, DUPLICATE_FILE.
    /// </summary>
    [Index]
    public string? State { get; set; }

    /// <summary>User-defined folder, surfaced in the UI and as a "category=" metadata_filter</summary>
    [Index]
    public string? Category { get; set; }
    public string? SourceUrl { get; set; }

    [Index]
    public long? SourceId { get; set; }
    /// <summary>Non-null source scope used by the portable unique constraint (SourceId ?? 0).</summary>
    [Default(0)]
    public long SourceScopeId { get; set; }
    [Index]
    public string? SourceKey { get; set; }
    public string? SourceEtag { get; set; }
    public string? ContentHash { get; set; }
    public string? MetadataHash { get; set; }
    public string? ExtractorVer { get; set; }
    public DateTime? TombstonedAt { get; set; }

    public string? CategoryPath { get; set; }       // JSON string[]
    [Index]
    public string? DocType { get; set; }
    [Index]
    public string? Status { get; set; }
    [Index]
    public string? Locale { get; set; }
    [Index]
    public string? Product { get; set; }
    public string? Versions { get; set; }           // JSON string[]
    public long? SourceUpdatedAt { get; set; }      // epoch seconds
    public string? Tags { get; set; }               // JSON string[]

    public DateTime? StartedAt { get; set; }
    public DateTime? UploadedAt { get; set; }

    /// <summary>Desired and completed signatures for the independent local Search index.</summary>
    [Index]
    public string? SearchHash { get; set; }
    public string? SearchIndexedHash { get; set; }
    public DateTime? SearchStartedAt { get; set; }
    public DateTime? SearchIndexedAt { get; set; }
    [StringLength(StringLengthAttribute.MaxText)]
    public string? SearchError { get; set; }

    public string? Metadata { get; set; }          // JSON
    [StringLength(StringLengthAttribute.MaxText)]
    public string? Error { get; set; }
    public string? Ref { get; set; }
}

/// <summary>A repeatable import definition such as a folder or ZIP source.</summary>
public class ChatSource
{
    [AutoIncrement] public long Id { get; set; }
    [Index] public long FilestoreId { get; set; }
    [Alias("user"), Index] public string? User { get; set; }
    [Index] public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public bool Enabled { get; set; } = true;
    public string? Config { get; set; }
    public string? Category { get; set; }
    public string? Rules { get; set; }
    public string? Include { get; set; }
    public string? Exclude { get; set; }
    public string? Extract { get; set; }
    public string? Chunking { get; set; }
    public string? Volatile { get; set; }
    public string? ExtractorVer { get; set; }
    public string? Schedule { get; set; }
    public string? OnDelete { get; set; }
    public string? Cursor { get; set; }
    public long? LastRunId { get; set; }
    public DateTime? LastRunAt { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Error { get; set; }
}

/// <summary>A dry-run preview or applied execution of a saved import.</summary>
public class ChatSourceRun
{
    [AutoIncrement] public long Id { get; set; }
    [Index] public long SourceId { get; set; }
    [Alias("user"), Index] public string? User { get; set; }
    [Index] public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Status { get; set; }
    public bool DryRun { get; set; }
    public int Discovered { get; set; }
    public int Added { get; set; }
    public int Changed { get; set; }
    public int MetadataOnly { get; set; }
    public int Unchanged { get; set; }
    public int Removed { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public long Bytes { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Plan { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Log { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Error { get; set; }
}

/// <summary>A configured website Assistant backed by one Gemini File Search Store.</summary>
public class ChatAssistant
{
    [AutoIncrement] public long Id { get; set; }
    [Index] public long FilestoreId { get; set; }
    [Alias("user"), Index] public string? User { get; set; }
    [Index] public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Name { get; set; }
    [Index(Unique = true)] public string? PublicId { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime? PublishedAt { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Config { get; set; }
}

/// <summary>A retained visitor conversation with a published Assistant.</summary>
[UniqueConstraint(nameof(AssistantId), nameof(SessionId))]
public class ChatAssistantConversation
{
    [AutoIncrement] public long Id { get; set; }
    [Index] public long AssistantId { get; set; }
    [Alias("user"), Index] public string? User { get; set; }
    [Index] public DateTime CreatedAt { get; set; }
    [Index] public DateTime UpdatedAt { get; set; }
    public string? SessionId { get; set; }
    public string? Origin { get; set; }
    public string? PageUrl { get; set; }
    public string? UserAgent { get; set; }
    public string? Title { get; set; }
    public string? Status { get; set; }
    public int MessageCount { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? LastMessage { get; set; }
}

/// <summary>A retained user or Assistant message, including resolved public citations.</summary>
public class ChatAssistantMessage
{
    [AutoIncrement] public long Id { get; set; }
    [Index] public long ConversationId { get; set; }
    [Index] public DateTime CreatedAt { get; set; }
    public string? Role { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Content { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Citations { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Error { get; set; }
}

/// <summary>A separately published, model-free Search widget backed by one File Store.</summary>
public class ChatSearchWidget
{
    [AutoIncrement] public long Id { get; set; }
    [Index] public long FilestoreId { get; set; }
    [Alias("user"), Index] public string? User { get; set; }
    [Index] public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Name { get; set; }
    [Index(Unique = true)] public string? PublicId { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTime? PublishedAt { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Config { get; set; }
}

/// <summary>A retained public Search request used to understand demand and missing coverage.</summary>
[CompositeIndex(nameof(SearchWidgetId), nameof(CreatedAt))]
[CompositeIndex(nameof(SearchWidgetId), nameof(GroupKey))]
public class ChatSearchQuery
{
    [AutoIncrement] public long Id { get; set; }
    public long SearchWidgetId { get; set; }
    public DateTime CreatedAt { get; set; }
    [StringLength(200)] public string? Query { get; set; }
    [StringLength(300)] public string? NormalizedQuery { get; set; }
    [StringLength(500)] public string? GroupKey { get; set; }
    [StringLength(500)] public string? Origin { get; set; }
    [StringLength(2000)] public string? PageUrl { get; set; }
    [StringLength(1000)] public string? UserAgent { get; set; }
    public int ResultCount { get; set; }
    public int DocumentCount { get; set; }
    public int DurationMs { get; set; }
}

/// <summary>A selected public Search result used to measure document popularity and search CTR.</summary>
[CompositeIndex(nameof(SearchWidgetId), nameof(CreatedAt))]
[CompositeIndex(nameof(SearchWidgetId), nameof(DocumentId))]
public class ChatSearchClick
{
    [AutoIncrement] public long Id { get; set; }
    [Index] public long SearchQueryId { get; set; }
    public long SearchWidgetId { get; set; }
    public long DocumentId { get; set; }
    public long? SectionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Position { get; set; }
    [StringLength(500)] public string? DocumentTitle { get; set; }
    [StringLength(2000)] public string? SourceUrl { get; set; }
    [StringLength(50)] public string? ResultType { get; set; }
}

/// <summary>An anonymous page view captured by an analytics-enabled public Search widget.</summary>
[CompositeIndex(nameof(SearchWidgetId), nameof(CreatedAt))]
[CompositeIndex(nameof(SearchWidgetId), nameof(DayKey))]
[CompositeIndex(nameof(SearchWidgetId), nameof(ClientId))]
[CompositeIndex(nameof(SearchWidgetId), nameof(SessionId))]
public class ChatSearchPageView
{
    [AutoIncrement] public long Id { get; set; }
    public long SearchWidgetId { get; set; }
    public DateTime CreatedAt { get; set; }
    [StringLength(13)] public string? HourKey { get; set; }
    [StringLength(10)] public string? DayKey { get; set; }
    [StringLength(100)] public string? ClientId { get; set; }
    [StringLength(100)] public string? SessionId { get; set; }
    public bool FirstVisit { get; set; }
    [StringLength(500)] public string? Origin { get; set; }
    [StringLength(2000)] public string? PageUrl { get; set; }
    [StringLength(2000)] public string? PagePath { get; set; }
    [StringLength(500)] public string? PageTitle { get; set; }
    [StringLength(2000)] public string? Referrer { get; set; }
    [StringLength(45)] public string? IpAddress { get; set; }
    [StringLength(1000)] public string? UserAgent { get; set; }
    [StringLength(50)] public string? Language { get; set; }
    [StringLength(500)] public string? Languages { get; set; }
    [StringLength(100)] public string? Timezone { get; set; }
    [StringLength(100)] public string? Platform { get; set; }
    [StringLength(20)] public string? DeviceType { get; set; }
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }
    public double DevicePixelRatio { get; set; }
    public int ColorDepth { get; set; }
    public int TouchPoints { get; set; }
    [StringLength(50)] public string? ConnectionType { get; set; }
    public double Downlink { get; set; }
    public int Rtt { get; set; }
    public bool SaveData { get; set; }
    [StringLength(50)] public string? NavigationType { get; set; }
    public int DurationMs { get; set; }
    public int DomContentLoadedMs { get; set; }
    public int LoadMs { get; set; }
    [StringLength(300)] public string? UtmSource { get; set; }
    [StringLength(300)] public string? UtmMedium { get; set; }
    [StringLength(300)] public string? UtmCampaign { get; set; }
    [StringLength(300)] public string? UtmTerm { get; set; }
    [StringLength(300)] public string? UtmContent { get; set; }
    public long? GeoAsn { get; set; }
    [StringLength(300)] public string? GeoOrganization { get; set; }
    [StringLength(2)] public string? GeoContinentCode { get; set; }
    [StringLength(2)] public string? GeoCountryCode { get; set; }
    [StringLength(100)] public string? GeoCountryName { get; set; }
    [StringLength(20)] public string? GeoRegionCode { get; set; }
    [StringLength(100)] public string? GeoRegionName { get; set; }
    [StringLength(100)] public string? GeoCity { get; set; }
    [StringLength(20)] public string? GeoPostalCode { get; set; }
    [StringLength(100)] public string? GeoTimeZone { get; set; }
    public double? GeoLatitude { get; set; }
    public double? GeoLongitude { get; set; }
}

/// <summary>A heading-aware local-search section extracted from a cached document.</summary>
public class ChatSearchSection
{
    [AutoIncrement] public long Id { get; set; }
    [Index] public long DocumentId { get; set; }
    [Index] public long FilestoreId { get; set; }
    [Alias("user"), Index] public string? User { get; set; }
    public int Ordinal { get; set; }
    public string? DocumentTitle { get; set; }
    public string? Heading { get; set; }
    public int HeadingLevel { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Hierarchy { get; set; }
    public string? Anchor { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Url { get; set; }
    public string? Kind { get; set; }
    [StringLength(StringLengthAttribute.MaxText)] public string? Content { get; set; }
    [Index] public string? Category { get; set; }
    [Index] public string? DocType { get; set; }
    [Index] public string? Status { get; set; }
    [Index] public string? Locale { get; set; }
    [Index] public string? Product { get; set; }
    public string? Versions { get; set; }
    public string? Tags { get; set; }
}

/// <summary>Document counts + total size per category (port of GeminiDB.document_categories)</summary>
public class AiChatDocumentCategory
{
    public string? Category { get; set; }
    public long Count { get; set; }
    public long? Size { get; set; }
}

/// <summary>
/// Locally recorded document count + total size for a file store (port of get_filestore_stats),
/// used to fill in the stats of a store Gemini hasn't reported any for yet.
/// </summary>
public class AiChatFilestoreStats
{
    public long Count { get; set; }
    public long? Size { get; set; }
}

public static class GeminiDtos
{
    /// <summary>Apply the fields of a Gemini FileSearchStore resource onto a local row</summary>
    public static ChatFilestore PopulateFrom(this ChatFilestore to, JsonObject store)
    {
        to.Name = store.GetString("name") ?? to.Name;
        to.DisplayName = store.GetString("displayName") ?? to.DisplayName;
        to.CreateTime = GeminiRemoteDocument.ToDbTime(store.GetString("createTime"));
        to.UpdateTime = GeminiRemoteDocument.ToDbTime(store.GetString("updateTime"));
        to.ActiveDocumentsCount = store.GetLong("activeDocumentsCount");
        to.PendingDocumentsCount = store.GetLong("pendingDocumentsCount");
        to.FailedDocumentsCount = store.GetLong("failedDocumentsCount");
        to.SizeBytes = store.GetLong("sizeBytes");
        return to;
    }

    public static JsonObject ToDto(this ChatFilestore x) => new()
    {
        ["id"] = x.Id,
        ["user"] = x.User,
        ["createdAt"] = ChatDb.ToDateString(x.CreatedAt),
        ["updatedAt"] = ChatDb.ToDateString(x.UpdatedAt),
        ["name"] = x.Name,
        ["displayName"] = x.DisplayName,
        ["createTime"] = x.CreateTime,
        ["updateTime"] = x.UpdateTime,
        ["activeDocumentsCount"] = x.ActiveDocumentsCount,
        ["pendingDocumentsCount"] = x.PendingDocumentsCount,
        ["failedDocumentsCount"] = x.FailedDocumentsCount,
        ["sizeBytes"] = x.SizeBytes,
        ["metadata"] = ChatDtos.ParseJson(x.Metadata),
        ["error"] = x.Error,
        ["ref"] = x.Ref,
        ["visibility"] = x.Visibility,
        ["facets"] = ChatDtos.ParseJson(x.Facets),
    };

    public static JsonObject ToDto(this ChatDocument x) => new()
    {
        ["id"] = x.Id,
        ["filestoreId"] = x.FilestoreId,
        ["user"] = x.User,
        ["createdAt"] = ChatDb.ToDateString(x.CreatedAt),
        ["updatedAt"] = ChatDb.ToDateString(x.UpdatedAt),
        ["filename"] = x.Filename,
        ["url"] = x.Url,
        ["hash"] = x.Hash,
        ["size"] = x.Size,
        ["displayName"] = x.DisplayName,
        ["name"] = x.Name,
        ["customMetadata"] = ChatDtos.ParseJson(x.CustomMetadata),
        ["createTime"] = x.CreateTime,
        ["updateTime"] = x.UpdateTime,
        ["sizeBytes"] = x.SizeBytes,
        ["mimeType"] = x.MimeType,
        ["state"] = x.State,
        ["category"] = x.Category,
        ["sourceUrl"] = x.SourceUrl,
        ["sourceId"] = x.SourceId,
        ["sourceKey"] = x.SourceKey,
        ["sourceEtag"] = x.SourceEtag,
        ["contentHash"] = x.ContentHash,
        ["metadataHash"] = x.MetadataHash,
        ["extractorVer"] = x.ExtractorVer,
        ["tombstonedAt"] = ChatDb.ToDateNode(x.TombstonedAt),
        ["categoryPath"] = ChatDtos.ParseJson(x.CategoryPath),
        ["docType"] = x.DocType,
        ["status"] = x.Status,
        ["locale"] = x.Locale,
        ["product"] = x.Product,
        ["versions"] = ChatDtos.ParseJson(x.Versions),
        ["sourceUpdatedAt"] = x.SourceUpdatedAt,
        ["tags"] = ChatDtos.ParseJson(x.Tags),
        ["startedAt"] = ChatDb.ToDateNode(x.StartedAt),
        ["uploadedAt"] = ChatDb.ToDateNode(x.UploadedAt),
        ["searchHash"] = x.SearchHash,
        ["searchIndexedHash"] = x.SearchIndexedHash,
        ["searchStartedAt"] = ChatDb.ToDateNode(x.SearchStartedAt),
        ["searchIndexedAt"] = ChatDb.ToDateNode(x.SearchIndexedAt),
        ["searchError"] = x.SearchError,
        ["metadata"] = ChatDtos.ParseJson(x.Metadata),
        ["error"] = x.Error,
        ["ref"] = x.Ref,
    };

    public static JsonObject ToDto(this ChatSource x) => new()
    {
        ["id"] = x.Id, ["filestoreId"] = x.FilestoreId, ["user"] = x.User,
        ["createdAt"] = ChatDb.ToDateString(x.CreatedAt), ["updatedAt"] = ChatDb.ToDateString(x.UpdatedAt),
        ["name"] = x.Name, ["type"] = x.Type, ["enabled"] = x.Enabled,
        ["config"] = ChatDtos.ParseJson(x.Config), ["category"] = ChatDtos.ParseJson(x.Category),
        ["rules"] = ChatDtos.ParseJson(x.Rules), ["include"] = ChatDtos.ParseJson(x.Include),
        ["exclude"] = ChatDtos.ParseJson(x.Exclude), ["extract"] = ChatDtos.ParseJson(x.Extract),
        ["chunking"] = ChatDtos.ParseJson(x.Chunking), ["volatile"] = ChatDtos.ParseJson(x.Volatile),
        ["extractorVer"] = x.ExtractorVer, ["schedule"] = x.Schedule, ["onDelete"] = x.OnDelete,
        ["cursor"] = ChatDtos.ParseJson(x.Cursor), ["lastRunId"] = x.LastRunId,
        ["lastRunAt"] = ChatDb.ToDateNode(x.LastRunAt), ["error"] = x.Error,
    };

    public static JsonObject ToDto(this ChatSourceRun x) => new()
    {
        ["id"] = x.Id, ["sourceId"] = x.SourceId, ["user"] = x.User,
        ["startedAt"] = ChatDb.ToDateString(x.StartedAt), ["completedAt"] = ChatDb.ToDateNode(x.CompletedAt),
        ["status"] = x.Status, ["dryRun"] = x.DryRun, ["discovered"] = x.Discovered,
        ["added"] = x.Added, ["changed"] = x.Changed, ["metadataOnly"] = x.MetadataOnly,
        ["unchanged"] = x.Unchanged, ["removed"] = x.Removed, ["skipped"] = x.Skipped,
        ["failed"] = x.Failed, ["bytes"] = x.Bytes, ["plan"] = ChatDtos.ParseJson(x.Plan),
        ["log"] = ChatDtos.ParseJson(x.Log), ["error"] = x.Error,
    };

    public static JsonObject ToDto(this ChatAssistant x) => new()
    {
        ["id"] = x.Id, ["filestoreId"] = x.FilestoreId, ["user"] = x.User,
        ["createdAt"] = ChatDb.ToDateString(x.CreatedAt), ["updatedAt"] = ChatDb.ToDateString(x.UpdatedAt),
        ["name"] = x.Name, ["publicId"] = x.PublicId, ["enabled"] = x.Enabled,
        ["publishedAt"] = ChatDb.ToDateNode(x.PublishedAt), ["config"] = ChatDtos.ParseJson(x.Config),
    };

    public static JsonObject ToDto(this ChatAssistantConversation x) => new()
    {
        ["id"] = x.Id, ["assistantId"] = x.AssistantId, ["user"] = x.User,
        ["createdAt"] = ChatDb.ToDateString(x.CreatedAt), ["updatedAt"] = ChatDb.ToDateString(x.UpdatedAt),
        ["sessionId"] = x.SessionId, ["origin"] = x.Origin, ["pageUrl"] = x.PageUrl,
        ["userAgent"] = x.UserAgent, ["title"] = x.Title, ["status"] = x.Status,
        ["messageCount"] = x.MessageCount, ["lastMessage"] = x.LastMessage,
    };

    public static JsonObject ToDto(this ChatAssistantMessage x) => new()
    {
        ["id"] = x.Id, ["conversationId"] = x.ConversationId,
        ["createdAt"] = ChatDb.ToDateString(x.CreatedAt), ["role"] = x.Role,
        ["content"] = x.Content, ["citations"] = ChatDtos.ParseJson(x.Citations), ["error"] = x.Error,
    };

    public static JsonObject ToDto(this ChatSearchWidget x) => new()
    {
        ["id"] = x.Id, ["filestoreId"] = x.FilestoreId, ["user"] = x.User,
        ["createdAt"] = ChatDb.ToDateString(x.CreatedAt), ["updatedAt"] = ChatDb.ToDateString(x.UpdatedAt),
        ["name"] = x.Name, ["publicId"] = x.PublicId, ["enabled"] = x.Enabled,
        ["publishedAt"] = ChatDb.ToDateNode(x.PublishedAt), ["config"] = ChatDtos.ParseJson(x.Config),
    };

    public static JsonObject ToDto(this AiChatDocumentCategory x) => new()
    {
        // Python emits IFNULL(category,'') so uncategorized documents group under ""
        ["category"] = x.Category ?? "",
        ["count"] = x.Count,
        ["size"] = x.Size ?? 0,
    };
}
