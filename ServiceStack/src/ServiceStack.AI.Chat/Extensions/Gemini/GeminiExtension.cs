using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Gemini File Search Stores (port of llms-py's "gemini" extension): manage RAG document stores,
/// upload documents to them (deduplicated by SHA256, uploaded in the background) and reconcile the
/// local catalogue with what Gemini reports. Threads then ground answers on a store by including an
/// OpenAI-shaped `file_search` tool, which <see cref="GoogleProvider"/> forwards to Gemini.
/// Self-disables when no Gemini API key is configured.
/// </summary>
public partial class GeminiExtension() : ChatExtension("gemini")
{
    /// <summary>Url prefix of the content-addressed cache documents are stored in</summary>
    public const string CacheUrlBase = "/~cache/";

    GeminiDb db = null!;
    GeminiClient client = null!;
    GeminiStores stores = null!;
    GeminiUploadWorker? worker;
    GeminiSearchWorker? searchWorker;
    string? writeRole;

    public override void Install(ExtensionContext ctx)
    {
        // Keep extension and provider precedence identical.
        var apiKey = ctx.Feature.ResolveVariable("$GOOGLE_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            apiKey = ctx.Feature.ResolveVariable("$GEMINI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
            apiKey = ctx.Feature.Providers.GetValueOrDefault("google")?.ApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            Log.LogInformation("GEMINI_API_KEY is not configured");
            ctx.Disabled = true;
            return;
        }

        if (ctx.Feature.ChatDb is not { } chatDb)
        {
            Log.LogInformation("ChatFeature.ChatDb is required by the gemini extension "
                + "— register an IDbConnectionFactory");
            ctx.Disabled = true;
            return;
        }

        db = new GeminiDb(chatDb);
        writeRole = ctx.Feature.ResolveVariable("$GEMINI_WRITE_ROLE")
            ?? ctx.Config.GetString("gemini_write_role");
        if (ctx.Feature.AutoInitSchema)
        {
            db.InitSchema();
        }

        client = new GeminiClient(ctx.Feature.HttpClientFactory, apiKey);
        stores = new GeminiStores(db, client, ctx.Log);
        worker = new GeminiUploadWorker(ctx, db, client, stores);
        searchWorker = new GeminiSearchWorker(ctx, db);
        ctx.RegisterShutdownHandler(worker.Stop);
        ctx.RegisterShutdownHandler(searchWorker.Stop);

        ctx.AddGet("filestores", QueryFilestoresAsync, allowAnon: true);
        ctx.AddPost("filestores", CreateFilestoreAsync);
        ctx.AddGet("filestores/{id}/delete-summary", FilestoreDeleteSummaryAsync);
        ctx.AddDelete("filestores/{id}", DeleteFilestoreAsync);
        ctx.AddGet("filestores/{id}/categories", FilestoreCategoriesAsync, allowAnon: true);
        ctx.AddGet("filestores/{id}/documents", FilestoreDocumentsAsync, allowAnon: true);
        ctx.AddPost("filestores/{id}/upload", UploadToFilestoreAsync);
        ctx.AddPost("filestores/{id}/sync", SyncFilestoreAsync);
        ctx.AddPost("filestores/{id}/prune", PruneFilestoreAsync);
        ctx.AddGet("documents", QueryDocumentsAsync, allowAnon: true);
        ctx.AddDelete("documents/{id}", DeleteDocumentAsync);
        ctx.AddPost("documents/{id}/upload", UploadDocumentAsync);
        ctx.AddGet("documents/count", CountDocumentsAsync, allowAnon: true);
        ctx.AddPost("documents/bulk", BulkDocumentsAsync);
        ctx.AddPost("documents/summary", SummarizeDocumentsAsync);
        ctx.AddPost("documents/delete", DeleteDocumentsAsync);
        ctx.AddGet("documents/pending", PendingDocumentsAsync, allowAnon: true);
        ctx.AddGet("filestores/{id}/facets", FilestoreFacetsAsync, allowAnon: true);
        ctx.AddPost("filestores/{id}/reindex", ReindexDocumentsAsync);
        ctx.AddGet("worker", WorkerStatusAsync, allowAnon: true);
        ctx.AddPost("worker/cancel", CancelWorkerAsync);
        ctx.AddGet("source-types", SourceTypesAsync, allowAnon: true);
        ctx.AddGet("sources", QuerySourcesAsync, allowAnon: true);
        ctx.AddPost("sources", CreateSourceAsync);
        ctx.AddPatch("sources/{id}", UpdateSourceAsync);
        ctx.AddDelete("sources/{id}", DeleteSourceAsync);
        ctx.AddGet("sources/{id}/runs", SourceRunsAsync, allowAnon: true);
        ctx.AddPost("sources/{id}/run", RunSourceAsync);
        ctx.AddGet("config/import-roots", GetImportRootsAsync, allowAnon: true);
        ctx.AddPost("config/import-roots", SaveImportRootsAsync);
        ctx.AddGet("capabilities", GetCapabilitiesAsync, allowAnon: true);
        ctx.AddPost("capabilities/probe", ProbeCapabilitiesAsync);
        ctx.AddGet("imports", ListCrawlImportsAsync);
        ctx.AddGet("imports/schema", CrawlImportSchemaAsync);
        ctx.AddGet("imports/{name}", GetCrawlImportAsync);
        ctx.AddGet("imports/{name}/pages", ListCrawlPagesAsync);
        ctx.AddGet("imports/{name}/page", GetCrawlPageAsync);
        ctx.AddPost("imports/crawl", StartCrawlAsync);
        ctx.AddPut("imports/{name}", SaveCrawlConfigAsync);
        ctx.AddPost("imports/{name}/transform", TransformCrawlImportAsync);
        InstallAssistantRoutes(ctx);
        InstallSearchRoutes(ctx);
    }

    /// <summary>Resume any uploads that were still queued when the app last shut down</summary>
    public override Task LoadAsync(ExtensionContext ctx, CancellationToken token = default)
    {
        worker?.Start();
        db.EnsureSearchDesiredHashes();
        searchWorker?.Start();
        return Task.CompletedTask;
    }

    // ── File stores ──

    /// <summary>
    /// Stores are listed with the document count + size the UI shows. A store whose stats were never
    /// recorded (created before its first upload completed) has them backfilled from Gemini, or from
    /// the local documents when Gemini can't report them.
    /// </summary>
    async Task<object?> QueryFilestoresAsync(ChatRequestContext req)
    {
        var user = UserOf(req);
        var rows = db.QueryFilestores(QueryOf(req), user);
        foreach (var row in rows)
        {
            if (row.ActiveDocumentsCount != null && row.SizeBytes != null)
                continue;
            if (await stores.RefreshAsync(row).ConfigAwait())
                continue;

            var stats = db.FilestoreStats(row.Id, user);
            row.ActiveDocumentsCount ??= stats.Count;
            row.SizeBytes ??= stats.Size ?? 0;
        }
        return rows.ToDtos(x => x.ToDto());
    }

    async Task<object?> CreateFilestoreAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var user = UserOf(req);
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var displayName = body.GetString("displayName");
        if (string.IsNullOrEmpty(displayName))
            throw new ArgumentException("displayName is required");

        Log.LogInformation("Creating filestore {DisplayName} in Gemini...", displayName);
        var result = await client.CreateFileSearchStoreAsync(displayName).ConfigAwait();
        if (result.GetString("name") == null)
            throw new Exception("Failed to create filestore in Gemini");

        var now = DateTime.Now;
        var filestore = new ChatFilestore
        {
            User = user,
            CreatedAt = now,
            UpdatedAt = now,
            Ref = body.GetString("ref"),
        };
        filestore.PopulateFrom(result);
        filestore.Id = db.InsertFilestore(filestore);
        return db.GetFilestore(filestore.Id, user)?.ToDto();
    }

    async Task<object?> FilestoreDeleteSummaryAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var summary = db.FilestoreDeleteSummary(IdOf(req), UserOf(req));
        if (summary == null) return ChatResult.NotFound("File Store does not exist");
        var name = summary.GetString("name");
        if (!string.IsNullOrEmpty(name))
        {
            try
            {
                var remote = await client.GetFileSearchStoreAsync(name).ConfigAwait();
                summary["remoteStoreExists"] = true;
                summary["remoteDocuments"] = (remote.GetLong("activeDocumentsCount") ?? 0)
                    + (remote.GetLong("pendingDocumentsCount") ?? 0)
                    + (remote.GetLong("failedDocumentsCount") ?? 0);
                summary["remoteDocumentBytes"] = remote.GetLong("sizeBytes") ?? 0;
            }
            catch (GeminiApiException e) when (e.StatusCode == 404)
            {
                summary["remoteStoreExists"] = false;
                summary["remoteDocuments"] = 0;
                summary["remoteDocumentBytes"] = 0;
            }
            catch (Exception e)
            {
                // Stored counts still provide a useful confirmation preview when Gemini's live
                // statistics are temporarily unavailable.
                Log.LogError(e, "Could not refresh delete summary for {Name}", name);
            }
        }
        return summary;
    }

    async Task<object?> DeleteFilestoreAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var user = UserOf(req);
        var id = IdOf(req);
        var filestore = db.GetFilestore(id, user);
        if (filestore == null) return ChatResult.NotFound("File Store does not exist");
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var confirmation = body.GetString("confirm");
        if (confirmation != filestore.DisplayName)
            return Error($"Type \"{filestore.DisplayName}\" to confirm permanent deletion",
                "ConfirmationRequired", 400);

        if (filestore.Name is { } name)
        {
            Log.LogInformation("Deleting filestore {Name} in Gemini...", name);
            try
            {
                await client.DeleteFileSearchStoreAsync(name).ConfigAwait();
            }
            catch (GeminiApiException e) when (e.StatusCode == 404)
            {
                Log.LogInformation("Filestore {Name} was already deleted in Gemini", name);
            }
        }
        else
        {
            Log.LogInformation("Filestore {Id} has no name, skipping Gemini deletion...", id);
        }

        JsonObject? deleted;
        try
        {
            deleted = db.DeleteFilestore(id, user, confirmation);
        }
        catch (ArgumentException e)
        {
            return Error(e.Message, "ConfirmationRequired", 400);
        }
        return deleted == null
            ? ChatResult.NotFound("File Store does not exist")
            : new JsonObject { ["deleted"] = deleted };
    }

    Task<object?> FilestoreCategoriesAsync(ChatRequestContext req)
    {
        var categories = db.DocumentCategories(IdOf(req), UserOf(req));
        return Task.FromResult<object?>(categories.ToDtos(x => x.ToDto()));
    }

    /// <summary>Live state of every document in the store, straight from Gemini</summary>
    async Task<object?> FilestoreDocumentsAsync(ChatRequestContext req)
    {
        var filestore = db.GetFilestore(IdOf(req), UserOf(req))
            ?? throw new Exception("Filestore does not exist");

        var documents = await client.ListDocumentsAsync(filestore.Name ?? "").ConfigAwait();
        return documents.ToDtos(doc =>
        {
            var remote = GeminiRemoteDocument.From(doc);
            return new JsonObject
            {
                ["name"] = remote.Name,
                ["displayName"] = remote.DisplayName,
                ["mimeType"] = remote.MimeType,
                ["sizeBytes"] = remote.SizeBytes,
                ["createTime"] = remote.CreateTime,
                ["updateTime"] = remote.UpdateTime,
                ["state"] = remote.State,
                ["customMetadata"] = ChatDtos.ParseJson(remote.CustomMetadata),
            };
        });
    }

    // ── Documents ──

    Task<object?> QueryDocumentsAsync(ChatRequestContext req)
    {
        var rows = db.QueryDocuments(QueryOf(req), UserOf(req));
        return Task.FromResult<object?>(rows.ToDtos(ToClientDto));
    }

    /// <summary>The Gemini UI uses document URLs directly, so include the configured mount path.</summary>
    JsonObject ToClientDto(ChatDocument document)
    {
        var dto = document.ToDto();
        dto["url"] = Feature.ResolveClientUrl(document.Url);
        return dto;
    }

    /// <summary>
    /// Accept multipart uploads into the store: each file is hashed, written to the content-addressed
    /// cache and recorded locally, then the background worker uploads it to Gemini.
    /// </summary>
    async Task<object?> UploadToFilestoreAsync(ChatRequestContext req)
    {
        var user = UserOf(req);
        var id = IdOf(req);
        var category = req.QueryString("category");
        Log.LogInformation("Uploading to filestore {Id} {User}", id, user);

        if (db.GetFilestore(id, user) == null)
            throw new Exception("Filestore does not exist");

        await AssertWriteAsync(req).ConfigAwait();
        return await QueueManualUploadsAsync(req, id, user, category).ConfigAwait();
    }

    async Task<object?> DeleteDocumentAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var user = UserOf(req);
        var id = IdOf(req);
        var doc = db.GetDocument(id, user)
            ?? throw new Exception("Document does not exist");

        if (doc.Name is { } name)
        {
            try
            {
                await client.DeleteDocumentAsync(name).ConfigAwait();
            }
            catch (GeminiApiException e) when (e.StatusCode == 404)
            {
                Log.LogInformation("Document {Name} already deleted in Gemini", name);
            }
        }

        db.DeleteDocument(id, user);

        // the store's counts + size no longer include this document
        if (doc.FilestoreId > 0)
        {
            await stores.RefreshAsync(doc.FilestoreId, user).ConfigAwait();
        }
        return new JsonObject();
    }

    /// <summary>Retry a failed upload, waiting for the worker to finish it (port of upload_document)</summary>
    async Task<object?> UploadDocumentAsync(ChatRequestContext req)
    {
        var user = UserOf(req);
        var id = IdOf(req);
        var doc = db.GetDocument(id, user)
            ?? throw new Exception("Document does not exist");

        await AssertWriteAsync(req).ConfigAwait();
        db.ResetDocumentUpload(id);
        worker?.Start();

        var timeout = DateTime.UtcNow.Add(client.Timeout);
        while (worker?.Running == true && DateTime.UtcNow < timeout)
        {
            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigAwait();
            doc = db.GetDocument(id, user) ?? doc;
            if (doc.UploadedAt != null || doc.Error != null)
                break;
        }
        return ToClientDto(db.GetDocument(id, user) ?? doc);
    }

    // ── Sync ──

    /// <summary>
    /// Reconcile the local catalogue with the store's remote contents (port of sync_filestore_documents):
    /// matches by the hash Gemini keeps in custom metadata (falling back to the document name), refreshes
    /// any stale local fields and records what didn't line up as the document's state.
    /// </summary>
    async Task<object?> SyncFilestoreAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var user = UserOf(req);
        var id = IdOf(req);
        var filestore = db.GetFilestore(id, user)
            ?? throw new Exception("Filestore does not exist");

        var localDocs = db.QueryAllDocuments(id, user).ToList();
        var localById = localDocs.ToDictionary(x => x.Id);
        var localByHash = new Dictionary<string, ChatDocument>();
        var localByName = new Dictionary<string, ChatDocument>();
        foreach (var doc in localDocs)
        {
            if (doc.Hash != null)
                localByHash[doc.Hash] = doc;
            if (doc.Name != null)
                localByName[doc.Name] = doc;
        }
        Log.LogInformation("Found {Count} local documents ({Hashes} hashes)",
            localDocs.Count, localByHash.Count);

        var localMissing = new List<GeminiRemoteDocument>();   // in Gemini, unknown locally
        var remoteMissing = new List<ChatDocument>();        // local, missing from Gemini
        var missingMetadata = new List<ChatDocument>();
        var metadataMismatch = new List<ChatDocument>();
        var unmatched = new List<ChatDocument>();
        var hashCounts = new Dictionary<string, int>();
        var matchedLocalIds = new HashSet<long>();
        var matchedByHash = 0;

        var remoteDocs = await client.ListDocumentsAsync(filestore.Name ?? "").ConfigAwait();
        foreach (var remoteJson in remoteDocs)
        {
            var remote = GeminiRemoteDocument.From(remoteJson);
            var local = remote.MetadataId is { } metadataId
                ? localById.GetValueOrDefault(metadataId)
                : remote.MetadataHash != null ? localByHash.GetValueOrDefault(remote.MetadataHash)
                : remote.Name != null ? localByName.GetValueOrDefault(remote.Name) : null;

            if (local == null)
            {
                Log.LogDebug("Remote doc not found locally: {Name}", remote.Name);
                localMissing.Add(remote);
                continue;
            }
            matchedLocalIds.Add(local.Id);
            if (remote.MetadataHash == null || remote.MetadataId == null)
            {
                Log.LogDebug("Remote doc missing metadata: {Name}", remote.Name);
                missingMetadata.Add(local);
                continue;
            }

            matchedByHash++;

            var diff = remote.Diff(local);
            if (diff.Count > 0)
            {
                Log.LogDebug("Updating local doc {Doc} unmatched fields: {Fields}",
                    FileNameOf(local), string.Join(", ", diff));
                unmatched.Add(local);
                remote.ApplyTo(local);
                db.UpdateDocument(local);
            }

            var remoteMetadata = ChatDtos.ParseJson(remote.CustomMetadata) as JsonArray;
            if (local.Id != remote.MetadataId || local.Hash != remote.MetadataHash
                || GeminiMetadata.Differs(local, remoteMetadata))
            {
                Log.LogDebug("Metadata mismatch: id={LocalId}|{RemoteId}, hash={LocalHash}|{RemoteHash}",
                    local.Id, remote.MetadataId, local.Hash, remote.MetadataHash);
                metadataMismatch.Add(local);
            }

            hashCounts[remote.MetadataHash] = hashCounts.GetValueOrDefault(remote.MetadataHash) + 1;
        }

        foreach (var local in localDocs)
        {
            if (!matchedLocalIds.Contains(local.Id))
                remoteMissing.Add(local);
        }

        var duplicates = hashCounts.Where(x => x.Value > 1)
            .Select(x => localByHash[x.Key])
            .ToList();

        foreach (var doc in remoteMissing)
            db.UpdateDocumentState(doc.Id, "MISSING_FROM_REMOTE");
        foreach (var doc in missingMetadata)
            db.UpdateDocumentState(doc.Id, "MISSING_METADATA");
        foreach (var doc in metadataMismatch)
            db.UpdateDocumentState(doc.Id, "METADATA_MISMATCH");
        foreach (var doc in duplicates)
            db.UpdateDocumentState(doc.Id, "DUPLICATE_FILE");

        await stores.RefreshAsync(filestore).ConfigAwait();
        db.EnsureSearchDesiredHashes();
        searchWorker?.Start();

        Log.LogInformation(
            "Sync complete: remote={Remote}, local={Local}, matched={Matched}, missing_metadata={MissingMetadata}, unmatched={Unmatched}",
            remoteDocs.Count, localDocs.Count, matchedByHash, missingMetadata.Count, localMissing.Count);

        return new JsonObject
        {
            ["Missing from Local"] = Issue(localMissing.Count, localMissing.Take(5).Select(x => x.FileName())),
            ["Missing from Gemini"] = Issue(remoteMissing.Count, remoteMissing.Take(5).Select(FileNameOf)),
            ["Missing Metadata"] = Issue(missingMetadata.Count, missingMetadata.Take(5).Select(FileNameOf)),
            ["Metadata Mismatch"] = Issue(metadataMismatch.Count, metadataMismatch.Take(5).Select(FileNameOf)),
            ["Unmatched Fields"] = Issue(unmatched.Count, unmatched.Take(5).Select(FileNameOf)),
            ["Duplicate Documents"] = Issue(duplicates.Count, duplicates.Take(5).Select(FileNameOf)),
            ["Local Search"] = new JsonObject { ["queued"] = db.SearchStats(id, user).Pending },
            ["Summary"] = new JsonObject
            {
                ["Local Documents"] = localDocs.Count,
                ["Remote Documents"] = remoteDocs.Count,
                ["Matched Documents"] = matchedByHash,
            },
        };
    }

    static JsonObject Issue(int count, IEnumerable<string> docs) => new()
    {
        ["count"] = count,
        ["docs"] = new JsonArray(docs.Select(x => (JsonNode)x).ToArray()),
    };

    static string FileNameOf(ChatDocument doc) => doc.Category != null
        ? $"{doc.Category}/{doc.DisplayName}"
        : doc.DisplayName ?? "";

    // ── Helpers ──

    static string? UserOf(ChatRequestContext req) => req.UserName ?? ChatDb.DefaultUser;

    static long IdOf(ChatRequestContext req) => long.TryParse(req.GetPathParam("id"), out var id)
        ? id
        : throw new ArgumentException("Invalid id");

    static JsonObject QueryOf(ChatRequestContext req)
    {
        var query = new JsonObject();
        foreach (var key in req.Request.QueryString.AllKeys)
        {
            if (key != null)
                query[key] = req.Request.QueryString[key];
        }
        return query;
    }
}
