using System.Net;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
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

    public ChatFeature()
    {
        ChatCompletionAsync = DefaultChatCompletionAsync;
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
        services.AddSingleton<IChatClients>(new ChatClients(this));
    }
    
    public ILogger<ChatFeature> Log { get; set; }

    public void Register(IAppHost appHost)
    {
        Services ??= appHost.GetApplicationServices() ?? throw new ArgumentNullException(nameof(Services));
        HttpClientFactory ??= Services.GetRequiredService<IHttpClientFactory>();
        Log ??= Services.GetRequiredService<ILogger<ChatFeature>>();

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
        if (Config.GetValueOrDefault("defaults") is Dictionary<string, object> defaults)
        {
            if (defaults.TryGetValue("headers", out var oHeaders)
                && oHeaders is Dictionary<string, object> headersDict)
            {
                headers = headersDict;
            }
        }
            
        if (Config!.TryGetValue("providers", out var oProviders)
            && oProviders is Dictionary<string, object> providers)
        {
            foreach (var entry in providers)
            {
                if (entry.Value is not Dictionary<string, object?> provider) continue;
                if (!provider.TryGetValue("type", out var oType)
                    || oType is not string type) continue;

                if (EnableProviders.Count > 0)
                {
                    provider["enabled"] = EnableProviders.Contains(entry.Key);
                }
                    
                var enabled = (bool) provider.GetValueOrDefault("enabled", false)!;
                if (!enabled) continue;
                    
                var definition = new Dictionary<string, object?>(provider);
                if (definition.TryGetValue("api_key", out var oApiKey) && oApiKey is string apiKey)
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

    public Func<CreateChatCompletion, IRequest, Task<ChatResponse>> ChatCompletionAsync { get; set; }
    
    public async Task<ChatResponse> DefaultChatCompletionAsync(CreateChatCompletion request, IRequest req)
    {
        var candidateProviders = Providers
            .Where(x => x.Value.Models.ContainsKey(request.Model))
            .ToList();
        if (candidateProviders.Count == 0)
            throw HttpError.NotFound($"Model {request.Model} not found");

        Exception? firstEx = null;
        var i = 0;
        var chatRequest = request.ToChatCompletion();
        foreach (var entry in candidateProviders)
        {
            i++;
            try
            {
                var provider = entry.Value;
                chatRequest.Model = request.Model;
                var ret = await provider.ChatAsync(chatRequest).ConfigAwait();
                return ret;
            }
            catch (Exception ex)
            {
                Log.LogError(ex, "Error calling {Name} ({CandidateIndex}/{CandidatesTotal}): {Message}", 
                    i, candidateProviders.Count, entry.Key, ex.Message);
                firstEx ??= ex;
            }
        }
        if (firstEx != null)
            throw firstEx;
        
        throw HttpError.NotFound($"Model {request.Model} not found");
    }
}

[ExcludeMetadata, Route("/chat/models")]
public class ChatModels : IGet, IReturn<string[]>
{
}

[ExcludeMetadata, Route("/chat/config")]
public class ChatConfig : IGet, IReturn<string>
{
}

[ExcludeMetadata, Route("/chat/status")]
public class ChatStatus : IGet, IReturn<ChatStatusResponse>
{
}
public class ChatStatusResponse
{
    public List<string> All { get; set; } = [];
    public List<string> Enabled { get; set; } = [];
    public List<string> Disabled { get; set; } = [];
    public ResponseStatus? ResponseStatus { get; set; }
}

[ValidateApiKey]
[ExcludeMetadata, Route("/chat/providers/{Id}")]
public class UpdateChatProvider : IPost, IReturn<UpdateChatProviderResponse>
{
    [ValidateNotEmpty]
    public string Id { get; set; }
    public bool? Enable { get; set; }
    public bool? Disable { get; set; }
}
public class UpdateChatProviderResponse
{
    public List<string> Enabled { get; set; } = [];
    public List<string> Disabled { get; set; } = [];
    public string? Feedback { get; set; }
    public ResponseStatus? ResponseStatus { get; set; }
}

[ValidateApiKey]
[ExcludeMetadata, Route("/chat/auth")]
public class ChatAuth : IGet, IReturn<AuthenticateResponse>
{
}

[ExcludeMetadata, Route("/chat/{**Path}")]
public class ChatStaticFile : IGet, IReturn<byte[]>
{
    public string? Path { get; set; }
}

public record StatusResult(List<string> All, List<string> Enabled, List<string> Disabled);

public class ChatServices(ILogger<ChatServices> log) : Service
{
    private async Task<(ChatFeature, IHttpResult?)> AssertAdminAccessAsync()
    {
        var (feature, error) = await AssertAccessAsync();
        if (error != null)
            return (feature, error);

        var apiKey = Request.GetApiKey();
        if (!apiKey.HasScope(RoleNames.Admin))
        {
            var user = await GetClaimsPrincipal(apiKey).ConfigAwait();
            if (!user.HasRole(RoleNames.Admin))
                return (feature, new HttpError(HttpStatusCode.Forbidden, "Requires Admin Role or Scope"));
        }
        return (feature, null);
    }
    private async Task<(ChatFeature, IHttpResult?)> AssertAccessAsync()
    {
        var feature = AssertPlugin<ChatFeature>();
        if (feature.ValidateRequest != null)
        {
            var result = await feature.ValidateRequest(Request!).ConfigAwait();
            return (feature, result);
        }
        return (feature, null);
    }

    public async Task<object> Get(ChatAuth request)
    {
        var (feature, error) = await AssertAccessAsync();
        if (error != null) return error;

        var apiKey = Request.GetApiKey()
            ?? throw HttpError.Unauthorized("API Key Required");
        var visibleKey = apiKey.Key.Length > 6 
            ? apiKey.Key[..3] + "***" + apiKey.Key[^3..]
            : null;

        var user = await GetClaimsPrincipal(apiKey).ConfigAwait();
        var roles = user.GetRoles().ToList();

        if (apiKey.HasScope(RoleNames.Admin))
            roles.AddIfNotExists(RoleNames.Admin);
        
        var profileUrl = user.GetPicture();

        return new AuthenticateResponse
        {
            UserId = apiKey.UserAuthId,
            UserName = user.GetUserName(),
            DisplayName = user.GetDisplayName(),
            ProfileUrl = profileUrl,
            Roles = roles,
            BearerToken = visibleKey,
        };
    }

    private async Task<ClaimsPrincipal?> GetClaimsPrincipal(IApiKey apiKey)
    {
        var user = Request.GetClaimsPrincipal();
        if (!user.IsAuthenticated())
        {
            var userResolver = Request!.GetService<IUserResolver>();
            if (userResolver != null && apiKey.UserAuthId != null)
            {
                user = await userResolver.CreateClaimsPrincipalAsync(Request!, apiKey.UserAuthId!).ConfigAwait();
            }
        }

        return user;
    }

    public StatusResult GetStatus(ChatFeature feature)
    {
        var all = feature.GetAllProviderKeys();
        var disabled = all.Except(feature.Providers.Keys).ToList();
        var enabled = feature.Providers.Keys.ToList();
        return new StatusResult(all, enabled, disabled);
    }

    public async Task<object> Get(ChatModels request)
    {
        var (feature, error) = await AssertAccessAsync();
        if (error != null) return error;
        var models = new HashSet<string>();
        foreach (var entry in feature.Providers)
        {
            models.AddDistinctRange(entry.Value.Models.Keys);
        }
        return models;
    }

    public async Task<object> Get(ChatConfig request)
    {
        var (feature, error) = await AssertAccessAsync();
        if (error != null) return error;
        var (all, enabled, disabled) = GetStatus(feature);
        var uiConfig = new Dictionary<string, object>(feature.UiConfig!)
        {
            ["defaults"] = feature.Config!["defaults"],
            ["status"] = new Dictionary<string, object> {
                ["all"] = all,
                ["enabled"] = enabled,
                ["disabled"] = disabled,
            },
        };
        return uiConfig;
    }

    public async Task<object> Get(ChatStatus request)
    {
        var (feature, error) = await AssertAccessAsync();
        if (error != null) return error;
        var (all, enabled, disabled) = GetStatus(feature);
        return new ChatStatusResponse
        {
            All = all, 
            Enabled = enabled, 
            Disabled = disabled,
        };
    }

    public async Task<object> Post(UpdateChatProvider request)
    {
        var (feature, error) = await AssertAdminAccessAsync();
        if (error != null) return error;

        string? feedback = null;
        if (request.Enable == true)
        {
            log.LogDebug("Enable Provider {Id}", request.Id);
            feedback = await feature.EnableProviderAsync(request.Id);
        }
        else if (request.Disable == true)
        {
            log.LogDebug("Disable Provider {Id}", request.Id);
            await feature.DisableProviderAsync(request.Id);
        }
        
        var (_, enabled, disabled) = GetStatus(feature);
        return new UpdateChatProviderResponse
        {
            Enabled = enabled, 
            Disabled = disabled,
            Feedback = feedback,
        };
    }
    
    public async Task<object> Post(CreateChatCompletion request)
    {
        var (feature, error) = await AssertAccessAsync();
        if (error != null) return error;
        var response = await feature.ChatCompletionAsync(request, Request!).ConfigAwait();

        // Remove IncludeNullValues jsconfig is specified to return cleaner output
        return new HttpResult(response) {
            ResultScope = () => {
                var jsScope = JsConfig.CreateScope(HostContext.Config.AllowJsConfig 
                    ? Request!.QueryString[Keywords.JsConfig] 
                    : null) ?? JsConfig.With(new(){});
                jsScope.IncludeNullValues = false;
                return jsScope;
            }
        };
    }
    
    public async Task<object> Get(ChatStaticFile request)
    {
        var path = request.Path.UrlDecode() ?? "";
        if (path.Length == 0 || path.EndsWith('/'))
            path = path.CombineWith("index.html");
        
        var checkPath = "chat".CombineWith(path);
        var file = VirtualFileSources.GetFile(checkPath);
        if (file == null)
        {
            if (path.IndexOf('.') >= 0)
                throw HttpError.NotFound($"File {checkPath} not found");
            file = VirtualFileSources.GetFile("chat/index.html");
        }

        if (file.Name.EndsWith(".html"))
        {
            var (feature, error) = await AssertAccessAsync();
            if (error != null) return error;
        }
        
        return new HttpResult(file);
    }
}
