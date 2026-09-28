#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using ModelContextProtocol.Authentication;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.Host;
using ServiceStack.OrmLite;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.Extensions.Tests;

[NonParallelizable]
public class McpClientTests
{
    [TestCase(400, "http_400")] [TestCase(401, "auth_required")]
    [TestCase(403, "access_denied")] [TestCase(404, "http_404")]
    [TestCase(429, "rate_limited")] [TestCase(503, "http_503")]
    public void Connection_errors_preserve_http_status_without_remote_response_bodies(int status, string code)
    {
        var remote = new System.Net.Http.HttpRequestException("echoed-secret", null, (HttpStatusCode)status);
        var failure = McpClientErrors.Connection(new Exception("wrapper-secret", remote));
        Assert.That(failure.Code, Is.EqualTo(code));
        Assert.That(failure.Message, Does.Contain("HTTP " + status).And.Not.Contain("secret"));
    }

    [Test]
    public void Connection_errors_preserve_known_failures_and_sanitize_unknown_exceptions()
    {
        var known = new McpClientException("network_denied", "Destination denied");
        Assert.That(McpClientErrors.Connection(new Exception("wrapper", known)), Is.SameAs(known));
        Assert.That(McpClientErrors.Connection(new Exception("echoed-secret")).Message, Does.Not.Contain("secret"));
        Assert.That(McpClientErrors.Connection(new OperationCanceledException()).Code, Is.EqualTo("timeout"));
    }

    static JsonObject Json(string value) => JsonNode.Parse(value)!.AsObject();
    const string Input = """{"type":"object","properties":{"user":{"type":"string"}},"required":["user"],"additionalProperties":false}""";
    sealed class Session : IMcpClientSession
    {
        public string? ProtocolVersion => "2026-07-28";
        public int Calls, Lists, Disposals;
        public JsonObject? Arguments;
        public string? Cursor;
        public string Schema = Input;
        public Task<JsonObject> ListToolsAsync(string? cursor, CancellationToken token) {
            Lists++;
            return Task.FromResult(new JsonObject { ["tools"] = new JsonArray(new JsonObject {
                ["name"] = "remote.name", ["description"] = "Remote tool", ["inputSchema"] = Json(Schema),
            }), ["nextCursor"] = Cursor });
        }
        public Task<JsonObject> CallAsync(string name, JsonObject arguments, CancellationToken token) {
            Calls++; Arguments = (JsonObject)arguments.DeepClone();
            return Task.FromResult(Json("""{"content":[{"type":"text","text":"ok"}],"structuredContent":[1,2]}"""));
        }
        public ValueTask DisposeAsync() { Disposals++; return ValueTask.CompletedTask; }
    }
    sealed class Factory(Session session) : IMcpClientSessionFactory
    {
        public McpClientCredential? Credential;
        public Task<IMcpClientSession> CreateAsync(McpClientServer server, McpClientCredential? credential, ClientOAuthOptions? oauth, CancellationToken token) { Credential = credential; return Task.FromResult<IMcpClientSession>(session); }
    }
    sealed class Auth : IChatAuth
    {
        public bool IsEnabled => true;
        public string? GetUserName(IRequest request) => request.Items.GetValueOrDefault("principal") as string;
        public string? AssertUserName(IRequest request) => GetUserName(request) ?? throw new UnauthorizedAccessException();
        public (bool IsAuthenticated, JsonObject? Session) CheckAuth(IRequest request) => (GetUserName(request) != null, null);
        public Task<JsonObject?> GetAuthInfoAsync(IRequest request) => Task.FromResult<JsonObject?>(null);
        public Task SignOutAsync(IRequest request) => Task.CompletedTask;
        public bool IsAdmin(IRequest request) => false;
    }
    static ChatContext Context(string user = "alice", string tools = "mcp_test") {
        var request = new BasicRequest(); request.Items["principal"] = user;
        return new ChatContext { Request = request, User = user, Tools = tools };
    }
    static McpClientServer Server(McpClientApproval approval = McpClientApproval.Always) => new() {
        Id = "test", Endpoint = new Uri("https://example.com/mcp"), AllowedTools = ["*"],
        Approval = approval, Authorize = (context, _) => Task.FromResult(context.User == "alice"),
    };

    [Test]
    public void Default_extension_is_disabled_and_does_not_pollute_local_registry() {
        var feature = new ChatFeature();
        Assert.That(feature.McpClient.Disabled, Is.True);
        Assert.That(feature.Tools.Providers, Is.Empty);
        Assert.That(feature.Tools.Tools, Is.Empty);
    }
    [Test]
    public async Task Scoped_configuration_is_owned_validated_and_invalidates_old_handles() {
        using var host = new BasicAppHost().Init();
        var directory = Path.Combine(Path.GetTempPath(), "mcp-config-" + Guid.NewGuid().ToString("N"));
        var factory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        var feature = new ChatFeature { AppData = new ChatAppData(directory), ChatDb = new ChatDb(factory), ChatAuth = new Auth() };
        var extension = feature.McpClient; extension.Enabled = true;
        extension.Servers.Add(Server());
        extension.Ctx = new ExtensionContext(feature, "mcp_client"); extension.Install(extension.Ctx);
        try {
            var original = extension.GetPersonalConfiguration("alice");
            JsonObject Payload(string servers, JsonObject? snapshot = null) => new() {
                ["servers"] = JsonNode.Parse(servers), ["revision"] = (snapshot ?? original)["revision"]!.DeepClone(),
            };
            const string personal = """[{"id":"mine","endpoint":"https://example.com/mcp","allowedTools":["*"]}]""";
            var saved = extension.SavePersonalConfiguration("alice", Payload(personal));
            Assert.That(File.Exists(Path.Combine(directory, "user/alice/mcp_client/config.json")), Is.True);
            Assert.That(extension.GetServers("bob").Select(x => x.Id), Is.EqualTo(new[] { "test" }));
            var old = extension.GetServers("alice").Single(x => x.Id == "mine");
            Assert.That(old.NeedsApproval("anything"), Is.True);
            await extension.AssertAccessAsync(old, Context(), "discover");
            Assert.ThrowsAsync<McpClientException>(() => extension.AssertAccessAsync(old, Context("bob"), "discover"));
            Assert.Throws<HttpError>(() => extension.SavePersonalConfiguration("alice", Payload("[]")));
            foreach (var invalid in new[] {
                """[{"id":"test","endpoint":"https://example.com/mcp"}]""",
                """[{"id":"mine","endpoint":"https://example.com/mcp","approval":"never"}]""",
                """[{"id":"mine","endpoint":"https://example.com/mcp","auth":{"mode":"host_secret","secretReference":"secret"},"allowedUsers":["alice"]}]""",
                """[{"id":"mine","endpoint":"http://example.com/mcp"}]""",
                """[{"id":"mine","endpoint":null}]""",
            }) Assert.Throws<HttpError>(() => extension.SavePersonalConfiguration("alice", Payload(invalid, saved)));
            Assert.Throws<HttpError>(() => extension.SavePersonalConfiguration("default", Payload("[]")));
            Assert.Throws<HttpError>(() => extension.GetPersonalConfiguration("../bob"));
            Assert.That(extension.GetPersonalConfiguration("default")["canEdit"]!.GetValue<bool>(), Is.False);
            extension.SavePersonalConfiguration("alice", Payload(personal.Replace("example.com", "changed.example.com"), saved));
            Assert.ThrowsAsync<McpClientException>(() => extension.AssertAccessAsync(old, Context(), "invoke"));
            var changed = extension.GetPersonalConfiguration("alice");
            extension.SavePersonalConfiguration("alice", Payload("[]", changed));
            Assert.That(extension.GetServers("alice").Count, Is.EqualTo(1));
        } finally {
            await feature.RunAsyncShutdownHandlers();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
    [Test]
    public async Task Personal_GitHub_OAuth_configuration_requires_callback_and_accepts_registered_client()
    {
        using var host = new BasicAppHost().Init();
        host.Container.Register<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        var directory = Path.Combine(Path.GetTempPath(), "mcp-oauth-config-" + Guid.NewGuid().ToString("N"));
        var factory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        var feature = new ChatFeature { AppData = new ChatAppData(directory), ChatDb = new ChatDb(factory), ChatAuth = new Auth() };
        var extension = feature.McpClient; extension.Enabled = true;
        extension.Ctx = new ExtensionContext(feature, "mcp_client"); extension.Install(extension.Ctx);
        try {
            var before = extension.GetPersonalConfiguration("alice");
            JsonObject Payload() => new() {
                ["servers"] = JsonNode.Parse("""[{"id":"github","endpoint":"https://api.githubcopilot.com/mcp/","auth":{"mode":"user_oauth"},"oauthClientId":"registered-client","oauthIssuer":"https://github.com/login/oauth","oauthScopes":["repo"],"allowedTools":["*"]}]"""),
                ["revision"] = before["revision"]!.DeepClone(),
            };
            var error = Assert.Throws<HttpError>(() => extension.SavePersonalConfiguration("alice", Payload()));
            Assert.That(error!.Message, Does.Contain("callback URL"));
            extension.OAuthRedirectUri = new Uri("https://chat.example/chat/ext/mcp_client/oauth/callback");
            extension.SavePersonalConfiguration("alice", Payload());
            var github = extension.GetServers("alice").Single();
            Assert.That(github.Auth.Mode, Is.EqualTo("user_oauth"));
            Assert.That(github.OAuthClientId, Is.EqualTo("registered-client"));
            Assert.That(github.OAuthIssuer!.AbsoluteUri, Is.EqualTo("https://github.com/login/oauth"));
            Assert.That(github.OAuthScopes, Is.EqualTo(new[] { "repo" }));
        } finally {
            await feature.RunAsyncShutdownHandlers();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [TestCase("127.0.0.1", false)] [TestCase("::1", false)] [TestCase("::ffff:127.0.0.1", false)]
    [TestCase("169.254.169.254", false)] [TestCase("10.1.2.3", false)] [TestCase("fc00::1", false)]
    [TestCase("100.100.100.200", false)] [TestCase("8.8.8.8", true)] [TestCase("2606:4700:4700::1111", true)]
    public void Network_policy_rejects_nonpublic_destinations(string ip, bool allowed) =>
        Assert.That(McpClientNetworkPolicy.IsPublicAddress(IPAddress.Parse(ip)), Is.EqualTo(allowed));

    [Test]
    public void Aliases_are_stable_case_sensitive_and_collision_resistant() {
        var alias = McpClientHash.Alias("test", new string('a', 300));
        Assert.That(alias.Length, Is.LessThanOrEqualTo(64));
        Assert.That(alias, Is.EqualTo(McpClientHash.Alias("test", new string('a', 300))));
        Assert.That(McpClientHash.Alias("test", "a.b"), Is.Not.EqualTo(McpClientHash.Alias("test", "a_b")));
        Assert.That(McpClientHash.Alias("test", "Tool"), Is.Not.EqualTo(McpClientHash.Alias("test", "tool")));
    }
    [Test]
    public void Validates_nested_schemas_and_local_references_without_network() {
        var schema = Json("""{"type":"object","$defs":{"item":{"type":"integer"}},"properties":{"values":{"type":"array","items":{"$ref":"#/$defs/item"}}},"required":["values"]}""");
        McpClientSchema.Check(schema, 65536);
        McpClientSchema.Validate(schema, Json("""{"values":[1,2]}"""), 65536);
        Assert.Throws<McpClientException>(() => McpClientSchema.Validate(schema, Json("""{"values":["bad"]}"""), 65536));
        Assert.Throws<McpClientException>(() => McpClientSchema.Check(Json("""{"$ref":"https://evil.example/schema"}"""), 65536));
        Assert.Throws<McpClientException>(() => McpClientSchema.Check(Json("""{"$defs":{"x":{"$ref":"#/$defs/x"}},"$ref":"#/$defs/x"}"""), 65536));
    }
    [Test]
    public void Mcp_schemas_default_to_draft_2020_12_and_accept_header_annotations()
    {
        var schema = Json("""{"type":"object","properties":{"sha":{"type":"string","x-mcp-header":"X-MCP-SHA"},"x-mcp-header":{"type":"integer"}},"required":["sha","x-mcp-header"]}""");
        var original = schema.ToJsonString();
        McpClientSchema.Check(schema, 65536);
        McpClientSchema.Validate(schema, Json("""{"sha":"main","x-mcp-header":1}"""), 65536);
        Assert.Throws<McpClientException>(() => McpClientSchema.Validate(schema, Json("""{"sha":1,"x-mcp-header":1}"""), 65536));
        Assert.Throws<McpClientException>(() => McpClientSchema.Validate(schema, Json("""{"sha":"main","x-mcp-header":"bad"}"""), 65536));
        Assert.That(schema.ToJsonString(), Is.EqualTo(original));
    }

    [Test]
    public async Task Catalog_rejects_cursor_loops_and_empty_allowlist_exposes_nothing() {
        var session = new Session { Cursor = "again" };
        Assert.ThrowsAsync<McpClientException>(async () => await McpClientCatalog.DiscoverAsync(session, Server(), new McpClientLimits(), default));
        session.Cursor = null;
        var server = Server(); server.AllowedTools.Clear();
        Assert.That(await McpClientCatalog.DiscoverAsync(session, server, new McpClientLimits(), default), Is.Empty);
    }
    [Test]
    public async Task Connect_all_skips_disabled_bindings_and_connects_enabled_servers()
    {
        using var host = new BasicAppHost().Init();
        var factory = new OrmLiteConnectionFactory($"DataSource=file:mcp{Guid.NewGuid():N}?mode=memory&cache=shared", SqliteDialect.Provider);
        using var keepAlive = factory.OpenDbConnection();
        var feature = new ChatFeature { ChatDb = new ChatDb(factory), ChatAuth = new Auth() };
        var extension = feature.McpClient;
        var server = Server();
        extension.Enabled = true; extension.Servers.Add(server);
        extension.Ctx = new ExtensionContext(feature, "mcp_client"); extension.Install(extension.Ctx);
        var session = new Session(); extension.Manager.Factory = new Factory(session);
        extension.Store.SetDisconnected(server, "alice", true);
        Assert.That(await extension.ConnectAllAsync(Context()), Is.Empty);
        Assert.That(session.Lists, Is.Zero);
        Assert.That(extension.Manager.Status(server, "alice")["disabled"]!.GetValue<bool>(), Is.True);
        extension.Store.SetDisconnected(server, "alice", false);
        var results = await extension.ConnectAllAsync(Context());
        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0]!["connected"]!.GetValue<bool>(), Is.True);
        Assert.That(session.Lists, Is.EqualTo(1));
        Assert.That(extension.Manager.Status(server, "alice")["disabled"]!.GetValue<bool>(), Is.False);
        Assert.That(await extension.ConnectAllAsync(Context("bob")), Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Principal_selection_approval_and_user_argument_boundaries(bool fromJson) {
        using var host = new BasicAppHost().Init();
        var factory = new OrmLiteConnectionFactory($"DataSource=file:mcp{Guid.NewGuid():N}?mode=memory&cache=shared", SqliteDialect.Provider);
        using var keepAlive = factory.OpenDbConnection();
        var feature = new ChatFeature { ChatDb = new ChatDb(factory), ChatAuth = new Auth() };
        var extension = feature.McpClient;
        var configDirectory = Path.Combine(Path.GetTempPath(), "mcp-shared-" + Guid.NewGuid().ToString("N"));
        if (fromJson) {
            feature.AppData = new ChatAppData(configDirectory);
            var configPath = Path.Combine(feature.AppData.GetUserPath(), "mcp_client", "config.json");
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, """
            {"servers":[{"id":"test","endpoint":"https://example.com/mcp",
              "allowedUsers":["alice"],"allowedTools":["*"],"approval":"never"}]}
            """);
            extension.Enabled = true;
            extension.Ctx = new ExtensionContext(feature, "mcp_client"); extension.Install(extension.Ctx);
        } else {
            extension.Enabled = true;
            extension.Servers.Add(Server(McpClientApproval.Never));
            extension.Ctx = new ExtensionContext(feature, "mcp_client"); extension.Install(extension.Ctx);
        }
        var session = new Session(); extension.Manager.Factory = new Factory(session);
        Assert.That((await extension.ResolveAsync(Context("bob"), "mcp_test")), Is.Empty);
        Assert.That((await extension.ResolveAsync(Context(), "all")), Is.Empty);
        Assert.That((await extension.ResolveAsync(Context(), "none")), Is.Empty);
        var context = Context(); context.RunId = 9; context.StepId = 10; context.Items["trace"] = "retained";
        context.ResolvedTools = await feature.Tools.ResolveAsync(context);
        var tool = context.ResolvedTools.Tools.Single();
        Assert.That(feature.Tools.Tools, Is.Empty);
        var result = await feature.ExecToolAsync(tool.Name, new JsonObject { ["user"] = "remote-account" }, context);
        Assert.That(result.Content, Does.Contain("structuredContent"));
        Assert.That(session.Calls, Is.EqualTo(1));
        Assert.That(session.Arguments!["user"]!.GetValue<string>(), Is.EqualTo("remote-account"));
        await feature.ExecToolAsync("mcp_guessed", new JsonObject(), context);
        Assert.That(session.Calls, Is.EqualTo(1));
        extension.Store.SetDisconnected(extension.GetServers("alice")[0], "alice", true);
        await feature.ExecToolAsync(tool.Name, new JsonObject { ["user"] = "remote-account" }, context);
        Assert.That(session.Calls, Is.EqualTo(1));
        await feature.RunAsyncShutdownHandlers();
        if (Directory.Exists(configDirectory)) Directory.Delete(configDirectory, true);
    }
    [Test]
    public async Task Direct_remote_calls_cannot_bypass_default_approval() {
        using var host = new BasicAppHost().Init();
        var factory = new OrmLiteConnectionFactory($"DataSource=file:mcp{Guid.NewGuid():N}?mode=memory&cache=shared", SqliteDialect.Provider);
        using var keepAlive = factory.OpenDbConnection();
        var feature = new ChatFeature { ChatDb = new ChatDb(factory), ChatAuth = new Auth() };
        var extension = feature.McpClient; extension.Enabled = true; extension.Servers.Add(Server());
        extension.Ctx = new ExtensionContext(feature, "mcp_client"); extension.Install(extension.Ctx);
        var session = new Session(); extension.Manager.Factory = new Factory(session);
        var context = Context(); context.ResolvedTools = await feature.Tools.ResolveAsync(context);
        var tool = context.ResolvedTools.Tools.Single();
        Assert.That(await tool.ApprovalHandler!(new JsonObject { ["user"] = "remote" }, context), Is.Not.Null);
        var result = await feature.ExecToolAsync(tool.Name, new JsonObject { ["user"] = "remote" }, context);
        Assert.That(result.Content, Does.Contain("approval"));
        Assert.That(session.Calls, Is.Zero);
        Assert.That(feature.ToolApprovalCoordinator, Is.Not.Null); // API Tools was never installed.
        await feature.RunAsyncShutdownHandlers();
    }
    [Test]
    public void Child_context_keeps_identity_metadata_and_correlation() {
        var context = Context(); context.ThreadId = 5; context.RunId = 6; context.StepId = 7;
        context.Items["trace"] = "same"; context.NoStore = true; context.ResolvedTools = new ResolvedChatTools([]);
        var child = context.CreateChild(default);
        Assert.That(child.Request, Is.SameAs(context.Request)); Assert.That(child.RunId, Is.EqualTo(6));
        Assert.That(child.StepId, Is.EqualTo(7)); Assert.That(child.NoStore, Is.True);
        Assert.That(child.Items["trace"], Is.EqualTo("same")); Assert.That(child.ResolvedTools, Is.SameAs(context.ResolvedTools));
    }
    [Test]
    public async Task Personal_bearer_tokens_are_encrypted_owner_bound_and_rotated() {
        using var host = new BasicAppHost().Init();
        host.Container.Register<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        var factory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);
        var feature = new ChatFeature { ChatDb = new ChatDb(factory), ChatAuth = new Auth() };
        var extension = feature.McpClient; extension.Enabled = true;
        var server = new McpClientServer { Id = "github", Endpoint = new Uri("https://api.githubcopilot.com/mcp/"),
            Auth = McpClientAuth.Bearer(), AllowedTools = ["*"] };
        extension.Servers.Add(server);
        extension.Ctx = new ExtensionContext(feature, "mcp_client"); extension.Install(extension.Ctx);
        try {
            extension.SaveBearerCredential(server, "alice", "fixture-pat");
            var binding = extension.Store.GetBinding(server, "alice")!;
            Assert.That(binding.ProtectedTokens, Does.Not.Contain("fixture-pat"));
            Assert.That(extension.ReadBearerCredential(server, "alice").AccessToken, Is.EqualTo("fixture-pat"));
            Assert.Throws<McpClientException>(() => extension.ReadBearerCredential(server, "bob"));
            Assert.Throws<HttpError>(() => extension.SaveBearerCredential(server, "alice", "Bearer secret"));
            var session = new Session(); var transport = new Factory(session); extension.Manager.Factory = transport;
            await extension.Manager.CatalogAsync(server, Context());
            Assert.That(transport.Credential!.AccessToken, Is.EqualTo("fixture-pat"));
            extension.SaveBearerCredential(server, "alice", "replacement-pat");
            Assert.That(extension.Store.GetBinding(server, "alice")!.Revision, Is.GreaterThan(binding.Revision));
            await extension.Manager.CatalogAsync(server, Context());
            Assert.That(transport.Credential!.AccessToken, Is.EqualTo("replacement-pat"));
            extension.Store.SetDisconnected(server, "alice", true, delete: true);
            Assert.Throws<McpClientException>(() => extension.ReadBearerCredential(server, "alice"));
        } finally { await feature.RunAsyncShutdownHandlers(); }
    }
    [Test]
    public void Credential_store_encrypts_tokens_and_prevents_stale_writes() {
        var factory = new OrmLiteConnectionFactory($"DataSource=file:mcp{Guid.NewGuid():N}?mode=memory&cache=shared", SqliteDialect.Provider);
        using var keepAlive = factory.OpenDbConnection();
        var store = new McpClientStore(new ChatDb(factory), new EphemeralDataProtectionProvider()); store.InitSchema();
        var server = Server(); var binding = store.EnsureBinding(server, "alice");
        store.WriteTokens(binding, "secret-value");
        var stored = store.GetBinding(server, "alice")!;
        Assert.That(stored.ProtectedTokens, Does.Not.Contain("secret-value"));
        Assert.That(store.ReadTokens(stored), Is.EqualTo("secret-value"));
        Assert.That(store.GetBinding(server, "bob"), Is.Null);
        store.SetDisconnected(server, "alice", true, true);
        Assert.Throws<McpClientException>(() => store.WriteTokens(stored, "stale-secret"));
        Assert.That(store.GetBinding(server, "alice")!.ProtectedTokens, Is.Null);
        store.InitSchema(); // additive migrations are repeatable
    }
    [Test]
    public void Approval_grants_are_owner_connection_tool_and_schema_scoped_and_revocable() {
        var factory = new OrmLiteConnectionFactory($"DataSource=file:mcp{Guid.NewGuid():N}?mode=memory&cache=shared", SqliteDialect.Provider);
        using var keepAlive = factory.OpenDbConnection();
        var store = new McpClientStore(new ChatDb(factory), null); store.InitSchema();
        var server = Server();
        var tool = new McpClientTool("echo", "alias", "description", Json(Input), null, "schema-one");
        Assert.That(store.HasApprovalGrant("alice", server, tool), Is.False);
        store.SaveApprovalGrant("alice", server, tool);
        Assert.That(store.HasApprovalGrant("alice", server, tool), Is.True);
        Assert.That(store.HasApprovalGrant("bob", server, tool), Is.False);
        Assert.That(store.ApprovalGrants("alice", server).Single().ToolName, Is.EqualTo("echo"));
        var changedTool = tool with { SchemaHash = "schema-two" };
        Assert.That(store.HasApprovalGrant("alice", server, changedTool), Is.False);
        store.RevokeApprovalGrant("alice", server, "echo");
        Assert.That(store.HasApprovalGrant("alice", server, tool), Is.False);
    }
    [Test]
    public void OAuth_transactions_are_owner_bound_one_time_encrypted_and_cancelable() {
        var factory = new OrmLiteConnectionFactory($"DataSource=file:mcp{Guid.NewGuid():N}?mode=memory&cache=shared", SqliteDialect.Provider);
        using var keepAlive = factory.OpenDbConnection();
        var store = new McpClientStore(new ChatDb(factory), new EphemeralDataProtectionProvider()); store.InitSchema();
        var row = new ChatMcpAuthorization { Id = "state-hash", Owner = "alice", ServerId = "test", ConfigurationHash = "config",
            Issuer = "https://issuer.example", RedirectUri = "https://host.example/callback", ExpiresAt = DateTime.UtcNow.AddMinutes(5) };
        store.CreateAuthorization(row);
        Assert.That(store.GetAuthorization(row.Id, "bob"), Is.Null);
        Assert.That(store.SubmitAuthorization(row, "secret-code"), Is.True);
        Assert.That(store.SubmitAuthorization(row, "replay"), Is.False);
        var stored = store.GetAuthorization(row.Id, "alice")!;
        Assert.That(stored.ProtectedResponse, Does.Not.Contain("secret-code"));
        Assert.That(store.AuthorizationResponse(stored), Is.EqualTo("secret-code"));
        store.CancelAuthorizations("alice", "test");
        Assert.That(store.GetAuthorization(row.Id, "alice")!.ProtectedResponse, Is.Null);
        Assert.That(store.SubmitAuthorization(row, "after-cancel"), Is.False);
    }
    [Test]
    public void Credential_leases_exclude_other_workers_and_invocations_never_replay() {
        var factory = new OrmLiteConnectionFactory($"DataSource=file:mcp{Guid.NewGuid():N}?mode=memory&cache=shared", SqliteDialect.Provider);
        using var keepAlive = factory.OpenDbConnection();
        var store = new McpClientStore(new ChatDb(factory), null); store.InitSchema();
        var binding = store.EnsureBinding(Server(), "alice");
        var lease = store.ClaimCredential(binding);
        Assert.Throws<McpClientException>(() => store.ClaimCredential(binding));
        store.ReleaseCredential(binding, "wrong-owner");
        Assert.Throws<McpClientException>(() => store.ClaimCredential(binding));
        store.ReleaseCredential(binding, lease);
        Assert.DoesNotThrow(() => store.ClaimCredential(binding));
        var invocation = new ChatMcpInvocation { Id = "one-call", Owner = "alice", ServerId = "test", ToolName = "tool",
            ConfigurationHash = "config", SchemaHash = "schema", UpdatedAt = DateTime.UtcNow };
        store.Prepare(invocation); store.Outcome(invocation.Id, "dispatched");
        Assert.That(Assert.Throws<McpClientException>(() => store.Prepare(invocation))!.Code, Is.EqualTo("outcome_unknown"));
    }
    [Test]
    public void Results_preserve_structured_values_and_order_without_duplicating_text() {
        var tool = new McpClientTool("tool", "alias", "description", Json(Input), null, "schema");
        var result = McpClientResultMapper.Map(Json("""{"structuredContent":[1,2],"content":[{"type":"text","text":"before"},{"type":"text","text":"[1,2]"},{"type":"resource_link","name":"ref","uri":"https://example.com"},{"type":"text","text":"after"}],"isError":true}"""), tool, Server(), new McpClientLimits());
        Assert.That(result["structuredContent"]!.ToJsonString(), Is.EqualTo("[1,2]"));
        Assert.That(result["content"]!.AsArray().Count, Is.EqualTo(3));
        Assert.That(result["content"]![0]!["text"]!.GetValue<string>(), Is.EqualTo("before"));
        Assert.That(result["content"]![2]!["text"]!.GetValue<string>(), Is.EqualTo("after"));
        Assert.That(result["isError"]!.GetValue<bool>(), Is.True);
        Assert.That(result["resources"]!.AsArray(), Is.Empty);
    }
    [Test]
    public void Media_is_inline_and_bad_mime_is_rejected_without_public_cache() {
        var tool = new McpClientTool("tool", "alias", "description", Json(Input), null, "schema");
        var input = Json("""{"content":[{"type":"image","mimeType":"image/png","data":"iVBORw0KGgo="}]}""");
        var result = McpClientResultMapper.Map(input, tool, Server(), new McpClientLimits());
        Assert.That(result["resources"]![0]!["image_url"]!["url"]!.GetValue<string>(), Does.StartWith("data:image/png;base64,"));
        input["content"]![0]!["mimeType"] = "image/jpeg";
        Assert.Throws<McpClientException>(() => McpClientResultMapper.Map(input, tool, Server(), new McpClientLimits()));
    }
    [TestCase(null)]
    [TestCase("fixture-secret")]
    public async Task Browser_OAuth_flow_exchanges_PKCE_and_stores_tokens_without_loopback_callback(string? clientSecret)
    {
        using var host = new BasicAppHost().Init();
        host.Container.Register<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        using var reserve = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        reserve.Start(); var port = ((IPEndPoint)reserve.LocalEndpoint).Port; reserve.Stop();
        var origin = $"http://127.0.0.1:{port}";
        var endpoint = new Uri(origin + "/mcp");
        using var listener = new HttpListener(); listener.Prefixes.Add(origin + "/"); listener.Start();
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        string? verifier = null, resource = null, submittedSecret = null, tokenAccept = null;
        var requests = new System.Collections.Concurrent.ConcurrentQueue<string>();
        var serving = Task.Run(async () => {
            try {
                while (!stop.IsCancellationRequested) {
                    var request = await listener.GetContextAsync().WaitAsync(stop.Token);
                    var path = request.Request.Url!.AbsolutePath;
                    requests.Enqueue(request.Request.HttpMethod + " " + path);
                    string json;
                    if (path == "/resource") json = new JsonObject { ["resource"] = endpoint.AbsoluteUri,
                        ["authorization_servers"] = new JsonArray(origin + "/issuer"), ["scopes_supported"] = new JsonArray("tools.read") }.ToJsonString();
                    else if (path.Contains(".well-known")) json = new JsonObject {
                        ["issuer"] = origin + "/issuer", ["authorization_endpoint"] = origin + "/authorize", ["token_endpoint"] = origin + "/token",
                        ["response_types_supported"] = new JsonArray("code"), ["grant_types_supported"] = new JsonArray("authorization_code", "refresh_token"),
                        ["code_challenge_methods_supported"] = new JsonArray("S256"), ["token_endpoint_auth_methods_supported"] = new JsonArray(clientSecret == null ? "none" : "client_secret_post"),
                    }.ToJsonString();
                    else if (path == "/token") {
                        tokenAccept = request.Request.Headers["Accept"];
                        using var reader = new System.IO.StreamReader(request.Request.InputStream);
                        var form = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(await reader.ReadToEndAsync());
                        verifier = form["code_verifier"].ToString(); resource = form["resource"].ToString();
                        submittedSecret = form.TryGetValue("client_secret", out var value) ? value.ToString() : null;
                        json = tokenAccept?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true
                            ? """{"access_token":"fixture-access","refresh_token":"fixture-refresh","token_type":"Bearer","expires_in":3600,"scope":"tools.read"}"""
                            : "access_token=fixture-access&token_type=Bearer";
                    } else if (request.Request.Headers["Authorization"] != "Bearer fixture-access") {
                        request.Response.StatusCode = 401;
                        request.Response.Headers["WWW-Authenticate"] = $"Bearer resource_metadata=\"{origin}/resource\"";
                        request.Response.Close(); continue;
                    } else {
                        using var reader = new System.IO.StreamReader(request.Request.InputStream);
                        var body = Json(await reader.ReadToEndAsync());
                        if (body["id"] == null) { request.Response.StatusCode = 202; request.Response.Close(); continue; }
                        var result = body["method"]?.GetValue<string>() == "server/discover"
                            ? Json("""{"supportedVersions":["2026-07-28"],"capabilities":{"tools":{}}}""")
                            : Json("""{"tools":[]}""");
                        json = new JsonObject { ["jsonrpc"] = "2.0", ["id"] = body["id"]!.DeepClone(), ["result"] = result }.ToJsonString();
                    }
                    request.Response.ContentType = "application/json";
                    await request.Response.OutputStream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(json)); request.Response.Close();
                }
            } catch (OperationCanceledException) { } catch (HttpListenerException) { }
        });
        var factory = new OrmLiteConnectionFactory($"DataSource=file:mcp{Guid.NewGuid():N}?mode=memory&cache=shared", SqliteDialect.Provider);
        using var keepAlive = factory.OpenDbConnection();
        var feature = new ChatFeature { ChatDb = new ChatDb(factory), ChatAuth = new Auth() };
        var extension = feature.McpClient; extension.Enabled = true;
        extension.OAuthRedirectUri = new Uri("https://chat.example/chat/ext/mcp_client/oauth/callback");
        extension.NetworkPolicy.AllowHttp = true; extension.NetworkPolicy.AllowedPorts.Add(port);
        extension.NetworkPolicy.AllowPrivateAddress = (name, ip) => name == "127.0.0.1" && IPAddress.IsLoopback(ip);
        var server = new McpClientServer { Id = "oauth", Endpoint = endpoint, Auth = McpClientAuth.UserOAuth(),
            OAuthClientId = "fixture-public-client", OAuthIssuer = new Uri(origin + "/issuer"), OAuthScopes = ["tools.read"] };
        extension.Servers.Add(server); extension.Ctx = new ExtensionContext(feature, "mcp_client"); extension.Install(extension.Ctx);
        if (clientSecret != null) extension.Store.SaveClientSecret(server, "alice", clientSecret);
        try {
            string? url;
            try { url = await extension.OAuth.StartAsync(server, Context()); }
            catch { TestContext.WriteLine(string.Join("\n", requests)); if (serving.IsFaulted) TestContext.WriteLine(serving.Exception); throw; }
            Assert.That(url, Does.StartWith(origin + "/authorize"));
            var parameters = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(url!).Query);
            Assert.That(parameters["redirect_uri"].ToString(), Is.EqualTo(extension.OAuthRedirectUri.AbsoluteUri));
            var callback = new BasicRequest(); callback.Items["principal"] = "alice";
            callback.QueryString["state"] = parameters["state"].ToString(); callback.QueryString["code"] = "fixture-code";
            callback.QueryString["iss"] = origin + "/issuer";
            try { Assert.That(await extension.OAuth.CompleteAsync(new ChatRequestContext(feature, callback, [])), Is.EqualTo(server.Id)); }
            catch { TestContext.WriteLine(string.Join("\n", requests)); if (serving.IsFaulted) TestContext.WriteLine(serving.Exception); throw; }
            Assert.That(extension.Manager.Status(server, "alice")["state"]!.GetValue<string>(), Is.EqualTo("ready"));
            Assert.That(extension.Store.GetBinding(server, "alice")!.LeaseId, Is.Null);
            var requestsBeforeCachedCatalog = requests.Count;
            await extension.Manager.CatalogAsync(server, Context());
            Assert.That(requests.Count, Is.EqualTo(requestsBeforeCachedCatalog));
            var challenge = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier!)));
            Assert.That(challenge, Is.EqualTo(parameters["code_challenge"].ToString()));
            Assert.That(resource, Is.EqualTo(endpoint.AbsoluteUri));
            Assert.That(submittedSecret, Is.EqualTo(clientSecret));
            Assert.That(tokenAccept, Does.Contain("application/json"));
            var binding = extension.Store.GetBinding(server, "alice")!;
            Assert.That(binding.ProtectedClientSecret, clientSecret == null ? Is.Null : Does.Not.Contain(clientSecret));
            Assert.That(binding.ProtectedTokens, Does.Not.Contain("fixture-access"));
            Assert.That(extension.Store.ReadTokens(binding), Does.Contain("fixture-access"));
            Assert.ThrowsAsync<McpClientException>(async () => await extension.OAuth.CompleteAsync(new ChatRequestContext(feature, callback, [])));
            extension.Store.SetDisconnected(server, "alice", true, delete: true, preserveClientSecret: true);
            var cleared = extension.Store.GetBinding(server, "alice")!;
            Assert.That(cleared.ProtectedClientSecret, clientSecret == null ? Is.Null : Is.Not.Null);
            Assert.That(cleared.ProtectedTokens, Is.Null);
            extension.Store.SetDisconnected(server, "alice", false);
            var nextUrl = await extension.OAuth.StartAsync(server, Context());
            var nextParameters = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(new Uri(nextUrl!).Query);
            Assert.That(nextParameters["state"].ToString(), Is.Not.EqualTo(parameters["state"].ToString()));
            var nextCallback = new BasicRequest(); nextCallback.Items["principal"] = "alice";
            nextCallback.QueryString["state"] = nextParameters["state"].ToString();
            nextCallback.QueryString["code"] = "fixture-code-again";
            nextCallback.QueryString["iss"] = origin + "/issuer";
            Assert.That(await extension.OAuth.CompleteAsync(new ChatRequestContext(feature, nextCallback, [])), Is.EqualTo(server.Id));
            Assert.That(submittedSecret, Is.EqualTo(clientSecret));
            Assert.That(extension.Store.ReadTokens(extension.Store.GetBinding(server, "alice")!), Does.Contain("fixture-access"));
            var github = new McpClientServer { Id = "github", Endpoint = new Uri("https://api.githubcopilot.com/mcp/"),
                Auth = McpClientAuth.UserOAuth(), OAuthClientId = "fixture-github-client",
                OAuthIssuer = new Uri("https://github.com/login/oauth") };
            extension.Servers.Add(github);
            var missingSecret = Assert.ThrowsAsync<McpClientException>(async () => await extension.OAuth.StartAsync(github, Context()));
            Assert.That(missingSecret!.Code, Is.EqualTo("oauth_client_secret_required"));
        } finally { await feature.RunAsyncShutdownHandlers(); stop.Cancel(); listener.Stop(); await serving; }
    }
    sealed class JsonRequest(string json) : BasicRequest, IRequest
    {
        public new string GetRawBody() => json;
        public new Task<string> GetRawBodyAsync() => Task.FromResult(json);
    }
    [Test]
    public async Task MCP_approval_executes_edited_arguments_once_and_keeps_batch_paused()
    {
        using var host = new BasicAppHost().Init();
        var factory = new OrmLiteConnectionFactory($"DataSource=file:mcp{Guid.NewGuid():N}?mode=memory&cache=shared", SqliteDialect.Provider);
        using var conn = factory.OpenDbConnection();
        var db = new ChatDb(factory); db.InitSchema();
        var feature = new ChatFeature { ChatDb = db, ChatAuth = new Auth(),
            ThreadApi = new DbThreadApi(db, new ThreadUpdates(), Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance) };
        var extension = feature.McpClient; extension.Enabled = true; extension.Servers.Add(Server());
        extension.Ctx = new ExtensionContext(feature, "mcp_client"); extension.Install(extension.Ctx);
        var session = new Session(); extension.Manager.Factory = new Factory(session);
        try {
            var context = Context();
            context.ThreadId = db.InsertThread(new ChatThread { User = "alice", CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now, Messages = "[]" });
            context.ResolvedTools = await feature.Tools.ResolveAsync(context);
            var tool = context.ResolvedTools.Tools.Single();
            var proposed = new JsonObject { ["user"] = "proposed-remote-user" };
            var approval = (await tool.ApprovalHandler!(proposed, context))!;
            var coordinator = (ApiToolApprovalCoordinator)feature.ToolApprovalCoordinator!;
            await coordinator.PauseAsync([
                new PendingChatToolCall { ToolCallId = "one", ToolName = tool.Name, Arguments = proposed, Approval = approval, Sequence = 0 },
                new PendingChatToolCall { ToolCallId = "two", ToolName = tool.Name, Arguments = proposed, Approval = approval, Sequence = 1 },
            ], context);
            var row = conn.Select<ChatToolApproval>().Single(x => x.ToolCallId == "one");
            var route = feature.Routes.Match("POST", $"/ext/mcp_client/approvals/{row.Id}/approve")!.Value;
            var request = new JsonRequest("""{"args":{"user":"edited-remote-user"}}""");
            request.Items["principal"] = "alice"; request.Headers["X-Mcp-Client"] = "1";
            await route.Route.Handler(new ChatRequestContext(feature, request, route.Params));
            Assert.That(session.Calls, Is.EqualTo(1));
            Assert.That(session.Arguments!["user"]!.GetValue<string>(), Is.EqualTo("edited-remote-user"));
            Assert.That(conn.SingleById<ChatToolApproval>(row.Id).Status, Is.EqualTo("completed"));
            Assert.That(coordinator.HasPending(context.ThreadId.Value, "alice"), Is.True);
            await route.Route.Handler(new ChatRequestContext(feature, request, route.Params));
            Assert.That(session.Calls, Is.EqualTo(1));
            request.Items["principal"] = "bob";
            Assert.ThrowsAsync<HttpError>(async () => await route.Route.Handler(new ChatRequestContext(feature, request, route.Params)));
            Assert.That(session.Calls, Is.EqualTo(1));
            await coordinator.CancelThreadAsync(context.ThreadId.Value, "alice");
            Assert.That(conn.Select<ChatToolApproval>().Single(x => x.ToolCallId == "two").Status, Is.EqualTo("canceled"));
        } finally { await feature.RunAsyncShutdownHandlers(); }
    }
}
