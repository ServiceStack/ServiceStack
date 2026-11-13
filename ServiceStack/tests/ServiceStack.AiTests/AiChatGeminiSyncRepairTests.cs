#nullable enable
using System.Net;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace ServiceStack.AiTests;

public partial class AiChatMigrationGeminiRouteTests
{
    static JsonObject RemoteCopy(ChatDocument doc, string name, string state = "STATE_ACTIVE", string? hash = null) => new()
    {
        ["name"] = name, ["displayName"] = doc.DisplayName, ["state"] = state,
        ["createTime"] = "2026-11-13T00:00:00Z",
        ["customMetadata"] = hash == null ? GeminiMetadata.ToCustomMetadata(doc) : new JsonArray(
            new JsonObject { ["key"] = "id", ["numericValue"] = doc.Id },
            new JsonObject { ["key"] = "hash", ["stringValue"] = hash }),
    };
    static void CacheDocument(AiChatMigrationTestHost host, GeminiDb db, ChatDocument doc)
    {
        doc.Url = "/~cache/aa/" + doc.Id + ".md";
        var path = host.Feature.AppData.GetCachePath("aa/" + doc.Id + ".md");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, "cached source");
        db.UpdateDocument(doc);
    }

    [Test]
    public async Task Sync_keeps_healthy_copy_and_removes_failed_and_active_duplicates_without_reuploading()
    {
        using var host = new AiChatMigrationTestHost();
        var remote = new JsonArray(); var deleted = new List<string>();
        var db = Install(host, (request, _) => {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Delete) { deleted.Add(path); return Task.FromResult(Response()); }
            return Task.FromResult(Response(new JsonObject { ["documents"] = remote.DeepClone() }.ToJsonString()));
        });
        var store = Store(db); var doc = Doc(db, store, "release.md");
        doc.State = "DUPLICATE_FILE"; doc.Error = "previous timeout";
        doc.UploadOperationName = "operations/obsolete"; doc.UploadOperationHash = "receipt"; db.UpdateDocument(doc);
        remote.Add(RemoteCopy(doc, doc.Name!));
        remote.Add(RemoteCopy(doc, "remote/failed", "STATE_FAILED"));
        remote.Add(RemoteCopy(doc, "remote/extra"));
        await host.Feature.RunAsyncShutdownHandlers();
        var result = (JsonObject)(await host.SendAsync("POST", "/ext/gemini/filestores/" + store + "/sync", "alice", new JsonObject()))!;
        var saved = db.GetDocument(doc.Id, "alice")!;
        Assert.Multiple(() => {
            Assert.That(deleted, Is.EquivalentTo(new[] { "/v1beta/remote/failed", "/v1beta/remote/extra" }));
            Assert.That(saved.Name, Is.EqualTo(doc.Name)); Assert.That(saved.State, Is.EqualTo("STATE_ACTIVE"));
            Assert.That(saved.Error, Is.Null); Assert.That(saved.UploadOperationName, Is.Null);
            Assert.That(saved.PendingDeleteNames, Is.Null);
            Assert.That(result.GetObject("Duplicate Documents").GetInt("count"), Is.Zero);
            Assert.That(result.GetObject("Summary").GetInt("Removed Remote Copies"), Is.EqualTo(2));
            Assert.That(result.GetObject("Pending Uploads").GetInt("count"), Is.Zero);
        });
    }

    [Test]
    public async Task Sync_preserves_distinct_document_identities_with_identical_content_and_unknown_remote_rows()
    {
        using var host = new AiChatMigrationTestHost(); var remote = new JsonArray(); var deletes = 0;
        var db = Install(host, (request, _) => { if (request.Method == HttpMethod.Delete) deletes++;
            return Task.FromResult(Response(new JsonObject { ["documents"] = remote.DeepClone() }.ToJsonString())); });
        var store = Store(db); var a = Doc(db, store, "a.md"); var b = Doc(db, store, "b.md");
        remote.Add(RemoteCopy(a, a.Name!)); remote.Add(RemoteCopy(b, b.Name!));
        var foreign = RemoteCopy(a, "remote/not-local"); foreign["customMetadata"] = new JsonArray(
            new JsonObject { ["key"] = "id", ["numericValue"] = 99999 },
            new JsonObject { ["key"] = "hash", ["stringValue"] = a.Hash }); remote.Add(foreign);
        await host.Feature.RunAsyncShutdownHandlers();
        var result = (JsonObject)(await host.SendAsync("POST", "/ext/gemini/filestores/" + store + "/sync", "alice", new JsonObject()))!;
        Assert.That(deletes, Is.Zero);
        Assert.That(result.GetObject("Duplicate Documents").GetInt("count"), Is.Zero);
        Assert.That(result.GetObject("Missing from Local").GetInt("count"), Is.EqualTo(1));
        Assert.That(db.GetDocument(a.Id, "alice")!.Name, Is.EqualTo(a.Name));
        Assert.That(db.GetDocument(b.Id, "alice")!.Name, Is.EqualTo(b.Name));
    }

    [Test]
    public async Task Sync_requeues_failed_copies_with_fresh_receipt_and_preserves_fallbacks_for_changed_metadata()
    {
        using var host = new AiChatMigrationTestHost(); var remote = new JsonArray(); var deletes = 0;
        var db = Install(host, (request, _) => { if (request.Method == HttpMethod.Delete) deletes++;
            return Task.FromResult(Response(new JsonObject { ["documents"] = remote.DeepClone() }.ToJsonString())); });
        var store = Store(db); var failed = Doc(db, store, "failed.md");
        CacheDocument(host, db, failed); failed.Error = "Indexing timed out"; failed.UploadedAt = null;
        failed.UploadOperationName = "operations/failed"; failed.UploadOperationHash = "hash"; db.UpdateDocument(failed);
        remote.Add(RemoteCopy(failed, failed.Name!, "STATE_FAILED"));
        remote.Add(RemoteCopy(failed, "remote/failed-extra", "STATE_FAILED"));
        remote.Add(RemoteCopy(failed, "remote/healthy-old-content", hash: "old-content"));
        var changed = Doc(db, store, "desired-title.md"); CacheDocument(host, db, changed);
        var stale = RemoteCopy(changed, changed.Name!); stale["displayName"] = "Old title";
        remote.Add(stale); remote.Add(RemoteCopy(changed, "remote/old-version", hash: "old-hash"));
        changed.UploadedAt = null; changed.Category = "edited"; db.UpdateDocument(changed);
        await host.Feature.RunAsyncShutdownHandlers();
        var result = (JsonObject)(await host.SendAsync("POST", "/ext/gemini/filestores/" + store + "/sync", "alice", new JsonObject()))!;
        var saved = db.GetDocument(failed.Id, "alice")!; var edited = db.GetDocument(changed.Id, "alice")!;
        Assert.Multiple(() => {
            Assert.That(deletes, Is.Zero); Assert.That(saved.UploadedAt, Is.Null); Assert.That(saved.Error, Is.Null);
            Assert.That(saved.UploadOperationName, Is.Null); Assert.That(saved.PendingDeleteNames, Does.Contain("failed-extra"));
            Assert.That(saved.Name, Is.EqualTo("remote/healthy-old-content"));
            Assert.That(edited.DisplayName, Is.EqualTo("desired-title.md")); Assert.That(edited.Category, Is.EqualTo("edited"));
            Assert.That(edited.Name, Is.EqualTo(changed.Name)); Assert.That(edited.UploadedAt, Is.Null);
            Assert.That(edited.PendingDeleteNames, Does.Contain("old-version"));
            Assert.That(result.GetObject("Pending Uploads").GetInt("count"), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task Sync_preserves_delete_failure_receipt_and_repairs_it_on_the_next_sync()
    {
        using var host = new AiChatMigrationTestHost(); var remote = new JsonArray(); var fail = true;
        var db = Install(host, (request, _) => Task.FromResult(request.Method == HttpMethod.Delete && fail
            ? Response("{\"error\":{\"message\":\"cleanup unavailable\"}}", HttpStatusCode.ServiceUnavailable)
            : Response(new JsonObject { ["documents"] = remote.DeepClone() }.ToJsonString())));
        var store = Store(db); var doc = Doc(db, store, "a.md");
        remote.Add(RemoteCopy(doc, doc.Name!)); remote.Add(RemoteCopy(doc, "remote/extra"));
        await host.Feature.RunAsyncShutdownHandlers();
        async Task<JsonObject> Sync() => (JsonObject)(await host.SendAsync("POST", "/ext/gemini/filestores/" + store + "/sync", "alice", new JsonObject()))!;
        var result = await Sync();
        Assert.That(db.GetDocument(doc.Id, "alice")!.PendingDeleteNames, Does.Contain("remote/extra"));
        Assert.That(result.GetObject("Repair Errors").GetInt("count"), Is.EqualTo(1));
        Assert.That(db.GetDocument(doc.Id, "alice")!.State, Is.EqualTo("DUPLICATE_FILE"));
        fail = false; result = await Sync();
        Assert.That(db.GetDocument(doc.Id, "alice")!.PendingDeleteNames, Is.Null);
        Assert.That(db.GetDocument(doc.Id, "alice")!.State, Is.EqualTo("STATE_ACTIVE"));
        Assert.That(result.GetObject("Repair Errors").GetInt("count"), Is.Zero);
    }

    [Test]
    public async Task Sync_does_not_wait_for_or_overwrite_an_inflight_upload()
    {
        using var host = new AiChatMigrationTestHost(); var remote = new JsonArray();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var db = Install(host, async (request, token) => {
            if (request.RequestUri!.AbsolutePath == "/v1beta/remote/a.md") {
                entered.TrySetResult(); await release.Task.WaitAsync(token); return Response(remote[0]!.ToJsonString());
            }
            return Response(new JsonObject { ["documents"] = remote.DeepClone() }.ToJsonString());
        });
        var store = Store(db); var doc = Doc(db, store, "a.md"); doc.UploadedAt = null;
        doc.UploadOperationName = "operations/inflight"; db.UpdateDocument(doc); remote.Add(RemoteCopy(doc, doc.Name!));
        var ctx = new ExtensionContext(host.Feature, "gemini"); var client = new GeminiClient(host.Feature.HttpClientFactory, "fixture");
        var worker = new GeminiUploadWorker(ctx, db, client, new GeminiStores(db, client, NullLogger.Instance));
        try {
            worker.Start(); await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
            var result = (JsonObject)(await host.SendAsync("POST", "/ext/gemini/filestores/" + store + "/sync", "alice", new JsonObject()).WaitAsync(TimeSpan.FromSeconds(2)))!;
            Assert.That(result.GetObject("Pending Uploads").GetInt("count"), Is.EqualTo(1));
            Assert.That(db.GetDocument(doc.Id, "alice")!.UploadOperationName, Is.EqualTo("operations/inflight"));
        }
        finally { release.TrySetResult(); await worker.StopAsync(); }
    }
}
