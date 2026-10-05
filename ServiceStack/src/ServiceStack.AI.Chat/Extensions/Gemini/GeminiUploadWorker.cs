using Microsoft.Extensions.Logging;
using ServiceStack.Text;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>
/// Background uploader for queued documents (port of gemini/upload_worker.py): drains every document
/// without an uploadedAt or error, uploads it to its file search store, then refreshes the store's
/// stats. Started on extension load (to pick up anything left over from a previous run) and again
/// after each upload request; it stops itself once the queue is empty.
/// </summary>
public class GeminiUploadWorker
{
    /// <summary>Extensions Gemini needs told about explicitly, as "ext:mime/type" pairs</summary>
    public const string DefaultMimeTypes = "mdx:text/markdown,cshtml:text/html";

    readonly ExtensionContext ctx;
    readonly GeminiDb db;
    readonly GeminiClient client;
    readonly GeminiStores stores;
    readonly Dictionary<string, string> includeMimeTypes = new(StringComparer.OrdinalIgnoreCase);
    readonly object syncRoot = new();
    readonly int concurrency;
    readonly int maxRetries;
    CancellationTokenSource? cts;
    Task runTask = Task.CompletedTask;
    bool stopped;
    bool restartRequested;
    bool cancelRequested;
    long total, done, failed;
    DateTime? startedAt;

    public bool Running { get; private set; }

    public JsonObject Status()
    {
        lock (syncRoot)
        {
            var elapsed = startedAt == null ? 0 : Math.Max(0.001, (DateTime.UtcNow - startedAt.Value).TotalSeconds);
            var rate = done / elapsed;
            return new JsonObject
            {
                ["total"] = total, ["done"] = done, ["failed"] = failed,
                ["startedAt"] = startedAt == null ? null : ChatDb.ToDateString(startedAt.Value),
                ["rate"] = rate, ["etaSeconds"] = rate > 0 ? Math.Max(0, (total - done - failed) / rate) : null,
                ["running"] = Running, ["cancelled"] = cancelRequested,
            };
        }
    }

    /// <summary>Stop dequeuing new work after the current batch finishes.</summary>
    public void Cancel()
    {
        lock (syncRoot) cancelRequested = true;
    }

    public GeminiUploadWorker(ExtensionContext ctx, GeminiDb db, GeminiClient client, GeminiStores stores)
    {
        this.ctx = ctx;
        this.db = db;
        this.client = client;
        this.stores = stores;

        var mimeTypes = ctx.Feature.ResolveVariable("$GEMINI_UPLOAD_MIME_TYPES") ?? DefaultMimeTypes;
        concurrency = Math.Max(1, ParseInt(ctx.Feature.ResolveVariable("$GEMINI_UPLOAD_CONCURRENCY"), 4));
        maxRetries = Math.Max(1, ParseInt(ctx.Feature.ResolveVariable("$GEMINI_UPLOAD_MAX_RETRIES"), 4));
        foreach (var entry in mimeTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var ext = entry.LeftPart(':').Trim().TrimStart('.');
            var mimeType = entry.RightPart(':').Trim();
            if (ext.Length > 0 && mimeType.Length > 0)
                includeMimeTypes[ext] = mimeType;
        }
    }

    static int ParseInt(string? value, int defaultValue) => int.TryParse(value, out var parsed) ? parsed : defaultValue;

    public void Start()
    {
        lock (syncRoot)
        {
            if (stopped) return;
            restartRequested = true;
            cancelRequested = false;
            if (Running) return;
            Running = true;
            total = done = failed = 0;
            startedAt = DateTime.UtcNow;
            var source = cts = new CancellationTokenSource();
            // Publish ownership and the joinable task atomically; no database/network work in this lock.
            runTask = Task.Run(() => RunAsync(source));
        }
    }

    public void Stop()
    {
        lock (syncRoot) { stopped = true; restartRequested = false; cts?.Cancel(); }
    }

    public async Task StopAsync(CancellationToken token = default)
    {
        Task task;
        lock (syncRoot) { stopped = true; restartRequested = false; cts?.Cancel(); task = runTask; }
        await task.WaitAsync(token).ConfigAwait();
    }

    async Task RunAsync(CancellationTokenSource source)
    {
        var token = source.Token;
        var completed = new HashSet<long>();
        var filestoreIds = new HashSet<long>();
        try
        {
            while (!token.IsCancellationRequested && !IsCancelRequested())
            {
                lock (syncRoot)
                {
                    if (restartRequested) { restartRequested = false; completed.Clear(); }
                }
                List<ChatDocument> pending;
                try { pending = db.GetPendingDocuments(concurrency, completed); }
                catch (Exception e)
                {
                    ctx.Log.LogError(e, "Gemini UploadWorker failed to read its queue; retrying");
                    await Task.Delay(TimeSpan.FromSeconds(5), token).ConfigAwait();
                    continue;
                }
                if (pending.Count > 0)
                {
                    foreach (var doc in pending) { completed.Add(doc.Id); filestoreIds.Add(doc.FilestoreId); }
                    lock (syncRoot) total = Math.Max(total, done + failed + pending.Count);
                    await Task.WhenAll(pending.Select(async doc =>
                    {
                        var succeeded = await ProcessDocumentAsync(doc, token).ConfigAwait();
                        lock (syncRoot) { if (succeeded) done++; else failed++; }
                    })).ConfigAwait();
                    continue;
                }
                // Keep sole ownership through cleanup and the last stats refresh. Start() can
                // request another drain while either network operation is in flight.
                foreach (var doc in db.GetPendingRemoteDeletes())
                {
                    await CleanupRemoteCopiesAsync(doc, token).ConfigAwait();
                    filestoreIds.Add(doc.FilestoreId);
                }
                foreach (var filestoreId in filestoreIds)
                    await stores.RefreshAsync(filestoreId, null, token).ConfigAwait();
                filestoreIds.Clear();
                lock (syncRoot)
                {
                    if (restartRequested && !stopped && !cancelRequested) continue;
                    if (ReferenceEquals(cts, source)) { Running = false; cts = null; }
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception e) { ctx.Log.LogError(e, "Gemini UploadWorker failed"); }
        finally
        {
            var restart = false;
            lock (syncRoot)
            {
                if (ReferenceEquals(cts, source))
                {
                    restart = restartRequested && !stopped && !cancelRequested;
                    Running = false; cts = null;
                }
            }
            source.Dispose();
            if (restart) Start();
        }
    }

    async Task CleanupRemoteCopiesAsync(ChatDocument doc, CancellationToken token)
    {
        var remaining = GeminiMetadata.AsList(doc.PendingDeleteNames);
        foreach (var name in remaining.ToArray())
        {
            if (name == doc.Name) continue; // never delete the current live document
            try { await client.DeleteDocumentAsync(name, token).ConfigAwait(); }
            catch (GeminiApiException e) when (e.StatusCode is 404 or 410) { }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                ctx.Log.LogWarning(e, "Could not remove superseded Gemini document {Name}", name);
                continue;
            }
            remaining.Remove(name);
        }
        doc.PendingDeleteNames = remaining.Count == 0 ? null : GeminiMetadata.JsonArrayOf(remaining);
        db.UpdatePendingDeleteNames(doc.Id,doc.PendingDeleteNames);
    }

    bool IsCancelRequested()
    {
        lock (syncRoot) return cancelRequested;
    }

    async Task<bool> ProcessDocumentAsync(ChatDocument doc, CancellationToken token)
    {
        try
        {
            var filestore = db.GetFilestore(doc.FilestoreId, null);
            if (filestore == null || (filestore.User ?? "") != (doc.User ?? ""))
                throw new Exception("Filestore does not belong to this document's owner");
            var storeName = filestore.Name
                ?? throw new Exception("Filestore has no name (not created in Gemini?)");

            // The process can stop after Gemini commits an upload but before UploadedAt is saved.
            // If the named active remote copy already carries this exact content hash, adopt it
            // instead of uploading a duplicate and leaving the queue spinner running forever.
            if (doc.Name != null)
            {
                try
                {
                    var remote = GeminiRemoteDocument.From(
                        await client.GetDocumentAsync(doc.Name, token).ConfigAwait());
                    if (remote.State == "STATE_ACTIVE" && remote.MetadataHash == doc.Hash
                        && remote.DisplayName == doc.DisplayName
                        && !GeminiMetadata.Differs(doc, ChatDtos.ParseJson(remote.CustomMetadata) as JsonArray))
                    {
                        remote.ApplyTo(doc);
                        db.UpdateDocument(doc);
                        await CleanupRemoteCopiesAsync(doc, token).ConfigAwait();
                        ctx.Log.LogInformation("Recovered completed Gemini upload for {Document}", doc.DisplayName);
                        return true;
                    }
                }
                catch (GeminiApiException e) when (e.StatusCode is 404 or 410) { }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    ctx.Log.LogWarning(e, "Could not verify prior Gemini upload for {Document}", doc.DisplayName);
                }
            }

            if (doc.Url == null || !doc.Url.StartsWith(GeminiExtension.CacheUrlBase))
                throw new Exception("Invalid URL");
            var fullPath = ctx.GetCachePath(doc.Url[GeminiExtension.CacheUrlBase.Length..]);
            if (!File.Exists(fullPath))
                throw new Exception("File not found on disk");

            ctx.Log.LogInformation("Uploading {Document} to {Store}", doc.DisplayName, storeName);
            var priorName = doc.Name;
            doc.StartedAt = DateTime.Now;
            db.UpdateDocument(doc);

            var config = new JsonObject
            {
                ["displayName"] = doc.DisplayName,
                ["customMetadata"] = GeminiMetadata.ToCustomMetadata(doc),
            };

            if (doc.SourceId is { } sourceId && db.GetSource(sourceId, doc.User)?.Chunking is { Length: > 0 } chunking)
            {
                if (ChatJson.TryParseObject(chunking) is { } chunkingConfig)
                    config["chunkingConfig"] = chunkingConfig;
            }

            // uploads fail when mimeType is sent for some types (e.g. application/json) but succeed
            // without it, so only the configured overrides are declared (Python parity)
            var ext = fullPath.LastRightPart('.');
            var mimeTypeOverride = includeMimeTypes.GetValueOrDefault(ext);
            if (mimeTypeOverride != null)
                config["mimeType"] = mimeTypeOverride;

            var contentType = mimeTypeOverride
                ?? doc.MimeType
                ?? MimeTypes.GetMimeType(doc.DisplayName ?? fullPath);

            ctx.Log.LogDebug("Uploading {Document} to {Store}\n{Config}",
                doc.DisplayName, storeName, config.ToJsonString(ChatJson.Indented));

            var operation = await UploadWithRetryAsync(storeName, fullPath, config, contentType, token).ConfigAwait();
            operation = await client.WaitForOperationAsync(operation, token).ConfigAwait();

            if (operation.GetObject("error") is { } error)
                throw new Exception(error.GetString("message") ?? "Gemini upload failed");

            var response = operation.GetObject("response");
            var documentName = response.GetString("documentName") ?? response.GetString("name")
                ?? throw new Exception("Gemini upload did not return a document name");

            doc.UploadedAt = DateTime.Now;
            doc.StartedAt = null;
            doc.Name = documentName;
            doc.Error = null;
            if (priorName != null && priorName != documentName)
                doc.PendingDeleteNames = GeminiMetadata.JsonArrayOf(GeminiMetadata.AsList(doc.PendingDeleteNames).Append(priorName));
            db.UpdateDocument(doc);

            // read the document back for its assigned state/size/metadata
            try
            {
                var remote = GeminiRemoteDocument.From(
                    await client.GetDocumentAsync(documentName, token).ConfigAwait());
                remote.ApplyTo(doc);
                db.UpdateDocument(doc);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // The upload operation succeeded and the local row already points at it. A
                // follow-up read is enrichment, so a transient GET must not turn a live document
                // into a permanently failed queue item.
                ctx.Log.LogWarning(e, "Uploaded document {Name}, but could not refresh its remote fields", documentName);
            }

            await CleanupRemoteCopiesAsync(doc, token).ConfigAwait();
            return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw; // shutting down: leave the document queued for the next run
        }
        catch (Exception e)
        {
            ctx.Log.LogError(e, "Failed to upload document {Id}", doc.Id);
            db.UpdateDocumentError(doc.Id, ChatJson.ToErrorMessage(e));
            return false;
        }
    }

    async Task<JsonObject> UploadWithRetryAsync(string storeName, string fullPath, JsonObject config,
        string contentType, CancellationToken token)
    {
        Exception? last = null;
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await client.UploadToFileSearchStoreAsync(storeName, fullPath, config, contentType, token)
                    .ConfigAwait();
            }
            catch (Exception e) when (!token.IsCancellationRequested)
            {
                last = e;
                if (attempt == maxRetries - 1 || !IsRetryable(e)) throw;
                var rateLimited = IsRateLimited(e);
                var seconds = Math.Min(60, Math.Pow(2, attempt) * (rateLimited ? 5 : 1));
                seconds += Random.Shared.NextDouble() * seconds * .25;
                ctx.Log.LogInformation("Gemini upload retry {Attempt}/{Max} in {Delay:F1}s: {Message}",
                    attempt + 1, maxRetries, seconds, e.Message);
                await Task.Delay(TimeSpan.FromSeconds(seconds), token).ConfigAwait();
            }
        }
        throw last ?? new Exception("Gemini upload failed");
    }

    static bool IsRateLimited(Exception e)
    {
        var message = e.Message.ToLowerInvariant();
        return e is GeminiApiException { StatusCode: 429 }
            || message.Contains("resource_exhausted") || message.Contains("rate limit") || message.Contains("quota");
    }

    static bool IsRetryable(Exception e)
    {
        if (e is GeminiApiException { StatusCode: 500 or 502 or 503 or 504 }) return true;
        var message = e.Message.ToLowerInvariant();
        return IsRateLimited(e) || message.Contains("unavailable") || message.Contains("deadline");
    }
}
