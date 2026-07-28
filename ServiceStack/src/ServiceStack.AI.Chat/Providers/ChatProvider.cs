using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Base class for all chat providers — the C# port of llms-py's OpenAiCompatible (main.py:1172).
/// Constructed from a merged provider definition (models.dev providers.json entry + llms.json overrides).
/// </summary>
public class ChatProvider
{
    public virtual string Sdk => "@ai-sdk/openai-compatible";

    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Api { get; set; } = "";
    public string? ApiKey { get; set; }
    public List<string> Env { get; set; } = [];
    public Dictionary<string, string> Headers { get; set; } = [];

    /// <summary>model id → model info (models.dev shape)</summary>
    public Dictionary<string, JsonObject> Models { get; set; } = [];
    /// <summary>model id → provider model id</summary>
    public Dictionary<string, string> MapModels { get; set; } = [];

    public double? FrequencyPenalty { get; set; }
    public int? MaxCompletionTokens { get; set; }
    public int? N { get; set; }
    public bool? ParallelToolCalls { get; set; }
    public double? PresencePenalty { get; set; }
    public string? PromptCacheKey { get; set; }
    public string? ReasoningEffort { get; set; }
    public string? SafetyIdentifier { get; set; }
    public int? Seed { get; set; }
    public string? ServiceTier { get; set; }
    public JsonNode? Stop { get; set; }
    public bool? Store { get; set; }
    public double? Temperature { get; set; }
    public int? TopLogprobs { get; set; }
    public double? TopP { get; set; }
    public string? Verbosity { get; set; }
    public bool Stream { get; set; } = true;
    public bool? EnableThinking { get; set; }
    public JsonObject? Check { get; set; }
    public Dictionary<string, ChatProvider> Modalities { get; set; } = [];
    public List<string> ServerTools { get; set; } = [];

    /// <summary>The original merged definition this provider was created from</summary>
    public JsonObject Definition { get; set; } = new();

    public ILogger Log { get; set; } = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    public IHttpClientFactory? HttpClientFactory { get; set; }

    /// <summary>The owning ChatFeature (Python providers access the g_app global)</summary>
    public ChatFeature? Feature { get; set; }

    public virtual string ChatUrl => $"{Api}/chat/completions";

    /// <summary>Python id_to_name: "fireworks-ai" → "Fireworks Ai"</summary>
    public static string IdToName(string id) =>
        string.Join(" ", id.Split('-').Select(word => word.Length > 0
            ? char.ToUpper(word[0]) + word[1..].ToLower()
            : word));

    /// <summary>Populate from a merged provider definition (port of OpenAiCompatible.__init__)</summary>
    public virtual void Populate(JsonObject kwargs)
    {
        Definition = kwargs;
        Id = kwargs.GetString("id") ?? throw new ArgumentException("Missing required argument: id");
        Api = (kwargs.GetString("api") ?? throw new ArgumentException("Missing required argument: api")).TrimEnd('/');
        ApiKey = kwargs.GetString("api_key");
        Name = kwargs.GetString("name") ?? IdToName(Id);

        if (kwargs.GetArray("env") is { } env)
        {
            Env = env.Select(x => x?.GetValue<string>()).Where(x => x != null).Cast<string>().ToList();
        }
        if (kwargs.GetObject("headers") is { } headers)
        {
            Headers = new Dictionary<string, string>();
            foreach (var entry in headers)
            {
                if (entry.Value is JsonValue v && v.TryGetValue<string>(out var s))
                    Headers[entry.Key] = s;
            }
        }
        if (ApiKey != null)
        {
            Headers["Authorization"] = $"Bearer {ApiKey}";
        }

        SetModels(kwargs);

        FrequencyPenalty = kwargs.GetDouble("frequency_penalty");
        MaxCompletionTokens = kwargs.GetInt("max_completion_tokens");
        N = kwargs.GetInt("n");
        ParallelToolCalls = kwargs.TryGetPropertyValue("parallel_tool_calls", out _) ? kwargs.GetBool("parallel_tool_calls") : null;
        PresencePenalty = kwargs.GetDouble("presence_penalty");
        PromptCacheKey = kwargs.GetString("prompt_cache_key");
        ReasoningEffort = kwargs.GetString("reasoning_effort");
        SafetyIdentifier = kwargs.GetString("safety_identifier");
        Seed = kwargs.GetInt("seed");
        ServiceTier = kwargs.GetString("service_tier");
        Stop = kwargs.TryGetPropertyValue("stop", out var stop) ? stop?.DeepClone() : null;
        Store = kwargs.TryGetPropertyValue("store", out _) ? kwargs.GetBool("store") : null;
        Temperature = kwargs.GetDouble("temperature");
        TopLogprobs = kwargs.GetInt("top_logprobs");
        TopP = kwargs.GetDouble("top_p");
        Verbosity = kwargs.GetString("verbosity");
        Stream = kwargs.TryGetPropertyValue("stream", out _) ? kwargs.GetBool("stream") : true;
        EnableThinking = kwargs.TryGetPropertyValue("enable_thinking", out _) ? kwargs.GetBool("enable_thinking") : null;
        Check = kwargs.GetObject("check")?.Clone();

        if (kwargs.GetArray("server_tools") is { } serverTools)
        {
            ServerTools = serverTools.Select(x => x?.GetValue<string>()).Where(x => x != null).Cast<string>().ToList();
        }
    }

    /// <summary>Port of OpenAiCompatible.set_models: map_models/include_models/exclude_models filtering</summary>
    public void SetModels(JsonObject kwargs)
    {
        var models = new Dictionary<string, JsonObject>();
        if (kwargs.GetObject("models") is { } modelsObj)
        {
            foreach (var entry in modelsObj)
            {
                if (entry.Value is JsonObject model)
                    models[entry.Key] = model;
            }
        }

        MapModels = [];
        if (kwargs.GetObject("map_models") is { } mapModels)
        {
            foreach (var entry in mapModels)
            {
                if (entry.Value is JsonValue v && v.TryGetValue<string>(out var s))
                    MapModels[entry.Key] = s;
            }
        }

        if (MapModels.Count > 0)
        {
            Models = [];
            foreach (var providerModelId in MapModels.Values)
            {
                if (models.TryGetValue(providerModelId, out var model))
                    Models[providerModelId] = model;
            }
        }
        else
        {
            Models = models;
        }

        var includeModels = kwargs.GetString("include_models");
        if (!string.IsNullOrEmpty(includeModels))
        {
            Models = Models.Where(x => Regex.IsMatch(x.Key, includeModels))
                .ToDictionary(x => x.Key, x => x.Value);
        }
        var excludeModels = kwargs.GetString("exclude_models");
        if (!string.IsNullOrEmpty(excludeModels))
        {
            Models = Models.Where(x => !Regex.IsMatch(x.Key, excludeModels))
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }

    public virtual string? Validate() => string.IsNullOrEmpty(ApiKey)
        ? $"Provider '{Name}' requires API Key {string.Join(", ", Env)}"
        : null;

    public bool Test()
    {
        var errorMsg = Validate();
        if (errorMsg != null)
        {
            Log.LogInformation("{Message}", errorMsg);
            return false;
        }
        return true;
    }

    public virtual Task LoadAsync(CancellationToken token = default) => Task.CompletedTask;

    public virtual JsonObject? ModelInfo(string model)
    {
        var providerModel = ProviderModel(model) ?? model;
        foreach (var entry in Models)
        {
            if (entry.Key.EqualsIgnoreCase(providerModel))
                return entry.Value;
        }
        return null;
    }

    public JsonObject? ModelCost(string model) => ModelInfo(model)?.GetObject("cost");

    /// <summary>Case-insensitive model id resolution (port of OpenAiCompatible.provider_model)</summary>
    public virtual string? ProviderModel(string model)
    {
        foreach (var entry in MapModels)
        {
            if (entry.Key.EqualsIgnoreCase(model))
                return entry.Value;
        }
        foreach (var providerModel in MapModels.Values)
        {
            if (providerModel.EqualsIgnoreCase(model))
                return providerModel;
        }
        foreach (var entry in Models)
        {
            var id = entry.Value.GetString("id") ?? entry.Key;
            if (entry.Key.EqualsIgnoreCase(model) || id.EqualsIgnoreCase(model))
                return id;
            var name = entry.Value.GetString("name");
            if (name != null && name.EqualsIgnoreCase(model))
                return id;
        }
        foreach (var entry in Models)
        {
            var id = entry.Value.GetString("id") ?? entry.Key;
            if (id.IndexOf('/') >= 0)
            {
                var modelName = id.LastRightPart('/');
                if (modelName.EqualsIgnoreCase(model))
                    return id;
            }
        }
        if (model.IndexOf('/') >= 0)
        {
            return ProviderModel(model.LastRightPart('/'));
        }
        return null;
    }

    public bool HasModel(string model) => ProviderModel(model) != null;

    /// <summary>Send the chat request. Implemented by OpenAiCompatibleProvider and custom providers in Phase 2.</summary>
    public virtual Task<JsonObject> ChatAsync(JsonObject chat, ChatContext context) =>
        throw new NotImplementedException($"{GetType().Name} does not implement ChatAsync");

    // ── Streaming resilience (port of stream_writer/stream_error_message) ──

    /// <summary>
    /// Checkpoints the in-flight response to the thread's `streamingMessage`. It never writes
    /// `messages`, so a provider dying mid-stream cannot damage the conversation.
    /// </summary>
    public StreamCheckpointWriter CreateStreamWriter(ChatContext? context) =>
        new(Feature?.ThreadApi, context?.ThreadId, context?.User,
            Feature?.Limits.StreamCheckpointInterval);

    /// <summary>Message for an error a provider reported mid-stream as an SSE chunk</summary>
    public static string StreamErrorMessage(JsonNode? error, string defaultMessage = "Streaming error")
    {
        if (error is JsonObject obj)
            return obj.GetString("message") ?? obj.ToJsonString(ChatJson.Options);
        var message = error is JsonValue v && v.TryGetValue<string>(out var s) ? s : error?.ToJsonString();
        return string.IsNullOrEmpty(message) ? defaultMessage : message;
    }

    /// <summary>How long to wait for the next chunk of a streaming response before giving up</summary>
    protected TimeSpan StreamReadTimeout => TimeSpan.FromSeconds(Feature?.Limits.ClientTimeout ?? 120);

    /// <summary>Read the provider's SSE response with a per-chunk (not total) timeout</summary>
    protected Task<SseReader> OpenSseAsync(HttpResponseMessage httpRes, ChatContext context) =>
        SseReader.CreateAsync(httpRes, context, StreamReadTimeout, Name);

    /// <summary>
    /// Send a streaming request with a deadline on the connect + response-headers phase only. Once the
    /// provider starts responding <see cref="SseReader"/>'s per-chunk deadline takes over, since a
    /// deadline on the response as a whole would kill long streams (the streaming HttpClient has no
    /// timeout of its own for exactly that reason).
    /// </summary>
    protected async Task<HttpResponseMessage> SendStreamingAsync(HttpClient client, HttpRequestMessage httpReq,
        ChatContext context)
    {
        // cancelling this after the headers arrive would abort the response stream, so it outlives this
        // method (with its timer stopped) and is collected with the request
        var cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
        cts.CancelAfter(StreamReadTimeout);
        try
        {
            var httpRes = await client.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                .ConfigAwait();
            cts.CancelAfter(Timeout.InfiniteTimeSpan); // responding now, stop the clock
            return httpRes;
        }
        catch (OperationCanceledException) when (!context.CancellationToken.IsCancellationRequested)
        {
            cts.Dispose();
            throw new TimeoutException(
                $"Provider {Name} did not start responding within {StreamReadTimeout.TotalSeconds:0}s");
        }
        catch
        {
            cts.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Wire up a modality sub-provider, sharing this provider's runtime services and credentials
    /// (Python attaches these in each provider's __init__ as self.modalities[...]).
    /// A sub-provider explicitly configured in llms.json "modalities" overrides the built-in one.
    /// </summary>
    protected ChatProvider AttachGenerator(GeneratorProvider generator, JsonObject kwargs)
    {
        generator.Log = Log;
        generator.HttpClientFactory = HttpClientFactory;
        generator.Feature = Feature;

        var generatorArgs = kwargs.Clone();
        generatorArgs.Remove("modalities");
        generatorArgs["id"] = Id;
        generatorArgs["api_key"] ??= ApiKey;
        generator.Populate(generatorArgs);
        return generator;
    }
}
