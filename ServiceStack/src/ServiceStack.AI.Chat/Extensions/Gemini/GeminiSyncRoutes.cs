using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using ServiceStack.Text;

namespace ServiceStack.AI;

// The worker and Sync share ownership of a document while changing remote copies/receipts.
static class GeminiDocumentLocks
{
    static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.Ordinal);
    public static SemaphoreSlim For(ExtensionContext ctx, long id) =>
        Locks.GetOrAdd(ctx.Feature.AppData.BasePath + "\0" + id, _ => new SemaphoreSlim(1, 1));
}

public partial class GeminiExtension
{
    sealed class SyncRepairs
    {
        public readonly List<GeminiRemoteDocument> LocalMissing = [];
        public readonly List<ChatDocument> RemoteMissing = [], MissingMetadata = [], MetadataMismatch = [],
            Unmatched = [], Duplicates = [], PendingUploads = [];
        public readonly List<string> Errors = [];
        public int Matched, RemovedCopies;
    }

    async Task<SyncRepairs> RepairDocumentsAsync(List<ChatDocument> localDocs, List<JsonObject> remoteDocs,
        string? user, CancellationToken token)
    {
        var result = new SyncRepairs();
        var byId = localDocs.ToDictionary(x => x.Id);
        var byName = localDocs.Where(x => x.Name != null).GroupBy(x => x.Name!, StringComparer.Ordinal)
            .Where(x => x.Count() == 1).ToDictionary(x => x.Key, x => x.Single(), StringComparer.Ordinal);
        var byHash = localDocs.Where(x => x.Hash != null).GroupBy(x => x.Hash!, StringComparer.Ordinal)
            .Where(x => x.Count() == 1).ToDictionary(x => x.Key, x => x.Single(), StringComparer.Ordinal);
        var copiesById = localDocs.ToDictionary(x => x.Id, _ => new List<GeminiRemoteDocument>());
        foreach (var remote in remoteDocs.Select(GeminiRemoteDocument.From))
        {
            // An explicit id takes precedence over content. Equal text in different source
            // documents is legitimate and must never cause one identity to delete another.
            var local = remote.MetadataId is { } id ? byId.GetValueOrDefault(id) : null;
            if (local == null && remote.Name != null) local = byName.GetValueOrDefault(remote.Name);
            if (local == null && remote.MetadataId == null && remote.MetadataHash != null)
                local = byHash.GetValueOrDefault(remote.MetadataHash);
            if (local == null) result.LocalMissing.Add(remote);
            else copiesById[local.Id].Add(remote);
        }

        using var deleteLimit = new SemaphoreSlim(4, 4);
        foreach (var snapshot in localDocs)
        {
            token.ThrowIfCancellationRequested();
            var gate = GeminiDocumentLocks.For(Ctx, snapshot.Id);
            // Never wait for a ten-minute upload or overwrite its operation receipt.
            if (!await gate.WaitAsync(0, token).ConfigAwait())
            {
                if (snapshot.UploadedAt == null) result.PendingUploads.Add(snapshot);
                continue;
            }
            try
            {
                var local = db.GetDocument(snapshot.Id, user);
                if (local == null || local.TombstonedAt != null) continue;
                // The worker may have finished while documents.list was being paged.
                if (local.UpdatedAt != snapshot.UpdatedAt)
                {
                    if (local.UploadedAt == null && local.Error == null) result.PendingUploads.Add(local);
                    continue;
                }
                var copies = copiesById[local.Id];
                bool Owned(GeminiRemoteDocument copy) => copy.MetadataId == local.Id || copy.Name == snapshot.Name;
                bool Current(GeminiRemoteDocument copy) => copy.MetadataId == local.Id
                    && copy.MetadataHash == local.Hash && copy.DisplayName == local.DisplayName
                    && !GeminiMetadata.Differs(local, ChatDtos.ParseJson(copy.CustomMetadata) as JsonArray);
                var keep = copies.Where(x => x.State == "STATE_ACTIVE" && Current(x))
                    .OrderByDescending(x => x.Name == local.Name)
                    .ThenByDescending(x => ParseRemoteTime(x.CreateTime)).ThenBy(x => x.Name, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (keep != null)
                {
                    if (keep.Diff(local).Count > 0) result.Unmatched.Add(local);
                    keep.ApplyTo(local);
                    local.Error = null; local.StartedAt = null;
                    local.UploadOperationName = local.UploadOperationHash = null;
                    result.Matched++;
                    // Persist the healthy copy and cleanup receipts before attempting deletion.
                    var extras = copies.Where(x => x.Name != keep.Name && Owned(x) && x.Name != null)
                        .Select(x => x.Name!).Concat(GeminiMetadata.AsList(local.PendingDeleteNames))
                        .Where(x => x != keep.Name).Distinct(StringComparer.Ordinal).ToList();
                    local.PendingDeleteNames = extras.Count == 0 ? null : GeminiMetadata.JsonArrayOf(extras);
                    db.UpdateDocument(local);
                    var deleted = await Task.WhenAll(extras.Select(async name =>
                    {
                        await deleteLimit.WaitAsync(token).ConfigAwait();
                        try
                        {
                            try { await client.DeleteDocumentAsync(name, token).ConfigAwait(); }
                            catch (GeminiApiException e) when (e.StatusCode is 404 or 410) { }
                            return (Name: name, Error: (string?)null);
                        }
                        catch (Exception e) when (e is not OperationCanceledException)
                        {
                            return (Name: name, Error: ChatJson.ToErrorMessage(e));
                        }
                        finally { deleteLimit.Release(); }
                    })).ConfigAwait();
                    var remaining = deleted.Where(x => x.Error != null).Select(x => x.Name).ToList();
                    result.RemovedCopies += deleted.Count(x => x.Error == null);
                    foreach (var error in deleted.Where(x => x.Error != null))
                        result.Errors.Add($"{FileNameOf(local)}: could not remove {error.Name}: {error.Error}");
                    if (copies.Any(x => x.Name != keep.Name && !Owned(x)))
                    {
                        result.Errors.Add($"{FileNameOf(local)}: a remote copy has ambiguous identity; it was preserved.");
                        result.Duplicates.Add(local);
                    }
                    if (remaining.Count > 0 && !result.Duplicates.Contains(local)) result.Duplicates.Add(local);
                    local.PendingDeleteNames = remaining.Count == 0 ? null : GeminiMetadata.JsonArrayOf(remaining);
                    if (result.Duplicates.Contains(local)) local.State = "DUPLICATE_FILE";
                    db.UpdateDocument(local);
                    continue;
                }

                // Keep source fields authoritative. A stale remote title/metadata must not
                // overwrite the desired replacement while repairing its identity.
                if (copies.Count == 0)
                {
                    if (local.UploadedAt != null || local.Error != null) result.RemoteMissing.Add(local);
                    local.Name = null;
                    local.State = "MISSING_FROM_REMOTE";
                }
                else if (copies.Any(x => x.MetadataId == null || x.MetadataHash == null))
                {
                    result.MissingMetadata.Add(local); local.State = "MISSING_METADATA";
                }
                else if (copies.Any(x => !Current(x)))
                {
                    result.MetadataMismatch.Add(local); local.State = "METADATA_MISMATCH";
                }
                else local.State = copies.Any(x => x.State == "STATE_FAILED") ? "STATE_FAILED" : "STATE_PENDING";

                if (!LocalFileExists(local))
                {
                    if (copies.Count > 1) { local.State = "DUPLICATE_FILE"; result.Duplicates.Add(local); }
                    result.Errors.Add($"{FileNameOf(local)}: no healthy current remote copy and the local cached file is missing.");
                    db.UpdateDocument(local);
                    continue;
                }
                // Preserve an available fallback until the replacement succeeds. The worker
                // then removes all copies whose identity is known, including old content hashes.
                var fallback = copies.Where(x => Owned(x) && x.State == "STATE_ACTIVE")
                    .OrderByDescending(x => x.Name == local.Name).ThenByDescending(x => ParseRemoteTime(x.CreateTime))
                    .FirstOrDefault() ?? copies.FirstOrDefault(x => Owned(x) && x.Name == local.Name);
                local.Name = fallback?.Name;
                var cleanup = copies.Where(x => Owned(x) && x.Name != null && x.Name != local.Name)
                    .Select(x => x.Name!).Concat(GeminiMetadata.AsList(local.PendingDeleteNames))
                    .Distinct(StringComparer.Ordinal).ToList();
                local.PendingDeleteNames = cleanup.Count == 0 ? null : GeminiMetadata.JsonArrayOf(cleanup);
                var desiredCopies = copies.Where(x => x.MetadataHash == local.Hash).ToList();
                if (desiredCopies.Count > 0 && desiredCopies.All(x => x.State == "STATE_FAILED"))
                    local.UploadOperationName = local.UploadOperationHash = null;
                local.UploadedAt = null; local.StartedAt = null; local.Error = null;
                db.UpdateDocument(local);
                result.PendingUploads.Add(local);
            }
            finally { gate.Release(); }
        }
        return result;
    }
}
