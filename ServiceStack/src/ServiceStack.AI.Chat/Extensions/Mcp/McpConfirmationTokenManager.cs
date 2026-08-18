#nullable enable

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ServiceStack.Caching;

namespace ServiceStack.AI;

/// <summary>
/// Mode determining how MCP handles operations requiring approval.
/// </summary>
public enum McpApprovalMode
{
    /// <summary>
    /// Enforce Two-Phase Dry-Run & Confirmation Token on mutating tools/APIs (Default).
    /// </summary>
    ConfirmationToken,

    /// <summary>
    /// Fail-closed: Reject all tools requiring approval with an error message.
    /// </summary>
    Reject,

    /// <summary>
    /// Delegate approval to MCP Client native confirmation dialogs (no server token checks).
    /// </summary>
    DelegateToClient,
}

/// <summary>
/// Data payload embedded within a signed MCP confirmation token.
/// </summary>
public class McpTokenPayload
{
    public string Sub { get; set; } = null!;
    public string Tool { get; set; } = null!;
    public string Target { get; set; } = null!;
    public string ArgsHash { get; set; } = "";
    public long Iat { get; set; }
    public long Exp { get; set; }
    public string Jti { get; set; } = null!;
}

/// <summary>
/// Result of verifying an MCP confirmation token.
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public McpTokenPayload? Payload { get; set; }

    public static TokenValidationResult Success(McpTokenPayload payload) => new() { IsValid = true, Payload = payload };
    public static TokenValidationResult Failed(string error) => new() { IsValid = false, ErrorMessage = error };
}

/// <summary>
/// Manages cryptographic creation, argument binding, expiration, single-use tracking,
/// and validation of MCP Two-Phase confirmation tokens.
/// </summary>
public class McpConfirmationTokenManager
{
    private readonly byte[] secretBytes;
    private readonly TimeSpan expiry;
    private readonly ICacheClient? cache;
    private readonly ConcurrentDictionary<string, long> usedTokens = new();

    public McpConfirmationTokenManager(string secret, TimeSpan expiry, ICacheClient? cache = null)
    {
        this.secretBytes = Encoding.UTF8.GetBytes(secret);
        this.expiry = expiry;
        this.cache = cache;
    }

    public string CreateToken(string? user, string toolName, string targetApi, JsonObject? args)
    {
        var now = DateTimeOffset.UtcNow;
        var payload = new McpTokenPayload
        {
            Sub = user ?? "anonymous",
            Tool = toolName,
            Target = targetApi,
            ArgsHash = ComputeArgumentsHash(args),
            Iat = now.ToUnixTimeSeconds(),
            Exp = now.Add(expiry).ToUnixTimeSeconds(),
            Jti = Guid.NewGuid().ToString("N"),
        };

        var headerJson = ToBase64Url(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"MCP+CONFIRM\"}"));
        var payloadJson = ToBase64Url(Encoding.UTF8.GetBytes(ChatJson.Serialize(payload)));
        var unsigned = $"{headerJson}.{payloadJson}";
        var signature = Sign(unsigned, secretBytes);

        return $"mcp_cf_{unsigned}.{signature}";
    }

    public TokenValidationResult ValidateToken(
        string tokenString, string? user, string toolName, string targetApi, JsonObject? currentArgs)
    {
        if (string.IsNullOrEmpty(tokenString) || !tokenString.StartsWith("mcp_cf_"))
            return TokenValidationResult.Failed("Invalid token format.");

        var raw = tokenString["mcp_cf_".Length..];
        var parts = raw.Split('.');
        if (parts.Length != 3)
            return TokenValidationResult.Failed("Malformed confirmation token.");

        var unsigned = $"{parts[0]}.{parts[1]}";
        var expectedSig = Sign(unsigned, secretBytes);
        var providedSigBytes = FromBase64Url(parts[2]);
        var expectedSigBytes = FromBase64Url(expectedSig);

        if (providedSigBytes.Length == 0 || expectedSigBytes.Length == 0 ||
            !CryptographicOperations.FixedTimeEquals(providedSigBytes, expectedSigBytes))
        {
            return TokenValidationResult.Failed("Invalid token signature.");
        }

        McpTokenPayload? payload;
        try
        {
            var payloadBytes = FromBase64Url(parts[1]);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);
            payload = ChatJson.Deserialize<McpTokenPayload>(payloadJson);
        }
        catch (Exception ex)
        {
            return TokenValidationResult.Failed($"Invalid token payload: {ex.Message}");
        }

        if (payload == null)
            return TokenValidationResult.Failed("Token payload could not be read.");

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now > payload.Exp)
            return TokenValidationResult.Failed("Confirmation token has expired.");

        var currentUser = user ?? "anonymous";
        if (!string.Equals(payload.Sub, currentUser, StringComparison.Ordinal))
            return TokenValidationResult.Failed("User mismatch for confirmation token.");

        if (!string.Equals(payload.Tool, toolName, StringComparison.OrdinalIgnoreCase))
            return TokenValidationResult.Failed($"Tool mismatch for confirmation token (expected '{payload.Tool}').");

        if (!string.Equals(payload.Target, targetApi, StringComparison.OrdinalIgnoreCase))
            return TokenValidationResult.Failed($"Target API mismatch for confirmation token (expected '{payload.Target}').");

        var currentHash = ComputeArgumentsHash(currentArgs);
        if (!string.Equals(payload.ArgsHash, currentHash, StringComparison.Ordinal))
            return TokenValidationResult.Failed("Arguments have been modified since confirmation was issued.");

        // Check & enforce single-use replay protection (atomic check-and-set to avoid TOCTOU)
        var cacheKey = $"urn:mcp:used:{payload.Jti}";
        if (cache != null)
        {
            // ICacheClient.Add returns false if the key already exists — atomic single-use guarantee
            var ttl = TimeSpan.FromSeconds(Math.Max(1, payload.Exp - now));
            if (!cache.Add(cacheKey, true, ttl))
                return TokenValidationResult.Failed("Confirmation token has already been used.");
        }
        else
        {
            CleanupExpiredTokens(now);
            if (!usedTokens.TryAdd(payload.Jti, payload.Exp))
                return TokenValidationResult.Failed("Confirmation token has already been used.");
        }

        return TokenValidationResult.Success(payload);
    }

    private void CleanupExpiredTokens(long now)
    {
        foreach (var (jti, exp) in usedTokens)
        {
            if (now > exp)
                usedTokens.TryRemove(jti, out _);
        }
    }

    public static string ComputeArgumentsHash(JsonObject? args)
    {
        if (args == null || args.Count == 0)
            return "";

        var canonical = Canonicalize(args);
        var json = canonical?.ToJsonString(ChatJson.Options) ?? "";
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
    }

    public static JsonNode? Canonicalize(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            var sorted = new JsonObject();
            foreach (var property in obj.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                sorted[property.Key] = Canonicalize(property.Value);
            }
            return sorted;
        }
        if (node is JsonArray array)
        {
            var newArray = new JsonArray();
            foreach (var item in array)
            {
                newArray.Add(Canonicalize(item));
            }
            return newArray;
        }
        if (node is JsonValue value)
        {
            // Normalize numbers so that 1, 1.0, 1e0 and 1.10 all hash identically — models
            // frequently re-emit JSON between Phase 1 and Phase 2 with different numeric
            // formatting. Integers collapse to Int64; other numbers collapse to Double, which
            // STJ then serializes in round-trip form ("1" for 1.0, "1.1" for 1.10, etc.).
            // TryGetValue<T> works for both JsonElement-backed and raw-CLR-backed JsonValues.
            if (value.TryGetValue<long>(out var l))
                return JsonValue.Create(l);
            if (value.TryGetValue<int>(out var i))
                return JsonValue.Create((long)i);
            if (value.TryGetValue<double>(out var d))
                return JsonValue.Create(d);
            if (value.TryGetValue<decimal>(out var dec))
                return JsonValue.Create((double)dec);
        }
        return node?.DeepClone();
    }

    public static JsonObject CreateRequiresConfirmationResponse(
        string apiOrToolName,
        string safety,
        string token,
        int expiresInSeconds,
        string summary,
        JsonObject? args,
        string? instruction = null)
    {
        // NOTE: don't embed the token in the human-readable instruction — the token is already
        // in the structured confirmationToken field. Duplicating it doubles payload size and
        // increases the chance the model paraphrases it into user-visible chat.
        var defaultInstruction = "Display this summary to the user for explicit confirmation. When approved, re-invoke this tool with the same arguments and the provided confirmationToken from this response.";

        return new JsonObject
        {
            ["status"] = "requires_confirmation",
            ["api"] = apiOrToolName,
            ["safety"] = safety,
            ["confirmationToken"] = token,
            ["expiresInSeconds"] = expiresInSeconds,
            ["summary"] = summary,
            ["args"] = args?.Clone() ?? new JsonObject(),
            ["instruction"] = instruction ?? defaultInstruction,
        };
    }

    private static string Sign(string data, byte[] key)
    {
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return ToBase64Url(hash);
    }

    public static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static byte[] FromBase64Url(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        try
        {
            return Convert.FromBase64String(s);
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }
}
