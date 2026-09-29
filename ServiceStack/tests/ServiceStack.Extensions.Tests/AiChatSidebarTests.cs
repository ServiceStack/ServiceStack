#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.OrmLite;

namespace ServiceStack.Extensions.Tests;

/// <summary>
/// Project sidebar, thread metadata and title contracts (ports of llms-py
/// tests/test_chat_threads_refactor.py).
/// </summary>
public class AiChatSidebarTests
{
    ChatDb db = null!;

    [SetUp]
    public void SetUp()
    {
        var dbFactory = new OrmLiteConnectionFactory(
            $"DataSource=file:sidebar{Guid.NewGuid():n}?mode=memory&cache=shared", SqliteDialect.Provider);
        db = new ChatDb(dbFactory);
        db.InitSchema();
    }

    long Create(string? project = null, string user = "alice", bool withMessages = true, string? title = "New Chat")
    {
        var id = db.InsertThread(new ChatThread
        {
            User = user, Title = title, ProjectId = project,
            Messages = withMessages ? """[{"role":"user","content":"Hello","timestamp":1}]""" : null,
        });
        if (withMessages)
            db.SyncChatMessages(id, JsonNode.Parse("""[{"role":"user","content":"Hello","timestamp":1}]""")!.AsArray());
        return id;
    }

    static List<long> Ids(JsonObject page) =>
        page["items"]!.AsArray().Select(x => x!["id"]!.GetValue<long>()).ToList();

    [Test]
    public void New_threads_get_placeholder_title_ownership_and_activity()
    {
        var id = Create(title: "New Chat");
        var row = db.GetThread(id, "alice", includeMessages: false)!;
        Assert.That(row.TitleSource, Is.EqualTo(ChatDb.TitleSources.Placeholder));
        Assert.That(row.TitleStatus, Is.EqualTo(ChatDb.TitleStatuses.Idle));
        Assert.That(row.LastActivityAt, Is.Not.Null);
        Assert.That(row.MetadataVersion, Is.EqualTo(0));

        var named = db.GetThread(Create(title: "My chat"), "alice", includeMessages: false)!;
        Assert.That(named.TitleSource, Is.EqualTo(ChatDb.TitleSources.Manual));
    }

    [Test]
    public void Migration_classifies_existing_titles_as_legacy()
    {
        using (var conn = db.OpenDb())
            conn.Insert(new ChatThread { User = "alice", Title = "Old", CreatedAt = DateTime.Now, UpdatedAt = DateTime.Now });
        db.InitSchema(); // idempotent re-run backfills pre-sidebar rows
        var row = db.QueryThreads(new JsonObject(), "alice").Single(x => x.Title == "Old");
        Assert.That(row.TitleSource, Is.EqualTo(ChatDb.TitleSources.Legacy));
        Assert.That(row.LastActivityAt, Is.Not.Null);
    }

    [Test]
    public void Scoped_keyset_pages_without_history()
    {
        var ids = Enumerable.Range(0, 12).Select(_ => Create("a")).ToList();
        Create(null);
        Create("a", user: "bob");

        var first = db.SidebarPage("alice", "a", 5);
        Assert.That(Ids(first), Is.EqualTo(ids.AsEnumerable().Reverse().Take(5)));
        Assert.That(first["hasMore"]!.GetValue<bool>(), Is.True);

        var second = db.SidebarPage("alice", "a", 10, first["nextCursor"]!.GetValue<string>());
        Assert.That(Ids(second), Is.EqualTo(ids.AsEnumerable().Reverse().Skip(5)));
        Assert.That(second["hasMore"]!.GetValue<bool>(), Is.False);
        Assert.That(second["items"]![0]!.AsObject().ContainsKey("messages"), Is.False);

        Assert.That(Ids(db.SidebarPage("alice", null)).Count, Is.EqualTo(1));
        // a cursor from one scope can't be replayed against another
        Assert.Throws<ArgumentException>(() => db.SidebarPage("alice", "b", 10, first["nextCursor"]!.GetValue<string>()));
        Assert.Throws<ArgumentException>(() => db.SidebarPage("alice", "a", 10, "not-a-cursor"));
        Assert.Throws<ArgumentException>(() => db.SidebarPage("alice", "a", 0));
    }

    [Test]
    public void Project_folders_require_messages()
    {
        var active = Create("active");
        Create("empty", withMessages: false);
        Assert.That(db.ProjectIdsWithMessages("alice"), Is.EquivalentTo(new[] { "active" }));
        Assert.That(Ids(db.SidebarPage("alice", "active")), Is.EqualTo(new[] { active }));
    }

    [Test]
    public void Sidebar_reports_only_active_run_status()
    {
        var id = Create("a");
        var runId = db.CreateAgentRun(id, "alice", "model");
        Assert.That(db.SidebarPage("alice", "a")["items"]![0]!["runStatus"]!.GetValue<string>(), Is.EqualTo("queued"));
        var run = db.GetAgentRun(runId)!;
        run.Status = AgentRunStatus.Failed;
        db.UpdateAgentRun(run);
        Assert.That(db.SidebarPage("alice", "a")["items"]![0]!["runStatus"], Is.Null);
    }

    [Test]
    public void Rename_and_move_do_not_touch_history_or_activity()
    {
        var id = Create("a");
        var before = db.GetThread(id, "alice")!;
        Assert.That(db.MoveThread(id, "b", before.MembershipVersion ?? 0, "alice"), Is.True);
        Assert.That(db.RenameThread(id, "  Renamed \n title ", "alice"), Is.True);
        var after = db.GetThread(id, "alice")!;
        Assert.That(after.ProjectId, Is.EqualTo("b"));
        Assert.That(after.Title, Is.EqualTo("Renamed title"));
        Assert.That(after.TitleSource, Is.EqualTo(ChatDb.TitleSources.Manual));
        Assert.That(after.Messages, Is.EqualTo(before.Messages));
        Assert.That(after.LastActivityAt, Is.EqualTo(before.LastActivityAt));
        Assert.That(after.MetadataVersion, Is.EqualTo(2));
        // a stale membership version is a conflict
        Assert.That(db.MoveThread(id, "c", before.MembershipVersion ?? 0, "alice"), Is.False);
        // another user can't move it
        Assert.That(db.MoveThread(id, "c", after.MembershipVersion ?? 0, "bob"), Is.False);
        Assert.Throws<ArgumentException>(() => db.RenameThread(id, "   ", "alice"));
    }

    [Test]
    public void Active_run_prevents_move_and_orphans_reconcile()
    {
        var id = Create("a");
        db.CreateAgentRun(id, "alice", "model");
        Assert.That(db.MoveThread(id, "b", 0, "alice"), Is.False);
        Assert.That(db.HasActiveRunsInProjects(["a"], "alice"), Is.True);
        db.ReconcileProjects([], "bob");
        Assert.That(db.GetThread(id, "alice", includeMessages: false)!.ProjectId, Is.EqualTo("a"));
        db.ReconcileProjects(["other"], "alice");
        Assert.That(db.GetThread(id, "alice", includeMessages: false)!.ProjectId, Is.Null);
    }

    [Test]
    public void Generated_title_only_replaces_the_fallback_it_was_generated_for()
    {
        var id = Create();
        using (var conn = db.OpenDb())
            conn.UpdateOnly(() => new ChatThread { TitleSource = ChatDb.TitleSources.Fallback }, x => x.Id == id);
        db.RenameThread(id, "Manual wins", "alice");
        Assert.That(db.CompleteGeneratedTitle(id, 0, "Generated"), Is.False);
        Assert.That(db.GetThread(id, "alice", includeMessages: false)!.Title, Is.EqualTo("Manual wins"));
    }

    [TestCase("Title: \"A\n useful title\"", ExpectedResult = "A useful title")]
    [TestCase("**Bold title**", ExpectedResult = "Bold title")]
    [TestCase("", ExpectedResult = null)]
    public string? Normalizes_titles(string input) => ThreadTitles.NormalizeTitle(input);

    static ExtensionContext TitleContext() => new(new ChatFeature
    {
        Config = JsonNode.Parse("""{"defaults":{"summarize":{"model":"fake","messages":[{"role":"system","content":"Title it"}]}}}""")!.AsObject(),
    }, "app");

    long CreateFallbackThread(string prompt)
    {
        var id = db.InsertThread(new ChatThread
        {
            User = "alice", Title = prompt, TitleSource = ChatDb.TitleSources.Fallback,
            Messages = $$"""[{"role":"user","content":"{{prompt}}","timestamp":1}]""",
        });
        db.SyncChatMessages(id, JsonNode.Parse($$"""[{"role":"user","content":"{{prompt}}","timestamp":1}]""")!.AsArray());
        return id;
    }

    [Test]
    public async Task Title_generation_is_claimed_once_and_manual_rename_wins()
    {
        var id = CreateFallbackThread("First prompt");
        var requests = new List<JsonObject>();
        var titles = new ThreadTitles(db, TitleContext(), _ => { })
        {
            RequestTitleOverride = (chat, _, _) =>
            {
                requests.Add(chat);
                db.RenameThread(id, "Manual wins", "alice");
                return Task.FromResult<string?>("Generated title");
            },
        };
        var row = db.GetThread(id, "alice", includeMessages: false)!;
        var messages = JsonNode.Parse("""[{"role":"user","content":"First prompt"}]""")!.AsArray();
        var first = titles.Enqueue(row, messages, "alice");
        Assert.That(titles.Enqueue(row, messages, "alice"), Is.Null, "a retried submission must not start a second request");
        await first!;

        Assert.That(db.GetThread(id, "alice", includeMessages: false)!.Title, Is.EqualTo("Manual wins"));
        Assert.That(requests.Count, Is.EqualTo(1));
        var sent = requests[0]["messages"]!.AsArray();
        Assert.That(sent.Select(x => x!["role"]!.GetValue<string>()), Is.EqualTo(new[] { "system", "user" }));
        Assert.That(requests[0].ContainsKey("tools"), Is.False);
    }

    [Test]
    public async Task Title_interrupted_by_a_restart_is_regenerated()
    {
        var id = CreateFallbackThread("Plan a trip");
        db.SetTitleStatus(id, ChatDb.TitleStatuses.Pending, ChatDb.TitleStatuses.Idle);
        var prompts = new List<string>();
        var notified = new TaskCompletionSource<long>();
        var titles = new ThreadTitles(db, TitleContext(), x => notified.TrySetResult(x))
        {
            RequestTitleOverride = (chat, _, _) =>
            {
                prompts.Add(chat["messages"]!.AsArray().Last()!["content"]!.GetValue<string>());
                return Task.FromResult<string?>("\"Generated title\"");
            },
        };
        titles.Start();
        Assert.That(await notified.Task.WaitAsync(TimeSpan.FromSeconds(10)), Is.EqualTo(id));

        var row = db.GetThread(id, "alice", includeMessages: false)!;
        Assert.That(prompts, Is.EqualTo(new[] { "Plan a trip" }));
        Assert.That((row.Title, row.TitleSource, row.TitleStatus),
            Is.EqualTo(("Generated title", ChatDb.TitleSources.Generated, ChatDb.TitleStatuses.Complete)));
    }

    [Test]
    public async Task Sidebar_signal_wakes_waiters_and_clears_cached_revisions()
    {
        var updates = new ThreadUpdates();
        updates.SidebarRevisions["alice"] = "cached";

        var idle = updates.NextSidebarSignalAsync();
        Assert.That(await Task.WhenAny(idle, Task.Delay(200)), Is.Not.SameAs(idle), "no change, no wakeup");

        updates.NotifySidebar();
        await idle.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(updates.SidebarRevisions, Is.Empty);

        // thread updates (titles, moves, activity, run state) also signal the sidebar
        var next = updates.NextSidebarSignalAsync();
        Assert.That(next.IsCompleted, Is.False, "a consumed signal must not carry over");
        updates.NotifyThreadUpdate(42);
        await next.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task Workspace_scope_is_isolated_across_concurrent_runs()
    {
        var feature = new ChatFeature();
        feature.SetAllowedDirectories(["/tmp/global"], "alice");

        async Task<List<string>> Run(string dir)
        {
            using (WorkspaceScope.Enter(new WorkspaceScope("p", [dir], "alice", 1)))
            {
                await Task.Delay(50);
                return await Task.Run(() => feature.ResolveAllowedDirectories("alice"));
            }
        }

        var results = await Task.WhenAll(Run("/tmp/one"), Run("/tmp/two"));
        Assert.That(results[0], Is.EqualTo(new[] { "/tmp/one" }));
        Assert.That(results[1], Is.EqualTo(new[] { "/tmp/two" }));
        // outside a run the user's standalone selection still applies, and scopes don't leak across users
        Assert.That(feature.ResolveAllowedDirectories("alice"), Is.EqualTo(new[] { "/tmp/global" }));
        using (WorkspaceScope.Enter(new WorkspaceScope("p", ["/tmp/one"], "alice", 1)))
            Assert.That(feature.ResolveAllowedDirectories("bob"), Is.Empty);
    }
}

public class AiChatProjectIdentityTests
{
    string appDataPath = null!;
    ProjectsExtension ext = null!;
    ChatFeature feature = null!;

    [SetUp]
    public void SetUp()
    {
        appDataPath = Path.Combine(Path.GetTempPath(), "chat-" + Guid.NewGuid().ToString("N"));
        feature = new ChatFeature { AppData = new ChatAppData(appDataPath) };
        ext = new ProjectsExtension();
        ext.Ctx = new ExtensionContext(feature, ext.Name);
        ext.Install(ext.Ctx);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(appDataPath))
            Directory.Delete(appDataPath, recursive: true);
    }

    [Test]
    public void Reading_assigns_and_persists_stable_ids()
    {
        var dir = Path.Combine(feature.AppData.GetUserPath("bob"), "projects");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "projects.json"), """[{"name":"One"},{"name":"Two"}]""");

        var first = ext.GetUserProjects("bob").Select(p => p.GetString("id")).ToList();
        var second = ext.GetUserProjects("bob").Select(p => p.GetString("id")).ToList();
        Assert.That(first.All(x => !string.IsNullOrEmpty(x)), Is.True);
        Assert.That(second, Is.EqualTo(first), "repeated reads must not invent different IDs");
        Assert.That(File.ReadAllText(Path.Combine(dir, "projects.json")), Does.Contain(first[0]));
    }

    [Test]
    public void Resolves_workspace_only_for_the_owners_projects()
    {
        var dir = Path.Combine(feature.AppData.GetUserPath("bob"), "projects");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "projects.json"), """[{"id":"p1","name":"My App"}]""");

        var workspace = ext.ResolveWorkspace("p1", "bob");
        Assert.That(workspace.GetString("projectId"), Is.EqualTo("p1"));
        Assert.That(workspace["directories"]![0]!.GetValue<string>(), Is.EqualTo(Path.Combine(dir, "my-app")));
        Assert.Throws<ArgumentException>(() => ext.ResolveWorkspace("p1", "eve"));
        Assert.Throws<ArgumentException>(() => ext.ResolveWorkspace("missing", "bob"));
    }
}
