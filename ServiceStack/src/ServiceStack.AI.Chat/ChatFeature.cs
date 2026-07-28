using ServiceStack.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ServiceStack.Configuration;
using ServiceStack.Web;

namespace ServiceStack.AI;

public enum ChatAuthType
{
    /// <summary>Stock llms-py SignIn: GET /auth with Authorization: Bearer (requires ApiKeysFeature)</summary>
    ApiKey,
    /// <summary>ASP.NET Identity Auth cookies; SignIn redirects to the host's Identity login page</summary>
    OAuth,
    /// <summary>
    /// ASP.NET Identity Auth cookies with the Chat UI's own username/password SignIn, authenticated
    /// with the host's 'credentials' auth provider (requires AuthFeature + options.CredentialsAuth()).
    /// </summary>
    Credentials,
}

public class ChatToolsConfig
{
    /// <summary>Allow LLM tools to execute code on the server (core_tools run_*, computer run_bash). OFF by default.</summary>
    public bool EnableCodeExecution { get; set; }
    /// <summary>Allow LLM filesystem tools (computer extension). OFF by default.</summary>
    public bool EnableFilesystemTools { get; set; }
    /// <summary>Directories LLM filesystem/code tools may access</summary>
    public List<string> AllowedDirectories { get; set; } = [];
    public TimeSpan ToolTimeout { get; set; } = TimeSpan.FromSeconds(60);
}

/// <summary>llms.json "limits" (Python DEFAULT_LIMITS + max_iterations)</summary>
public class ChatLimits
{
    public int ClientTimeout { get; set; } = 120;
    public long ClientMaxSize { get; set; } = 20 * 1024 * 1024;
    public int Retries { get; set; } = 3;
    public int MaxIterations { get; set; } = 10;

    /// <summary>
    /// How often an in-flight streamed response is persisted. This is also how often the UI sees it,
    /// so it trades smoothness against write volume. Cheap now that a checkpoint writes one small
    /// column instead of rewriting the whole conversation.
    /// </summary>
    public TimeSpan StreamCheckpointInterval { get; set; } = TimeSpan.FromMilliseconds(250);
}

/// <summary>
/// ServiceStack plugin hosting the llms-py v4 web app: OpenAI-compatible /v1/chat/completions,
/// the Chat UI (synced verbatim from llms-py), and its modular extensions — using
/// ServiceStack Identity Auth, OrmLite persistence and App_Data/chat file storage.
/// </summary>
public partial class ChatFeature : IPlugin, Model.IHasStringId, IConfigureServices, IPreInitPlugin, IRequireLoadAsync
{
    public string Id => Plugins.AiChat;

    /// <summary>IRequest.Items key holding the raw /v1/chat/completions request JSON</summary>
    public const string ChatJsonKey = "__chatJson";

    // ── Host configuration ──

    /// <summary>Path the Chat UI + APIs are mounted at. "" mounts at root (full llms-py fidelity).</summary>
    public string RoutePrefix { get; set; } = "/chat";

    /// <summary>When false everything runs as the "default" user without authentication (Python parity)</summary>
    public bool RequireAuth { get; set; } = true;

    public ChatAuthType AuthType { get; set; } = ChatAuthType.Credentials;

    /// <summary>Identity Auth login page the UI redirects to when AuthType=OAuth</summary>
    public string SignInUrl { get; set; } = "/Account/Login";

    /// <summary>File storage root. Default: {ContentRoot}/App_Data/chat</summary>
    public string? AppDataPath { get; set; }

    public bool AutoInitSchema { get; set; } = true;

    /// <summary>Extension names to disable (llms.json disable_extensions also applies)</summary>
    public List<string> DisableExtensions { get; set; } = [];
    
    /// <summary>
    /// When true, the Chat Admin UI /admin-ui/chat is disabled.
    /// </summary>
    public bool DisableAdminUi { get; set; }

    /// <summary>Force-enable only these providers (overrides llms.json "enabled" flags)</summary>
    public List<string> EnableProviders { get; set; } = [];

    /// <summary>$VAR substitutions for api keys etc. (checked before environment variables)</summary>
    public Dictionary<string, string> Variables { get; set; } = [];

    public ChatToolsConfig ToolsConfig { get; set; } = new();

    /// <summary>Optional server-side request validation for all Chat UI + API requests</summary>
    public Func<IRequest, Task<IHttpResult?>>? ValidateRequest { get; set; }

    // ── Config (llms.json + providers.json) ──

    public JsonObject Config { get; set; } = new();
    public JsonObject ProviderModels { get; set; } = new();
    public ChatLimits Limits { get; set; } = new();
    public List<string> LoadingMessages { get; set; } = ["Computing", "Cooking", "Crafting", "Creating"];

    public string? ConfigJson { set => Config = ChatJson.ParseObject(value ?? "{}"); }

    // ── Shared registries (Python AppExtensions state) ──

    public RouteRegistry Routes { get; } = new();
    public ChatFilters Filters { get; } = new();
    public ToolRegistry Tools { get; } = new();
    public Dictionary<string, string> ImportMaps { get; set; } = new()
    {
        ["vue-prod"] = "/ui/lib/vue.min.mjs",
        ["vue"] = "/ui/lib/vue.mjs",
        ["vue-router"] = "/ui/lib/vue-router.min.mjs",
        ["@servicestack/client"] = "/ui/lib/servicestack-client.mjs",
        ["@servicestack/vue"] = "/ui/lib/servicestack-vue.mjs",
        ["marked"] = "/ui/lib/marked.min.mjs",
        ["highlight.js"] = "/ui/lib/highlight.min.mjs",
        ["chart.js"] = "/ui/lib/chart.js",
        ["color.js"] = "/ui/lib/color.js",
        ["ctx.mjs"] = "/ui/ctx.mjs",
    };
    public List<JsonObject> UiExtensions { get; } = [];
    public List<string> IndexHeaders { get; } = [];
    public List<string> IndexFooters { get; } = [];

    /// <summary>npm sdk id → provider factory (Python g_app.all_providers)</summary>
    public Dictionary<string, Func<ChatProvider>> ProviderTypes { get; set; } = new()
    {
        ["@ai-sdk/openai-compatible"] = () => new OpenAiCompatibleProvider(),
        ["@ai-sdk/openai"] = () => new OpenAiProvider(),
        ["@ai-sdk/groq"] = () => new GroqProvider(),
        ["@ai-sdk/xai"] = () => new XaiProvider(),
        ["@ai-sdk/cerebras"] = () => new CerebrasProvider(),
        ["@ai-sdk/mistral"] = () => new MistralProvider(),
        ["@ai-sdk/anthropic"] = () => new AnthropicProvider(),
        // upstream's -cli variant shells out to a local `claude` binary to reuse a Claude Code
        // subscription, which doesn't apply to a web host — use the API provider for both so the
        // bundled `anthropic` config works with ANTHROPIC_API_KEY out of the box
        ["@ai-sdk/anthropic-cli"] = () => new AnthropicProvider(),
        ["@ai-sdk/google"] = () => new GoogleProvider(),
        ["@openrouter/ai-sdk-provider"] = () => new OpenRouterProvider(),
        ["@fireworks/ai-sdk-provider"] = () => new FireworksProvider(),
        ["llms-sdk-provider"] = () => new LlmsPyProvider(),
        ["codestral"] = () => new CodestralProvider(),
        ["ollama"] = () => new OllamaProvider(),
        ["lmstudio"] = () => new LmStudioProvider(),
        ["openai-local"] = () => new OpenAiLocalProvider(),

        // modality sub-providers (image/audio/speech/transcription generators)
        ["openai/image"] = () => new OpenAiImageGenerator(),
        ["openrouter/image"] = () => new OpenRouterImageGenerator(),
        ["openrouter/audio"] = () => new OpenRouterAudioGenerator(),
        ["openrouter/text-to-speech"] = () => new OpenRouterTextToSpeech(),
        ["fireworks/image"] = () => new FireworksImageGenerator(),
        ["zai/image"] = () => new ZaiImageGenerator(),
        ["chutes/image"] = () => new ChutesImageGenerator(),
        ["nvidia/image"] = () => new NvidiaImageGenerator(),
        ["mistral/transcriptions"] = () => new MistralTranscriptionGenerator(),
    };

    /// <summary>Live (enabled + configured) providers — Python g_handlers</summary>
    public Dictionary<string, ChatProvider> Providers { get; set; } = [];

    public List<IChatExtension> Extensions { get; set; } = [];
    
    readonly List<(IChatExtension Extension, ExtensionContext Ctx)> installedExtensions = [];
    public List<string> InstalledExtensionNames => installedExtensions.Map(x => x.Extension.Name);

    public IThreadApi ThreadApi { get; set; } = new NullThreadApi();
    public IMediaApi MediaApi { get; set; } = new NullMediaApi();
    public IProjectsApi ProjectsApi { get; set; } = new NullProjectsApi();

    public Dictionary<string, List<string>> AllowedDirectories { get; } = [];
    public Dictionary<string, string> AliasedDirectories { get; } = [];

    /// <summary>Whitelisted per-thread request args (Python AppExtensions.request_args)</summary>
    public static readonly HashSet<string> RequestArgs =
    [
        "image_config", "temperature", "max_completion_tokens", "seed", "top_p",
        "frequency_penalty", "presence_penalty", "stop", "reasoning_effort", "verbosity",
        "service_tier", "top_logprobs", "safety_identifier", "store", "enable_thinking",
    ];

    public static readonly Dictionary<string, string> AspectRatios = new()
    {
        ["1:1"] = "1024×1024", ["2:3"] = "832×1248", ["3:2"] = "1248×832",
        ["3:4"] = "864×1184", ["4:3"] = "1184×864", ["4:5"] = "896×1152",
        ["5:4"] = "1152×896", ["9:16"] = "768×1344", ["16:9"] = "1344×768",
        ["21:9"] = "1536×672",
    };

    // ── Runtime services ──

    public IServiceProvider Services { get; set; } = null!;
    public IHttpClientFactory HttpClientFactory { get; set; } = null!;
    public ILogger Log { get; set; } = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    public ChatAppData AppData { get; set; } = null!;
    public IChatAuth ChatAuth { get; set; } = null!;

    /// <summary>OrmLite persistence for threads/requests/media. Resolved from the host's IDbConnectionFactory.</summary>
    public ChatDb? ChatDb { get; set; }

    /// <summary>Use a named OrmLite connection for chat data instead of the default database</summary>
    public string? NamedConnection { get; set; }

    public string SvgIcon =
        "<svg xmlns='http://www.w3.org/2000/svg' width='24' height='24' viewBox='0 0 24 24'><path fill='currentColor' d='M14 5H4v13.385L5.763 17H20v-6h2v7a1 1 0 0 1-1 1H6.454L2 22.5V4a1 1 0 0 1 1-1h11zm5.53-3.68a.507.507 0 0 1 .94 0l.254.61a4.37 4.37 0 0 0 2.25 2.327l.717.32a.53.53 0 0 1 0 .962l-.758.338a4.36 4.36 0 0 0-2.22 2.25l-.246.566a.506.506 0 0 1-.934 0l-.247-.565a4.36 4.36 0 0 0-2.219-2.251l-.76-.338a.53.53 0 0 1 0-.963l.718-.32a4.37 4.37 0 0 0 2.251-2.325z'/></svg>";

    public ChatFeature()
    {
        Extensions =
        [
            new SystemPromptsExtension(),
            new AppExtension(),
            new AgentsExtension(),
            new ProjectsExtension(),
            new ToolsExtension(),
            new CoreToolsExtension(),
            new ComputerExtension(),
            new GalleryExtension(),
            new SkillsExtension(),
            new VoiceExtension(),
            new PublishExtension(),
            new GeminiExtension(),
            new KatexExtension(),
            new AnalyticsExtension(),
            new IdentityUiExtension(),
            new CredentialsExtension(),
        ];
    }

    public void Configure(IServiceCollection services)
    {
        services.AddSingleton(this);
        services.RegisterService<ChatServices>();
        if (!DisableAdminUi)
        {
            services.RegisterService<AdminChatServices>();
            services.ConfigurePlugin<UiFeature>(feature =>
            {
                feature.AddAdminLink(AdminUiFeature.Dynamic, new LinkInfo {
                    Id = "chat",
                    Label = "AI Chat",
                    Icon = Svg.ImageSvg(SvgIcon),
                    Show = $"role:{RoleNames.Admin}",
                });
                feature.AddAdminComponent("chat", "AdminChat");
            });
        }        
        services.AddSingleton<IChatClient>(new ChatClient(this));
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        appHost.Config.EmbeddedResourceBaseTypes.AddIfNotExists(GetType());
    }

    public void Register(IAppHost appHost)
    {
        Services = appHost.GetApplicationServices();
        HttpClientFactory = Services.GetRequiredService<IHttpClientFactory>();
        Log = Services.GetRequiredService<ILogger<ChatFeature>>();
        ChatAuth ??= new IdentityChatAuth(this);
        RoutePrefix = RoutePrefix.TrimEnd('/');

        AppData = new ChatAppData(AppDataPath ?? appHost.MapProjectPath("~/App_Data/chat"));

        if (ChatDb == null && Services.GetService<Data.IDbConnectionFactory>() is { } dbFactory)
        {
            ChatDb = new ChatDb(dbFactory, NamedConnection);
        }

        LoadConfig(appHost);
        CreateProviders();

        // seed the "default" user's allowed directories before extensions install, so the projects
        // extension inherits them as the baseline when a user has no active project
        if (ToolsConfig.AllowedDirectories.Count > 0)
        {
            SetAllowedDirectories(ToolsConfig.AllowedDirectories);
        }

        RegisterCoreRoutes();
        InstallExtensions(appHost);

        // extensions register cleanup with ctx.RegisterShutdownHandler; run it when the AppHost is
        // disposed (before the IOC is), which also covers hosts recreated in-process (e.g. tests)
        if (appHost is ServiceStackHost serviceStackHost)
        {
            serviceStackHost.OnDisposeCallbacks.Add(_ => RunShutdownHandlers());
        }

        // CatchAllHandlers work in both classic and endpoint-routing (MapEndpoints) hosts
        appHost.CatchAllHandlers.Add(req =>
        {
            var pathInfo = req.PathInfo;
            if (RoutePrefix.Length == 0)
                return new ChatHttpHandler(this, pathInfo);
            if (pathInfo.StartsWith(RoutePrefix, StringComparison.OrdinalIgnoreCase)
                && (pathInfo.Length == RoutePrefix.Length || pathInfo[RoutePrefix.Length] == '/'))
                return new ChatHttpHandler(this, pathInfo);
            return null!;
        });

        // OpenAI's wire format is richer than the typed DTO can express (e.g. message content can be
        // a plain string or an array of parts), so the raw request JSON is the source of truth.
        appHost.RegisterRequestBinder<ChatCompletion>(req =>
        {
            var json = req.GetRawBody();
            var chat = ChatJson.TryParseObject(json) ?? new JsonObject();
            req.Items[ChatJsonKey] = chat;
            return new ChatCompletion { Model = chat.GetString("model") ?? "" };
        });

        appHost.ScriptContext.Args[nameof(Chat)] = new Chat(this);
    }

    public class Chat(ChatFeature feature)
    {
        public List<string> Models => feature.Providers.Values
            .SelectMany(x => x.Models.Keys)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }

    void LoadConfig(IAppHost appHost)
    {
        // llms.json: programmatic Config > App_Data/chat/llms.json > embedded chat/llms.json (seeded to App_Data)
        if (Config.Count == 0)
        {
            var configJson = AppData.SeedFile("llms.json", () =>
                appHost.VirtualFileSources.GetFile("chat/llms.json")?.ReadAllText()
                ?? throw new Exception("chat/llms.json not found"));
            Config = ChatJson.ParseObject(configJson);
        }

        // replace top-level $ENV values (Python init_llms)
        foreach (var key in Config.Select(x => x.Key).ToList())
        {
            if (Config[key] is JsonValue v && v.TryGetValue<string>(out var s) && s.StartsWith('$'))
            {
                Config[key] = ResolveVariable(s) ?? "";
            }
        }

        if (Config.GetObject("limits") is { } limits)
        {
            Limits = new ChatLimits
            {
                ClientTimeout = limits.GetInt("client_timeout") ?? 120,
                ClientMaxSize = limits.GetLong("client_max_size") ?? 20 * 1024 * 1024,
                Retries = limits.GetInt("retries") ?? 3,
                MaxIterations = limits.GetInt("max_iterations") ?? 10,
                StreamCheckpointInterval = TimeSpan.FromSeconds(
                    limits.GetDouble("stream_checkpoint_interval") ?? 0.25),
            };
        }
        if (Config.GetArray("loading") is { } loading)
        {
            LoadingMessages = loading.Select(x => x?.GetValue<string>()).Where(x => x != null).Cast<string>().ToList();
        }
        if (Config.GetArray("disable_extensions") is { } disableExts)
        {
            foreach (var ext in disableExts)
            {
                if (ext?.GetValue<string>() is { } name)
                    DisableExtensions.AddIfNotExists(name);
            }
        }

        var providersJson = AppData.SeedFile("providers.json", () =>
            appHost.VirtualFileSources.GetFile("chat/providers.json")?.ReadAllText()
            ?? throw new Exception("chat/providers.json not found"));
        ProviderModels = ChatJson.ParseObject(providersJson);

        // seed providers-extra.json for future --update-providers merges
        if (appHost.VirtualFileSources.GetFile("chat/providers-extra.json")?.ReadAllText() is { } extraJson)
        {
            AppData.SeedFile("providers-extra.json", () => extraJson);
        }
    }

    public string? ResolveVariable(string value)
    {
        if (!value.StartsWith('$'))
            return value;
        var varName = value[1..];
        return Variables.TryGetValue(varName, out var varValue)
            ? varValue
            : Environment.GetEnvironmentVariable(varName);
    }

    // ── Provider lifecycle (port of init_llms/create_provider_from_definition) ──

    public void CreateProviders()
    {
        Providers = [];
        var providers = Config.GetObject("providers");
        if (providers == null)
            return;

        foreach (var entry in providers)
        {
            if (entry.Value is not JsonObject definition)
                continue;

            var enabled = EnableProviders.Count > 0
                ? EnableProviders.Contains(entry.Key)
                : definition.GetBool("enabled", true);
            if (!enabled)
                continue;

            var provider = CreateProviderFromDefinition(entry.Key, definition);
            if (provider != null && provider.Test())
            {
                Providers[entry.Key] = provider;
                Log.LogDebug("Registered provider {Name} ({Sdk})", entry.Key, provider.Sdk);
            }
        }
    }

    public ChatProvider? CreateProviderFromDefinition(string id, JsonObject orig)
    {
        var definition = orig.Clone();
        definition["id"] ??= id;
        var providerId = definition.GetString("id") ?? id;
        var kwargs = CreateProviderKwargs(definition, ProviderModels.GetObject(providerId));
        return kwargs == null ? null : CreateProvider(kwargs);
    }

    /// <summary>Merge llms.json definition over the models.dev providers.json entry (definition wins)</summary>
    JsonObject? CreateProviderKwargs(JsonObject definition, JsonObject? providerEntry)
    {
        var merged = providerEntry?.Clone() ?? new JsonObject();
        foreach (var entry in definition)
        {
            merged[entry.Key] = entry.Value?.DeepClone();
        }

        if (merged.GetString("api_key") is { } apiKey && apiKey.StartsWith('$'))
        {
            merged["api_key"] = ResolveVariable(apiKey) ?? "";
        }
        if (merged.GetString("api_key").IsNullOrEmpty() && merged.GetArray("env") is { } env)
        {
            foreach (var envVar in env)
            {
                if (envVar?.GetValue<string>() is { } varName)
                {
                    var val = Variables.GetValueOrDefault(varName) ?? Environment.GetEnvironmentVariable(varName);
                    if (!string.IsNullOrEmpty(val))
                    {
                        merged["api_key"] = val;
                        break;
                    }
                }
            }
        }

        if (Config.GetObject("defaults").GetObject("headers") is { } headers)
        {
            merged["headers"] = headers.Clone();
        }

        return merged;
    }

    public ChatProvider? CreateProvider(JsonObject kwargs)
    {
        var label = kwargs.GetString("id") ?? kwargs.GetString("name") ?? "unknown";
        var npmSdk = kwargs.GetString("npm");
        if (npmSdk == null)
        {
            Log.LogInformation("Provider {Label} is missing 'npm' sdk", label);
            return null;
        }
        if (!ProviderTypes.TryGetValue(npmSdk, out var factory))
        {
            Log.LogInformation("Could not find provider {Label} with npm sdk {Sdk}", label, npmSdk);
            return null;
        }

        var provider = factory();
        provider.Log = Log;
        provider.HttpClientFactory = HttpClientFactory;
        provider.Feature = this;

        // sub-generators for non-text modalities
        if (kwargs.GetObject("modalities") is { } modalities)
        {
            var subProviders = new Dictionary<string, ChatProvider>();
            foreach (var entry in modalities)
            {
                if (entry.Value is not JsonObject modalityDef)
                    continue;
                var subKwargs = CreateProviderKwargs(modalityDef, null);
                var subProvider = subKwargs != null ? CreateProvider(subKwargs) : null;
                if (subProvider == null)
                {
                    // Unlike llms-py (where every sdk is registered) an unported generator sdk
                    // shouldn't disable an otherwise usable provider — drop just that modality.
                    Log.LogWarning(
                        "Provider {Label} does not support '{Modality}' modality: no provider for npm sdk {Sdk}",
                        label, entry.Key, modalityDef.GetString("npm"));
                    continue;
                }
                subProviders[entry.Key] = subProvider;
            }
            kwargs.Remove("modalities");
            provider.Populate(kwargs);
            provider.Modalities = subProviders;
        }
        else
        {
            provider.Populate(kwargs);
        }
        return provider;
    }

    public (List<string> Enabled, List<string> Disabled) ProviderStatus()
    {
        var enabled = Providers.Keys.OrderBy(x => x).ToList();
        var disabled = (Config.GetObject("providers")?.Select(x => x.Key) ?? [])
            .Where(x => !Providers.ContainsKey(x))
            .OrderBy(x => x)
            .ToList();
        return (enabled, disabled);
    }

    /// <summary>Enable a provider at runtime + persist to App_Data/chat/llms.json (port of enable_provider)</summary>
    public async Task<string?> EnableProviderAsync(string providerId)
    {
        var providerConfig = Config.GetObject("providers").GetObject(providerId);
        if (providerConfig == null)
            return $"Provider {providerId} not found";

        var provider = CreateProviderFromDefinition(providerId, providerConfig);
        var msg = provider?.Validate() ?? (provider == null ? $"Provider {providerId} could not be created" : null);
        if (msg != null)
            return msg;

        providerConfig["enabled"] = true;
        SaveConfig();
        CreateProviders();
        await LoadProvidersAsync().ConfigAwait();
        return null;
    }

    public async Task DisableProviderAsync(string providerId)
    {
        var providerConfig = Config.GetObject("providers").GetObject(providerId);
        if (providerConfig == null)
            return;
        providerConfig["enabled"] = false;
        SaveConfig();
        CreateProviders();
        await LoadProvidersAsync().ConfigAwait();
    }

    public void SaveConfig()
    {
        AppData.WriteTextFile(AppData.GetHomePath("llms.json"), Config.ToJsonString(ChatJson.Indented));
    }

    /// <summary>
    /// Refresh the model catalogue from models.dev, keeping only configured providers and merging
    /// providers-extra.json overrides (port of llms-py's --update-providers).
    /// </summary>
    public async Task<int> UpdateProviderModelsAsync(CancellationToken token = default)
    {
        using var client = HttpClientFactory.CreateClient();
        var json = await client.GetStringAsync("https://models.dev/api.json", token).ConfigAwait();
        var allProviders = ChatJson.ParseObject(json);

        var extraProviders = ChatJson.TryParseObject(
            AppData.TextFromFile(AppData.GetHomePath("providers-extra.json"))) ?? new JsonObject();

        var configured = Config.GetObject("providers");
        var filtered = new JsonObject();
        foreach (var entry in allProviders)
        {
            if (configured?.ContainsKey(entry.Key) != true || entry.Value is not JsonObject provider)
                continue;
            var merged = provider.Clone();

            // layer in any extra models defined locally
            if (extraProviders.GetObject(entry.Key)?.GetObject("models") is { } extraModels)
            {
                var models = merged.GetObject("models");
                if (models == null)
                {
                    models = new JsonObject();
                    merged["models"] = models;
                }
                foreach (var modelEntry in extraModels)
                {
                    if (modelEntry.Value is not JsonObject model)
                        continue;
                    var modelCopy = model.Clone();
                    modelCopy["id"] ??= modelEntry.Key;
                    modelCopy["name"] ??= ChatProvider.IdToName(modelCopy.GetString("id") ?? modelEntry.Key);
                    models[modelEntry.Key] = modelCopy;
                }
            }
            filtered[entry.Key] = merged;
        }

        AppData.WriteTextFile(AppData.GetHomePath("providers.json"), filtered.ToJsonString(ChatJson.Options));
        ProviderModels = filtered;
        CreateProviders();
        await LoadProvidersAsync(token).ConfigAwait();

        Log.LogInformation("Updated {Count} providers from models.dev", filtered.Count);
        return filtered.Count;
    }

    // ── Extensions ──

    void InstallExtensions(IAppHost appHost)
    {
        foreach (var extension in Extensions)
        {
            if (DisableExtensions.Contains(extension.Name))
            {
                Log.LogInformation("Extension {Name} is disabled", extension.Name);
                continue;
            }
            var ctx = new ExtensionContext(this, extension.Name);
            try
            {
                extension.Install(ctx);
            }
            catch (Exception e)
            {
                Log.LogError(e, "Failed to install extension {Name}", extension.Name);
                continue;
            }
            if (ctx.Disabled)
            {
                Log.LogInformation("Extension {Name} was disabled", extension.Name);
                continue;
            }

            // auto-register synced UI assets (Python: ui/ dir → static files + index.mjs → UI extension)
            var uiDir = appHost.VirtualFileSources.GetDirectory($"chat/ext/{extension.Name}");
            if (uiDir != null)
            {
                ctx.AddStaticFiles();
                if (appHost.VirtualFileSources.GetFile($"chat/ext/{extension.Name}/index.mjs") != null)
                {
                    ctx.RegisterUiExtension();
                }
            }
            installedExtensions.Add((extension, ctx));
            Log.LogInformation("Extension {Name} installed", extension.Name);
        }
    }

    public async Task LoadAsync(CancellationToken token = default)
    {
        await LoadProvidersAsync(token).ConfigAwait();

        var tasks = installedExtensions.Map(x => x.Extension.LoadAsync(x.Ctx, token));
        try
        {
            await Task.WhenAll(tasks).ConfigAwait();
        }
        catch (Exception e)
        {
            Log.LogError(e, "Failed to load extensions");
        }
    }

    async Task LoadProvidersAsync(CancellationToken token = default)
    {
        foreach (var provider in Providers.Values)
        {
            try
            {
                await provider.LoadAsync(token).ConfigAwait();
            }
            catch (Exception e)
            {
                Log.LogError(e, "Failed to load provider {Name}", provider.Id);
            }
        }
    }

    // ── Per-request hooks ──

    readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> lastSeen = new();

    /// <summary>First-request-per-user setup (port of AppExtensions.on_request)</summary>
    public async Task OnRequestAsync(IRequest req)
    {
        // Chat UI routes bypass the ApiKeysFeature request filter, so resolve Bearer keys here
        await IdentityChatAuth.ResolveApiKeyAsync(req).ConfigAwait();

        var user = ChatAuth.GetUserName(req);
        var username = user ?? "default";
        var firstTime = !lastSeen.ContainsKey(username);
        lastSeen[username] = DateTime.UtcNow;
        if (firstTime)
        {
            Log.LogInformation("First time request from user '{User}'", username);
            foreach (var handler in Filters.SetupUserHandlers)
            {
                await handler(req).ConfigAwait();
            }
        }
    }

    /// <summary>
    /// Run the extensions' cleanup handlers (port of llms-py's shutdown signals), e.g. stopping
    /// background workers. Handlers are independent: one throwing must not skip the rest.
    /// </summary>
    public void RunShutdownHandlers()
    {
        foreach (var handler in Filters.ShutdownHandlers)
        {
            try
            {
                handler();
            }
            catch (Exception e)
            {
                Log.LogError(e, "Shutdown handler failed");
            }
        }
    }

    public JsonObject ErrorAuthRequired() =>
        ChatJson.CreateErrorResponse("Authentication required", "Unauthorized");

    // ── Misc helpers ──

    string lastLoadingMessage = "Loading";

    public string NextLoadingMessage()
    {
        if (LoadingMessages.Count == 0)
            return lastLoadingMessage;
        string next;
        do
        {
            next = LoadingMessages[Random.Shared.Next(LoadingMessages.Count)];
        } while (next == lastLoadingMessage && LoadingMessages.Count > 1);
        lastLoadingMessage = next;
        return next;
    }

    /// <summary>Replace the allowed directories for a user (port of set_allowed_directories)</summary>
    public void SetAllowedDirectories(IEnumerable<string> directories, string? user = null)
    {
        AllowedDirectories[user ?? "default"] = directories
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.StartsWith('$') ? x : Path.GetFullPath(x))
            .ToList();
    }

    /// <summary>Resolve a $alias or return the absolute path (port of resolve_directory)</summary>
    public string? ResolveDirectory(string dir) => dir.StartsWith('$')
        ? AliasedDirectories.TryGetValue(dir, out var aliased) ? Path.GetFullPath(aliased) : null
        : Path.GetFullPath(dir);

    public void AddAllowedDirectory(string path, string? user = null)
    {
        var dirs = AllowedDirectories.GetOrAdd(user ?? "default", _ => []);
        var absPath = path.StartsWith('$') ? path : Path.GetFullPath(path);
        dirs.AddIfNotExists(absPath);
    }

    public List<string> ResolveAllowedDirectories(string? user = null)
    {
        var dirs = AllowedDirectories.GetValueOrDefault(user ?? "default") ?? [];
        var ret = new List<string>();
        foreach (var dir in dirs)
        {
            if (dir.StartsWith('$'))
            {
                if (AliasedDirectories.TryGetValue(dir, out var aliased))
                    ret.Add(Path.GetFullPath(aliased));
                else
                    Log.LogInformation("Alias '{Dir}' not found", dir);
            }
            else
            {
                ret.Add(Path.GetFullPath(dir));
            }
        }
        return ret;
    }

    /// <summary>Model catalog across live providers (port of get_active_models)</summary>
    public JsonArray GetActiveModels()
    {
        var ret = new List<JsonObject>();
        var existingNames = new HashSet<string>();
        foreach (var entry in Providers)
        {
            foreach (var model in entry.Value.Models.Values)
            {
                var name = model.GetString("name");
                if (name == null)
                {
                    Log.LogInformation("Provider {Provider} model has no name", entry.Key);
                    continue;
                }
                if (!existingNames.Add(name))
                    continue;
                var item = model.Clone();
                item["provider"] = entry.Key;
                ret.Add(item);
            }
        }
        var arr = new JsonArray();
        foreach (var item in ret.OrderBy(x => x.GetString("id"), StringComparer.Ordinal))
        {
            arr.Add(item);
        }
        return arr;
    }

    /// <summary>Optional URL validation before downloading files referenced in chat messages</summary>
    public Action<string>? ValidateDownloadUrl { get; set; }

    /// <summary>Set by the app extension to detect thread cancellation in the DB (Python should_cancel_thread)</summary>
    public Func<ChatContext, bool>? CancelThreadCheck { get; set; }

    public bool ShouldCancelThread(ChatContext context)
    {
        if (context.Items.GetValueOrDefault("cancelled") is true || context.Items.GetValueOrDefault("completed") is true)
            return true;
        if (CancelThreadCheck?.Invoke(context) == true)
        {
            context.Items["cancelled"] = true;
            return true;
        }
        return false;
    }
}
    
public class ChatExtension
{
    public const string SystemPrompts = "system_prompts";
    public const string App = "app";
    public const string Agents = "agents";
    public const string Projects = "projects";
    public const string Tools = "tools";
    public const string CoreTools = "core_tools";
    public const string Computer = "computer";
    public const string Gallery = "gallery";
    public const string Skills = "skills";
    public const string Voice = "voice";
    public const string Publish = "publish";
    public const string Gemini = "gemini";
    public const string Katex = "katex";
    public const string Analytics = "analytics";
    public const string Identity = "identity";
    public const string Credentials = "credentials";
}

public class ChatApiKey
{
    public const string Xai = "XAI_API_KEY";
    public const string Zai = "ZAI_API_KEY";
    public const string Gemini = "GEMINI_API_KEY";
    public const string OpenAI = "OPENAI_API_KEY";
    public const string Groq = "GROQ_API_KEY";
    public const string Chutes = "CHUTES_API_KEY";
    public const string OllamaCloud = "OLLAMA_API_KEY";
    public const string Mistral = "MISTRAL_API_KEY";
    public const string Cerebras = "CEREBRAS_API_KEY";
    public const string Codestral = "CODESTRAL_API_KEY";
    public const string ZaiCodingPlan = "ZHIPU_API_KEY";
    public const string DeepSeek = "DEEPSEEK_API_KEY";
    public const string Fireworks = "FIREWORKS_API_KEY";
    public const string MiniMax = "MINIMAX_API_KEY";
    public const string OpenRouter = "OPENROUTER_API_KEY";
    public const string Nvidia = "NVIDIA_API_KEY";
    public const string Anthropic = "ANTHROPIC_API_KEY";
    public const string Moonshot = "MOONSHOT_API_KEY";
}
