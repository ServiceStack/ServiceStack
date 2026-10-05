#nullable enable
using System.Text;
using System.Text.Json.Nodes;
using NUnit.Framework;
using ServiceStack.AI;

namespace ServiceStack.AiTests;

public class AiChatStaticPublishTests
{
    static JsonObject Settings(AiChatMigrationTestHost host, string? baseUrl = "http://127.0.0.1:8080/p") => new() {
        ["directory"] = Path.Combine(host.DirectoryPath, "public", "p"), ["baseUrl"] = baseUrl, ["basePath"] = "/p/",
    };

    static void WriteConfig(AiChatMigrationTestHost host, JsonNode? settings)
    {
        var path=Path.Combine(host.Feature.AppData.GetUserPath(null),"share_static","config.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path,settings?.ToJsonString()??"null");
    }

    static ShareStaticExtension Install(AiChatMigrationTestHost host, JsonObject? settings = null)
    {
        WriteConfig(host, settings ?? Settings(host));
        host.Install(new ProjectsExtension());
        return host.Install(new ShareStaticExtension());
    }

    static async Task<(JsonObject Project, string Source)> Project(AiChatMigrationTestHost host, ShareStaticExtension extension, string user = "alice", string? output = "dist")
    {
        var project = new JsonObject { ["name"] = "Site", ["folder"] = "site", ["publishedUrl"] = "https://remote.example/site" };
        if (output != null) project["publish"] = output;
        await host.SendAsync("POST", host.Feature.RoutePrefix + "/ext/projects/save/Site", user, project);
        project = host.Feature.ProjectsApi.GetUserProjects(user).Single();
        var source = Path.Combine(ProjectsExtension.GetProjectDir(extension.Ctx.GetUserPath(user), project), output ?? "");
        Directory.CreateDirectory(source);
        await File.WriteAllTextAsync(Path.Combine(source, "index.html"), "<html><head><title>Site</title></head><body><img src='/assets/logo.png'><a href=/page.html>Page</a></body></html>");
        Directory.CreateDirectory(Path.Combine(source, "assets"));
        await File.WriteAllBytesAsync(Path.Combine(source, "assets", "logo.png"), [1, 2, 3]);
        Directory.CreateDirectory(Path.Combine(source, "empty"));
        return (project, source);
    }

    static async Task<JsonObject> Publish(AiChatMigrationTestHost host, JsonObject project, string user = "alice")
    {
        var response = (ChatResult)(await host.SendAsync("POST", host.Feature.RoutePrefix + "/ext/share_static/project/" + project.GetString("id") + "/folder", user))!;
        Assert.That(response.Status, Is.EqualTo(200));
        return ChatJson.ParseObject(response.Text!);
    }

    [Test]
    public void Configuration_defaults_resolve_against_startup_directory_and_null_or_empty_URL_uses_path()
    {
        var startup = Path.Combine(Path.GetTempPath(), "static-settings");
        var defaults = StaticProjectPublisher.Configure(new JsonObject(), startup);
        Assert.That(defaults.Enabled, Is.True);
        Assert.That(defaults.Directory, Is.EqualTo(Path.Combine(startup, "p")));
        Assert.That(defaults.BaseUrl, Is.EqualTo(""));
        Assert.That(new StaticPublishConfig().BaseUrl, Is.EqualTo(""));
        foreach (var url in new string?[] { null, "" }) {
            var configured = StaticProjectPublisher.Configure(new JsonObject { ["basePath"] = "/sites", ["baseUrl"] = url }, startup);
            Assert.That(configured.BaseUrl, Is.EqualTo(url));
            Assert.That(configured.BasePath, Is.EqualTo("/sites/"));
        }
        var custom = StaticProjectPublisher.Configure(new JsonObject { ["baseUrl"] = "https://static.example/output/" }, startup);
        Assert.That(custom.BasePath, Is.EqualTo("/output/"));
        Assert.That(custom.BaseUrl, Is.EqualTo("https://static.example/output"));
        Assert.That(new ShareStaticExtension().Enabled, Is.True);
    }

    [Test]
    public void Missing_directory_uses_web_content_root_for_JSON_and_partial_code_overrides()
    {
        var startup = Path.Combine(Path.GetTempPath(), "static-working-directory");
        var webroot = Path.Combine(Path.GetTempPath(), "custom-web-content");
        var defaults = StaticProjectPublisher.Configure(new JsonObject(), startup, webContentDirectory: webroot);
        Assert.That(defaults.Directory, Is.EqualTo(Path.Combine(webroot, "p")));
        var code = StaticProjectPublisher.Configure(new JsonObject(), startup,
            new StaticPublishConfig { BaseUrl = "https://example.com/p" }, webroot);
        Assert.That(code.Directory, Is.EqualTo(Path.Combine(webroot, "p")));
        var relative = StaticProjectPublisher.Configure(new JsonObject { ["directory"] = "./exports" }, startup, webContentDirectory: webroot);
        Assert.That(relative.Directory, Is.EqualTo(Path.Combine(startup, "exports")));
        Assert.Throws<ArgumentException>(() => StaticProjectPublisher.Configure(new JsonObject { ["directory"] = null }, startup, webContentDirectory: webroot));
    }

    [TestCase("null")]
    [TestCase("[]")]
    [TestCase("{\"enabled\":null}")]
    [TestCase("{\"enabled\":\"yes\"}")]
    [TestCase("{\"directory\":\"\"}")]
    [TestCase("{\"directory\":123}")]
    [TestCase("{\"basePath\":\"relative\"}")]
    [TestCase("{\"basePath\":\"/../p\"}")]
    [TestCase("{\"baseUrl\":true}")]
    [TestCase("{\"baseUrl\":\"file:///tmp/p\"}")]
    [TestCase("{\"baseUrl\":\"https://user:secret@static.example/p\"}")]
    [TestCase("{\"baseUrl\":\"https://static.example/p?q=x\"}")]
    [TestCase("{\"baseUrl\":\"https://static.example/p#x\"}")]
    [TestCase("{\"baseUrl\":\"https://static.example/p/../other\"}")]
    public void Invalid_settings_fail_before_publishing(string json) =>
        Assert.Throws<ArgumentException>(() => StaticProjectPublisher.Configure(JsonNode.Parse(json) as JsonObject ?? throw new ArgumentException("Expected object"), Path.GetTempPath()));

    [Test]
    public async Task Typed_code_override_wins_over_JSON_and_drives_publication_and_UI_configuration()
    {
        using var host = new AiChatMigrationTestHost();
        WriteConfig(host,new JsonObject {
            ["enabled"] = false, ["directory"] = "/ignored", ["basePath"] = "/ignored/", ["baseUrl"] = "invalid-ignored-url",
        });
        host.Install(new ProjectsExtension());
        var supplied = new StaticPublishConfig {
            Directory = Path.Combine(host.DirectoryPath, "code-output"), BaseUrl = "https://static.example/sites/",
        };
        var extension = host.Install(new ShareStaticExtension { StaticPublish = supplied });
        Assert.That(extension.StaticPublish, Is.TypeOf<StaticPublishConfig>());
        Assert.That(extension.StaticPublish!.Enabled, Is.True);
        Assert.That(extension.StaticPublish.BasePath, Is.EqualTo("/sites/"));
        Assert.That(supplied.BaseUrl, Is.EqualTo("https://static.example/sites/"));
        var config = (JsonObject)(await host.SendAsync("GET", "/ext/share_static/config.json", "alice"))!;
        var settings = config;
        Assert.That(settings.GetBool("enabled"), Is.True);
        Assert.That(settings.GetString("directory"), Is.EqualTo(supplied.Directory));
        Assert.That(settings.GetString("basePath"), Is.EqualTo("/sites/"));
        Assert.That(settings.GetString("baseUrl"), Is.EqualTo("https://static.example/sites"));
        var (project, _) = await Project(host, extension);
        var publication = await Publish(host, project);
        Assert.That(publication.GetString("publishedPath"), Is.EqualTo(Path.Combine(supplied.Directory, "alice", "site")));
        Assert.That(publication.GetString("publishedUrl"), Is.EqualTo("https://static.example/sites/alice/site/"));
        Assert.That(await File.ReadAllTextAsync(Path.Combine(publication.GetString("publishedPath")!, "index.html")), Does.Contain("<base href=\"/sites/alice/site/\">"));
    }

    [Test]
    public async Task JSON_deserializes_into_typed_property_and_code_override_can_disable_links_or_publishing()
    {
        using var host = new AiChatMigrationTestHost();
        var extension = Install(host, new JsonObject { ["directory"] = Path.Combine(host.DirectoryPath, "json-output"), ["baseUrl"] = null });
        Assert.That(extension.StaticPublish!.Directory, Is.EqualTo(Path.Combine(host.DirectoryPath, "json-output")));
        Assert.That(extension.StaticPublish.Enabled, Is.True);
        Assert.That(extension.StaticPublish.BaseUrl, Is.Null);
        Assert.That(extension.StaticPublish.BasePath, Is.EqualTo("/p/"));
        var config = (JsonObject)(await host.SendAsync("GET", "/ext/share_static/config.json", "alice"))!;
        Assert.That(config.ContainsKey("baseUrl"), Is.True);
        Assert.That(config["baseUrl"], Is.Null);

        using var other = new AiChatMigrationTestHost();
        WriteConfig(other, JsonValue.Create(123)); // A complete code override bypasses JSON settings.
        other.Install(new ProjectsExtension());
        var disabled = other.Install(new ShareStaticExtension { StaticPublish = new StaticPublishConfig { Enabled = false, BaseUrl = null, BasePath = "/sites" } });
        Assert.That(disabled.StaticPublish!.Enabled, Is.False);
        Assert.That(disabled.StaticPublish.BasePath, Is.EqualTo("/sites/"));
        Assert.That(disabled.StaticPublish.Directory, Is.EqualTo(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "p")));
        var (project, _) = await Project(other, disabled);
        Assert.That((int)Assert.ThrowsAsync<HttpError>(async () => await Publish(other, project))!.StatusCode, Is.EqualTo(403));
    }

    [Test]
    public void Invalid_typed_settings_are_validated_and_relative_paths_are_normalized_without_mutating_input()
    {
        var startup = Path.Combine(Path.GetTempPath(), "typed-static-settings");
        var supplied = new StaticPublishConfig { Directory = "./output", BaseUrl = "", BasePath = "/sites" };
        var resolved = StaticProjectPublisher.Configure(new JsonObject(), startup, supplied);
        Assert.That(resolved.Directory, Is.EqualTo(Path.Combine(startup, "output")));
        Assert.That(resolved.BasePath, Is.EqualTo("/sites/"));
        Assert.That(resolved.BaseUrl, Is.EqualTo(""));
        Assert.That(supplied.Directory, Is.EqualTo("./output"));
        foreach (var invalid in new[] {
            new StaticPublishConfig { Directory = "" },
            new StaticPublishConfig { BasePath = "relative" },
            new StaticPublishConfig { BaseUrl = "file:///tmp/output" },
        }) Assert.Throws<ArgumentException>(() => StaticProjectPublisher.Configure(new JsonObject(), startup, invalid));
    }

    [TestCase(""), TestCase("/chat")]
    public async Task Protected_folder_endpoint_publishes_without_remote_account_preserves_source_and_persists_metadata(string prefix)
    {
        using var host = new AiChatMigrationTestHost(prefix);
        var extension = Install(host);
        var (project, source) = await Project(host, extension);
        var original = await File.ReadAllTextAsync(Path.Combine(source, "index.html"));
        var unauthorized = (ChatResult)(await host.SendAsync("POST", prefix + "/ext/share_static/project/" + project.GetString("id") + "/folder", null))!;
        Assert.That(unauthorized.Status, Is.EqualTo(401));
        var publication = await Publish(host, project);
        var destination = publication.GetString("publishedPath")!;
        Assert.That(destination, Is.EqualTo(Path.Combine(host.DirectoryPath, "public", "p", "alice", "site")));
        Assert.That(publication.GetString("urlPath"), Is.EqualTo("/p/alice/site/"));
        Assert.That(publication.GetString("publishedUrl"), Is.EqualTo("http://127.0.0.1:8080/p/alice/site/"));
        Assert.That(DateTimeOffset.Parse(publication.GetString("publishedAt")!).Offset, Is.EqualTo(TimeSpan.Zero));
        Assert.That(await File.ReadAllTextAsync(Path.Combine(source, "index.html")), Is.EqualTo(original));
        var exported = await File.ReadAllTextAsync(Path.Combine(destination, "index.html"));
        Assert.That(exported, Does.Contain("<base href=\"/p/alice/site/\">"));
        Assert.That(exported, Does.Contain("src=\"assets/logo.png\"").And.Contain("href=\"page.html\""));
        Assert.That(await File.ReadAllBytesAsync(Path.Combine(destination, "assets", "logo.png")), Is.EqualTo(new byte[] { 1, 2, 3 }));
        Assert.That(Directory.Exists(Path.Combine(destination, "empty")), Is.True);
        var saved = host.Feature.ProjectsApi.GetUserProjects("alice").Single();
        Assert.That(JsonNode.DeepEquals(saved["staticPublication"], publication), Is.True);
        Assert.That(saved.GetString("publishedUrl"), Is.EqualTo("https://remote.example/site"));
        Assert.That(host.Feature.PublisherApi.Available, Is.False);
        project["description"] = "Edited from stale form";
        project["staticPublication"] = new JsonObject { ["publishedPath"] = "/forged" };
        await host.SendAsync("POST", prefix + "/ext/projects/save/Site", "alice", project);
        saved = host.Feature.ProjectsApi.GetUserProjects("alice").Single();
        Assert.That(JsonNode.DeepEquals(saved["staticPublication"], publication), Is.True);
        Assert.That(saved.GetString("description"), Is.EqualTo("Edited from stale form"));
        Assert.That((int)Assert.ThrowsAsync<HttpError>(async () => await Publish(host, project, "bob"))!.StatusCode, Is.EqualTo(404));
    }

    [TestCase(null), TestCase(""), TestCase("https://static.example/sites")]
    public async Task URL_settings_control_exported_base_and_optional_link(string? url)
    {
        using var host = new AiChatMigrationTestHost();
        var extension = Install(host, Settings(host, url));
        var (project, _) = await Project(host, extension);
        var publication = await Publish(host, project);
        var path = string.IsNullOrEmpty(url) ? "/p/alice/site/" : "/sites/alice/site/";
        Assert.That(publication.GetString("urlPath"), Is.EqualTo(path));
        Assert.That(publication.GetString("publishedUrl"), Is.EqualTo(string.IsNullOrEmpty(url) ? null : url + "/alice/site/"));
        Assert.That(await File.ReadAllTextAsync(Path.Combine(publication.GetString("publishedPath")!, "index.html")), Does.Contain("<base href=\"" + path + "\">"));
    }

    [Test]
    public void HTML_rewrite_preserves_real_base_external_links_comments_raw_text_and_unrelated_attributes()
    {
        const string existing = "<head><BASE href='/already/'></head><img src='/untouched.png'>";
        Assert.That(StaticProjectPublisher.RewriteIndex(existing, "/p/test/"), Is.EqualTo(existing));
        const string content = "<HTML><HEAD class='head'><!-- <base href='/fake/'> --></HEAD><script>const fake=\"<base href='/fake/'>\"; const src='src=\"/raw\"';</script><style>/* <img src='/fake'> */</style><img data-x=\"src='/fake'\" SRC='/img?a=1&amp;b=2'><a href=//cdn.example/css>cdn</a><a href='https://external.example'>external</a></HTML>";
        var result = StaticProjectPublisher.RewriteIndex(content, "/p/test/");
        Assert.That(result, Does.Contain("<HEAD class='head'>\n    <base href=\"/p/test/\">"));
        Assert.That(result, Does.Contain("<!-- <base href='/fake/'> -->"));
        Assert.That(result, Does.Contain("const fake=\"<base href='/fake/'>\"; const src='src=\"/raw\"'"));
        Assert.That(result, Does.Contain("data-x=\"src='/fake'\" SRC=\"img?a=1&amp;b=2\""));
        Assert.That(result, Does.Contain("href=//cdn.example/css").And.Contain("href='https://external.example'"));
        Assert.That(StaticProjectPublisher.RewriteIndex("<img src=/image.png>", "/p/test/"), Is.EqualTo("<head>\n    <base href=\"/p/test/\">\n</head><img src=\"image.png\">"));
    }

    [Test]
    public async Task Republish_replaces_stale_files_and_failed_rewrite_or_metadata_commit_rolls_back()
    {
        using var host = new AiChatMigrationTestHost();
        var extension = Install(host);
        var (project, source) = await Project(host, extension);
        await File.WriteAllTextAsync(Path.Combine(source, "old.txt"), "Old");
        var first = await Publish(host, project);
        var destination = first.GetString("publishedPath")!;
        File.Delete(Path.Combine(source, "old.txt"));
        await File.WriteAllTextAsync(Path.Combine(source, "new.txt"), "New");
        var second = await Publish(host, project);
        Assert.That(File.Exists(Path.Combine(destination, "old.txt")), Is.False);
        Assert.That(File.Exists(Path.Combine(destination, "new.txt")), Is.True);
        var goodHtml = await File.ReadAllTextAsync(Path.Combine(destination, "index.html"));
        await File.WriteAllBytesAsync(Path.Combine(source, "index.html"), [0xff, 0xfe, 0xff]);
        var encodingError = Assert.ThrowsAsync<HttpError>(async () => await Publish(host, project))!;
        Assert.That(encodingError.Status, Is.EqualTo(500));
        Assert.That(encodingError.ErrorCode, Is.EqualTo("StaticPublishFailed"));
        Assert.That(encodingError.Message, Does.Contain(extension.StaticPublish!.Directory));
        Assert.That(encodingError.InnerException, Is.TypeOf<DecoderFallbackException>());
        Assert.That(await File.ReadAllTextAsync(Path.Combine(destination, "index.html")), Is.EqualTo(goodHtml));
        Assert.That(JsonNode.DeepEquals(host.Feature.ProjectsApi.GetUserProjects("alice").Single()["staticPublication"], second), Is.True);
        await File.WriteAllTextAsync(Path.Combine(source, "index.html"), "<head></head>New version");
        var projects = host.Feature.ProjectsApi;
        host.Feature.ProjectsApi = new CommitFailure(projects);
        var commitError = Assert.ThrowsAsync<HttpError>(async () => await Publish(host, project))!;
        Assert.That(commitError.Status, Is.EqualTo(500));
        Assert.That(commitError.Message, Does.Contain("Simulated metadata failure"));
        Assert.That(await File.ReadAllTextAsync(Path.Combine(destination, "index.html")), Is.EqualTo(goodHtml));
        Assert.That(JsonNode.DeepEquals(projects.GetUserProjects("alice").Single()["staticPublication"], second), Is.True);
        Assert.That(Directory.GetDirectories(Path.GetDirectoryName(destination)!, ".publish-*"), Is.Empty);
    }

    sealed class CommitFailure(IProjectsApi projects) : IProjectsApi
    {
        public List<JsonObject> GetUserProjects(string? user = null) => projects.GetUserProjects(user);
        public JsonObject ResolveWorkspace(string projectId, string? user = null) => projects.ResolveWorkspace(projectId, user);
        public Task UpdateStaticPublicationAsync(JsonObject captured, JsonObject publication, string? user = null) => throw new IOException("Simulated metadata failure");
    }

    [Test]
    public async Task Disabled_global_settings_cannot_be_overridden_by_account_updates()
    {
        using var host=new AiChatMigrationTestHost();
        var settings=Settings(host);settings["enabled"]=false;
        var extension=Install(host,settings);
        var (project,_)=await Project(host,extension);
        var config=(JsonObject)(await host.SendAsync("GET","/ext/share_static/config.json","alice"))!;
        Assert.That(config.GetBool("enabled"),Is.False);
        Assert.That(host.Feature.Routes.Match("POST","/ext/share_static/config.json"),Is.Null);
        Assert.That(host.Feature.Routes.Match("POST","/ext/share_static/disconnect"),Is.Null);
        Assert.That((int)Assert.ThrowsAsync<HttpError>(async()=>await Publish(host,project))!.StatusCode,Is.EqualTo(403));
    }

    [Test]
    public async Task Unrelated_destinations_source_and_destination_links_and_overlap_are_rejected()
    {
        using var host = new AiChatMigrationTestHost();
        var settings = Settings(host);
        var extension = Install(host, settings);
        var (project, source) = await Project(host, extension);
        var destination = Path.Combine(settings.GetString("directory")!, "alice", "site");
        Directory.CreateDirectory(destination);
        await File.WriteAllTextAsync(Path.Combine(destination, "unrelated.txt"), "Keep");
        var conflict = Assert.ThrowsAsync<HttpError>(async () => await Publish(host, project))!;
        Assert.That(conflict.Status, Is.EqualTo(409));
        Assert.That(conflict.Message, Does.Contain(destination).And.Contain("move the existing folder"));
        Assert.That(await File.ReadAllTextAsync(Path.Combine(destination, "unrelated.txt")), Is.EqualTo("Keep"));
        Directory.Delete(destination, true);
        if (!OperatingSystem.IsWindows()) {
            File.CreateSymbolicLink(Path.Combine(source, "linked.txt"), Path.Combine(source, "index.html"));
            Assert.That((int)Assert.ThrowsAsync<HttpError>(async () => await Publish(host, project))!.StatusCode, Is.EqualTo(400));
            File.Delete(Path.Combine(source, "linked.txt"));
            Directory.CreateSymbolicLink(destination, source);
            Assert.That((int)Assert.ThrowsAsync<HttpError>(async () => await Publish(host, project))!.StatusCode, Is.EqualTo(400));
            Directory.Delete(destination);
        }
        var overlap = StaticProjectPublisher.Configure(new JsonObject { ["directory"] = source }, host.DirectoryPath);
        Assert.That((int)Assert.ThrowsAsync<HttpError>(async () => await new StaticProjectPublisher(extension.Ctx, overlap).PublishAsync("alice", project.GetString("id")!))!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task Missing_output_and_cancelled_publish_leave_no_publication()
    {
        using var host = new AiChatMigrationTestHost();
        var extension = Install(host);
        var (project, _) = await Project(host, extension, output: null);
        Assert.That((int)Assert.ThrowsAsync<HttpError>(async () => await Publish(host, project))!.StatusCode, Is.EqualTo(400));
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        var publisher = new StaticProjectPublisher(extension.Ctx, StaticProjectPublisher.Configure(Settings(host), host.DirectoryPath));
        Assert.CatchAsync<OperationCanceledException>(async () => await publisher.PublishAsync("alice", project.GetString("id")!, cancelled.Token));
        Assert.That(host.Feature.ProjectsApi.GetUserProjects("alice").Single()["staticPublication"], Is.Null);
    }

    [Test]
    public async Task Static_metadata_update_preserves_concurrent_edits_and_rejects_changed_output_settings()
    {
        using var host = new AiChatMigrationTestHost();
        var extension = Install(host);
        var (captured, _) = await Project(host, extension);
        var current = captured.Clone(); current["description"] = "Concurrent edit";
        await host.SendAsync("POST", "/ext/projects/save/Site", "alice", current);
        var publication = new JsonObject { ["publishedPath"] = "/test", ["publishedAt"] = "2026-10-05T00:00:00Z" };
        await host.Feature.ProjectsApi.UpdateStaticPublicationAsync(captured, publication, "alice");
        Assert.That(host.Feature.ProjectsApi.GetUserProjects("alice").Single().GetString("description"), Is.EqualTo("Concurrent edit"));
        current["publish"] = "other";
        await host.SendAsync("POST", "/ext/projects/save/Site", "alice", current);
        Assert.That((int)Assert.ThrowsAsync<HttpError>(async () => await host.Feature.ProjectsApi.UpdateStaticPublicationAsync(captured, publication, "alice"))!.StatusCode, Is.EqualTo(409));
    }
}
