using System.Text.Json.Nodes;
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
        if (body.GetArray("ids") is not { Count: > 0 } && body.GetObject("filter") is not { Count: > 0 })
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
        if (body.GetBool("dryRun")) ret["dryRun"] = true;
        return ret;
    }

    async Task<object?> SummarizeDocumentsAsync(ChatRequestContext req)
    {
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var docs = db.SelectDocuments(BulkSelector(body), UserOf(req), includeTombstoned: true);
        return db.DocumentSummary(docs, body.GetArray("fields"));
    }

    async Task<object?> DeleteDocumentsAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var docs = db.SelectDocuments(BulkSelector(body), UserOf(req), includeTombstoned: true);
        var ids = new JsonArray(); var errors = new JsonArray(); var storeIds = new HashSet<long>();
        foreach (var doc in docs)
        {
            try
            {
                if (doc.Name != null)
                    await client.DeleteDocumentAsync(doc.Name).ConfigAwait();
                db.DeleteDocument(doc.Id, UserOf(req)); ids.Add(doc.Id); storeIds.Add(doc.FilestoreId);
            }
            catch (GeminiApiException e) when (e.StatusCode == 404)
            {
                db.DeleteDocument(doc.Id, UserOf(req)); ids.Add(doc.Id); storeIds.Add(doc.FilestoreId);
            }
            catch (Exception e)
            {
                errors.Add(new JsonObject { ["id"] = doc.Id, ["displayName"] = doc.DisplayName,
                    ["error"] = ChatJson.ToErrorMessage(e) });
            }
        }
        foreach (var storeId in storeIds) await stores.RefreshAsync(storeId, UserOf(req)).ConfigAwait();
        return new JsonObject { ["selected"] = docs.Count, ["deleted"] = ids.Count, ["ids"] = ids, ["errors"] = errors };
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
            ["worker"] = worker?.Status() ?? new JsonObject { ["running"] = false },
        });
    }

    async Task<object?> ReindexDocumentsAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var body = await req.GetJsonBodyAsync().ConfigAwait();
        var pending = db.PendingMetadata(IdOf(req), UserOf(req));
        if (body.GetArray("ids") is { Count: > 0 } ids)
        {
            var wanted = ids.Select(x => x!.GetValue<long>()).ToHashSet();
            pending = pending.Where(x => wanted.Contains(x.Doc.Id)).ToList();
        }
        foreach (var row in pending) db.ResetDocumentUpload(row.Doc.Id);
        worker?.Start();
        return new JsonObject { ["queued"] = pending.Count,
            ["ids"] = new JsonArray(pending.Select(x => (JsonNode)x.Doc.Id).ToArray()) };
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
