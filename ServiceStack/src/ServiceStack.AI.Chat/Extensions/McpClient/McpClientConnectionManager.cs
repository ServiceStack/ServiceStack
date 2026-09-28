using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>
/// Bounded principal partitions. Sessions are deliberately operation-scoped: no stale bearer headers,
/// request objects, or live SDK clients survive credential/policy checks. Catalogs alone are cached.
/// </summary>
internal sealed class McpClientConnectionManager(McpClientExtension extension) : IAsyncDisposable
{
    internal sealed class Entry(int concurrency)
    {
        internal readonly SemaphoreSlim Calls = new(concurrency);
        internal readonly SemaphoreSlim Discovery = new(1);
        internal int Waiting;
        internal int Users;
        internal DateTime LastUsed = DateTime.UtcNow;
        internal DateTime Discovered;
        internal DateTime RetryAfter;
        internal string? Revision;
        internal IReadOnlyList<McpClientTool>? Catalog;
        internal string State = "disconnected";
        internal string? Error;
        internal string? Protocol;
    }
    readonly Dictionary<string, Entry> entries = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, SemaphoreSlim> servers = new(StringComparer.Ordinal);
    readonly CancellationTokenSource shutdown = new();
    readonly object sync = new();
    bool disposed;
    internal IMcpClientSessionFactory Factory { get; set; } = new McpClientSessionFactory(extension);

    internal async Task<T> UseAsync<T>(McpClientServer server, ChatContext context,
        Func<IMcpClientSession, Entry, string, CancellationToken, Task<T>> action, bool discovery = false)
    {
        await extension.AssertAccessAsync(server, context, discovery ? "discover" : "invoke").ConfigureAwait(false);
        var key = Key(server, context.User!);
        Entry entry;
        lock (sync) {
            ObjectDisposedException.ThrowIf(disposed, this);
            foreach (var idle in entries.Where(x => x.Value.Users == 0
                && DateTime.UtcNow - x.Value.LastUsed > extension.Limits.IdleTimeout).Select(x => x.Key).ToArray()) {
                entries[idle].Calls.Dispose(); entries[idle].Discovery.Dispose(); entries.Remove(idle);
            }
            if (!entries.TryGetValue(key, out entry!)) {
                if (entries.Count >= extension.Limits.MaxClients) throw new McpClientException("busy", "Connection capacity reached");
                entries[key] = entry = new Entry(extension.Limits.CallsPerPrincipal);
            }
            entry.Users++;
        }
        var gate = servers.GetOrAdd(server.Id, _ => new SemaphoreSlim(extension.Limits.CallsPerServer));
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, shutdown.Token);
        cts.CancelAfter(discovery ? extension.Limits.DiscoveryTimeout :
            TimeSpan.FromTicks(Math.Min(extension.Limits.CallTimeout.Ticks, extension.Feature.Tools.ToolTimeout.Ticks)));
        var principalAcquired = false;
        var serverAcquired = false;
        try {
            if (Interlocked.Increment(ref entry.Waiting) > extension.Limits.QueueLength + extension.Limits.CallsPerPrincipal) {
                Interlocked.Decrement(ref entry.Waiting);
                throw new McpClientException("busy", "Connection queue is full");
            }
            try { await entry.Calls.WaitAsync(cts.Token).ConfigureAwait(false); principalAcquired = true; }
            finally { Interlocked.Decrement(ref entry.Waiting); }
            await gate.WaitAsync(cts.Token).ConfigureAwait(false); serverAcquired = true;
            await extension.AssertAccessAsync(server, context, discovery ? "discover" : "invoke").ConfigureAwait(false);
            var binding = extension.Store.GetBinding(server, context.User!);
            if (binding?.Disconnected == true) throw new McpClientException("disconnected", "Connect this account before using its tools");
            var credential = server.Auth.Mode == "host_secret"
                ? await (extension.CredentialStore ?? throw new McpClientException("auth_required", "Host credential store is not configured"))
                    .ResolveAsync(server.Auth.SecretReference!, cts.Token).ConfigureAwait(false)
                : server.Auth.Mode == "bearer" ? extension.ReadBearerCredential(server, context.User!) : null;
            if (server.Auth.Mode == "host_secret" && credential == null)
                throw new McpClientException("auth_required", "Host credentials are unavailable");
            var revision = server.ConfigurationHash + ":" + (credential?.Revision ?? binding?.Revision.ToString() ?? "0");
            if (entry.RetryAfter > DateTime.UtcNow) throw new McpClientException("backoff", "Connection retry is temporarily delayed");
            entry.State = "connecting";
            var oauthBinding = server.Auth.Mode == "user_oauth" ? extension.Store.EnsureBinding(server, context.User!) : null;
            var credentialLease = oauthBinding == null ? null : extension.Store.ClaimCredential(oauthBinding);
            try {
            var oauth = server.Auth.Mode == "user_oauth" ? extension.OAuth.Options(server, context.User!) : null;
            var session = await Factory.CreateAsync(server, credential, oauth, cts.Token).ConfigureAwait(false);
            try {
                entry.Protocol = session.ProtocolVersion;
                var result = await action(session, entry, revision, cts.Token).ConfigureAwait(false);
                entry.State = "ready"; entry.Error = null; entry.RetryAfter = default;
                return result;
            } finally {
                await session.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
            } finally { if (oauthBinding != null && credentialLease != null) extension.Store.ReleaseCredential(oauthBinding, credentialLease); }
        } catch (Exception e) {
            if (e is not McpClientException { Code: "backoff" }) entry.RetryAfter = DateTime.UtcNow.AddSeconds(3);
            var failure = McpClientErrors.Connection(e);
            entry.Error = failure.Code;
            entry.State = entry.Error == "auth_required" ? "auth_required" : entry.Error == "disconnected" ? "disconnected"
                : entry.Error.StartsWith("unsupported", StringComparison.Ordinal) ? "unsupported" : "degraded";
            if (e is OperationCanceledException && context.CancellationToken.IsCancellationRequested) throw;
            throw failure;
        } finally {
            if (serverAcquired) gate.Release();
            if (principalAcquired) entry.Calls.Release();
            lock (sync) { entry.Users--; entry.LastUsed = DateTime.UtcNow; }
        }
    }
    internal async Task<IReadOnlyList<McpClientTool>> CatalogAsync(McpClientServer server, ChatContext context, bool refresh = false)
    {
        await extension.AssertAccessAsync(server, context, "discover").ConfigureAwait(false);
        var binding = extension.Store.GetBinding(server, context.User!);
        if (binding?.Disconnected == true) throw new McpClientException("disconnected", "Connect this account first");
        var credential = server.Auth.Mode == "host_secret" && extension.CredentialStore != null
            ? await extension.CredentialStore.ResolveAsync(server.Auth.SecretReference!, context.CancellationToken).ConfigureAwait(false) : null;
        var currentRevision = server.ConfigurationHash + ":" + (credential?.Revision ?? binding?.Revision.ToString() ?? "0");
        lock (sync) {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!refresh && entries.TryGetValue(Key(server, context.User!), out var cached) && cached.State == "ready"
                && cached.Catalog != null && cached.Revision == currentRevision
                && DateTime.UtcNow - cached.Discovered < extension.Limits.CatalogFreshness) {
                cached.LastUsed = DateTime.UtcNow;
                return cached.Catalog;
            }
        }
        return await UseAsync<IReadOnlyList<McpClientTool>>(server, context, async (session, entry, revision, token) => {
            await entry.Discovery.WaitAsync(token).ConfigureAwait(false);
            try {
                if (refresh || entry.Catalog == null || entry.Revision != revision
                    || DateTime.UtcNow - entry.Discovered > extension.Limits.CatalogFreshness) {
                    var catalog = await McpClientCatalog.DiscoverAsync(session, server, extension.Limits, token).ConfigureAwait(false);
                    entry.Catalog = catalog; entry.Discovered = DateTime.UtcNow; entry.Revision = revision;
                }
                return entry.Catalog;
            } finally { entry.Discovery.Release(); }
        }, discovery: true).ConfigureAwait(false);
    }

    internal JsonObject Status(McpClientServer server, string user)
    {
        lock (sync) {
            entries.TryGetValue(Key(server, user), out var entry);
            var binding = extension.Store.GetBinding(server, user);
            return new JsonObject {
                ["id"] = server.Id, ["name"] = server.DisplayName ?? server.Id,
                ["accountMode"] = server.Auth.Mode == "host_secret" ? "application_account" : server.Auth.Mode,
                ["disabled"] = binding?.Disconnected == true,
                ["state"] = binding?.Disconnected == true ? "disconnected" : entry?.State ?? "disconnected",
                ["reason"] = entry?.Error, ["protocol"] = entry?.Protocol,
                ["lastDiscovery"] = entry?.Discovered == default ? null : entry?.Discovered.ToString("O"),
                ["group"] = "mcp_" + server.Id,
            };
        }
    }
    internal void PrimeOAuthCatalog(McpClientServer server, string user, long revision,
        IReadOnlyList<McpClientTool> catalog, string? protocol)
    {
        var binding = extension.Store.GetBinding(server, user);
        if (binding == null || binding.Revision != revision || binding.Disconnected || binding.ProtectedTokens == null)
            throw new McpClientException("connection_changed", "Connection changed during authorization");
        lock (sync) {
            ObjectDisposedException.ThrowIf(disposed, this);
            var key = Key(server, user);
            if (!entries.TryGetValue(key, out var entry)) {
                if (entries.Count >= extension.Limits.MaxClients)
                    throw new McpClientException("busy", "Connection capacity reached");
                entries[key] = entry = new Entry(extension.Limits.CallsPerPrincipal);
            }
            entry.Catalog = catalog;
            entry.Revision = server.ConfigurationHash + ":" + revision;
            entry.Protocol = protocol;
            entry.Discovered = entry.LastUsed = DateTime.UtcNow;
            entry.State = "ready";
            entry.Error = null;
            entry.RetryAfter = default;
        }
    }
    internal void Invalidate(McpClientServer server, string user)
    {
        lock (sync) if (entries.TryGetValue(Key(server, user), out var entry)) {
            entry.Catalog = null; entry.State = "disconnected"; entry.Error = null; entry.RetryAfter = default;
        }
    }
    static string Key(McpClientServer server, string user) => server.Id + "\0" + user;
    public async ValueTask DisposeAsync()
    {
        lock (sync) { if (disposed) return; disposed = true; }
        await shutdown.CancelAsync().ConfigureAwait(false);
        // Active operations own and dispose their sessions; stop waiting after the host's drain budget.
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline) {
            lock (sync) if (entries.Values.All(x => x.Users == 0)) return;
            await Task.Delay(25).ConfigureAwait(false);
        }
    }
}

internal static class McpClientDiagnostics
{
    internal static readonly ActivitySource Source = new("ServiceStack.AI.McpClient");
    internal static readonly Meter Meter = new("ServiceStack.AI.McpClient");
    internal static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("mcp.client.call.duration", "s");
    internal static readonly Counter<long> Errors = Meter.CreateCounter<long>("mcp.client.call.errors");
    internal static readonly UpDownCounter<long> Active = Meter.CreateUpDownCounter<long>("mcp.client.call.active");
}
