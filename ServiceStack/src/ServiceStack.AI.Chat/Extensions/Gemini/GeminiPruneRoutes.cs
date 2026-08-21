using ServiceStack.Text;

namespace ServiceStack.AI;

public partial class GeminiExtension
{
    /// <summary>Remove unreachable duplicate remote copies, retaining the newest copy per content hash.</summary>
    async Task<object?> PruneFilestoreAsync(ChatRequestContext req)
    {
        await AssertWriteAsync(req).ConfigAwait();
        var id = IdOf(req);
        var user = UserOf(req);
        var store = db.GetFilestore(id, user) ?? throw new Exception("Filestore does not exist");
        if (store.Name == null) throw new Exception("Filestore has no Gemini resource name");
        var dryRunText = req.QueryString("dryRun");
        var dryRun = dryRunText is not null && dryRunText is not "" and not "0" and not "false";

        var remote = (await client.ListDocumentsAsync(store.Name).ConfigAwait())
            .Select(GeminiRemoteDocument.From).Where(x => x.MetadataHash != null)
            .ToList();
        var remoteByHash = System.Linq.Enumerable.GroupBy(remote, x => x.MetadataHash!, StringComparer.Ordinal);
        var localById = db.QueryAllDocuments(id, user).ToDictionary(x => x.Id);
        var samples = new JsonArray();
        var errors = new JsonArray();
        var documents = 0;
        var removed = 0;

        foreach (var group in remoteByHash)
        {
            var copies = group.OrderByDescending(x => ParseRemoteTime(x.CreateTime)).ToList();
            if (copies.Count < 2) continue;
            documents++;
            var keep = copies[0];
            if (!dryRun && keep.MetadataId is { } localId && localById.TryGetValue(localId, out var local)
                && keep.Name != null && local.Name != keep.Name)
            {
                keep.ApplyTo(local);
                db.UpdateDocument(local);
            }
            foreach (var extra in copies.Skip(1))
            {
                if (samples.Count < 5) samples.Add(extra.DisplayName ?? extra.Name);
                if (dryRun) { removed++; continue; }
                try
                {
                    if (extra.Name != null) await client.DeleteDocumentAsync(extra.Name).ConfigAwait();
                    removed++;
                }
                catch (GeminiApiException e) when (e.StatusCode == 404) { removed++; }
                catch (Exception e)
                {
                    errors.Add(new JsonObject { ["name"] = extra.Name, ["error"] = ChatJson.ToErrorMessage(e) });
                }
            }
        }
        if (!dryRun && removed > 0) await stores.RefreshAsync(id, user).ConfigAwait();
        return new JsonObject
        {
            ["dryRun"] = dryRun, ["documents"] = documents, ["removed"] = removed,
            ["samples"] = samples, ["errors"] = errors,
        };
    }

    static DateTime ParseRemoteTime(string? value) => DateTime.TryParse(value, out var time) ? time : DateTime.MinValue;
}
