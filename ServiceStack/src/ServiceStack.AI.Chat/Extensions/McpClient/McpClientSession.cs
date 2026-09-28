using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol;
using ModelContextProtocol.Authentication;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ServiceStack.AI;

internal interface IMcpClientSession : IAsyncDisposable
{
    string? ProtocolVersion { get; }
    Task<JsonObject> ListToolsAsync(string? cursor, CancellationToken token);
    Task<JsonObject> CallAsync(string name, JsonObject arguments, CancellationToken token);
}

internal interface IMcpClientSessionFactory
{
    Task<IMcpClientSession> CreateAsync(McpClientServer server, McpClientCredential? credential,
        ClientOAuthOptions? oauth, CancellationToken token);
}

internal sealed class McpClientSessionFactory(McpClientExtension extension) : IMcpClientSessionFactory
{
    public async Task<IMcpClientSession> CreateAsync(McpClientServer server, McpClientCredential? credential,
        ClientOAuthOptions? oauth, CancellationToken token)
    {
        extension.NetworkPolicy.AssertUri(server.Endpoint);
        var http = extension.NetworkPolicy.CreateClient(extension.Limits.MaxResponseBytes);
        var transport = new HttpClientTransport(new HttpClientTransportOptions {
            Endpoint = server.Endpoint, TransportMode = HttpTransportMode.StreamableHttp,
            Name = server.Id, ConnectionTimeout = oauth == null ? extension.Limits.DiscoveryTimeout : TimeSpan.FromMinutes(5),
            EnableStandaloneGetStream = false, MaxReconnectionAttempts = 0, OAuth = oauth,
            AdditionalHeaders = credential == null ? null : new Dictionary<string, string> {
                ["Authorization"] = "Bearer " + credential.AccessToken,
            },
        }, http, ownsHttpClient: true);
        try {
            var client = await McpClient.CreateAsync(transport, new McpClientOptions {
                ClientInfo = new Implementation { Name = "ServiceStack.AI.Chat", Version = "1.0" },
                Capabilities = new ClientCapabilities(), InitializationTimeout = oauth == null ? extension.Limits.DiscoveryTimeout : TimeSpan.FromMinutes(5),
                DiscoverProbeTimeout = oauth == null ? extension.Limits.DiscoveryTimeout : TimeSpan.FromMinutes(5),
            }, cancellationToken: token).ConfigureAwait(false);
            return new Session(client, transport);
        } catch { await transport.DisposeAsync().ConfigureAwait(false); http.Dispose(); throw; }
    }

    sealed class Session(McpClient client, HttpClientTransport transport) : IMcpClientSession
    {
        public string? ProtocolVersion => client.NegotiatedProtocolVersion;
        public async Task<JsonObject> ListToolsAsync(string? cursor, CancellationToken token)
        {
            var result = await client.ListToolsAsync(new ListToolsRequestParams { Cursor = cursor }, token).ConfigureAwait(false);
            // The raw paged SDK overload does not populate its tool cache. GitHub and other
            // MCP servers use x-mcp-header annotations to require Mcp-Param-* headers on
            // tools/call; register each discovered page in this operation-scoped session.
            client.AddKnownTools(result.Tools);
            return JsonSerializer.SerializeToNode(result, McpJsonUtilities.DefaultOptions)!.AsObject();
        }
        public async Task<JsonObject> CallAsync(string name, JsonObject arguments, CancellationToken token)
        {
            // The HTTP policy guard blocks the SDK's automatic MRTR/auth continuation before a second call can be sent.
            var result = await client.CallToolAsync(new CallToolRequestParams {
                Name = name, Arguments = arguments.ToDictionary(x => x.Key,
                    x => JsonSerializer.SerializeToElement(x.Value)),
            }, token).ConfigureAwait(false);
            return JsonSerializer.SerializeToNode(result, McpJsonUtilities.DefaultOptions)!.AsObject();
        }
        public async ValueTask DisposeAsync()
        {
            try { await client.DisposeAsync().ConfigureAwait(false); }
            finally { await transport.DisposeAsync().ConfigureAwait(false); }
        }
    }
}
