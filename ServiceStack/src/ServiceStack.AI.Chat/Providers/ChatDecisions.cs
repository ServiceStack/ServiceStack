using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace ServiceStack.AI;

/// <summary>
/// A failed Decisions request. <see cref="ProviderStatus"/> keeps OpenRouter's status; the HTTP status
/// never reports the provider's credential failures (401/403) as the caller's own authentication.
/// </summary>
public sealed class ChatDecisionException(string message, int providerStatus = 502, JsonNode? raw = null)
    : HttpError(PublicStatus(providerStatus), "DecisionFailed", message)
{
    /// <summary>OpenRouter's status, or 502/503/504 for transport, configuration and timeout failures</summary>
    public int ProviderStatus { get; } = providerStatus;
    /// <summary>The provider's response when it was readable but not a complete, valid set of answers</summary>
    public JsonNode? Raw { get; } = raw;

    static int PublicStatus(int status) => status is 400 or 402 or 413 or 429 or 503 or 504 ? status : 502;
}

/// <summary>
/// OpenRouter's non-chat Decisions API (POST /api/alpha/decisions): one bounded request with typed
/// answers. Deliberately outside the chat pipeline, which may retry or fail over: a paid decision
/// with an uncertain outcome is never replayed. No tools, history, threads or chat filters apply.
/// </summary>
public sealed class ChatDecisions(ChatFeature feature, Func<HttpMessageHandler>? handlerFactory = null)
{
    public const string ProviderId = "openrouter";
    public const string Endpoint = "https://openrouter.ai/api/alpha/decisions";
    public const string DefaultModel = "~typesafe/jev-latest";
    public const int MaxResponseBytes = 2 * 1024 * 1024;
    public static readonly string[] QuestionTypes = ["noul", "choice", "score"];
    static readonly SocketsHttpHandler Transport = new() { AllowAutoRedirect = false, UseCookies = false, ConnectTimeout = TimeSpan.FromSeconds(10) };

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(45);

    /// <summary>The configured OpenRouter provider, or why decisions are unavailable</summary>
    public (ChatProvider? Provider, string Message) Status()
    {
        if (!feature.Providers.TryGetValue(ProviderId, out var provider))
            return (null, "Enable OpenRouter in provider settings to run Jev decisions.");
        if (string.IsNullOrEmpty(provider.ApiKey))
            return (null, "Add your OpenRouter API key in provider settings to run Jev decisions.");
        return (provider, "Connected to OpenRouter");
    }

    /// <summary>The provider's configured API root (".../api/v1") selects the matching decisions endpoint</summary>
    public static string EndpointFor(ChatProvider provider)
    {
        var api = provider.Api.TrimEnd('/');
        return Uri.TryCreate(api, UriKind.Absolute, out var uri) && uri.Scheme == "https" && api.EndsWith("/v1", StringComparison.Ordinal)
            ? api[..^3] + "/alpha/decisions"
            : Endpoint;
    }

    /// <summary>
    /// Check a request against OpenRouter's documented contract before spending a paid call:
    /// a model, non-empty state and at least one noul/choice/score question with matching criteria.
    /// </summary>
    public static JsonObject Validate(JsonNode? request)
    {
        static void Require(bool valid, string message) { if (!valid) throw HttpError.BadRequest(message); }
        static bool Text(JsonNode? value) => value is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s);

        Require(request is JsonObject, "Expected a decision request object");
        var body = (JsonObject)request!.DeepClone();
        body["model"] ??= DefaultModel;
        Require(Text(body["model"]) && body["model"]!.GetValue<string>().Length <= 200, "model must be a decision model id");
        var state = body["state"];
        Require(state is JsonObject or JsonArray || Text(state), "state must be non-empty text, or a JSON object or array of context");
        Require(body["questions"] is JsonObject { Count: > 0 }, "questions must define at least one question");
        foreach (var (name, value) in (JsonObject)body["questions"]!)
        {
            Require(name.Trim().Length > 0 && value is JsonObject, $"Question '{name}' must be an object");
            var question = (JsonObject)value!;
            var type = question["type"] is JsonValue t && t.TryGetValue<string>(out var s) ? s : null;
            Require(type != null && QuestionTypes.Contains(type), $"Question '{name}' type must be noul, choice or score");
            Require(Text(question["instructions"]) || question["instructions"] is JsonObject or JsonArray, $"Question '{name}' needs instructions");
            var criteria = question["criteria"];
            switch (type)
            {
                case "noul":
                    Require(criteria == null || criteria is JsonObject c && c.Count == 2 && Text(c["true"]) && Text(c["false"]),
                        $"Question '{name}' criteria must describe both true and false");
                    break;
                case "choice":
                    Require(criteria is JsonObject { Count: > 0 } options && options.All(x => x.Key.Length > 0 && Text(x.Value)),
                        $"Question '{name}' criteria must name each option with a description");
                    break;
                default:
                    Require(criteria is JsonArray { Count: > 0 } scale && scale.All(Text),
                        $"Question '{name}' criteria must be an ordered list of descriptions");
                    break;
            }
        }
        foreach (var field in new[] { "session_id", "user" })
            Require(body[field] == null || Text(body[field]) && body[field]!.GetValue<string>().Length <= 256, $"{field} must be at most 256 characters");
        return body;
    }

    /// <summary>
    /// Send one request and return the provider's JSON with its answers validated against the
    /// submitted questions. Any failure throws <see cref="ChatDecisionException"/> and is never retried.
    /// </summary>
    public async Task<(JsonNode Raw, JsonObject Answers)> SendAsync(JsonObject payload, CancellationToken token = default)
    {
        var (provider, message) = Status();
        if (provider == null) throw new ChatDecisionException(message, 503);
        using var request = new HttpRequestMessage(HttpMethod.Post, EndpointFor(provider))
        {
            Content = new StringContent(JevJson.Encode(payload), Encoding.UTF8, "application/json"),
        };
        foreach (var header in provider.Headers.ToArray())
            if (!header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) && !header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", provider.ApiKey);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(RequestTimeout);
        using var client = new HttpClient(handlerFactory?.Invoke() ?? Transport, disposeHandler: handlerFactory != null) { Timeout = Timeout.InfiniteTimeSpan };
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var status = (int)response.StatusCode;
                throw new ChatDecisionException(status switch {
                    401 => "OpenRouter rejected the API key. Check provider settings.",
                    402 => "Your OpenRouter account needs credits.",
                    429 => "OpenRouter is rate limiting requests. Wait a moment, then run again.",
                    400 => "OpenRouter rejected this decision request. Review the questions and input.",
                    413 => "This input is too large for OpenRouter. Try a shorter document.",
                    _ => $"OpenRouter could not complete the request (HTTP {status}). Try again later.",
                }, status);
            }
            if (response.RequestMessage?.RequestUri is { } final && final != request.RequestUri)
                throw new ChatDecisionException("Decision redirects are not supported.");
            if (response.Content.Headers.ContentLength > MaxResponseBytes)
                throw new ChatDecisionException("The provider response exceeded the size limit.");
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            using var output = new MemoryStream();
            var buffer = new byte[65536];
            while (true)
            {
                var count = await stream.ReadAsync(buffer, timeout.Token).ConfigureAwait(false);
                if (count == 0) break;
                if (output.Length + count > MaxResponseBytes) throw new ChatDecisionException("The provider response exceeded the size limit.");
                output.Write(buffer, 0, count);
            }
            JsonNode raw;
            try { raw = JsonNode.Parse(new UTF8Encoding(false, true).GetString(output.ToArray())) ?? throw new FormatException(); JevJson.Encode(raw); }
            catch (Exception e) when (e is System.Text.Json.JsonException or DecoderFallbackException or FormatException or JevValidationException)
            {
                throw new ChatDecisionException("OpenRouter returned an unreadable response.");
            }
            try { return (raw, DecisionRecorded.Normalize(raw, (JsonObject)payload["questions"]!)); }
            catch (JevValidationException e) { throw new ChatDecisionException(e.Message, raw: raw); }
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            throw new ChatDecisionException("The request timed out. It may have been processed by OpenRouter; use Run again for a new attempt.", 504);
        }
        catch (Exception e) when (e is HttpRequestException or IOException)
        {
            throw new ChatDecisionException("Could not reach OpenRouter. Check your connection, then run again.");
        }
    }

    /// <summary>
    /// Validate, send once and return OpenRouter's response shape (id, model, provider, usage) with
    /// answers normalized against the submitted questions (complete, and score legends filled in).
    /// </summary>
    public async Task<JsonObject> CreateAsync(JsonNode? request, CancellationToken token = default)
    {
        var payload = Validate(request);
        var (raw, answers) = await SendAsync(payload, token).ConfigureAwait(false);
        var response = raw as JsonObject ?? new JsonObject();
        var result = new JsonObject
        {
            ["id"] = response["id"]?.DeepClone(),
            ["model"] = response["model"]?.DeepClone() ?? payload["model"]?.DeepClone(),
            ["provider"] = response["provider"]?.DeepClone(),
            ["answers"] = answers,
        };
        if (response["usage"] is JsonObject usage) result["usage"] = usage.DeepClone();
        return result;
    }
}

public partial class ChatFeature
{
    /// <summary>Optional transport seam for the Decisions API (tests/host integration); it must not follow redirects.</summary>
    public Func<HttpMessageHandler>? DecisionHandlerFactory { get; set; }

    /// <summary>OpenRouter's Decisions API via the configured "openrouter" provider (stateless, cheap to create)</summary>
    public ChatDecisions Decisions => new(this, DecisionHandlerFactory);

    /// <summary>Create one decision (POST /api/alpha/decisions). Never retried and never stored in chat history.</summary>
    public Task<JsonObject> CreateDecisionAsync(JsonNode? request, CancellationToken token = default) =>
        Decisions.CreateAsync(request, token);
}
