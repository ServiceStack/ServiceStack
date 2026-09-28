using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ServiceStack.AI;

/// <summary>Finds the authorization servers advertised by an MCP protected resource.</summary>
internal static class McpClientIssuerDiscovery
{
    static readonly Regex MetadataLink = new("(?:^|[,\\s])resource_metadata=\"([^\"\\r\\n]+)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    internal static async Task<string[]> DiscoverAsync(Uri endpoint, McpClientNetworkPolicy policy, McpClientLimits limits)
    {
        policy.AssertUri(endpoint);
        var resource = endpoint.GetLeftPart(UriPartial.Path);
        using var http = policy.CreateClient(limits.MaxResponseBytes);
        using var timeout = new CancellationTokenSource(limits.DiscoveryTimeout);
        var token = timeout.Token;
        using var challenge = new HttpRequestMessage(HttpMethod.Post, endpoint) {
            Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"server/discover\",\"params\":{\"_meta\":{\"io.modelcontextprotocol/protocolVersion\":\"2026-07-28\",\"io.modelcontextprotocol/clientInfo\":{\"name\":\"AI.Chat\",\"version\":\"1.0\"},\"io.modelcontextprotocol/clientCapabilities\":{}}}}", Encoding.UTF8, "application/json"),
        };
        challenge.Headers.TryAddWithoutValidation("Accept", "application/json, text/event-stream");
        challenge.Headers.TryAddWithoutValidation("MCP-Protocol-Version", "2026-07-28");
        challenge.Headers.TryAddWithoutValidation("Mcp-Method", "server/discover");
        using var challengeResponse = await http.SendAsync(challenge, token).ConfigureAwait(false);
        string? advertised = null;
        if (challengeResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized
            && challengeResponse.Headers.TryGetValues("WWW-Authenticate", out var headers)) {
            foreach (var header in headers) {
                var match = MetadataLink.Match(header);
                if (match.Success) { advertised = match.Groups[1].Value; break; }
            }
        }
        var path = endpoint.AbsolutePath.TrimEnd('/');
        var origin = endpoint.GetLeftPart(UriPartial.Authority);
        var location = advertised ?? origin + "/.well-known/oauth-protected-resource" + path;
        var protectedResource = await ReadMetadataAsync(http, location, policy, token).ConfigureAwait(false);
        if (protectedResource == null && advertised == null)
            protectedResource = await ReadMetadataAsync(http, origin + "/.well-known/oauth-protected-resource", policy, token).ConfigureAwait(false);
        if (protectedResource is not JsonObject || protectedResource["resource"]?.ToString() != resource
            || protectedResource["authorization_servers"] is not JsonArray servers || servers.Count is < 1 or > 16)
            throw new McpClientException("invalid_oauth_metadata", "This MCP server did not advertise a usable sign-in provider");
        var issuers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in servers) {
            if (entry is not JsonValue value || !value.TryGetValue<string>(out var text)
                || !Uri.TryCreate(text, UriKind.Absolute, out var uri))
                throw new McpClientException("invalid_oauth_metadata", "The MCP server advertised an invalid sign-in provider");
            policy.AssertUri(uri);
            issuers.Add(uri.AbsoluteUri);
        }
        return issuers.ToArray();
    }

    static async Task<JsonNode?> ReadMetadataAsync(HttpClient http, string location, McpClientNetworkPolicy policy, CancellationToken token)
    {
        if (!Uri.TryCreate(location, UriKind.Absolute, out var uri))
            throw new McpClientException("invalid_oauth_metadata", "The MCP server advertised an invalid metadata URL");
        policy.AssertUri(uri);
        using var response = await http.GetAsync(uri, token).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode)
            throw new McpClientException("invalid_oauth_metadata", "Could not read this MCP server's sign-in information");
        try { return JsonNode.Parse(await response.Content.ReadAsStringAsync(token).ConfigureAwait(false)); }
        catch (System.Text.Json.JsonException) {
            throw new McpClientException("invalid_oauth_metadata", "This MCP server returned invalid sign-in information");
        }
    }
}
