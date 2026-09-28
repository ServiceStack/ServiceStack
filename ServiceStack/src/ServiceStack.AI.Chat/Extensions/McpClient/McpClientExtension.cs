using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.DataProtection;
using ServiceStack.Web;

namespace ServiceStack.AI;

/// <summary>Host-configured, user-scoped outbound Streamable HTTP tools. Independent of the MCP server.</summary>
public sealed partial class McpClientExtension() : ChatExtension("mcp_client"), IChatToolProvider, IHasSchema
{
    public List<McpClientServer> Servers { get; } = [];
    public McpClientLimits Limits { get; } = new();
    public McpClientNetworkPolicy NetworkPolicy { get; } = new();
    public IMcpClientCredentialStore? CredentialStore { get; set; }
    public Uri? OAuthRedirectUri { get; set; }
    /// <summary>Resolve current host identity for durable runs after restart. No username-only authorization fallback.</summary>
    public Func<string, CancellationToken, Task<IRequest?>>? ReauthorizeBackgroundRequest { get; set; }
    public bool SingleUserMode { get; set; }
    public string SingleUserPartition { get; set; } = "default";
    public string? InitializationError { get; private set; }
    internal McpClientStore Store { get; private set; } = null!;
    internal McpClientOAuthService OAuth { get; private set; } = null!;
    internal McpClientConnectionManager Manager { get; private set; } = null!;
    ApiToolApprovalCoordinator? approvals;
    bool installed;

    public override void Install(ExtensionContext ctx)
    {
        try {
            Limits.Validate();
            _ = GetServers(null);
            if (Servers.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != Servers.Count)
                throw new ArgumentException("Duplicate MCP server ids");
            foreach (var server in Servers) { server.Validate(); NetworkPolicy.AssertUri(server.Endpoint); }
            if (!ctx.Feature.RequireAuth && (!SingleUserMode || SingleUserPartition != ChatDb.DefaultUser))
                throw new ArgumentException("MCP without authentication requires explicit single-user mode with the default partition");
            if (Servers.Any(x => x.Auth.Mode == "user_oauth") && (OAuthRedirectUri == null || !ctx.Feature.RequireAuth))
                throw new ArgumentException("OAuth requires authentication and a trusted callback URL");
            if (OAuthRedirectUri != null) {
                McpClientNetworkPolicy.ValidateUri(OAuthRedirectUri);
                if (OAuthRedirectUri.Scheme != "https" || OAuthRedirectUri.Query.Length != 0)
                    throw new ArgumentException("OAuth callback must be a trusted HTTPS URL without query parameters");
            }
            var protection = HostContext.TryResolve<IDataProtectionProvider>();
            if (Servers.Any(x => x.Auth.Mode == "user_oauth") && protection == null)
                throw new ArgumentException("OAuth requires host Data Protection with persistent shared keys");
            Store = new McpClientStore(ctx.Feature.ChatDb ?? throw new ArgumentException("MCP requires ChatDb"), protection);
            if (ctx.Feature.AutoInitSchema) Store.InitSchema();
        } catch {
            InitializationError = "invalid_configuration"; ctx.Disabled = true; Disabled = true; throw;
        }
        Manager = new McpClientConnectionManager(this);
        OAuth = new McpClientOAuthService(this);
        approvals = ApiToolApprovalCoordinator.GetOrCreate(ctx.Feature.ApiTools, ctx);
        ctx.RegisterToolProvider(this);
        ctx.RegisterShutdownHandler(async token => {
            installed = false;
            await OAuth.DisposeAsync().ConfigureAwait(false);
            await Manager.DisposeAsync().ConfigureAwait(false);
        });
        approvals.Executors[Name] = ExecuteApprovalAsync;
        approvals.RegisterRoutes(ctx);
        installed = true;
        RegisterConfigurationRoutes(ctx);
        ctx.AddPost("oauth/issuers", async req => {
            AssertMutation(req);
            _ = Context(req);
            var body = await req.GetJsonBodyAsync().ConfigureAwait(false);
            if (!Uri.TryCreate(body.GetString("endpoint"), UriKind.Absolute, out var endpoint))
                throw HttpError.BadRequest("Enter a valid MCP server URL");
            var issuers = await McpClientIssuerDiscovery.DiscoverAsync(endpoint, NetworkPolicy, Limits).ConfigureAwait(false);
            return new JsonObject { ["issuers"] = new JsonArray(issuers.Select(x => (JsonNode)JsonValue.Create(x)!).ToArray()) };
        });
        ctx.AddGet("connections", async req => {
            var context = Context(req);
            var result = new JsonArray();
            foreach (var server in GetServers(context.User))
                if (await CanAccessAsync(server, context, "discover").ConfigureAwait(false)) {
                    var status = Manager.Status(server, context.User!);
                    status["scope"] = server.ConfigurationScope == "shared" ? "shared" : "personal";
                    result.Add(status);
                }
            return result;
        });
        ctx.AddPost("connections/connect-all", async req => {
            AssertMutation(req);
            return await ConnectAllAsync(Context(req)).ConfigureAwait(false);
        });
        ctx.AddGet("connections/{id}/tools", async req => {
            var server = Server(req);
            var context = Context(req);
            var catalog = await Manager.CatalogAsync(server, context).ConfigureAwait(false);
            return new JsonArray(catalog.Select(x => (JsonNode)new JsonObject {
                ["name"] = x.Alias, ["remoteName"] = x.Name, ["description"] = x.Description,
                ["schema"] = x.Schema.DeepClone(), ["requiresApproval"] = NeedsApproval(server, x, context.User!),
                ["alwaysApproved"] = Store.HasApprovalGrant(context.User!, server, x),
                ["group"] = "mcp_" + server.Id,
            }).ToArray());
        });
        ctx.AddGet("connections/{id}/approval-grants", async req => {
            var server = Server(req); var context = Context(req);
            await AssertAccessAsync(server, context, "discover").ConfigureAwait(false);
            return new JsonArray(Store.ApprovalGrants(context.User!, server).Select(x => (JsonNode)new JsonObject {
                ["tool"] = x.ToolName, ["schemaHash"] = x.SchemaHash,
            }).ToArray());
        });
        ctx.AddPost("connections/{id}/approval-grants/revoke", async req => {
            AssertMutation(req);
            var server = Server(req); var context = Context(req);
            await AssertAccessAsync(server, context, "connect").ConfigureAwait(false);
            var body = await req.GetJsonBodyAsync().ConfigureAwait(false);
            var tool = body.GetString("tool");
            if (string.IsNullOrWhiteSpace(tool)) throw HttpError.BadRequest("Tool name required");
            Store.RevokeApprovalGrant(context.User!, server, tool);
            return new JsonObject { ["revoked"] = true };
        });
        ctx.AddPost("connections/{id}/refresh", async req => {
            AssertMutation(req);
            var server = Server(req);
            await Manager.CatalogAsync(server, Context(req), refresh: true).ConfigureAwait(false);
            return Manager.Status(server, req.UserName!);
        });
        ctx.AddPost("connections/{id}/connect", async req => {
            AssertMutation(req);
            var server = Server(req); var context = Context(req);
            await AssertAccessAsync(server, context, "connect").ConfigureAwait(false);
            Store.SetDisconnected(server, context.User!, false);
            var url = server.Auth.Mode == "user_oauth" ? await OAuth.StartAsync(server, context).ConfigureAwait(false) : null;
            if (server.Auth.Mode != "user_oauth") await Manager.CatalogAsync(server, context, true).ConfigureAwait(false);
            return new JsonObject { ["authorizationUrl"] = url };
        });
        ctx.AddGet("oauth/callback", async req => {
            try {
                var serverId = await OAuth.CompleteAsync(req).ConfigureAwait(false);
                return OAuthCallbackPage(true, serverId: serverId);
            } catch (McpClientException ex) {
                // Browser callback responses never reflect the authorization code or state.
                return OAuthCallbackPage(false, ex.Code);
            }
        });
        ctx.AddPost("connections/{id}/disconnect", async req => {
            AssertMutation(req); var server = Server(req);
            await AssertAccessAsync(server, Context(req), "connect").ConfigureAwait(false);
            Store.SetDisconnected(server, req.UserName!, true);
            OAuth.Cancel(req.UserName!, server.Id); Manager.Invalidate(server, req.UserName!);
            return new JsonObject { ["state"] = "disconnected" };
        });
        ctx.AddPost("connections/{id}/credentials", async req => {
            AssertMutation(req);
            var server = Server(req);
            await AssertAccessAsync(server, Context(req), "connect").ConfigureAwait(false);
            var body = await req.GetJsonBodyAsync().ConfigureAwait(false);
            SaveBearerCredential(server, req.UserName!, body.GetString("token") ?? "");
            return new JsonObject { ["saved"] = true };
        });
        ctx.AddPost("connections/{id}/oauth-client-secret", async req => {
            AssertMutation(req);
            var server = Server(req);
            await AssertAccessAsync(server, Context(req), "connect").ConfigureAwait(false);
            if (server.Auth.Mode != "user_oauth") throw HttpError.BadRequest("This connection does not use OAuth");
            var body = await req.GetJsonBodyAsync().ConfigureAwait(false);
            var secret = body.GetString("secret") ?? "";
            if (string.IsNullOrWhiteSpace(secret) || secret.Length > 8192 || secret.Any(char.IsControl))
                throw HttpError.BadRequest("Enter a valid OAuth client secret");
            Store.SaveClientSecret(server, req.UserName!, secret);
            OAuth.Cancel(req.UserName!, server.Id);
            Manager.Invalidate(server, req.UserName!);
            return new JsonObject { ["saved"] = true };
        });
        ctx.AddDelete("connections/{id}/credentials", async req => {
            AssertMutation(req); var server = Server(req);
            await AssertAccessAsync(server, Context(req), "connect").ConfigureAwait(false);
            // Clearing an OAuth account removes its tokens, not its registered app secret.
            Store.SetDisconnected(server, req.UserName!, true, delete: true,
                preserveClientSecret: server.Auth.Mode == "user_oauth");
            OAuth.Cancel(req.UserName!, server.Id); Manager.Invalidate(server, req.UserName!);
            return new JsonObject { ["state"] = "disconnected" };
        });
    }

    internal async Task<JsonArray> ConnectAllAsync(ChatContext context)
    {
        var results = new JsonArray();
        foreach (var server in GetServers(context.User)) {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (!await CanAccessAsync(server, context, "connect").ConfigureAwait(false)) continue;
            if (Store.GetBinding(server, context.User!)?.Disconnected == true) continue;
            try {
                // Reuse saved credentials. Never enable a disabled binding or start browser sign-in.
                await Manager.CatalogAsync(server, context, refresh: true).ConfigureAwait(false);
                results.Add(new JsonObject { ["id"] = server.Id, ["name"] = server.DisplayName ?? server.Id, ["connected"] = true });
            } catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested) { throw; }
            catch (Exception e) {
                var error = McpClientErrors.Connection(e);
                results.Add(new JsonObject { ["id"] = server.Id, ["name"] = server.DisplayName ?? server.Id,
                    ["connected"] = false, ["error"] = error.Message });
            }
        }
        return results;
    }

    internal static void AssertMutation(ChatRequestContext req)
    {
        if (!req.Feature.RequireAuth) throw HttpError.Forbidden("Connection mutation requires authentication");
        // A non-simple header plus same-origin Fetch Metadata prevents cookie CSRF. Hosts must not
        // grant credentialed CORS access to untrusted origins on these endpoints.
        if (req.Request.Headers["X-Mcp-Client"] != "1" || req.Request.Headers["Sec-Fetch-Site"] is "cross-site")
            throw HttpError.Forbidden("MCP connection request verification failed");
    }
    internal async Task AssertAccessAsync(McpClientServer server, ChatContext context, string operation)
    {
        if (!await CanAccessAsync(server, context, operation).ConfigureAwait(false))
            throw new McpClientException("access_denied", "This connection is not available to the current principal");
    }
    async Task<bool> CanAccessAsync(McpClientServer server, ChatContext context, string operation)
    {
        if (!installed || Disabled || !GetServers(context.User).Any(x => x.Id == server.Id && x.ConfigurationHash == server.ConfigurationHash)) return false;
        if (context.RunId != null && ReauthorizeBackgroundRequest != null) {
            var currentRequest = await ReauthorizeBackgroundRequest(context.User ?? "", context.CancellationToken).ConfigureAwait(false);
            if (currentRequest == null) return false;
            context.Request = currentRequest;
        }
        if (context.Request == null) return false;
        var user = Feature.ChatAuth.GetUserName(context.Request);
        if (string.IsNullOrEmpty(user) || user != context.User || user is "all" or "*") return false;
        if (Feature.RequireAuth && !Feature.ChatAuth.CheckAuth(context.Request).IsAuthenticated) return false;
        if (!Feature.RequireAuth && (!SingleUserMode || user != SingleUserPartition)) return false;
        if (server.AllowedUsers.Count != 0 && !server.AllowedUsers.Contains(user, StringComparer.Ordinal)) return false;
        if (server.RequiredRoles.Count != 0) {
            var session = context.Request.GetSession();
            if (!server.RequiredRoles.All(role => session?.Roles?.Contains(role) == true
                || context.Request.GetClaimsPrincipal()?.IsInRole(role) == true)) return false;
        }
        return server.Authorize == null || await server.Authorize(context, operation).ConfigureAwait(false);
    }
    static ChatContext Context(ChatRequestContext req) => new() { User = req.UserName, Request = req.Request, CancellationToken = req.Request.RequestAborted };
    McpClientServer Server(ChatRequestContext req) => GetServers(req.UserName).SingleOrDefault(x => x.Id == req.GetPathParam("id"))
        ?? throw HttpError.NotFound("Connection not found");
    bool NeedsApproval(McpClientServer server, McpClientTool tool, string owner) =>
        server.NeedsApproval(tool.Name) && !Store.HasApprovalGrant(owner, server, tool);

    public async Task<IReadOnlyList<ChatTool>> ResolveAsync(ChatContext context, string selector)
    {
        var result = new List<ChatTool>();
        if (!installed || Disabled || selector == "none" || context.Items.ContainsKey(ChatContext.McpTransport)
            || context.ModelInfo?["tool_call"] is JsonValue capability && capability.TryGetValue<bool>(out var supported) && !supported) return result;
        var selected = selector.Split(',').Select(x => x.Trim()).ToHashSet(StringComparer.Ordinal);
        foreach (var server in GetServers(context.User)) {
            if (selector != "__list" && !(selector == "all" && server.IncludeInAll)
                && !selected.Contains("mcp_" + server.Id) && !selected.Any(x => x.StartsWith("mcp_" + server.Id + "_", StringComparison.Ordinal))) continue;
            if (!await CanAccessAsync(server, context, "discover").ConfigureAwait(false)) continue;
            IReadOnlyList<McpClientTool> catalog;
            try { catalog = await Manager.CatalogAsync(server, context).ConfigureAwait(false); }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested) { throw; }
            catch { continue; } // One failed connection must not suppress local or other servers' tools.
            foreach (var remote in catalog) {
                if (selector != "__list" && !(selector == "all" && server.IncludeInAll) && !selected.Contains("mcp_" + server.Id) && !selected.Contains(remote.Alias)) continue;
                if (context.Provider is GoogleProvider && !McpClientSchema.GoogleCompatible(remote.Schema))
                    throw new McpClientException("unsupported_model_schema", "A selected MCP tool uses schema constraints this model cannot represent. Select another model or fewer tools.");
                if (Feature.Tools.GetTool(remote.Alias) != null) throw new McpClientException("alias_collision", "Imported alias conflicts with a local tool");
                var config = server.ConfigurationHash;
                var owner = context.User!;
                var binding = Store.GetBinding(server, owner)?.Revision ?? 0;
                var credentialRevision = await CredentialRevisionAsync(server, context.CancellationToken).ConfigureAwait(false);
                result.Add(new ChatTool {
                    Source = Name, Group = "mcp_" + server.Id, Safety = ToolSafety.Write,
                    Definition = new JsonObject { ["type"] = "function", ["function"] = new JsonObject {
                        ["name"] = remote.Alias, ["description"] = remote.Description,
                        ["parameters"] = remote.Schema.DeepClone(),
                    } },
                    OutputSchema = (JsonObject?)remote.OutputSchema?.DeepClone(),
                    ApprovalHandler = async (args, call) => {
                        await AssertAccessAsync(server, call, "invoke").ConfigureAwait(false);
                        AssertHandle(server, remote, call, owner, config, binding);
                        McpClientSchema.Validate(remote.Schema, args, Limits.MaxSchemaBytes);
                        return !NeedsApproval(server, remote, owner) ? null : new ChatToolApprovalRequest {
                            Title = (server.DisplayName ?? server.Id) + ": " + remote.Name,
                            Description = remote.Description, Schema = (JsonObject)remote.Schema.DeepClone(), Arguments = (JsonObject)args.DeepClone(),
                            Safety = ToolSafety.Write, Metadata = new JsonObject {
                                ["source"] = Name, ["serverId"] = server.Id, ["remoteName"] = remote.Name,
                                ["configurationHash"] = config, ["schemaHash"] = remote.SchemaHash,
                                ["bindingRevision"] = binding, ["credentialRevision"] = credentialRevision, ["version"] = 1,
                            },
                        };
                    },
                    Handler = (args, call) => InvokeAsync(server, remote, args, call, owner, config, binding, credentialRevision, approved: false),
                });
            }
        }
        if (result.Sum(x => Encoding.UTF8.GetByteCount(x.Definition.ToJsonString())) > Limits.MaxDefinitionBytes)
            throw new McpClientException("definition_limit", "Select fewer MCP connections or tools");
        return result;
    }
    void AssertHandle(McpClientServer server, McpClientTool remote, ChatContext context, string owner, string config, long binding)
    {
        var current = Store.GetBinding(server, owner);
        if (context.User != owner || server.ConfigurationHash != config || !server.Allows(remote.Name)
            || current?.Disconnected == true || (current?.Revision ?? 0) != binding)
            throw new McpClientException("stale_tool", "Connection or authorization changed; select the tool again");
    }
    async Task<object?> InvokeAsync(McpClientServer server, McpClientTool remote, JsonObject args, ChatContext context,
        string owner, string config, long binding, string credentialRevision, bool approved, string? invocationId = null)
    {
        await AssertAccessAsync(server, context, "invoke").ConfigureAwait(false);
        AssertHandle(server, remote, context, owner, config, binding);
        McpClientSchema.Validate(remote.Schema, args, Limits.MaxSchemaBytes);
        if (NeedsApproval(server, remote, owner) && !approved) throw new McpClientException("approval_required", "Interactive approval is required");
        var id = invocationId ?? context.ToolInvocationId ?? Guid.NewGuid().ToString("N");
        context.CancellationToken.ThrowIfCancellationRequested();
        if (Feature.ShouldCancelThread(context)) throw new McpClientException("canceled", "The conversation was canceled");
        Store.Prepare(new ChatMcpInvocation { Id = id, Owner = owner, ServerId = server.Id, ToolName = remote.Name,
            ConfigurationHash = config, SchemaHash = remote.SchemaHash, ThreadId = context.ThreadId, RunId = context.RunId, UpdatedAt = DateTime.UtcNow });
        if (approved) Store.Outcome(id, "approved");
        var dispatched = false;
        var received = false;
        using var activity = McpClientDiagnostics.Source.StartActivity("mcp.invoke", ActivityKind.Internal);
        activity?.SetTag("mcp.server", server.Id); activity?.SetTag("mcp.tool", remote.Alias); activity?.SetTag("mcp.invocation", id);
        var timer = Stopwatch.StartNew();
        McpClientDiagnostics.Active.Add(1);
        try {
            var result = await Manager.UseAsync(server, context, async (session, _, sessionRevision, token) => {
                // Re-discover immediately before dispatch to bind approval to the current schema.
                var tools = await McpClientCatalog.DiscoverAsync(session, server, Limits, token).ConfigureAwait(false);
                var current = tools.SingleOrDefault(x => x.Name == remote.Name);
                if (current?.SchemaHash != remote.SchemaHash) throw new McpClientException("stale_tool", "Tool schema changed; request a new approval");
                AssertHandle(server, remote, context, owner, config, binding);
                await AssertAccessAsync(server, context, "invoke").ConfigureAwait(false);
                if (await CredentialRevisionAsync(server, token).ConfigureAwait(false) != credentialRevision
                    || server.Auth.Mode == "host_secret" && !sessionRevision.EndsWith(":" + credentialRevision, StringComparison.Ordinal))
                    throw new McpClientException("stale_tool", "Credentials changed; request a new approval");
                if (!approved && NeedsApproval(server, remote, owner))
                    throw new McpClientException("approval_required", "Interactive approval is required");
                token.ThrowIfCancellationRequested();
                if (Feature.ShouldCancelThread(context)) throw new McpClientException("canceled", "The conversation was canceled");
                if (context.ThreadId is { } threadId && Feature.ChatDb?.GetThread(threadId, owner) is { } thread
                    && (thread.CompletedAt != null || thread.Error != null))
                    throw new McpClientException("canceled", "The conversation is no longer active");
                Store.Outcome(id, "dispatched"); dispatched = true;
                var response = await session.CallAsync(remote.Name, args, token).ConfigureAwait(false);
                received = true;
                return McpClientResultMapper.Map(response, remote, server, Limits);
            }).ConfigureAwait(false);
            var toolError = result["isError"]?.GetValue<bool>() == true;
            Store.Outcome(id, toolError ? "failed" : "completed", result.ToJsonString());
            if (toolError) {
                activity?.SetStatus(ActivityStatusCode.Error, "tool_error");
                McpClientDiagnostics.Errors.Add(1, new KeyValuePair<string, object?>("category", "tool_error"));
            }
            return result;
        } catch (Exception e) {
            var code = e is McpClientException m ? m.Code : "connection_error";
            Store.Outcome(id, dispatched && !received ? "outcome_unknown" : "failed");
            activity?.SetStatus(ActivityStatusCode.Error, code);
            McpClientDiagnostics.Errors.Add(1, new KeyValuePair<string, object?>("category", code));
            throw new McpClientException(dispatched && !received ? "outcome_unknown" : code,
                dispatched && !received ? (code is "unsupported_operation" or "replay_blocked" ? "Unsupported server continuation was blocked. " : "")
                    + "The remote outcome is uncertain. Do not repeat the operation without reconciliation." : (received ? "MCP returned an invalid result: " : "MCP call was not dispatched: ") + code);
        } finally {
            McpClientDiagnostics.Active.Add(-1);
            McpClientDiagnostics.Duration.Record(timer.Elapsed.TotalSeconds);
        }
    }
    async Task<string> ExecuteApprovalAsync(ChatToolApproval row, JsonObject args, ChatRequestContext request)
    {
        AssertMutation(request);
        var meta = JsonNode.Parse(row.SourceMetadata ?? "{}")!.AsObject();
        var server = GetServers(request.UserName).SingleOrDefault(x => x.Id == meta["serverId"]?.GetValue<string>())
            ?? throw new McpClientException("stale_tool", "Connection no longer exists");
        var context = Context(request); context.ThreadId = row.ThreadId;
        var catalog = await Manager.CatalogAsync(server, context, true).ConfigureAwait(false);
        var remote = catalog.SingleOrDefault(x => x.Name == meta["remoteName"]?.GetValue<string>() && x.SchemaHash == meta["schemaHash"]?.GetValue<string>())
            ?? throw new McpClientException("stale_tool", "Tool changed; request a new approval");
        var result = await InvokeAsync(server, remote, args, context, request.UserName!, meta["configurationHash"]!.GetValue<string>(),
            meta["bindingRevision"]!.GetValue<long>(), meta["credentialRevision"]?.GetValue<string>() ?? "", true, row.InvocationId).ConfigureAwait(false);
        return ((JsonNode)result!).ToJsonString();
    }
    internal async Task SaveApprovalGrantAsync(ChatToolApproval row, ChatRequestContext request)
    {
        AssertMutation(request);
        var meta = JsonNode.Parse(row.SourceMetadata ?? "{}")!.AsObject();
        var server = GetServers(request.UserName).SingleOrDefault(x => x.Id == meta.GetString("serverId"))
            ?? throw new McpClientException("stale_tool", "Connection no longer exists");
        var context = Context(request);
        await AssertAccessAsync(server, context, "invoke").ConfigureAwait(false);
        if (server.ConfigurationHash != meta.GetString("configurationHash"))
            throw new McpClientException("stale_tool", "Connection changed; approval was not saved");
        if ((Store.GetBinding(server, context.User!)?.Revision ?? 0) != meta["bindingRevision"]?.GetValue<long>()
            || await CredentialRevisionAsync(server, context.CancellationToken).ConfigureAwait(false) != meta.GetString("credentialRevision"))
            throw new McpClientException("stale_tool", "Authorization changed; approval was not saved");
        var catalog = await Manager.CatalogAsync(server, context).ConfigureAwait(false);
        var tool = catalog.SingleOrDefault(x => x.Name == meta.GetString("remoteName") && x.SchemaHash == meta.GetString("schemaHash"))
            ?? throw new McpClientException("stale_tool", "Tool changed; approval was not saved");
        Store.SaveApprovalGrant(context.User!, server, tool);
    }
    async Task<string> CredentialRevisionAsync(McpClientServer server, CancellationToken token)
    {
        if (server.Auth.Mode != "host_secret") return "";
        var credential = CredentialStore == null ? null : await CredentialStore.ResolveAsync(server.Auth.SecretReference!, token).ConfigureAwait(false);
        return credential?.Revision ?? throw new McpClientException("auth_required", "Host credential is unavailable");
    }
    internal void SaveBearerCredential(McpClientServer server, string user, string token)
    {
        if (server.Auth.Mode != "bearer") throw HttpError.BadRequest("This connection does not use a Bearer token");
        if (string.IsNullOrWhiteSpace(token) || token.Length > 8192 || token.Any(char.IsWhiteSpace) || token.Any(char.IsControl))
            throw HttpError.BadRequest("Enter the token only, without the Bearer prefix");
        if (HostContext.TryResolve<IDataProtectionProvider>() == null)
            throw HttpError.BadRequest("Host Data Protection is required to store Bearer tokens");
        Store.SetDisconnected(server, user, false, delete: true);
        var binding = Store.EnsureBinding(server, user);
        Store.WriteTokens(binding, new JsonObject { ["accessToken"] = token }.ToJsonString());
        Manager.Invalidate(server, user);
    }
    internal McpClientCredential ReadBearerCredential(McpClientServer server, string user)
    {
        var binding = Store.GetBinding(server, user);
        var json = binding == null ? null : Store.ReadTokens(binding);
        if (json == null || binding!.Disconnected) throw new McpClientException("auth_required", "Add a Bearer token in connection settings");
        return new McpClientCredential(JsonNode.Parse(json)!["accessToken"]!.GetValue<string>(), binding.Revision.ToString());
    }
    public void DeleteUserCredentials(string user)
    {
        foreach (var server in GetServers(user)) { OAuth.Cancel(user, server.Id); Manager.Invalidate(server, user); }
        Store.DeleteUser(user);
    }
    static ChatResult OAuthCallbackPage(bool success, string? errorCode = null, string? serverId = null)
    {
        var title = success ? "Connection authorized" : "Connection could not be completed";
        var detail = success ? "Returning to AI.Chat. If this tab stays open, close it to continue."
            : errorCode == "oauth_failed" ? "Authorization reached AI.Chat, but the token exchange or MCP connection failed. Check the OAuth App credentials and host logs, then try again."
            : errorCode == "oauth_timeout" ? "Authorization reached AI.Chat, but the connection is still starting. Return to chat and refresh the connection in a moment."
            : "Return to AI.Chat and try connecting again. If it continues to fail, check the OAuth App credentials and host logs.";
        var mark = success ? "✓" : "!";
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var html = """
            <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>__TITLE__ · AI.Chat</title><style>
            :root{color-scheme:light dark}*{box-sizing:border-box}body{margin:0;min-height:100vh;display:grid;place-items:center;padding:24px;font:15px/1.6 system-ui,-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif;background:#f8fafc;color:#172033}
            main{width:min(100%,430px);padding:32px;border:1px solid #e2e8f0;border-radius:16px;background:#fff;box-shadow:0 14px 40px #0f172a0d}.mark{display:grid;place-items:center;width:42px;height:42px;border-radius:12px;background:__MARK_BACKGROUND__;color:__MARK_COLOR__;font-size:24px;font-weight:700}h1{margin:20px 0 8px;font-size:21px;line-height:1.3}p{margin:0;color:#5f6b7a}@media(prefers-color-scheme:dark){body{background:#0f172a;color:#f1f5f9}main{background:#172033;border-color:#344155;box-shadow:none}p{color:#aeb8c6}}
            button{margin-top:24px;padding:10px 15px;border:1px solid #cbd5e1;border-radius:8px;background:#fff;color:#172033;font:600 14px system-ui,-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif;cursor:pointer}button:hover{background:#f1f5f9}button:focus-visible{outline:2px solid #2563eb;outline-offset:2px}@media(prefers-color-scheme:dark){button{background:#243248;color:#f1f5f9;border-color:#516078}button:hover{background:#30415c}}
            </style></head><body data-server-id="__SERVER_ID__"><main><div class="mark" aria-hidden="true">__MARK__</div><h1>__TITLE__</h1><p>__DETAIL__</p><button id="close-tab" type="button">Close this tab</button></main>
            <script nonce="__NONCE__">(() => {
              const close = () => window.close();
              document.getElementById('close-tab').addEventListener('click', close);
              if (document.body.dataset.serverId) {
                try {
                  const channel = new BroadcastChannel('ai-chat-mcp-oauth');
                  channel.postMessage({ type: 'authorized', serverId: document.body.dataset.serverId });
                  setTimeout(() => channel.close(), 1000);
                } catch (_) { /* The close button remains available. */ }
                setTimeout(close, 500);
              }
            })();</script></body></html>
            """;
        var result = ChatResult.Html(html.Replace("__TITLE__", title).Replace("__DETAIL__", detail)
            .Replace("__MARK__", mark).Replace("__MARK_BACKGROUND__", success ? "#eaf8ef" : "#fff2e9")
            .Replace("__MARK_COLOR__", success ? "#16803d" : "#b45309")
            .Replace("__SERVER_ID__", System.Net.WebUtility.HtmlEncode(success ? serverId ?? "" : ""))
            .Replace("__NONCE__", nonce));
        result.Status = success ? 200 : 400;
        result.Headers = new Dictionary<string, string> {
            ["Cache-Control"] = "no-store", ["Referrer-Policy"] = "no-referrer",
            ["Content-Security-Policy"] = $"default-src 'none'; script-src 'nonce-{nonce}'; style-src 'unsafe-inline'; base-uri 'none'; form-action 'none'",
            ["X-Content-Type-Options"] = "nosniff",
        };
        return result;
    }
    public void InitSchema() { Store?.InitSchema(); approvals?.InitSchema(); }
    public void DropSchema() => Store?.DropSchema();
}
