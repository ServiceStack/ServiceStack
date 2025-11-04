using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.AI;

public class ChatFeature : IPlugin, Model.IHasStringId, IConfigureServices, IPreInitPlugin, IRequireLoadAsync
{
    public string Id => Plugins.AiChat;
    public Dictionary<string, object>? Config { get; set; }
    public Dictionary<string, object>? UiConfig { get; set; }
    
    public Func<IRequest, Task<IHttpResult?>>? ValidateRequest;

    public string ConfigJson
    {
        set => Config = (Dictionary<string, object>)JSON.parse(value);
    }

    public string UiConfigJson
    {
        set => UiConfig = (Dictionary<string, object>)JSON.parse(value);
    }

    public List<string> EnableProviders { get; set; } = [];

    public Dictionary<string, string> Variables { get; set; } = [];

    public Dictionary<string, OpenAiProviderBase> Providers { get; set; } = [];
    public IServiceProvider Services { get; set; }
    public IHttpClientFactory HttpClientFactory { get; set; }
    
    /// <summary>
    /// Whether to automatically resolve files in Chat messages using this VirtualFiles provider
    /// </summary>
    public IVirtualFiles? VirtualFiles { get; set; }
    
    public IChatStore? ChatStore { get; set; }
    
    /// <summary>
    /// How to download embedded URLs
    /// </summary>
    public Func<OpenAiProviderBase, string, Task<(string base64, string mimeType)>>? DownloadUrlAsBase64Async { get; set; }
    
    public Action<string>? ValidateUrl { get; set; }

    public ChatFeature()
    {
        ChatCompletionAsync = DefaultChatCompletionAsync;
        DownloadUrlAsBase64Async = DefaultDownloadUrlAsBase64Async;
    }

    public List<string> GetAllProviderKeys()
    {
        var all = (Config.GetValueOrDefault("providers") as Dictionary<string, object>)?
            .Keys.ToList() ?? [];
        return all;
    }
    
    public OpenAiProviderBase? GetProvider(string providerId) => 
        Providers.GetValueOrDefault(providerId);
    public T GetRequiredProvider<T>(string providerId) where T : OpenAiProviderBase
    {
        var provider = Providers.GetValueOrDefault(providerId);
        if (provider == null)
            throw new ArgumentException($"Chat Provider '{providerId}' is not available");
        return (T)provider;
    }
    public OpenAiProvider GetOpenAiProvider(string providerId) => GetRequiredProvider<OpenAiProvider>(providerId);
    public OllamaProvider GetOllamaProvider(string providerId) => GetRequiredProvider<OllamaProvider>(providerId);
    public GoogleProvider GetGoogleProvider(string providerId) => GetRequiredProvider<GoogleProvider>(providerId);

    public void Configure(IServiceCollection services)
    {
        services.RegisterService<ChatServices>();
        services.TryAddSingleton<IChatClients>(c => 
            new ChatClients(c.GetRequiredService<ILogger<ChatClients>>(), this));
        services.TryAddSingleton<IChatClient>(c => 
            c.GetRequiredService<IChatClients>());
    }
    
    public ILogger<ChatFeature> Log { get; set; }

    public void Register(IAppHost appHost)
    {
        Services ??= appHost.GetApplicationServices() ?? throw new ArgumentNullException(nameof(Services));
        HttpClientFactory ??= Services.GetRequiredService<IHttpClientFactory>();
        Log ??= Services.GetRequiredService<ILogger<ChatFeature>>();
        ChatStore ??= Services.GetService<IChatStore>();

        if (Services.GetService<IApiKeySource>() == null || Services.GetService<IApiKeyResolver>() == null)
            throw new Exception("API Keys are required to use ChatFeature");

        if (Providers.Count == 0)
        {
            if (Config == null || UiConfig == null)
            {
                if (Config == null)
                {
                    var configJson = appHost.VirtualFileSources.GetFile("chat/llms.json")?.ReadAllText();
                    if (configJson != null)
                    {
                        ConfigJson = configJson; 
                    }
                }
                if (UiConfig == null)
                {
                    var uiConfigJson = appHost.VirtualFileSources.GetFile("chat/ui.json")?.ReadAllText();
                    if (uiConfigJson != null)
                    {
                        UiConfigJson = uiConfigJson; 
                    }
                }
            }

            if (Config == null)
                throw new ArgumentNullException(nameof(Config));
            if (UiConfig == null)
                throw new ArgumentNullException(nameof(UiConfig));

            CreateProviders(Services);
        }
        
        appHost.ScriptContext.Args[nameof(Chat)] = new Chat(this);

        ChatStore?.InitSchema();
    }
    
    public class Chat(ChatFeature feature)
    {
        public List<string> Models => feature.Providers.Values
            .SelectMany(x => x.Models.Keys)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }

    public void CreateProviders(IServiceProvider services)
    {
        Providers = [];
        Dictionary<string, object>? headers = null;
        if (Config.TryGetObject("defaults", out var defaults))
        {
            defaults.TryGetObject("headers", out headers);
        }
            
        if (Config.TryGetObject("providers", out var providers))
        {
            foreach (var entry in providers)
            {
                if (entry.Value is not Dictionary<string, object?> provider) continue;
                if (!provider.TryGetValue("type", out string type)) continue;

                if (EnableProviders.Count > 0)
                {
                    provider["enabled"] = EnableProviders.Contains(entry.Key);
                }
                    
                var enabled = (bool) provider.GetValueOrDefault("enabled", false)!;
                if (!enabled) continue;
                    
                var definition = new Dictionary<string, object?>(provider);
                if (definition.TryGetValue("api_key", out string apiKey))
                {
                    if (apiKey.StartsWith('$'))
                    {
                        var varName = apiKey[1..];
                        definition["api_key"] = Variables.TryGetValue(varName, out var apiKeyValue) 
                            ? apiKeyValue 
                            : Environment.GetEnvironmentVariable(varName);
                    }
                }
                    
                if (!definition.ContainsKey("headers") && headers != null)
                {
                    definition["headers"] = headers;
                }
                    
                var p = type switch
                {
                    nameof(OpenAiProvider) => OpenAiProvider.Create(
                        services.GetRequiredService<ILogger<OpenAiProvider>>(), HttpClientFactory, definition),
                    nameof(GoogleProvider) => GoogleProvider.Create(
                        services.GetRequiredService<ILogger<GoogleProvider>>(), HttpClientFactory, definition),
                    nameof(OllamaProvider) => OllamaProvider.Create(
                        services.GetRequiredService<ILogger<OllamaProvider>>(), HttpClientFactory, definition),
                    _ => throw new ArgumentException($"Unknown provider type: {type}")
                };
                if (p != null)
                {
                    p.VirtualFiles = VirtualFiles;
                    p.DownloadUrlAsBase64Async = DownloadUrlAsBase64Async;
                    Providers[entry.Key] = p;
                    Log.LogDebug("Registered {Type} provider {Name}", type, entry.Key);
                }
            }
                
            // Update which providers are enabled
            EnableProviders = Providers.Keys.ToList();
        }
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        appHost.Config.EmbeddedResourceBaseTypes.Add(GetType());

        // appHost.ConfigurePlugin<UiFeature>(feature =>
        // {
        //     feature.Info.AdminLinks.Add(new LinkInfo
        //     {
        //         Id = Id, 
        //         Label = "AI Chat", 
        //         Icon = Svg.ImageSvg(SvgIcons.Chat),
        //         Href = "/aichat",
        //     });
        // });
    }

    public async Task LoadAsync(CancellationToken token = default)
    {
        if (Providers.Count == 0) 
            return;
        
        foreach (var entry in Providers)
        {
            await entry.Value.LoadAsync(token).ConfigAwait();
        }
    }

    public async Task<string?> EnableProviderAsync(string requestId)
    {
        EnableProviders.AddIfNotExists(requestId);
        var provider = GetProviderDefinition(requestId);

        string? feedback = null;
        // If the provider definition has an API Key, check for non-empty value
        if (provider.TryGetValue("api_key" , out var oApiKey)
            && oApiKey is string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                feedback = $"WARNING: {provider} is not configured with an API Key";
            }
            else if (apiKey.StartsWith('$'))
            {
                // Check if exists in Environment Variable or Variables
                var varName = apiKey[1..];
                var apiKeyValue = Variables.TryGetValue(varName, out var val) 
                    ? val
                    : Environment.GetEnvironmentVariable(varName);
                
                if (string.IsNullOrEmpty(apiKeyValue))
                {
                    feedback = $"WARNING: {provider} requires missing API Key in Environment Variable {apiKey}";
                }
            }
        }
        
        CreateProviders(Services);
        await LoadAsync();
        return feedback;
    }

    private Dictionary<string, object> GetProviderDefinition(string requestId)
    {
        var definition = Config?["providers"] as Dictionary<string, object>
            ?? throw new ArgumentNullException(nameof(Config));

        var provider = definition.GetValueOrDefault(requestId) as Dictionary<string, object>
            ?? throw new ArgumentException($"Definition for Provider '{requestId}' not found");
        return provider;
    }

    public async Task DisableProviderAsync(string requestId)
    {
        EnableProviders.Remove(requestId);
        CreateProviders(Services);
        await LoadAsync();
    }

    public async Task<(string base64, string mimeType)> DefaultDownloadUrlAsBase64Async(OpenAiProviderBase provider, string url)
    {
        ValidateUrl?.Invoke(url);
        Log.LogDebug("Downloading: {Url}", url);
        var httpReq = new HttpRequestMessage(HttpMethod.Get, url);
        using var httpClient = provider.Factory.CreateClient();
        var httpRes = await httpClient.SendAsync(httpReq).ConfigAwait();
        httpRes.EnsureSuccessStatusCode();

        var mimeType = httpRes.Content.Headers.ContentType?.MediaType
            ?? provider.GetMimeType(url);
                        
        var bytes = await httpRes.Content.ReadAsByteArrayAsync().ConfigAwait();
        var base64 = Convert.ToBase64String(bytes);
        return (base64, mimeType);
    }

    public Func<ChatCompletion, IRequest, Task<ChatResponse>> ChatCompletionAsync { get; set; }
    public Func<ChatCompletion, ChatResponse, IRequest, Task>? OnChatCompletionSuccessAsync { get; set; }
    public Func<ChatCompletion, Exception, IRequest, Task>? OnChatCompletionFailedAsync { get; set; }

    public async Task<ChatResponse> DefaultChatCompletionAsync(ChatCompletion request, IRequest req)
    {
        var candidateProviders = GetModelProviders(request);

        var oLong = req.GetItem(Keywords.RequestDuration);
        if (oLong == null)
        {
            req.SetItem(Keywords.RequestDuration, System.Diagnostics.Stopwatch.GetTimestamp());
        }

        Exception? firstEx = null;
        var i = 0;
        var chatRequest = request;
        foreach (var entry in candidateProviders)
        {
            i++;
            try
            {
                var provider = entry.Value;
                chatRequest.Model = request.Model;
                var ret = await provider.ChatAsync(chatRequest).ConfigAwait();
                if (ChatStore != null)
                    await ChatStore.ChatCompletedAsync(chatRequest, ret, req).ConfigAwait();
                var onSuccess = OnChatCompletionSuccessAsync;
                if (onSuccess != null)
                    await onSuccess(chatRequest, ret, req).ConfigAwait();
                return ret;
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Error calling {Name} ({CandidateIndex}/{CandidatesTotal}): {Message}", 
                    i, candidateProviders.Count, entry.Key, ex.Message);
                firstEx ??= ex;
            }
        }

        firstEx ??= HttpError.NotFound($"Model {request.Model} not found");
        if (ChatStore != null)
            await ChatStore.ChatFailedAsync(chatRequest, firstEx, req).ConfigAwait();
        var onFailed = OnChatCompletionFailedAsync; 
        if (onFailed != null)
            await onFailed(request, firstEx, req).ConfigAwait();
        throw firstEx;
    }

    public List<KeyValuePair<string, OpenAiProviderBase>> GetModelProviders(ChatCompletion request)
    {
        var candidateProviders = Providers
            .Where(x => x.Value.Models.ContainsKey(request.Model))
            .ToList();
        if (candidateProviders.Count == 0)
            throw HttpError.NotFound($"Model {request.Model} not found");
        return candidateProviders;
    }
}
