#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.OrmLite;
using ServiceStack.Testing;

namespace ServiceStack.AiTests;

[NonParallelizable]
public class AiChatMigrationAppTests
{
    sealed class Projects : IProjectsApi
    {
        public List<JsonObject> Items = [new JsonObject { ["id"] = "project", ["name"] = "Project", ["showInSidebar"] = true }];
        public List<JsonObject> GetUserProjects(string? user = null) => Items;
        public JsonObject ResolveWorkspace(string id, string? user = null) => new() { ["projectId"] = id, ["directories"] = new JsonArray() };
    }
    static long Thread(AiChatMigrationTestHost host, string? project = null)
    {
        var id = host.Db.InsertThread(new ChatThread { User = "alice", ProjectId = project, Model = "model", Messages = "[]" });
        host.Db.SyncChatMessages(id, new JsonArray(new JsonObject { ["role"] = "user", ["content"] = "Hello", ["timestamp"] = 1 }));
        return id;
    }

    [TestCase("")]
    [TestCase("/chat")]
    public async Task Archives_change_revision_and_hide_groups_without_detaching_history(string prefix)
    {
        using var host = new AiChatMigrationTestHost(prefix);
        var projects = new Projects();
        host.Feature.ProjectsApi = projects;
        var app = host.Install(new AppExtension());
        var id = Thread(host, "project");
        var beforeThread = host.Db.GetThread(id, "alice")!;
        var before = (JsonObject)(await host.SendAsync("GET", prefix + "/ext/app/thread-sidebar", "alice"))!;
        Assert.That(before["projects"]!.AsArray().Count, Is.EqualTo(1));
        projects.Items[0]["archived"] = true;
        host.Feature.NotifySidebar();
        var after = (JsonObject)(await host.SendAsync("GET", prefix + "/ext/app/thread-sidebar", "alice"))!;
        Assert.That(after["projects"]!.AsArray(), Is.Empty);
        Assert.That(after.GetString("revision"), Is.Not.EqualTo(before.GetString("revision")));
        var retained = host.Db.GetThread(id, "alice")!;
        Assert.That(retained.ProjectId, Is.EqualTo("project"));
        Assert.That(retained.Messages, Is.EqualTo(beforeThread.Messages));
        Assert.That(retained.LastActivityAt, Is.EqualTo(beforeThread.LastActivityAt));
        projects.Items[0]["archived"] = false;
        app.Updates.NotifySidebar();
        var restored = (JsonObject)(await host.SendAsync("GET", prefix + "/ext/app/thread-sidebar", "alice"))!;
        Assert.That(restored["projects"]!.AsArray().Count, Is.EqualTo(1));
    }

    [TestCase("")]
    [TestCase("/chat")]
    public async Task Mutation_blocks_submission_before_messages_are_touched(string prefix)
    {
        using var host = new AiChatMigrationTestHost(prefix);
        host.Install(new AppExtension());
        var id = Thread(host);
        var before = host.Db.GetThread(id, "alice")!;
        using (await host.Feature.WorkspaceOperations.AcquireMutationAsync(host.DirectoryPath, host.DirectoryPath))
        {
            var error = Assert.ThrowsAsync<HttpError>(async () => await host.SendAsync("POST", prefix + $"/ext/app/threads/{id}/chat", "alice",
                new JsonObject { ["model"] = "model", ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = "blocked" }) }));
            Assert.That((int)error!.StatusCode, Is.EqualTo(409));
            Assert.That(host.Db.GetThread(id, "alice")!.Messages, Is.EqualTo(before.Messages));
            Assert.That(host.Db.GetActiveAgentRun(id, "alice"), Is.Null);
            Assert.That(((ChatResult)(await host.SendAsync("POST", prefix + $"/ext/app/threads/{id}/chat", null))!).Status, Is.EqualTo(401));
        }
    }

    [Test]
    public async Task Concurrent_duplicate_submissions_create_one_run_and_capture_baseline_workspace()
    {
        using var appHost = new BasicAppHost().Init();
        using var host = new AiChatMigrationTestHost();
        host.Install(new AppExtension());
        var id = Thread(host);
        host.Feature.Providers["fake"] = new FakeChatProvider(ChatJson.ParseObject("""{"choices":[{"message":{"role":"assistant","content":"Done"}}]}""")) {
            Id = "fake", Models = { ["model"] = new JsonObject { ["id"] = "model" } },
        };
        var body = new JsonObject { ["submissionId"] = "same-turn", ["model"] = "model", ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = "One accepted turn", ["timestamp"] = 2 }) };
        await Task.WhenAll(host.SendAsync("POST", $"/ext/app/threads/{id}/chat", "alice", body), host.SendAsync("POST", $"/ext/app/threads/{id}/chat", "alice", body));
        using var db = host.Db.OpenDb();
        var runs = db.Select<AgentRun>(x => x.ThreadId == id);
        Assert.That(runs.Count, Is.EqualTo(1));
        Assert.That(ChatJson.ParseObject(runs[0].Workspace!)["projectId"], Is.Null);
        Assert.That(host.Db.GetThread(id, "alice")!.LastSubmissionId, Is.EqualTo("same-turn"));
    }
}
