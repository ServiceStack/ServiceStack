using System.Collections.Concurrent;
using System.Text.Json;
using ModelContextProtocol.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace ServiceStack.AI;

internal sealed class McpClientOAuthService(McpClientExtension extension) : IAsyncDisposable
{
    sealed class Pending(string user, string server, string config)
    {
        internal readonly string User = user, Server = server, Configuration = config;
        internal readonly TaskCompletionSource<string?> Url = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly CancellationTokenSource Timeout = new(TimeSpan.FromMinutes(5));
        internal string? State;
        internal Task? Work;
        internal bool Failed;
    }
    readonly ConcurrentDictionary<string, Pending> pending = new();

    internal ClientOAuthOptions Options(McpClientServer server, string user, Func<AuthorizationCallbackContext, CancellationToken, Task<AuthorizationResult?>>? callback = null)
    {
        var row = extension.Store.EnsureBinding(server, user);
        return new ClientOAuthOptions {
            ClientId = server.OAuthClientId,
            ClientSecret = extension.Store.ReadClientSecret(row),
            RedirectUri = extension.OAuthRedirectUri!, Scopes = server.OAuthScopes,
            AuthServerSelector = issuers => issuers.FirstOrDefault(x => x == server.OAuthIssuer)
                ?? throw new McpClientException("issuer_mismatch", "The configured issuer was not advertised"),
            AuthorizationCallbackHandler = callback ?? ((_, _) => throw new McpClientException("auth_required", "Sign in to this connection")),
            TokenCache = new TokenCache(extension.Store, row),
        };
    }
    sealed class TokenCache(McpClientStore store, ChatMcpBinding binding) : ITokenCache
    {
        public ValueTask<TokenContainer?> GetTokensAsync(CancellationToken token = default) =>
            ValueTask.FromResult(store.ReadTokens(binding) is { } json ? JsonSerializer.Deserialize<TokenContainer>(json) : null);
        public ValueTask StoreTokensAsync(TokenContainer tokens, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            store.WriteTokens(binding, JsonSerializer.Serialize(tokens));
            return ValueTask.CompletedTask;
        }
    }
    internal async Task<string?> StartAsync(McpClientServer server, ChatContext context)
    {
        await extension.AssertAccessAsync(server, context, "connect").ConfigureAwait(false);
        if (!extension.Feature.RequireAuth) throw new McpClientException("auth_required", "OAuth requires host authentication");
        if (server.Endpoint.DnsSafeHost.Equals("api.githubcopilot.com", StringComparison.OrdinalIgnoreCase)
            && extension.Store.ReadClientSecret(extension.Store.EnsureBinding(server, context.User!)) == null)
            throw new McpClientException("oauth_client_secret_required", "Add the GitHub OAuth App client secret in connection settings before signing in");
        if (pending.Count >= 128) throw new McpClientException("busy", "Too many sign-in requests");
        var key = context.User + "\0" + server.Id;
        var flow = new Pending(context.User!, server.Id, server.ConfigurationHash);
        if (!pending.TryAdd(key, flow)) { flow.Timeout.Dispose(); throw new McpClientException("busy", "Sign-in is already in progress"); }
        flow.Work = CompleteFlowAsync(server, flow, key);
        // The supervised task lives until callback/expiry; no request object is retained.
        try { return await flow.Url.Task.WaitAsync(extension.Limits.DiscoveryTimeout).ConfigureAwait(false); }
        catch (TimeoutException) { CancelFlow(flow); throw new McpClientException("oauth_timeout", "The authorization service did not respond in time. Try connecting again"); }
        catch { CancelFlow(flow); throw new McpClientException("auth_required", "Could not start authorization. Check the OAuth connection settings and try again"); }
    }
    async Task CompleteFlowAsync(McpClientServer server, Pending flow, string key)
    {
        ChatMcpBinding? binding = null;
        string? lease = null;
        try {
            binding = extension.Store.EnsureBinding(server, flow.User);
            lease = extension.Store.ClaimCredential(binding);
            var options = Options(server, flow.User, async (callback, token) => {
                await extension.NetworkPolicy.AssertDestinationAsync(callback.AuthorizationUri, token).ConfigureAwait(false);
                if (callback.RedirectUri != extension.OAuthRedirectUri) throw new McpClientException("redirect_mismatch", "Invalid callback URL");
                var query = QueryHelpers.ParseQuery(callback.AuthorizationUri.Query);
                flow.State = query["state"].ToString();
                if (string.IsNullOrEmpty(flow.State)) throw new McpClientException("invalid_state", "Missing authorization state");
                var id = McpClientHash.Of(flow.State);
                extension.Store.CreateAuthorization(new ChatMcpAuthorization {
                    Id = id, Owner = flow.User, ServerId = server.Id, ConfigurationHash = server.ConfigurationHash,
                    RedirectUri = callback.RedirectUri.AbsoluteUri, Issuer = server.OAuthIssuer!.AbsoluteUri,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                });
                flow.Url.TrySetResult(callback.AuthorizationUri.AbsoluteUri);
                while (true) {
                    token.ThrowIfCancellationRequested();
                    var transaction = extension.Store.GetAuthorization(id, flow.User);
                    if (transaction == null || transaction.ExpiresAt < DateTime.UtcNow || transaction.Status is "failed" or "canceled")
                        throw new McpClientException("auth_required", "Authorization expired or canceled");
                    if (extension.Store.AuthorizationResponse(transaction) is { } response)
                        return JsonSerializer.Deserialize<AuthorizationResult>(response)!;
                    await Task.Delay(250, token).ConfigureAwait(false);
                }
            });
            IReadOnlyList<McpClientTool> catalog;
            string? protocol;
            await using (var session = await extension.Manager.Factory.CreateAsync(server, null, options, flow.Timeout.Token).ConfigureAwait(false)) {
                catalog = await McpClientCatalog.DiscoverAsync(session, server, extension.Limits, flow.Timeout.Token).ConfigureAwait(false);
                protocol = session.ProtocolVersion;
            }
            // The callback must not report success while the OAuth lease is still held.
            extension.Store.ReleaseCredential(binding, lease);
            lease = null;
            extension.Manager.PrimeOAuthCatalog(server, flow.User, binding.Revision, catalog, protocol);
            if (flow.State != null) extension.Store.AuthorizationOutcome(McpClientHash.Of(flow.State), "completed");
            flow.Url.TrySetResult(null);
        } catch (Exception ex) {
            // OAuth exceptions can contain codes or tokens. Log only the exception type.
            extension.Ctx.Feature.Log.LogWarning("MCP OAuth connection {ServerId} failed with {ExceptionType} ({ErrorCode})",
                server.Id, ex.GetType().Name, ex is McpClientException mcp ? mcp.Code : "unexpected");
            flow.Url.TrySetException(new McpClientException("auth_required", "Authorization failed; start sign-in again"));
            flow.Failed = true;
            if (flow.State != null) extension.Store.AuthorizationOutcome(McpClientHash.Of(flow.State), "failed");
        } finally {
            if (binding != null && lease != null) extension.Store.ReleaseCredential(binding, lease);
            pending.TryRemove(key, out _);
            flow.Timeout.Dispose();
        }
    }
    internal async Task<string> CompleteAsync(ChatRequestContext request)
    {
        var state = request.QueryString("state");
        if (string.IsNullOrEmpty(state) || state.Length > 1024) throw new McpClientException("invalid_state", "Missing or invalid authorization state");
        var id = McpClientHash.Of(state);
        var transaction = extension.Store.GetAuthorization(id, request.UserName!)
            ?? throw new McpClientException("invalid_state", "Authorization expired or belongs to another account");
        var server = extension.GetServers(request.UserName).SingleOrDefault(x => x.Id == transaction.ServerId && x.ConfigurationHash == transaction.ConfigurationHash)
            ?? throw new McpClientException("connection_changed", "Connection changed during authorization");
        if (transaction.RedirectUri != extension.OAuthRedirectUri?.AbsoluteUri || transaction.Issuer != server.OAuthIssuer?.AbsoluteUri)
            throw new McpClientException("connection_changed", "Authorization configuration changed");
        await extension.AssertAccessAsync(server, new ChatContext { User = request.UserName, Request = request.Request }, "connect").ConfigureAwait(false);
        var code = request.QueryString("code");
        if (string.IsNullOrEmpty(code) || code.Length > 8192 || !extension.Store.SubmitAuthorization(transaction,
            JsonSerializer.Serialize(new AuthorizationResult { Code = code, State = state, Iss = request.QueryString("iss") })))
            throw new McpClientException("invalid_state", "Invalid or already consumed callback");
        // Token exchange and the initial MCP tool listing can outlast discovery timeout.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
        while (DateTime.UtcNow < deadline) {
            transaction = extension.Store.GetAuthorization(id, request.UserName!);
            if (transaction?.Status == "completed") return server.Id;
            if (transaction == null || transaction.Status is "failed" or "canceled") break;
            await Task.Delay(100).ConfigureAwait(false);
        }
        if (transaction?.Status == "failed")
            throw new McpClientException("oauth_failed", "Authorization was received, but the token exchange or MCP connection failed. Check your OAuth App credentials and try again");
        throw new McpClientException("oauth_timeout", "Authorization was received, but the connection is still starting. Return to chat and refresh this connection in a moment");
    }

    internal void Cancel(string user, string server)
    {
        extension.Store.CancelAuthorizations(user, server);
        if (pending.TryGetValue(user + "\0" + server, out var flow)) CancelFlow(flow);
    }
    static void CancelFlow(Pending flow)
    {
        try { flow.Timeout.Cancel(); } catch (ObjectDisposedException) { }
    }
    public async ValueTask DisposeAsync()
    {
        var flows = pending.Values.ToArray();
        foreach (var flow in flows) CancelFlow(flow);
        try { await Task.WhenAll(flows.Where(x => x.Work != null).Select(x => x.Work!)).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false); }
        catch { /* All flows are canceled; callers receive a sanitized sign-in error. */ }
    }
}
