using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>Durably drains local Search work; desired/completed hashes make restarts idempotent.</summary>
public class GeminiSearchWorker
{
    static readonly HashSet<string> BinaryExtensions = ["pdf", "docx", "pptx", "xlsx"];
    readonly ExtensionContext ctx;
    readonly GeminiDb db;
    readonly object syncRoot = new();
    CancellationTokenSource? cts;
    bool restartRequested, cancelRequested;
    long total, done, failed;
    DateTime? startedAt;
    public bool Running { get; private set; }

    public GeminiSearchWorker(ExtensionContext ctx, GeminiDb db) { this.ctx = ctx; this.db = db; }

    public JsonObject Status()
    {
        lock (syncRoot) return new JsonObject
        {
            ["total"] = total, ["done"] = done, ["failed"] = failed,
            ["startedAt"] = startedAt == null ? null : ChatDb.ToDateString(startedAt.Value),
            ["running"] = Running, ["cancelled"] = cancelRequested,
        };
    }

    public void Start()
    {
        CancellationTokenSource source;
        lock (syncRoot)
        {
            restartRequested = true; cancelRequested = false;
            if (Running) return;
            Running = true; total = done = failed = 0; startedAt = DateTime.UtcNow;
            source = cts = new CancellationTokenSource();
        }
        _ = Task.Run(() => RunAsync(source));
    }

    public void Cancel() { lock (syncRoot) cancelRequested = true; }
    public void Stop() { lock (syncRoot) cts?.Cancel(); }

    async Task RunAsync(CancellationTokenSource source)
    {
        var completed = new HashSet<(long Id, string Hash)>(); var token = source.Token;
        try
        {
            while (!token.IsCancellationRequested)
            {
                lock (syncRoot) { if (cancelRequested) break; restartRequested = false; }
                List<ChatDocument> rows;
                try { rows = db.GetSearchCandidates(100).Where(x => !completed.Contains((x.Id, GeminiSearch.DesiredHash(x)))).ToList(); }
                catch (Exception e)
                {
                    ctx.Log.LogError(e, "Gemini SearchWorker failed reading its queue; retrying");
                    await Task.Delay(TimeSpan.FromSeconds(5), token).ConfigAwait(); continue;
                }
                if (rows.Count == 0)
                {
                    lock (syncRoot) { if (restartRequested) continue; Running = false; if (ReferenceEquals(cts, source)) cts = null; break; }
                }
                lock (syncRoot) total += rows.Count;
                foreach (var doc in rows)
                {
                    completed.Add((doc.Id, GeminiSearch.DesiredHash(doc)));
                    try { await IndexDocumentAsync(doc, token).ConfigAwait(); lock (syncRoot) done++; }
                    catch (Exception e)
                    {
                        ctx.Log.LogError(e, "Failed indexing document {DocumentId} for Search", doc.Id);
                        db.UpdateSearchError(doc.Id, ChatJson.ToErrorMessage(e)); lock (syncRoot) failed++;
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { ctx.Log.LogError(e, "Gemini SearchWorker failed"); }
        finally
        {
            lock (syncRoot) { if (ReferenceEquals(cts, source)) { Running = false; restartRequested = false; cts = null; } }
            source.Dispose();
        }
    }

    async Task IndexDocumentAsync(ChatDocument doc, CancellationToken token)
    {
        var desired = GeminiSearch.DesiredHash(doc);
        db.MarkSearchStarted(doc.Id);
        if (doc.Url == null || !doc.Url.StartsWith(GeminiExtension.CacheUrlBase, StringComparison.Ordinal))
            throw new InvalidOperationException("Document has no local cached content");
        var path = ctx.GetCachePath(doc.Url[GeminiExtension.CacheUrlBase.Length..]);
        if (!File.Exists(path)) throw new FileNotFoundException("Cached document content is missing", path);
        var bytes = await File.ReadAllBytesAsync(path, token).ConfigAwait();
        var filename = doc.Filename ?? doc.SourceKey ?? doc.DisplayName ?? "document.txt";
        var extracted = GeminiIngest.Extract(bytes, filename, new JsonObject { ["minWords"] = 0 });
        if (extracted.Skip != null)
        {
            var ext = Path.GetExtension(filename).TrimStart('.').ToLowerInvariant();
            if (BinaryExtensions.Contains(ext))
            {
                db.ReplaceSearchSections(doc, [], desired);
                db.UpdateSearchError(doc.Id, $"Not locally searchable: {extracted.Skip}");
                return;
            }
            throw new InvalidOperationException(extracted.Skip);
        }
        db.ReplaceSearchSections(doc, GeminiSearch.SplitSections(extracted.Text, doc,
            documentTitle: extracted.Frontmatter.GetString("title")), desired);
    }
}
