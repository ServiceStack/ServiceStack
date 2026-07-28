using Microsoft.Extensions.Logging;
using ServiceStack.Text;

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
    CancellationTokenSource? cts;

    public bool Running { get; private set; }

    public GeminiUploadWorker(ExtensionContext ctx, GeminiDb db, GeminiClient client, GeminiStores stores)
    {
        this.ctx = ctx;
        this.db = db;
        this.client = client;
        this.stores = stores;

        var mimeTypes = ctx.Feature.ResolveVariable("$GEMINI_UPLOAD_MIME_TYPES") ?? DefaultMimeTypes;
        foreach (var entry in mimeTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var ext = entry.LeftPart(':').Trim().TrimStart('.');
            var mimeType = entry.RightPart(':').Trim();
            if (ext.Length > 0 && mimeType.Length > 0)
                includeMimeTypes[ext] = mimeType;
        }
    }

    public void Start()
    {
        lock (syncRoot)
        {
            if (Running)
                return;
            Running = true;
            cts = new CancellationTokenSource();
        }
        _ = Task.Run(() => RunAsync(cts.Token));
    }

    public void Stop()
    {
        lock (syncRoot)
        {
            cts?.Cancel();
        }
    }

    async Task RunAsync(CancellationToken token)
    {
        try
        {
            ctx.Log.LogInformation("Gemini UploadWorker started");
            // documents already handled in this run: updates may not be visible to the next read yet
            var completed = new HashSet<long>();
            var filestoreIds = new HashSet<long>();

            while (!token.IsCancellationRequested)
            {
                var pending = db.GetPendingDocuments()
                    .Where(x => !completed.Contains(x.Id))
                    .ToList();
                if (pending.Count == 0)
                    break;

                foreach (var doc in pending)
                {
                    if (token.IsCancellationRequested)
                        break;
                    await ProcessDocumentAsync(doc, token).ConfigAwait();
                    completed.Add(doc.Id);
                    filestoreIds.Add(doc.FilestoreId);
                }
            }

            // the uploads changed each store's document counts + size
            if (!token.IsCancellationRequested)
            {
                foreach (var filestoreId in filestoreIds)
                {
                    await stores.RefreshAsync(filestoreId, null, token).ConfigAwait();
                }
            }
        }
        catch (OperationCanceledException)
        {
            ctx.Log.LogInformation("Gemini UploadWorker cancelled");
        }
        catch (Exception e)
        {
            ctx.Log.LogError(e, "Gemini UploadWorker failed");
        }
        finally
        {
            lock (syncRoot)
            {
                Running = false;
                cts?.Dispose();
                cts = null;
            }
            ctx.Log.LogInformation("Gemini UploadWorker stopped");
        }
    }

    async Task ProcessDocumentAsync(ChatDocument doc, CancellationToken token)
    {
        try
        {
            // a document uploaded before auth was enabled lives in another partition, so fall back to any
            var filestore = db.GetFilestore(doc.FilestoreId, doc.User)
                ?? db.GetFilestore(doc.FilestoreId, null)
                ?? throw new Exception("Filestore not found");
            var storeName = filestore.Name
                ?? throw new Exception("Filestore has no name (not created in Gemini?)");

            if (doc.Url == null || !doc.Url.StartsWith(GeminiExtension.CacheUrlBase))
                throw new Exception("Invalid URL");
            var fullPath = ctx.GetCachePath(doc.Url[GeminiExtension.CacheUrlBase.Length..]);
            if (!File.Exists(fullPath))
                throw new Exception("File not found on disk");

            ctx.Log.LogInformation("Uploading {Document} to {Store}", doc.DisplayName, storeName);
            doc.StartedAt = DateTime.Now;
            db.UpdateDocument(doc);

            var customMetadata = new JsonArray
            {
                new JsonObject { ["key"] = "id", ["numericValue"] = doc.Id },
                new JsonObject { ["key"] = "hash", ["stringValue"] = doc.Hash },
            };
            if (!string.IsNullOrEmpty(doc.Category))
                customMetadata.Add(new JsonObject { ["key"] = "category", ["stringValue"] = doc.Category });

            var config = new JsonObject
            {
                ["displayName"] = doc.DisplayName,
                ["customMetadata"] = customMetadata,
            };

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

            var operation = await client
                .UploadToFileSearchStoreAsync(storeName, fullPath, config, contentType, token).ConfigAwait();
            operation = await client.WaitForOperationAsync(operation, token).ConfigAwait();

            if (operation.GetObject("error") is { } error)
                throw new Exception(error.GetString("message") ?? "Gemini upload failed");

            var response = operation.GetObject("response");
            var documentName = response.GetString("documentName") ?? response.GetString("name")
                ?? throw new Exception("Gemini upload did not return a document name");

            doc.UploadedAt = DateTime.Now;
            doc.Name = documentName;
            db.UpdateDocument(doc);

            // read the document back for its assigned state/size/metadata
            var remote = GeminiRemoteDocument.From(
                await client.GetDocumentAsync(documentName, token).ConfigAwait());
            remote.ApplyTo(doc);
            db.UpdateDocument(doc);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw; // shutting down: leave the document queued for the next run
        }
        catch (Exception e)
        {
            ctx.Log.LogError(e, "Failed to upload document {Id}", doc.Id);
            db.UpdateDocumentError(doc.Id, ChatJson.ToErrorMessage(e));
        }
    }
}
