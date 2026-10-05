using System.Text.Json.Nodes;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

public partial class GeminiExtension
{
    Task<object?> CountDocumentsAsync(ChatRequestContext req) =>
        Task.FromResult<object?>(new JsonObject { ["count"] = db.CountDocuments(QueryOf(req), UserOf(req)) });

    Task<object?> FilestoreFacetsAsync(ChatRequestContext req)
    {
        var fields = req.QueryString("fields")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return Task.FromResult<object?>(db.DocumentFacets(IdOf(req), fields, UserOf(req)));
    }

    static JsonObject BulkSelector(JsonObject body)
    {
        if (!body.ContainsKey("ids") && body.GetObject("filter") is not { Count: > 0 })
            throw new ArgumentException("Either 'ids' or 'filter' is required");
        return body;
    }

    static JsonArray BulkChanges(JsonObject body)
    {
        var changes = body.GetArray("changes");
        if (changes == null)
        {
            changes = new JsonArray(new JsonObject
            {
                ["field"] = body.GetString("field"), ["op"] = body.GetString("op") ?? "fill",
                ["value"] = body["value"]?.DeepClone(),
            });
        }
        foreach (var change in changes.OfType<JsonObject>())
        {
            var field = change.GetString("field") ?? throw new ArgumentException("field is required");
            var op = change.GetString("op") ?? "fill";
            if (!GeminiDb.BulkColumns.Contains(field)) throw new ArgumentException($"'{field}' is not bulk-editable");
            if (!GeminiDb.BulkOps.Contains(op)) throw new ArgumentException($"Unknown op '{op}'");
            change["op"] = op;
        }
        if (changes.Count == 0) throw new ArgumentException("'changes' is required");
        return changes;
    }

    async Task<object?> BulkDocumentsAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var docs = db.SelectDocuments(BulkSelector(body), UserOf(req));
        var changes = BulkChanges(body);
        var ret = db.BulkPreview(docs, changes, apply: !body.GetBool("dryRun"));
        if (!body.GetBool("dryRun") && ret.GetInt("changed") > 0) searchWorker?.Start();
        if (body.GetBool("dryRun")) ret["dryRun"] = true;
        return ret;
    }

    const string RootDeleteMessage = "Cannot delete the filestore root or all its documents. Delete the filestore using its confirmation dialog instead.";
    static readonly ConcurrentDictionary<string, SemaphoreSlim> ManualDeletionLocks = new(StringComparer.Ordinal);
    void AssertSafeDocumentDelete(List<ChatDocument> docs, string? user, JsonObject? selector = null)
    {
        var filter = selector?.GetObject("filter") ?? selector;
        if (filter?.ContainsKey("categoryUnder") == true)
        {
            var path = (filter.GetString("categoryUnder") ?? "").Trim().Trim('/', '\\');
            if (path.Length == 0 || path is "." or "..") throw HttpError.BadRequest(RootDeleteMessage);
        }
        foreach (var group in docs.Where(x => x.FilestoreId > 0).GroupBy(x => x.FilestoreId))
        {
            var visible = group.Count(x => x.TombstonedAt == null);
            if (group.Count() >= db.CountDocuments(new JsonObject { ["filestoreId"] = group.Key, ["includeTombstoned"] = true }, user)
                || visible > 0 && visible >= db.CountDocuments(new JsonObject { ["filestoreId"] = group.Key }, user))
                throw HttpError.BadRequest(RootDeleteMessage);
        }
    }
    async Task<object?> SummarizeDocumentsAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var body = BulkSelector(await req.GetJsonBodyAsync().ConfigAwait());
        var docs = db.SelectDocuments(body, UserOf(req), includeTombstoned: true);
        var summary = db.DocumentSummary(docs, body.GetArray("fields"));
        try { AssertSafeDocumentDelete(docs, UserOf(req), body); summary["deleteAllowed"] = true; }
        catch (HttpError e) { summary["deleteAllowed"] = false; summary["deleteError"] = e.Message; }
        return summary;
    }
    async Task RemoveDocumentAsync(ChatDocument doc, string? user, CancellationToken token)
    {
        foreach (var name in GeminiMetadata.AsList(doc.PendingDeleteNames).Concat(doc.Name == null ? [] : new[] { doc.Name }).Distinct(StringComparer.Ordinal))
        {
            try { await client.DeleteDocumentAsync(name, token).ConfigAwait(); }
            catch (GeminiApiException e) when (e.StatusCode is 404 or 410) { }
        }
        db.DeleteDocument(doc.Id, user);
    }
    async Task<object?> DeleteDocumentsAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var body = BulkSelector(await req.GetJsonBodyAsync().ConfigAwait());
        var user = UserOf(req);
        var gate = ManualDeletionLocks.GetOrAdd(ImportLockKey(user), _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(req.Request.RequestAborted).ConfigAwait();
        try
        {
            var docs = db.SelectDocuments(body, user, includeTombstoned: true);
            AssertSafeDocumentDelete(docs, user, body);
            using var limit = new SemaphoreSlim(4, 4);
            var results = await Task.WhenAll(docs.Select(async doc =>
            {
                await limit.WaitAsync(req.Request.RequestAborted).ConfigAwait();
                try { await RemoveDocumentAsync(doc, user, req.Request.RequestAborted).ConfigAwait(); return (JsonObject?)null; }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    return new JsonObject { ["id"] = doc.Id, ["displayName"] = doc.DisplayName, ["error"] = ChatJson.ToErrorMessage(e) };
                }
                finally { limit.Release(); }
            })).ConfigAwait();
            var deleted = docs.Where((doc, index) => results[index] == null).ToArray();
            foreach (var storeId in deleted.Select(x => x.FilestoreId).Distinct())
                await stores.RefreshAsync(storeId, user, req.Request.RequestAborted).ConfigAwait();
            return new JsonObject { ["selected"] = docs.Count, ["deleted"] = deleted.Length,
                ["ids"] = new JsonArray(deleted.Select(x => (JsonNode)x.Id).ToArray()),
                ["errors"] = new JsonArray(results.Where(x => x != null).Select(x => (JsonNode)x!).ToArray()) };
        }
        finally { gate.Release(); }
    }

    Task<object?> PendingDocumentsAsync(ChatRequestContext req)
    {
        long? storeId = long.TryParse(req.QueryString("filestoreId"), out var id) ? id : null;
        var pending = db.PendingMetadata(storeId, UserOf(req));
        var fieldCounts = pending.SelectMany(x => x.Fields).GroupBy(x => x)
            .OrderByDescending(x => x.Count()).Select(x => (JsonNode)new JsonObject
                { ["field"] = x.Key, ["count"] = x.Count() }).ToArray();
        var uploading = storeId == null ? 0 : db.CountDocuments(new JsonObject
        {
            ["filestoreId"] = storeId.Value, ["null"] = "uploadedAt,error",
        }, UserOf(req));
        return Task.FromResult<object?>(new JsonObject
        {
            ["count"] = pending.Count, ["uploading"] = uploading,
            ["ids"] = new JsonArray(pending.Select(x => (JsonNode)x.Doc.Id).ToArray()),
            ["fields"] = new JsonArray(fieldCounts),
            ["neverPushed"] = pending.Count(x => string.IsNullOrEmpty(x.Doc.CustomMetadata)),
            ["sources"] = storeId == null ? new JsonObject() : SourceUploadCounts(storeId.Value, UserOf(req)),
            ["worker"] = worker?.Status() ?? new JsonObject { ["running"] = false },
        });
    }

    async Task<object?> ReindexDocumentsAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var pending = db.PendingMetadata(IdOf(req), UserOf(req));
        // Also recover documents incorrectly classified by an earlier store sync while their
        // initial upload was still pending. These have no UploadedAt, so PendingMetadata omits them.
        var missingUploads = db.QueryAllDocuments(IdOf(req), UserOf(req))
            .Where(x => x.State == "MISSING_FROM_REMOTE" && x.TombstonedAt == null)
            .ToList();
        if (body.ContainsKey("ids"))
        {
            var ids = body.GetArray("ids") ?? throw HttpError.BadRequest("ids must be an array");
            var wanted = ids.Select(x => x!.GetValue<long>()).ToHashSet();
            pending = pending.Where(x => wanted.Contains(x.Doc.Id)).ToList();
            missingUploads = missingUploads.Where(x => wanted.Contains(x.Id)).ToList();
        }
        foreach (var row in pending) db.ResetDocumentUpload(row.Doc.Id);
        foreach (var doc in missingUploads)
        {
            db.ResetDocumentUpload(doc.Id);
            db.UpdateDocumentState(doc.Id, "STATE_PENDING");
        }
        worker?.Start();
        var queuedIds = pending.Select(x => x.Doc.Id).Concat(missingUploads.Select(x => x.Id)).Distinct().ToArray();
        Log.LogInformation("Queued {Count} Gemini documents for upload ({Recovered} recovered from MISSING_FROM_REMOTE)",
            queuedIds.Length, missingUploads.Count);
        return new JsonObject { ["queued"] = queuedIds.Length, ["recovered"] = missingUploads.Count,
            ["ids"] = new JsonArray(queuedIds.Select(x => (JsonNode)x).ToArray()) };
    }

    JsonObject SourceUploadCounts(long storeId, string? user)
    {
        var counts = new JsonObject();
        foreach (var group in db.QueryAllDocuments(storeId, user).Where(x => x.TombstonedAt == null).GroupBy(x => x.SourceId))
            counts[group.Key?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "None"] = new JsonObject
            {
                ["sourceId"] = group.Key,
                ["pending"] = group.Count(x => x.UploadedAt == null && x.Error == null),
                ["uploading"] = group.Count(x => x.UploadedAt == null && x.Error == null && x.StartedAt != null),
                ["failed"] = group.Count(x => x.Error != null),
                ["total"] = group.Count(),
            };
        return counts;
    }
    async Task<object?> ResumeUploadsAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        if (db.GetFilestore(IdOf(req), UserOf(req)) == null) throw HttpError.NotFound("Filestore does not exist");
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var query = new JsonObject { ["filestoreId"] = IdOf(req), ["null"] = "uploadedAt,error,tombstonedAt" };
        foreach (var key in new[] { "ids", "category", "categoryUnder" }) if (body.ContainsKey(key)) query[key] = body[key]?.DeepClone();
        var docs = db.SelectDocuments(query, UserOf(req));
        if (docs.Count > 0) worker?.Start();
        return new JsonObject { ["queued"] = docs.Count, ["ids"] = new JsonArray(docs.Select(x => (JsonNode)x.Id).ToArray()),
            ["worker"] = worker?.Status() ?? new JsonObject { ["running"] = false } };
    }

    Task<object?> WorkerStatusAsync(ChatRequestContext req) =>
        Task.FromResult<object?>(worker?.Status() ?? new JsonObject { ["running"] = false });

    async Task<object?> CancelWorkerAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        worker?.Cancel();
        return worker?.Status() ?? new JsonObject { ["running"] = false };
    }
}
