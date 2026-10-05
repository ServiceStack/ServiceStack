using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>Host-owned endpoints. Defaults follow the public SIWC protocol verified 2026-10-05.</summary>
public sealed class OpenAiSubscriptionOptions
{
    public string Issuer { get; set; } = "https://auth.openai.com";
    public string AuthorizationUrl { get; set; } = "https://auth.openai.com/api/accounts/authorize";
    public string TokenUrl { get; set; } = "https://auth.openai.com/api/accounts/oauth/token";
    public string JwksUrl { get; set; } = "https://auth.openai.com/.well-known/jwks.json";
    public string ResponsesUrl { get; set; } = "https://api.openai.com/v1/responses";
    public string ModelsUrl { get; set; } = "https://api.openai.com/v1/models";
    public string Resource { get; set; } = "https://api.openai.com/v1";
    public string ClientId { get; set; } = "dynamic_agent_client";
    public string Scope { get; set; } = "openid profile email offline_access resource.invoke chatgpt.tokens.use.direct";
    public string AgentName { get; set; } = "ServiceStack AI.Chat";
    public string RedirectUri { get; set; } = "http://127.0.0.1:1455/auth/callback";
    public string DefaultModel { get; set; } = "gpt-5.5";
    public List<string> Models { get; set; } = ["gpt-5.5", "gpt-6-astra", "gpt-6.1-sol", "gpt-6-sol", "gpt-6-luna", "gpt-5.6-sol", "gpt-5.6-terra", "gpt-5.6-luna", "gpt-reserve", "codex-auto-review"];
    public TimeSpan TokenTimeout { get; set; } = TimeSpan.FromSeconds(20);
    public TimeSpan FlowLifetime { get; set; } = TimeSpan.FromMinutes(10);
    public TimeProvider Clock { get; set; } = TimeProvider.System;
    public Func<HttpMessageHandler>? HttpHandlerFactory { get; set; }
    /// <summary>Opt-in trusted local import: the host resolves a file for this identity, never ~/.codex implicitly.</summary>
    public Func<string, string?>? LocalCredentialsPath { get; set; }
    /// <summary>Additional authorization required for any local credential-file import.</summary>
    public Func<ChatRequestContext, bool>? CanImportLocalCredentials { get; set; }
}

/// <summary>One bounded, redirect-free transport shared by token, keys and catalog requests.</summary>
public sealed class OpenAiSubscriptionHttp(OpenAiSubscriptionOptions options)
{
    static readonly SocketsHttpHandler Transport = new() { AllowAutoRedirect = false, UseCookies = false, ConnectTimeout = TimeSpan.FromSeconds(10) };
    public HttpClient Client() => new(options.HttpHandlerFactory?.Invoke() ?? Transport, disposeHandler: options.HttpHandlerFactory != null) { Timeout = Timeout.InfiniteTimeSpan };
    public static Uri Endpoint(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.UserInfo.Length > 0 || uri.Fragment.Length > 0
            || uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback))
            throw new InvalidOperationException("Configure an HTTPS subscription endpoint (loopback HTTP is allowed for isolated tests).");
        return uri;
    }
    public async Task<JsonObject> JsonAsync(HttpRequestMessage request, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(options.TokenTimeout);
        using var client = Client();
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) throw HttpError.BadRequest($"OpenAI credential service returned HTTP {(int)response.StatusCode}. Start a new sign-in or try again later.");
            if (response.RequestMessage?.RequestUri != null && response.RequestMessage.RequestUri != request.RequestUri)
                throw HttpError.BadRequest("Subscription redirects are not supported.");
            if (response.Content.Headers.ContentLength > 1024 * 1024) throw HttpError.BadRequest("Subscription response exceeds the size limit.");
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            using var output = new MemoryStream(); var buffer = new byte[16384];
            while (true)
            {
                var count = await stream.ReadAsync(buffer, timeout.Token).ConfigureAwait(false); if (count == 0) break;
                if (output.Length + count > 1024 * 1024) throw HttpError.BadRequest("Subscription response exceeds the size limit.");
                output.Write(buffer, 0, count);
            }
            return Parse(new UTF8Encoding(false, true).GetString(output.ToArray()));
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { throw new HttpError(504, "SubscriptionTimeout", "OpenAI credential service timed out. Start a new sign-in or try again later."); }
        catch (Exception e) when (e is HttpRequestException or IOException or System.Text.Json.JsonException or DecoderFallbackException)
        { throw HttpError.BadRequest("Could not read the OpenAI credential response. Start a new sign-in or try again later."); }
    }
    public static JsonObject Parse(string text)
    {
        using var document = System.Text.Json.JsonDocument.Parse(text);
        void Finite(System.Text.Json.JsonElement value)
        {
            if (value.ValueKind == System.Text.Json.JsonValueKind.Number && (!value.TryGetDouble(out var number) || !double.IsFinite(number)))
                throw HttpError.BadRequest("OpenAI returned non-finite JSON.");
            if (value.ValueKind == System.Text.Json.JsonValueKind.Object) foreach (var property in value.EnumerateObject()) Finite(property.Value);
            if (value.ValueKind == System.Text.Json.JsonValueKind.Array) foreach (var item in value.EnumerateArray()) Finite(item);
        }
        Finite(document.RootElement);
        return JsonNode.Parse(text) as JsonObject ?? throw HttpError.BadRequest("OpenAI returned an invalid credential response.");
    }
    public Task<JsonObject> GetAsync(string url, CancellationToken token) => SendGetAsync(url, token);
    async Task<JsonObject> SendGetAsync(string url, CancellationToken token)
    { using var request = new HttpRequestMessage(HttpMethod.Get, Endpoint(url)); return await JsonAsync(request, token).ConfigureAwait(false); }
    public async Task<JsonObject> TokenAsync(Dictionary<string, string> form, CancellationToken token)
    { using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint(options.TokenUrl)) { Content = new FormUrlEncodedContent(form) }; return await JsonAsync(request, token).ConfigureAwait(false); }
}

/// <summary>Claims are display metadata only until signature, issuer, audience, expiry and nonce pass.</summary>
public sealed class OpenAiSubscriptionIdentity(OpenAiSubscriptionOptions options, OpenAiSubscriptionHttp http)
{
    readonly SemaphoreSlim keysGate = new(1, 1);
    JsonObject? keys;
    DateTimeOffset loadedAt;
    public static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public static byte[] Decode(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4));
    public static JsonObject Claims(string? jwt)
    { try { var parts = jwt?.Split('.'); return parts?.Length == 3 ? ChatJson.ParseObject(Encoding.UTF8.GetString(Decode(parts[1]))) : new(); } catch { return new(); } }
    public async Task<JsonObject> VerifyAsync(string jwt, string clientId, string? nonce, CancellationToken token)
    {
        try
        {
            if (jwt.Length > 128 * 1024) throw new FormatException();
            var parts = jwt.Split('.'); if (parts.Length != 3) throw new FormatException();
            var header = ChatJson.ParseObject(Encoding.UTF8.GetString(Decode(parts[0])));
            if (header.GetString("alg") != "RS256" || string.IsNullOrEmpty(header.GetString("kid"))) throw new FormatException();
            var kid = header.GetString("kid");
            await keysGate.WaitAsync(token).ConfigureAwait(false);
            JsonObject? key;
            try
            {
                key = keys?.GetArray("keys")?.OfType<JsonObject>().FirstOrDefault(k => k.GetString("kid") == kid);
                if (key == null || options.Clock.GetUtcNow() - loadedAt > TimeSpan.FromHours(1))
                {
                    keys = await http.GetAsync(options.JwksUrl, token).ConfigureAwait(false); loadedAt = options.Clock.GetUtcNow();
                    key = keys.GetArray("keys")?.OfType<JsonObject>().FirstOrDefault(k => k.GetString("kid") == kid);
                }
            }
            finally { keysGate.Release(); }
            if (key?.GetString("kty") != "RSA" || key.GetString("alg") is { } alg && alg != "RS256" || key.GetString("use") is { } use && use != "sig") throw new FormatException();
            using var rsa = RSA.Create(); rsa.ImportParameters(new RSAParameters { Modulus = Decode(key!.GetString("n")!), Exponent = Decode(key.GetString("e")!) });
            if (!rsa.VerifyData(Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), Decode(parts[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) throw new FormatException();
            var claims = Claims(jwt); var now = options.Clock.GetUtcNow().ToUnixTimeSeconds();
            var aud = claims["aud"]; var audience = aud is JsonArray arr ? arr.Any(v => v?.GetValue<string>() == clientId) : claims.GetString("aud") == clientId;
            if (claims.GetString("iss") != options.Issuer || !audience || string.IsNullOrEmpty(claims.GetString("sub"))
                || claims.GetLong("exp") is not { } exp || exp <= now || claims.GetLong("nbf") is { } nbf && nbf > now + 30
                || nonce != null && claims.GetString("nonce") != nonce || aud is JsonArray { Count: > 1 } && claims.GetString("azp") != clientId)
                throw new FormatException();
            return claims;
        }
        catch (Exception e) when (e is FormatException or CryptographicException or System.Text.Json.JsonException or ArgumentException or InvalidOperationException)
        { throw HttpError.BadRequest("OpenAI identity verification failed. Start a new sign-in."); }
    }
    public static JsonObject Account(JsonObject claims)
    {
        var auth = claims.GetObject("https://api.openai.com/auth");
        var id = auth.GetString("chatgpt_account_id") ?? auth.GetString("account_id") ?? claims.GetString("chatgpt_account_id") ?? claims.GetString("account_id")
            ?? claims.GetArray("organizations")?.OfType<JsonObject>().FirstOrDefault().GetString("id") ?? "";
        var rawPlan = auth.GetString("chatgpt_plan_type") ?? auth.GetString("plan_type") ?? claims.GetString("plan");
        var email = claims.GetString("email") ?? claims.GetObject("https://api.openai.com/profile").GetString("email") ?? "";
        var name = claims.GetString("name") ?? claims.GetString("preferred_username") ?? (email.Length > 0 ? email.Split('@')[0] : "ChatGPT User");
        var plan = rawPlan == null ? "ChatGPT Subscription" : rawPlan.StartsWith("chatgpt", StringComparison.OrdinalIgnoreCase) ? rawPlan : "ChatGPT " + System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(rawPlan);
        return new() { ["email"] = email, ["name"] = name, ["plan"] = plan, ["account_id"] = id };
    }
}
