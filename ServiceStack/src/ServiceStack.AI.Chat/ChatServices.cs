using System.Net;
using System.Runtime.Serialization;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.AI;

[ExcludeMetadata, Route("/chat/models")]
public class ChatModels : IGet, IReturn<ModelInfo[]>
{
}

[DataContract]
public class ModelInfo
{
    [DataMember]
    public string Id { get; set; }
    [DataMember]
    public string Provider { get; set; }
    [DataMember(Name = "provider_model")]
    public string ProviderModel { get; set; }
    [DataMember]
    public ModelPrice? Pricing { get; set; }
}
[DataContract]
public class ModelPrice
{
    [DataMember]
    public string? Input { get; set; }
    [DataMember]
    public string? Output { get; set; }
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
        var existingModels = new HashSet<string>();
        var ret = new List<ModelInfo>();
        foreach (var entry in feature.Providers)
        {
            var providerId = entry.Key;
            var provider = entry.Value;
            foreach (var model in provider.Models)
            {
                if (!existingModels.Add(model.Key))
                    continue;
                existingModels.Add(model.Key);
                var pricing = provider.Pricing?.GetValueOrDefault(model.Value) 
                    ?? provider.DefaultPricing;
                ret.Add(new ModelInfo
                {
                    Id = model.Key,
                    Provider = providerId,
                    ProviderModel = model.Value,
                    Pricing = pricing, 
                });
            }
        }
        return ret;
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
            ["requiresAuth"] = "true",
            ["authType"] = "apikey",
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
    
    public async Task<object> Post(ChatCompletion request)
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