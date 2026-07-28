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
}

/// <summary>
/// A document uploaded to a file search store (port of gemini/db.py's "document" table).
/// The file itself lives in the content-addressed cache; this row tracks both the local copy
/// (filename/url/hash/size) and the remote Gemini document (name/state/customMetadata).
/// </summary>
[UniqueConstraint(nameof(FilestoreId), nameof(Hash))]
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
    public string? Tags { get; set; }              // JSON

    public DateTime? StartedAt { get; set; }
    public DateTime? UploadedAt { get; set; }

    public string? Metadata { get; set; }          // JSON
    [StringLength(StringLengthAttribute.MaxText)]
    public string? Error { get; set; }
    public string? Ref { get; set; }
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
        ["tags"] = ChatDtos.ParseJson(x.Tags),
        ["startedAt"] = ChatDb.ToDateNode(x.StartedAt),
        ["uploadedAt"] = ChatDb.ToDateNode(x.UploadedAt),
        ["metadata"] = ChatDtos.ParseJson(x.Metadata),
        ["error"] = x.Error,
        ["ref"] = x.Ref,
    };

    public static JsonObject ToDto(this AiChatDocumentCategory x) => new()
    {
        // Python emits IFNULL(category,'') so uncategorized documents group under ""
        ["category"] = x.Category ?? "",
        ["count"] = x.Count,
        ["size"] = x.Size ?? 0,
    };
}
