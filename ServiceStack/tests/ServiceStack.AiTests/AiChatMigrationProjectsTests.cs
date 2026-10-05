#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.OrmLite;

namespace ServiceStack.AiTests;

public class AiChatMigrationProjectsTests
{
    static JsonObject Details(string name = "My App", string folder = "my-app", string? requestId = null) => new() {
        ["requestId"] = requestId ?? Guid.NewGuid().ToString(),
        ["project"] = new JsonObject { ["name"] = name, ["folder"] = folder, ["publish"] = "dist" },
        ["source"] = new JsonObject { ["kind"] = "new", ["initializeGit"] = false },
    };
    static async Task<JsonObject> Done(ProjectCreation creation, string user, string id)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            var snapshot = await creation.GetAsync(user, id, null, default);
            if (snapshot.GetString("state") is "succeeded" or "failed" or "cancelled" or "interrupted") return snapshot;
            await Task.Delay(20);
        }
        Assert.Fail("Creation did not settle in five seconds"); return null!;
    }

    [TestCase("")]
    [TestCase("/chat")]
    public async Task Archive_order_and_legacy_saves_preserve_server_owned_fields(string prefix)
    {
        using var host = new AiChatMigrationTestHost(prefix);
        var projects = host.Install(new ProjectsExtension());
        var input = ChatJson.Parse("""[{"name":"One","folder":"one"},{"name":"Two","folder":"two","showInSidebar":false},{"name":"Three","folder":"three"}]""");
        var saved = (JsonArray)(await host.SendAsync("POST", prefix + "/ext/projects/projects.json", "alice", input))!;
        var id = saved[1]!["id"]!.GetValue<string>();
        host.Feature.AppData.SetUserPref("project", JsonValue.Create("Two"), "alice");
        var archived = (JsonArray)(await host.SendAsync("PATCH", prefix + "/ext/projects/archive/" + id, "alice", new JsonObject { ["archived"] = true }))!;
        Assert.That(archived.Last()!["name"]!.GetValue<string>(), Is.EqualTo("Two"));
        Assert.That(host.Feature.AppData.GetUserPrefs("alice")["project"], Is.Null);
        var activeOnly = new JsonArray(archived.Where(x => !(x as JsonObject).GetBool("archived")).Select(x => x!.DeepClone()).ToArray());
        var legacy = (JsonArray)(await host.SendAsync("POST", prefix + "/ext/projects/projects.json", "alice", activeOnly))!;
        Assert.That(legacy.Count, Is.EqualTo(3));
        Assert.That(legacy.OfType<JsonObject>().Single(x => x.GetString("id") == id).GetBool("archived"), Is.True);
        var stale = Assert.ThrowsAsync<HttpError>(async () => await host.SendAsync("POST", prefix + "/ext/projects/order", "alice", new JsonObject { ["ids"] = new JsonArray(id) }));
        Assert.That((int)stale!.StatusCode, Is.EqualTo(409));
        Assert.ThrowsAsync<HttpError>(async () => await host.SendAsync("POST", prefix + "/ext/projects/active", "alice", new JsonObject { ["name"] = "Two" }));
        Assert.ThrowsAsync<HttpError>(async () => await host.SendAsync("PATCH", prefix + "/ext/projects/sidebar/" + id, "alice", new JsonObject { ["showInSidebar"] = true }));
        var restored = (JsonArray)(await host.SendAsync("PATCH", prefix + "/ext/projects/archive/" + id, "alice", new JsonObject { ["archived"] = false }))!;
        Assert.That(restored.Last()!["id"]!.GetValue<string>(), Is.EqualTo(id));
        Assert.That((restored.Last() as JsonObject).GetBool("showInSidebar"), Is.False);
        Assert.That(projects.GetUserProjects("bob"), Is.Empty);
    }

    [Test]
    public async Task Explorer_baseline_is_the_users_own_workspace_never_a_project_or_another_users()
    {
        using var host = new AiChatMigrationTestHost();
        var shared = Path.Combine(host.DirectoryPath, "shared"); Directory.CreateDirectory(shared);
        host.Feature.Tools.AllowedDirectories = [shared];
        var projects = host.Install(new ProjectsExtension());
        string[] Roots(JsonObject workspace) => workspace["directories"]!.AsArray().Select(x => x!.GetValue<string>()).ToArray();
        var alice = Path.Combine(host.DirectoryPath, "user", "alice", "workspace");
        var bob = Path.Combine(host.DirectoryPath, "user", "bob", "workspace");
        var saved = (JsonArray)(await host.SendAsync("POST", "/ext/projects/projects.json", "alice", ChatJson.Parse("""[{"name":"One","folder":"one"}]""")))!;
        await host.SendAsync("POST", "/ext/projects/active", "alice", new JsonObject { ["name"] = "One" });
        // No central workspace: each user browses only their own, regardless of the legacy selection
        Assert.That(Roots(projects.ResolveExplorerWorkspace(null, "alice")), Is.EqualTo(new[] { alice }));
        Assert.That(Roots(projects.ResolveExplorerWorkspace(null, "bob")), Is.EqualTo(new[] { bob }));
        Assert.That(Directory.Exists(bob), Is.True);
        // Anonymous requests use the 'default' user's workspace
        Assert.That(Roots(projects.ResolveExplorerWorkspace(null, null)), Is.EqualTo(new[] { Path.Combine(host.DirectoryPath, "user", "default", "workspace") }));
        // Host-shared directories are browsable by admins only (llms-py's admin fallback)
        Assert.That(Roots(projects.ResolveExplorerWorkspace(null, "bob", isAdmin: true)), Is.EqualTo(new[] { bob, shared }));
        var browsed = (JsonObject)(await host.SendAsync("GET", "/ext/projects/explorer", "bob"))!;
        Assert.That(browsed["roots"]!.AsArray().Select(x => x!.GetValue<string>()), Is.EqualTo(new[] { bob }));
        Assert.ThrowsAsync<HttpError>(async () => await host.SendAsync("GET", "/ext/projects/explorer?path=" + Uri.EscapeDataString(shared), "bob"));
        Assert.That(((JsonObject)(await host.SendAsync("GET", "/ext/projects/explorer?path=" + Uri.EscapeDataString(shared), "bob", admin: true))!).GetString("path"), Is.EqualTo(shared));
        Assert.Throws<ArgumentException>(() => projects.ResolveExplorerWorkspace(saved[0]!["id"]!.GetValue<string>(), "bob"));
        // Runs without a project use the owner's workspace plus host-shared directories, never alice's
        Assert.That(host.Feature.ResolveAllowedDirectories("bob"), Is.EqualTo(new[] { bob, shared }));
    }

    [Test]
    public void Explorer_preview_bounds_sorting_and_physical_containment()
    {
        using var host = new AiChatMigrationTestHost();
        var root = Path.Combine(host.DirectoryPath, "workspace"); Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "Z-folder"));
        File.WriteAllText(Path.Combine(root, "a.txt"), "Hello");
        File.WriteAllText(Path.Combine(root, "image.svg"), "<svg><script>unsafe()</script></svg>");
        File.WriteAllBytes(Path.Combine(root, "binary.bin"), [1, 0, 2]);
        File.WriteAllBytes(Path.Combine(root, "large.txt"), new byte[ProjectsExplorer.TextLimit + 1]);
        var text = ProjectsExplorer.Browse([root], root, Path.Combine(root, "a.txt"));
        Assert.That(text["entries"]![0]!["directory"]!.GetValue<bool>(), Is.True);
        Assert.That(text["file"]!["content"]!.GetValue<string>(), Is.EqualTo("Hello"));
        Assert.That(text["parent"], Is.Null);
        var svg = ProjectsExplorer.Browse([root], root, Path.Combine(root, "image.svg"));
        Assert.That(svg["file"]!["image"]!.GetValue<string>(), Does.StartWith("data:image/svg+xml;base64,"));
        Assert.That(svg["file"]!["content"]!.GetValue<string>(), Does.Contain("<script>"));
        Assert.That(ProjectsExplorer.Browse([root], root, Path.Combine(root, "binary.bin"))["file"]!["message"]!.GetValue<string>(), Does.Contain("Binary"));
        Assert.That(ProjectsExplorer.Browse([root], root, Path.Combine(root, "large.txt"))["file"]!["message"]!.GetValue<string>(), Does.Contain("too large"));
        Assert.Throws<HttpError>(() => ProjectsExplorer.Browse([root], host.DirectoryPath));
        if (!OperatingSystem.IsWindows())
        {
            Directory.CreateSymbolicLink(Path.Combine(root, "escape"), host.DirectoryPath);
            Assert.That(ProjectsExplorer.Browse([root])["entries"]!.AsArray().OfType<JsonObject>().Any(x => x.GetString("name") == "escape"), Is.False);
            Assert.Throws<HttpError>(() => ProjectsExplorer.Browse([root], Path.Combine(root, "escape")));
            // A looping link is skipped like an outside link instead of failing the whole listing
            File.CreateSymbolicLink(Path.Combine(root, "loop"), Path.Combine(root, "loop"));
            var names = ProjectsExplorer.Browse([root])["entries"]!.AsArray().OfType<JsonObject>().Select(x => x.GetString("name")).ToList();
            Assert.That(names, Does.Contain("a.txt").And.Not.Contain("loop"));
        }
    }

    [TestCase("")]
    [TestCase("/chat")]
    public async Task Creation_routes_are_owned_idempotent_and_persistent(string prefix)
    {
        using var host = new AiChatMigrationTestHost(prefix);
        var projects = host.Install(new ProjectsExtension());
        var options = (JsonObject)(await host.SendAsync("GET", prefix + "/ext/projects/creation/options", "alice"))!;
        Assert.That(options.GetBool("initializeGit"), Is.False);
        Assert.That(options.GetBool("clone"), Is.False);
        var details = Details();
        var result = (ChatResult)(await host.SendAsync("POST", prefix + "/ext/projects/create", "alice", details))!;
        Assert.That(result.Status, Is.EqualTo(202));
        var snapshot = ChatJson.ParseObject(result.Text!);
        var id = snapshot.GetString("id")!;
        var done = await Done(projects.Creation!, "alice", id);
        Assert.That(done.GetString("state"), Is.EqualTo("succeeded"), done.ToJsonString());
        Assert.That(Directory.Exists(Path.Combine(projects.ManagedRoot("alice"), "my-app")), Is.True);
        Assert.That(File.Exists(Path.Combine(projects.ManagedRoot("alice"), "my-app", ProjectCreation.Marker)), Is.False);
        var replay = await projects.Creation!.CreateAsync("alice", details);
        Assert.That(replay.GetString("id"), Is.EqualTo(id));
        details.GetObject("project")!["name"] = "Changed";
        Assert.ThrowsAsync<HttpError>(async () => await projects.Creation.CreateAsync("alice", details));
        Assert.ThrowsAsync<HttpError>(async () => await host.SendAsync("GET", prefix + "/ext/projects/creation/operations/" + id, "bob"));
        var bob = await projects.Creation.CreateAsync("bob", Details());
        Assert.That((await Done(projects.Creation, "bob", bob.GetString("id")!)).GetString("state"), Is.EqualTo("succeeded"));
        Assert.That(projects.GetUserProjects("alice").Count, Is.EqualTo(1));
        Assert.That(projects.GetUserProjects("bob").Count, Is.EqualTo(1));
    }

    [TestCase("CON")]
    [TestCase("com1.txt")]
    [TestCase("../escape")]
    [TestCase("bad/name")]
    [TestCase("bad.")]
    [TestCase("bad ")]
    [TestCase(".hidden")]
    [TestCase("bad:folder")]
    public void Creation_rejects_unsafe_folders_before_side_effects(string folder)
    {
        using var host = new AiChatMigrationTestHost();
        var projects = host.Install(new ProjectsExtension());
        Assert.ThrowsAsync<HttpError>(async () => await projects.Creation!.CreateAsync("alice", Details(folder: folder)));
        Assert.That(projects.GetUserProjects("alice"), Is.Empty);
    }

    [Test]
    public async Task Prepared_folder_survives_registration_failure_and_retry_recovers_it()
    {
        using var host = new AiChatMigrationTestHost();
        var projects = host.Install(new ProjectsExtension());
        var root = projects.ManagedRoot("alice"); Directory.CreateDirectory(root);
        var config = Path.Combine(root, "projects.json"); Directory.CreateDirectory(config);
        var queued = await projects.Creation!.CreateAsync("alice", Details());
        var id = queued.GetString("id")!;
        Assert.That((await Done(projects.Creation, "alice", id)).GetString("state"), Is.EqualTo("failed"));
        var row = projects.Creation.Read("alice", id);
        Assert.That(row.Prepared, Is.True);
        Assert.That(ProjectCreation.VerifiedMarker(row.Destination, id), Is.True);
        Directory.Delete(config);
        await projects.Creation.RetryAsync("alice", id);
        Assert.That((await Done(projects.Creation, "alice", id)).GetString("state"), Is.EqualTo("succeeded"));
        Assert.That(projects.GetUserProjects("alice").Count, Is.EqualTo(1));
    }

    [TestCase("showInSidebar")]
    [TestCase("description")]
    [TestCase("publish")]
    public void Explicit_null_project_fields_are_validation_errors(string field)
    {
        using var host = new AiChatMigrationTestHost();
        var projects = host.Install(new ProjectsExtension());
        var data = Details(); data.GetObject("project")![field] = null;
        var error = Assert.ThrowsAsync<HttpError>(async () => await projects.Creation!.CreateAsync("alice", data));
        Assert.That((int)error!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public void Invalid_source_shapes_and_null_initialization_are_validation_errors()
    {
        using var host = new AiChatMigrationTestHost();
        var projects = host.Install(new ProjectsExtension());
        foreach (var source in new JsonNode?[] { null, new JsonArray(), new JsonObject { ["kind"] = "new", ["initializeGit"] = null } })
        {
            var data = Details(); data["source"] = source;
            var error = Assert.ThrowsAsync<HttpError>(async () => await projects.Creation!.CreateAsync("alice", data));
            Assert.That((int)error!.StatusCode, Is.EqualTo(400));
        }
    }

    sealed class BlockingProvisioner : IGitProvisioner
    {
        public bool InitializeGit => true;
        public bool Clone => false;
        public TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Complete = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task ProvisionAsync(string directory, JsonObject source, string user, System.Threading.CancellationToken token)
        {
            Started.TrySetResult();
            await Complete.Task.WaitAsync(token);
            Directory.CreateDirectory(directory);
        }
    }
    [Test]
    public async Task Live_reservations_cancel_release_and_retry_without_losing_the_operation()
    {
        using var host = new AiChatMigrationTestHost();
        var projects = host.Install(new ProjectsExtension());
        var git = new BlockingProvisioner(); host.Feature.GitProvisioner = git;
        var data = Details(); data.GetObject("source")!["initializeGit"] = true;
        var created = await projects.Creation!.CreateAsync("alice", data);
        var id = created.GetString("id")!;
        await git.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.ThrowsAsync<HttpError>(async () => await projects.Creation.CreateAsync("alice", Details()));
        await projects.Creation.CancelAsync("alice", id);
        Assert.That((await Done(projects.Creation, "alice", id)).GetString("state"), Is.EqualTo("cancelled"));
        git.Complete.TrySetResult();
        await projects.Creation.RetryAsync("alice", id);
        Assert.That((await Done(projects.Creation, "alice", id)).GetString("state"), Is.EqualTo("succeeded"));
        using var db = host.Db.OpenDb();
        Assert.That(db.Count<ChatProjectCreationReservation>(), Is.Zero);
    }

    [Test]
    public async Task Shutdown_leaves_an_unstarted_creation_queued_for_restart()
    {
        // llms-py recover parity: only a started attempt is interrupted; a queued one resumes later
        using var host = new AiChatMigrationTestHost();
        var projects = host.Install(new ProjectsExtension());
        var id = Guid.NewGuid().ToString();
        var details = Details(); details.GetObject("project")!["id"] = Guid.NewGuid().ToString();
        using (var db = host.Db.OpenDb()) db.Insert(new ChatProjectCreation {
            Id = id, User = "alice", RequestId = Guid.NewGuid().ToString(), Fingerprint = "fixture", Payload = new JsonObject { ["project"] = details["project"]!.DeepClone(), ["source"] = details["source"]!.DeepClone() }.ToJsonString(),
            Name = "My App", Destination = Path.Combine(projects.ManagedRoot("alice"), "my-app"), State = "queued", Owner = "live-process", Lease = DateTime.UtcNow.AddMinutes(5),
            Created = DateTime.UtcNow, Updated = DateTime.UtcNow, Temporary = ".create-" + id,
        });
        await projects.Creation!.StartAsync();
        await Task.Delay(100); // waiting for the other owner's lease, never claimed here
        await projects.Creation.StopAsync();
        Assert.That(projects.Creation.Read("alice", id).State, Is.EqualTo("queued"));
    }

    [Test]
    public async Task Recovery_retains_uncertain_folders_and_forged_markers_are_rejected()
    {
        using var host = new AiChatMigrationTestHost();
        var projects = host.Install(new ProjectsExtension());
        var id = Guid.NewGuid().ToString();
        var details = Details(); details.GetObject("project")!["id"] = Guid.NewGuid().ToString();
        var temporary = Path.Combine(projects.ManagedRoot("alice"), ".create-" + id);
        Directory.CreateDirectory(temporary); File.WriteAllText(Path.Combine(temporary, ProjectCreation.Marker), id);
        using (var db = host.Db.OpenDb()) db.Insert(new ChatProjectCreation {
            Id = id, User = "alice", RequestId = Guid.NewGuid().ToString(), Fingerprint = "fixture", Payload = new JsonObject { ["project"] = details["project"]!.DeepClone(), ["source"] = details["source"]!.DeepClone() }.ToJsonString(),
            Name = "My App", Destination = Path.Combine(projects.ManagedRoot("alice"), "my-app"), State = "running", Owner = "previous-process", Lease = DateTime.UtcNow.AddMinutes(-1),
            Created = DateTime.UtcNow, Updated = DateTime.UtcNow, Temporary = ".create-" + id,
        });
        await projects.Creation!.StartAsync();
        Assert.That((await Done(projects.Creation, "alice", id)).GetString("state"), Is.EqualTo("interrupted"));
        Assert.That(Directory.Exists(temporary), Is.True);
        var work = Path.Combine(temporary, "workspace"); Directory.CreateDirectory(work);
        File.WriteAllText(Path.Combine(work, ProjectCreation.Marker), "another-operation");
        Assert.ThrowsAsync<HttpError>(async () => await projects.RegisterCreatedAsync(details.GetObject("project")!, "alice", work, id));
        Assert.That(Directory.Exists(work), Is.True);
    }
}
