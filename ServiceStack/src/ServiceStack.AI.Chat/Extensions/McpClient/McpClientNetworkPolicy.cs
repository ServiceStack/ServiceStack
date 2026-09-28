using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace ServiceStack.AI;

/// <summary>Validates each destination at socket connection time, including OAuth discovery.</summary>
public sealed class McpClientNetworkPolicy
{
    /// <summary>Explicit host exception for enterprise/private destinations. TLS is still validated.</summary>
    public Func<string, IPAddress, bool>? AllowPrivateAddress { get; set; }
    public HashSet<int> AllowedPorts { get; } = [443];
    public bool AllowHttp { get; set; }

    internal static void ValidateUri(Uri uri)
    {
        if (!uri.IsAbsoluteUri || uri.Scheme is not ("https" or "http") || uri.UserInfo.Length != 0
            || uri.Fragment.Length != 0 || uri.Host.Length == 0)
            throw new ArgumentException("MCP endpoints must be absolute HTTP(S) URLs without credentials or fragments");
    }
    internal void AssertUri(Uri uri)
    {
        ValidateUri(uri);
        if ((!AllowHttp && uri.Scheme != "https") || !AllowedPorts.Contains(uri.Port))
            throw new McpClientException("network_denied", "The destination is not permitted by host network policy");
    }
    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address)) return false;
        var b = address.GetAddressBytes();
        if (b.Length == 4)
            return b[0] is not (0 or 10 or 127) && b[0] < 224
                && !(b[0] == 100 && b[1] is >= 64 and <= 127)
                && !(b[0] == 169 && b[1] == 254) && !(b[0] == 172 && b[1] is >= 16 and <= 31)
                && !(b[0] == 192 && (b[1] == 168 || b[1] == 0))
                && !(b[0] == 198 && (b[1] is 18 or 19 || b[1] == 51 && b[2] == 100))
                && !(b[0] == 192 && b[1] == 88 && b[2] == 99)
                && !(b[0] == 203 && b[1] == 0 && b[2] == 113);
        // Global unicast only; excludes ULA, link-local, multicast, translation/tunnel ranges.
        return (b[0] & 0xe0) == 0x20 && !(b[0] == 0x20 && b[1] == 0x01 && (b[2] < 2 || b[2] == 0x0d && b[3] == 0xb8))
            && !(b[0] == 0x20 && b[1] == 0x02);
    }
    internal async Task AssertDestinationAsync(Uri uri, CancellationToken token)
    {
        AssertUri(uri);
        var addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, token).ConfigureAwait(false);
        if (addresses.Length == 0 || addresses.Any(ip => !IsPublicAddress(ip) && AllowPrivateAddress?.Invoke(uri.DnsSafeHost, ip) != true))
            throw new McpClientException("network_denied", "Destination address denied");
    }
    internal HttpClient CreateClient(int maxBytes)
    {
        var handler = new SocketsHttpHandler {
            AllowAutoRedirect = false, UseCookies = false, UseProxy = false,
            AutomaticDecompression = DecompressionMethods.None,
            ActivityHeadersPropagator = null,
            ConnectCallback = async (ctx, token) => {
                if (!AllowedPorts.Contains(ctx.DnsEndPoint.Port)) throw new McpClientException("network_denied", "Port denied");
                var addresses = await Dns.GetHostAddressesAsync(ctx.DnsEndPoint.Host, token).ConfigureAwait(false);
                if (addresses.Length == 0 || addresses.Any(ip => !IsPublicAddress(ip) && AllowPrivateAddress?.Invoke(ctx.DnsEndPoint.Host, ip) != true))
                    throw new McpClientException("network_denied", "Destination address denied");
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                try {
                    await socket.ConnectAsync(new IPEndPoint(addresses[0], ctx.DnsEndPoint.Port), token).ConfigureAwait(false);
                    return new NetworkStream(socket, ownsSocket: true);
                } catch { socket.Dispose(); throw; }
            },
        };
        return new HttpClient(new Guard(this, maxBytes) { InnerHandler = handler }) { Timeout = Timeout.InfiniteTimeSpan };
    }
    sealed class Guard(McpClientNetworkPolicy policy, int limit) : DelegatingHandler
    {
        int toolCalls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            policy.AssertUri(request.RequestUri!);
            // GitHub's OAuth token endpoint returns form-encoded data unless JSON is requested.
            // The MCP SDK parses token responses as JSON but does not set Accept on its form POST.
            if (request.Method == HttpMethod.Post && request.Content?.Headers.ContentType?.MediaType == "application/x-www-form-urlencoded"
                && request.Headers.Accept.Count == 0)
                request.Headers.Accept.ParseAdd("application/json");
            if (request.Content?.Headers.ContentType?.MediaType == "application/json") {
                var body = await request.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
                if (body.Length > limit) throw new McpClientException("request_limit", "MCP request is too large");
                using var document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = 64 });
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("method", out var method)) {
                    if (root.TryGetProperty("params", out var parameters) && parameters.ValueKind == JsonValueKind.Object
                        && (parameters.TryGetProperty("inputResponses", out _) || parameters.TryGetProperty("requestState", out _)))
                        throw new McpClientException("unsupported_operation", "Server-requested interactions are not supported; the request was not replayed");
                    // Sessions are operation-scoped. This also blocks SDK transport/auth retries,
                    // including a 401 refresh, after the first tools/call has been sent.
                    if (method.GetString() == "tools/call" && Interlocked.Increment(ref toolCalls) > 1)
                        throw new McpClientException("replay_blocked", "A dispatched tool call cannot be automatically repeated");
                }
            }
            var response = await base.SendAsync(request, token).ConfigureAwait(false);
            if ((int)response.StatusCode is >= 300 and < 400) {
                response.Dispose(); throw new McpClientException("redirect_denied", "Redirects are disabled");
            }
            try {
                // Bound bytes before the SDK parses JSON, frames or base64. Responses to individual
                // requests may stream, but the standalone infinite notification stream is disabled.
                await response.Content.LoadIntoBufferAsync(limit).WaitAsync(token).ConfigureAwait(false);
                return response;
            } catch { response.Dispose(); throw; }
        }
    }
}

public sealed class McpClientException(string code, string message) : Exception(message), IHasErrorCode
{
    public string Code { get; } = code;
    public string ErrorCode => Code;
}
