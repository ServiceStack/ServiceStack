using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ServiceStack.AI;

public enum McpClientApproval { Always, Never }

public sealed record McpClientAuth(string Mode, string? SecretReference = null)
{
    public static McpClientAuth HostSecret(string reference) => new("host_secret", reference);
    public static McpClientAuth UserOAuth() => new("user_oauth");
    public static McpClientAuth Bearer() => new("bearer");
    public static McpClientAuth Anonymous() => new("anonymous");
}

public sealed class McpClientServer
{
    public required string Id { get; init; }
    public string? DisplayName { get; init; }
    public required Uri Endpoint { get; init; }
    public McpClientAuth Auth { get; init; } = McpClientAuth.Anonymous();
    public List<string> AllowedUsers { get; init; } = [];
    public List<string> RequiredRoles { get; init; } = [];
    public List<string> AllowedTools { get; init; } = [];
    public List<string> DeniedTools { get; init; } = [];
    public List<string> ToolsWithoutApproval { get; init; } = [];
    public McpClientApproval Approval { get; init; } = McpClientApproval.Always;
    public bool IncludeInAll { get; init; }
    /// <summary>Host policy revision. Increment when changing callback-based authorization.</summary>
    public long Revision { get; init; } = 1;
    [JsonIgnore]
    public Func<ChatContext, string, Task<bool>>? Authorize { get; init; }
    [JsonPropertyName("oauthClientId")]
    public string? OAuthClientId { get; init; }
    [JsonPropertyName("oauthIssuer")]
    public Uri? OAuthIssuer { get; init; }
    [JsonPropertyName("oauthScopes")]
    public List<string> OAuthScopes { get; init; } = [];
    internal string ConfigurationScope { get; set; } = "shared";
    internal string ConfigurationHash => McpClientHash.Of(JsonSerializer.Serialize(new {
        ConfigurationScope, Id, Endpoint, Auth, AllowedUsers, RequiredRoles, AllowedTools, DeniedTools, ToolsWithoutApproval,
        Approval, IncludeInAll, Revision, OAuthClientId, OAuthIssuer, OAuthScopes,
    }));
    internal bool Allows(string name) => !DeniedTools.Contains("*") && !DeniedTools.Contains(name)
        && (AllowedTools.Contains("*") || AllowedTools.Contains(name));
    internal bool NeedsApproval(string name) => Approval != McpClientApproval.Never && !ToolsWithoutApproval.Contains(name);
    internal void Validate()
    {
        if (!Regex.IsMatch(Id, "^[a-z][a-z0-9_]{0,31}$")) throw new ArgumentException("Invalid MCP server id");
        McpClientNetworkPolicy.ValidateUri(Endpoint);
        if (Auth.Mode is not ("host_secret" or "user_oauth" or "anonymous" or "bearer")) throw new ArgumentException("Invalid MCP authentication mode");
        if (Auth.Mode == "host_secret" && (string.IsNullOrWhiteSpace(Auth.SecretReference) || (Authorize == null && AllowedUsers.Count == 0)))
            throw new ArgumentException("Host credentials require a secret reference and explicit audience (allowedUsers or authorization callback)");
        if (Auth.Mode == "user_oauth" && (OAuthIssuer == null || string.IsNullOrEmpty(OAuthClientId)))
            throw new ArgumentException("OAuth requires a host-registered client id and expected issuer");
    }
}

public sealed class McpClientLimits
{
    public TimeSpan DiscoveryTimeout { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan CallTimeout { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan CatalogFreshness { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxTools { get; set; } = 256;
    public int MaxPages { get; set; } = 100;
    public int MaxSchemaBytes { get; set; } = 64 * 1024;
    public int MaxCatalogBytes { get; set; } = 1024 * 1024;
    public int MaxResponseBytes { get; set; } = 4 * 1024 * 1024;
    public int MaxDefinitionBytes { get; set; } = 1024 * 1024;
    public int MaxClients { get; set; } = 128;
    public int CallsPerPrincipal { get; set; } = 4;
    public int CallsPerServer { get; set; } = 16;
    public int QueueLength { get; set; } = 32;
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(15);
    internal void Validate()
    {
        if (MaxTools is < 1 or > 1024 || MaxPages is < 1 or > 100 || MaxSchemaBytes is < 1 or > 262144
            || MaxCatalogBytes is < 1 or > 4194304 || MaxResponseBytes is < 1 or > 16777216
            || MaxDefinitionBytes is < 1 or > 4194304 || MaxClients is < 1 or > 1024
            || CallsPerPrincipal is < 1 or > 16 || CallsPerServer is < 1 or > 64 || QueueLength is < 0 or > 256
            || DiscoveryTimeout <= TimeSpan.Zero || DiscoveryTimeout > TimeSpan.FromMinutes(1)
            || CallTimeout <= TimeSpan.Zero || CallTimeout > TimeSpan.FromMinutes(5)
            || CatalogFreshness <= TimeSpan.Zero || IdleTimeout <= TimeSpan.Zero)
            throw new ArgumentException("Invalid MCP limits");
    }
}

/// <summary>Host-owned secret resolver. Revision must change whenever a secret rotates.</summary>
public interface IMcpClientCredentialStore
{
    Task<McpClientCredential?> ResolveAsync(string reference, CancellationToken token);
}
public sealed record McpClientCredential(string AccessToken, string Revision);

internal static class McpClientHash
{
    internal static string Of(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    internal static string Alias(string server, string name)
    {
        var prefix = Regex.Replace($"mcp_{server}_{name}", "[^a-zA-Z0-9_]", "_");
        // Always include a hash: alias stability must not depend on catalog order or collisions.
        return prefix[..Math.Min(prefix.Length, 47)] + "_" + Of(server + "\0" + name)[..16];
    }
}
