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
public partial class GeminiExtension() : ChatExtension("gemini"), IHasSchema
{
    /// <summary>Url prefix of the content-addressed cache documents are stored in</summary>
    public const string CacheUrlBase = "/~cache/";

    GeminiDb db = null!;
    GeminiClient client = null!;
    GeminiStores stores = null!;
    GeminiUploadWorker? worker;
    GeminiSearchWorker? searchWorker;
    string? writeRole;

    /// <summary>
    /// Optional request-IP resolver used by opt-in Search analytics. When null, the extension tries
    /// to resolve <see cref="IGeminiSearchGeoResolver"/> from the host IOC during installation.
    /// </summary>
    public IGeminiSearchGeoResolver? SearchGeoResolver { get; set; }

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
        SearchGeoResolver ??= ctx.Feature.Services.GetService(typeof(IGeminiSearchGeoResolver))
            as IGeminiSearchGeoResolver;
        writeRole = ctx.Feature.ResolveVariable("$GEMINI_WRITE_ROLE")
            ?? ctx.Config.GetString("gemini_write_role");
        if (ctx.Feature.AutoInitSchema)
        {
            InitSchema();
        }

        client = new GeminiClient(ctx.Feature.HttpClientFactory, apiKey);
        stores = new GeminiStores(db, client, ctx.Log);
        worker = new GeminiUploadWorker(ctx, db, client, stores);
        searchWorker = new GeminiSearchWorker(ctx, db);
        ctx.RegisterShutdownHandler(worker.StopAsync);
        ctx.RegisterShutdownHandler(searchWorker.StopAsync);

        ctx.AddGet("filestores", QueryFilestoresAsync, allowAnon: true);
        ctx.AddPost("filestores", CreateFilestoreAsync);
        ctx.AddGet("filestores/{id}/delete-summary", FilestoreDeleteSummaryAsync);
        ctx.AddDelete("filestores/{id}", DeleteFilestoreAsync);
        ctx.AddGet("filestores/{id}/categories", FilestoreCategoriesAsync, allowAnon: true);
        ctx.AddGet("filestores/{id}/documents", FilestoreDocumentsAsync, allowAnon: true);
        ctx.AddPost("filestores/{id}/upload", UploadToFilestoreAsync);
        ctx.AddPost("filestores/{id}/sync", req => SourceOperationAsync(req, SyncFilestoreAsync));
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
        ctx.AddPost("filestores/{id}/resume-uploads", ResumeUploadsAsync);
        ctx.AddGet("worker", WorkerStatusAsync, allowAnon: true);
        ctx.AddPost("worker/cancel", CancelWorkerAsync);
        ctx.AddGet("source-types", SourceTypesAsync, allowAnon: true);
        ctx.AddGet("sources", QuerySourcesAsync);
        ctx.AddPost("sources/load", req => SourceOperationAsync(req, LoadSavedImportAsync));
        ctx.AddGet("sources/{id}", req => Task.FromResult<object?>(EditableSource(db.GetSource(IdOf(req),UserOf(req)) ?? throw HttpError.NotFound("Source does not exist"),req)));
        ctx.AddPost("sources", req => SourceOperationAsync(req, CreateSourceAsync));
        ctx.AddPatch("sources/{id}", req => SourceOperationAsync(req, UpdateSourceAsync));
        ctx.AddDelete("sources/{id}", req => SourceOperationAsync(req, DeleteSourceAsync));
        ctx.AddGet("sources/{id}/runs", SourceRunsAsync, allowAnon: true);
        ctx.AddPost("sources/{id}/run", req => SourceOperationAsync(req, RunSourceAsync));
        ctx.AddGet("config/import-roots", GetImportRootsAsync, allowAnon: true);
        ctx.AddPost("config/import-roots", SaveImportRootsAsync);
        ctx.AddGet("capabilities", GetCapabilitiesAsync, allowAnon: true);
        ctx.AddPost("capabilities/probe", ProbeCapabilitiesAsync);
        ctx.AddGet("imports", ListCrawlImportsAsync);
        ctx.AddGet("imports/schema", CrawlImportSchemaAsync);
        // Progress must not acquire the source-operation lock held by an active crawl.
        ctx.AddGet("imports/crawl/progress", CrawlProgressAsync);
        ctx.AddGet("imports/browse", BrowseImportManifestsAsync);
        ctx.AddPost("imports/load", LoadImportManifestAsync);
        ctx.AddGet("imports/{name}", req => SourceOperationAsync(req, GetCrawlImportAsync));
        ctx.AddGet("imports/{name}/pages", req => SourceOperationAsync(req, ListCrawlPagesAsync));
        ctx.AddGet("imports/{name}/page", req => SourceOperationAsync(req, GetCrawlPageAsync));
        ctx.AddPost("imports/crawl", req => SourceOperationAsync(req, StartCrawlAsync));
        ctx.AddPut("imports/{name}", req => SourceOperationAsync(req, SaveCrawlConfigAsync));
        ctx.AddPost("imports/{name}/transform", req => SourceOperationAsync(req, TransformCrawlImportAsync));
        InstallAssistantRoutes(ctx);
        InstallSearchRoutes(ctx);
    }

    public void InitSchema()
    {
        db.InitSchema();
    }

    public void DropSchema()
    {
        db.DropSchema();
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
        dto["localFileExists"] = LocalFileExists(document);
        return dto;
    }

    /// <summary>True when the document's cached copy exists, so it can be (re)uploaded from it.</summary>
    bool LocalFileExists(ChatDocument document) =>
        document.Url?.StartsWith(CacheUrlBase, StringComparison.Ordinal) == true
        && File.Exists(Ctx.GetCachePath(document.Url[CacheUrlBase.Length..]));

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
        var gate = ManualDeletionLocks.GetOrAdd(ImportLockKey(user), _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(req.Request.RequestAborted).ConfigAwait();
        try
        {
            var doc = db.GetDocument(IdOf(req), user) ?? throw HttpError.NotFound("Document does not exist");
            AssertSafeDocumentDelete([doc], user);
            await RemoveDocumentAsync(doc, user, req.Request.RequestAborted).ConfigAwait();
            await stores.RefreshAsync(doc.FilestoreId, user, req.Request.RequestAborted).ConfigAwait();
            return new JsonObject();
        }
        finally { gate.Release(); }
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
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var selectedSourceDocuments = body.GetArray("sourceDocuments")?
            .Select(x => x?.GetValue<string>()).Where(x => x != null).Cast<string>().ToHashSet()
            ?? [];
        // Python parity: without an explicit choice, add every new source document unless some were selected
        var addAllSourceDocuments = body.ContainsKey("addAllSourceDocuments")
            ? body.GetBool("addAllSourceDocuments") : selectedSourceDocuments.Count == 0;
        var filestore = db.GetFilestore(id, user)
            ?? throw new Exception("Filestore does not exist");

        // Re-run recurring imports first, so Store Sync detects and queues changes to the
        // original files as well as reconciling the resulting local catalogue with Gemini.
        var sourceChanges = new List<string>();
        var newSourceDocuments = new JsonArray();
        var sourceErrors = new List<string>();
        var sourcesSynced = 0;
        foreach (var source in db.QuerySources(id, user))
        {
            try
            {
                if (!source.Enabled) continue;
                // Only the selection is forwarded: Store Sync never confirms deletions or overrides metadata
                var options = new JsonObject {
                    ["dryRun"] = false,
                    ["addAllSourceDocuments"] = addAllSourceDocuments,
                    ["sourceDocuments"] = new JsonArray(selectedSourceDocuments.Select(x => (JsonNode)x).ToArray()),
                };
                var summary = await RunSourcePipelineAsync(source, req, options, refreshCrawl: true, startUploads: false).ConfigAwait();
                sourcesSynced++;
                foreach (var item in summary.GetArray("sourceChanges") ?? []) if (item != null) sourceChanges.Add(item.GetValue<string>());
                foreach (var item in summary.GetArray("newSourceDocuments") ?? []) newSourceDocuments.Add(item?.DeepClone());
                if (summary.GetString("deleteRefused") is { } refused) sourceErrors.Add($"{source.Name}: {refused}");
                if (summary.GetInt("failed") is > 0 and var failed) sourceErrors.Add($"{source.Name}: {failed} files could not be read");
                foreach (var error in summary.GetArray("deleteErrors")?.OfType<JsonObject>() ?? [])
                    sourceErrors.Add($"{source.Name}: {error.GetString("displayName")}: {error.GetString("error")}");
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                sourceErrors.Add($"{source.Name}: {ChatJson.ToErrorMessage(e)}");
                source.Error = ChatJson.ToErrorMessage(e); db.UpdateSource(source);
                Log.LogWarning(e, "Could not scan saved import {SourceId} during store sync", source.Id);
            }
        }
        var localDocs = db.QueryAllDocuments(id, user).Where(x => x.TombstonedAt == null).ToList();
        var remoteDocs = await client.ListDocumentsAsync(filestore.Name ?? "", req.Request.RequestAborted).ConfigAwait();
        var repairs = await RepairDocumentsAsync(localDocs, remoteDocs, user, req.Request.RequestAborted).ConfigAwait();
        var localMissing = repairs.LocalMissing;
        var remoteMissing = repairs.RemoteMissing;
        var missingMetadata = repairs.MissingMetadata;
        var metadataMismatch = repairs.MetadataMismatch;
        var unmatched = repairs.Unmatched;
        var duplicates = repairs.Duplicates;
        var pendingUploads = repairs.PendingUploads;
        var matchedByHash = repairs.Matched;

        await stores.RefreshAsync(filestore).ConfigAwait();
        db.EnsureSearchDesiredHashes();
        searchWorker?.Start();
        if (pendingUploads.Count > 0)
        {
            Log.LogInformation("Store sync found {Count} queued Gemini uploads; ensuring UploadWorker is running",
                pendingUploads.Count);
            worker?.Start();
        }

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
            ["Source Changes"] = Issue(sourceChanges.Count, sourceChanges.Take(5)),
            ["Saved Imports"] = Issue(sourcesSynced, []),
            ["Source Errors"] = Issue(sourceErrors.Count, sourceErrors),
            ["Repair Errors"] = Issue(repairs.Errors.Count, repairs.Errors),
            ["Pending Uploads"] = Issue(pendingUploads.Count, pendingUploads.Take(5).Select(FileNameOf)),
            ["New Source Documents"] = new JsonObject {
                ["count"] = newSourceDocuments.Count,
                ["docs"] = new JsonArray(newSourceDocuments.Take(5)
                    .Select(x => (JsonNode?)x?["sourceKey"]?.DeepClone()).ToArray()),
                ["items"] = newSourceDocuments,
            },
            ["newSourceDocuments"] = newSourceDocuments.DeepClone(),
            ["Local Search"] = new JsonObject { ["queued"] = db.SearchStats(id, user).Pending },
            ["Summary"] = new JsonObject
            {
                ["Local Documents"] = localDocs.Count,
                ["Remote Documents"] = remoteDocs.Count,
                ["Matched Documents"] = matchedByHash,
                ["Removed Remote Copies"] = repairs.RemovedCopies,
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
