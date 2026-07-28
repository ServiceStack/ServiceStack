using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// File store operations shared by the gemini routes and its upload worker: Gemini owns the
/// authoritative document counts + size, so they're re-read and persisted locally whenever they
/// could have changed (after uploads, deletes, syncs, or when a store has none recorded yet).
/// </summary>
public class GeminiStores(GeminiDb db, GeminiClient client, ILogger log)
{
    /// <summary>
    /// Refresh a store's stats from Gemini and persist them, returning false when they're
    /// unavailable (a store that was never created remotely, or has since been deleted).
    /// </summary>
    public async Task<bool> RefreshAsync(ChatFilestore filestore, CancellationToken token = default)
    {
        if (filestore.Name == null)
            return false;
        try
        {
            var store = await client.GetFileSearchStoreAsync(filestore.Name, token).ConfigAwait();
            filestore.PopulateFrom(store);
            db.UpdateFilestore(filestore);
            return true;
        }
        catch (Exception e)
        {
            log.LogError(e, "Failed to fetch filestore stats from Gemini for {Name}", filestore.Name);
            return false;
        }
    }

    /// <summary>A null user matches any partition (the upload worker runs outside a request)</summary>
    public async Task<bool> RefreshAsync(long filestoreId, string? user, CancellationToken token = default)
    {
        var filestore = db.GetFilestore(filestoreId, user);
        return filestore != null && await RefreshAsync(filestore, token).ConfigAwait();
    }
}
