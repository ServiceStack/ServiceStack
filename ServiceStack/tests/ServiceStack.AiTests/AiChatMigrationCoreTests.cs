#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.Host;

namespace ServiceStack.AiTests;

public class AiChatMigrationCoreTests
{
    static JsonObject Reply() => ChatJson.ParseObject("""{"choices":[{"message":{"role":"assistant","content":"ok"}}],"usage":{"prompt_tokens":2,"completion_tokens":1}}""");
    static FakeChatProvider Provider(string id, string model = "model") => new(Reply()) {
        Id = id, Models = { [model] = new JsonObject { ["id"] = model, ["name"] = model, ["tool_call"] = true } },
    };

    [Test]
    public void Missing_model_ids_are_filled_on_clones_with_name_deduplication()
    {
        var provider = Provider("one");
        provider.Models["model"].Remove("id");
        var other = Provider("two");
        var feature = new ChatFeature { Providers = { ["one"] = provider, ["two"] = other } };
        var models = feature.GetActiveModels();
        Assert.That(models.Count, Is.EqualTo(1));
        Assert.That(models[0]!["id"]!.GetValue<string>(), Is.EqualTo("model"));
        Assert.That(models[0]!["provider"]!.GetValue<string>(), Is.EqualTo("one"));
        Assert.That(provider.Models["model"].ContainsKey("id"), Is.False);
    }

    [TestCase(null, "two", "model", "two")]
    [TestCase("one", "two", "model", "one")]
    [TestCase(null, null, "two/model", "two")]
    [TestCase(null, "missing", "model", "one")]
    public async Task Explicit_preference_is_applied_before_filters(string? contextPreference, string? requestPreference, string model, string expected)
    {
        var one = Provider("one");
        var two = Provider("two");
        var feature = new ChatFeature { Providers = { ["one"] = one, ["two"] = two } };
        string? filtered = null;
        feature.Filters.ChatRequestFilters.Add((_, ctx) => { filtered = ctx.Provider!.Id; return Task.CompletedTask; });
        var context = new ChatContext { PreferredProvider = contextPreference, Provider = one, Tools = "none" };
        await feature.ChatCompletionAsync(new JsonObject { ["model"] = model, ["provider"] = requestPreference }, context);
        Assert.That(filtered, Is.EqualTo(expected));
        Assert.That(context.Provider!.Id, Is.EqualTo(expected));
    }

    [Test]
    public async Task Preferred_provider_failure_retains_failover_and_correct_attribution()
    {
        var feature = new ChatFeature { Providers = { ["one"] = Provider("one"), ["two"] = new FailingProvider { Id = "two", Models = { ["model"] = new JsonObject { ["id"] = "model" } } } } };
        var context = new ChatContext { PreferredProvider = "two", Tools = "none" };
        await feature.ChatCompletionAsync(new JsonObject { ["model"] = "model" }, context);
        Assert.That(context.Provider!.Id, Is.EqualTo("one"));
    }

    [Test]
    public async Task Model_only_helpers_skip_request_filters_and_tools_but_keep_accounting_hooks()
    {
        var provider = Provider("one");
        var feature = new ChatFeature { Providers = { ["one"] = provider } };
        var requestFilters = 0;
        var responseFilters = 0;
        feature.Filters.ChatRequestFilters.Add((_, _) => { requestFilters++; return Task.CompletedTask; });
        feature.Filters.ChatResponseFilters.Add((_, ctx) => { Assert.That(ctx.User, Is.EqualTo("alice")); responseFilters++; return Task.CompletedTask; });
        var context = new ChatContext { ModelOnly = true, User = "alice", NoHistory = true, Request = new BasicRequest() };
        var chat = new JsonObject { ["model"] = "model", ["tools"] = new JsonArray(new JsonObject()), ["tool_choice"] = "auto" };
        await feature.ChatCompletionAsync(chat, context);
        Assert.That(requestFilters, Is.Zero);
        Assert.That(responseFilters, Is.EqualTo(1));
        Assert.That(provider.ReceivedChat!.ContainsKey("tools"), Is.False);
        Assert.That(provider.ReceivedChat.ContainsKey("tool_choice"), Is.False);
        Assert.That(chat.ContainsKey("tools"), Is.True);
        Assert.That(context.NoStore, Is.False);
        Assert.That(context.CreateChild(CancellationToken.None).ModelOnly, Is.True);
    }

    [Test]
    public async Task Submission_leases_are_shared_across_instances_and_exclude_mutation()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var first = new WorkspaceOperations();
        var second = new WorkspaceOperations();
        var a = first.AcquireSubmission(root);
        var b = second.AcquireSubmission(root + Path.DirectorySeparatorChar);
        Assert.ThrowsAsync<HttpError>(async () => await first.AcquireMutationAsync(root, root));
        a.Dispose(); a.Dispose(); b.Dispose();
        using (await second.AcquireMutationAsync(root, root))
        {
            await Task.Yield();
            Assert.Throws<HttpError>(() => first.AcquireSubmission(root));
            Assert.ThrowsAsync<HttpError>(async () => await first.AcquireMutationAsync(root, root));
        }
        using var released = first.AcquireSubmission(root);
    }

    [Test]
    public async Task Cancelled_acquisition_does_not_leak_exclusion()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var operations = new WorkspaceOperations();
        using var cts = new CancellationTokenSource(); cts.Cancel();
        Assert.Throws<OperationCanceledException>(() => operations.AcquireSubmission(root, cts.Token));
        Assert.ThrowsAsync<OperationCanceledException>(async () => await operations.AcquireMutationAsync(root, root, cts.Token));
        using var lease = await operations.AcquireMutationAsync(root, root);
    }

    [Test]
    public void Disabled_extension_seams_have_safe_defaults()
    {
        var feature = new ChatFeature();
        Assert.DoesNotThrow(() => feature.NotifySidebar());
        Assert.That(feature.GitProvisioner.InitializeGit, Is.False);
        Assert.That(feature.GitProvisioner.Clone, Is.False);
        Assert.That(feature.PublisherApi.Available, Is.False);
        Assert.That(feature.ProjectsApi.CreationAvailable, Is.False);
        Assert.That(feature.ProjectsApi.ResolveExplorerWorkspace(null)["directories"]!.AsArray(), Is.Empty);
    }

    [Test]
    public async Task Reload_restores_wrappers_and_isolates_failing_or_disabled_extensions()
    {
        var feature = new ChatFeature();
        var restored = new ReloadExtension("restore");
        var failed = new ReloadExtension("fail") { Fail = true };
        var disabled = new ReloadExtension("disabled") { Disabled = true };
        var installed = (IList)typeof(ChatFeature).GetField("installedExtensions", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(feature)!;
        foreach (var ext in new[] { failed, restored, disabled })
        {
            ext.Ctx = new ExtensionContext(feature, ext.Name);
            installed.Add(((ChatExtension)ext, ext.Ctx));
        }
        await feature.ReloadExtensionProvidersAsync();
        Assert.That(feature.Providers.ContainsKey("restore"), Is.True);
        Assert.That(disabled.Calls, Is.Zero);
        feature.CreateProviders();
        await feature.ReloadExtensionProvidersAsync();
        Assert.That(restored.Calls, Is.EqualTo(2));
        Assert.That(feature.Providers["restore"].Feature, Is.SameAs(feature));
    }

    [TestCase("")]
    [TestCase("/chat")]
    public async Task Harness_dispatches_real_routes_with_origin_identity(string prefix)
    {
        using var host = new AiChatMigrationTestHost(prefix);
        host.Feature.Routes.AddGet("/identity", req => Task.FromResult<object?>(new JsonObject { ["user"] = req.AssertUserName() }));
        foreach (var user in new[] { "alice", "bob" })
            Assert.That(((JsonObject)(await host.SendAsync("GET", prefix + "/identity", user))!)["user"]!.GetValue<string>(), Is.EqualTo(user));
        Assert.That(((ChatResult)(await host.SendAsync("GET", prefix + "/identity", null))!).Status, Is.EqualTo(401));
    }

    sealed class FailingProvider : ChatProvider
    {
        public override Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context) => throw new InvalidOperationException("fake failure");
    }
    sealed class ReloadExtension(string name) : ChatExtension(name)
    {
        public bool Fail;
        public int Calls;
        public override void Install(ExtensionContext ctx) { }
        public override Task ReloadProvidersAsync(ExtensionContext ctx, CancellationToken token = default)
        {
            Calls++;
            if (Fail) throw new InvalidOperationException("fake failure");
            ctx.RegisterProvider(Name, Provider(Name));
            return Task.CompletedTask;
        }
    }
}
