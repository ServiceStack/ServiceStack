#nullable enable
#if NET8_0_OR_GREATER

using System;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

public class ApiKeysFeature : IPlugin, IConfigureServices, IRequiresSchema, Model.IHasStringId, IPreInitPlugin
{
    public string Id { get; set; } = Plugins.ApiKeys;
    public string AdminRole { get; set; } = RoleNames.Admin;

    public string? ApiKeyPrefix = "ak-";
    public string? HttpHeader = HttpHeaders.XApiKey;
    public TimeSpan? CacheDuration = TimeSpan.FromMinutes(10);
    public Func<string>? ApiKeyGenerator { get; set; }
    public TimeSpan? DefaultExpiry { get; set; }
    public ConcurrentDictionary<int, DateTime> LastUsedApiKeys { get; set; } = new();
    public ConcurrentDictionary<string, IApiKey> ValidApiKeys { get; } = new();
    public Func<IDbConnection>? UseDb { get; set; }
    public string? NamedConnection { get; set; }

    public IDbConnection OpenDb()
    {
        return UseDb != null 
            ? UseDb() 
            : NamedConnection != null
                ? HostContext.AppHost.GetDbConnection(NamedConnection, null, db => db.WithTag(GetType().Name))
                : HostContext.AppHost.GetDbConnection(null, db => db.WithTag(GetType().Name));
    }
    
    public List<Type> RegisterServices { get; set; } = [
        typeof(AdminApiKeysService),
        typeof(UserApiKeysService),
    ];

    /// <summary>
    /// Available Scopes Admin Users can assign to API Keys
    /// </summary>
    public List<string> Scopes { get; set; } =
    [
        RoleNames.Admin
    ];
    
    /// <summary>
    /// Available Features Admin Users can assign to API Keys
    /// </summary>
    public List<string> Features { get; set; } = [];

    /// <summary>
    /// Hide 'RestrictTo' field from User API Key UI
    /// </summary>
    public List<string> Hide { get; set; } = [];

    /// <summary>
    /// Available Scopes Users can assign to their own API Keys
    /// </summary>
    public List<string> UserScopes { get; set; } = [];

    /// <summary>
    /// Available Features Users can assign to their own API Keys
    /// </summary>
    public List<string> UserFeatures { get; set; } = [];

    /// <summary>
    /// Hide 'RestrictTo' field from User API Key UI
    /// </summary>
    public List<string> UserHide { get; set; } = [];
    
    public List<KeyValuePair<string, string>> ExpiresIn { get; set; } = [
        new("", "Never"),
        new("1", "1 day"),
        new("7", "7 days"),
        new("30", "30 days"),
        new("90", "90 days"),
        new("180", "180 days"),
        new("365", "365 days"),
        new("730", "2 years"),
        new("1825", "5 years"),
        new("3650", "10 years")
    ];

    public string Label { get; set; }
    
    public class ApiKey : IApiKey
    {
        [AutoIncrement]
        public int Id { get; set; }
        
        /// <summary>
        /// The API Key
        /// </summary>
        [Index(Unique = true)]
        public string Key { get; set; }

        /// <summary>
        /// Name for the API Key
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// User Primary Key
        /// </summary>
        public string? UserId { get; set; }
        [Ignore]
        public string? UserAuthId => UserId;

        /// <summary>
        /// Name of the User or Worker using the API Key
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// What to show the User after they've created the API Key
        /// </summary>
        public string VisibleKey { get; set; }
    
        public DateTime CreatedDate { get; set; }
    
        public DateTime? ExpiryDate { get; set; }
    
        public DateTime? CancelledDate { get; set; }

        public DateTime? LastUsedDate { get; set; }

        public List<string> Scopes { get; set; } = [];

        public List<string> Features { get; set; } = [];

        /// <summary>
        /// Restricted to only access specific APIs
        /// </summary>
        public List<string> RestrictTo { get; set; } = [];

        public string? Environment { get; set; }

        public string? Notes { get; set; }

        //Custom Reference Data
        public int? RefId { get; set; }
        public string? RefIdStr { get; set; }
        
        public bool HasScope(string scope) => Scopes.Contains(scope);
        public bool HasFeature(string feature) => Features.Contains(feature);
        public bool CanAccess(Type requestType) => RestrictTo.IsEmpty() || RestrictTo.Contains(requestType.Name);

        public Dictionary<string, string>? Meta { get; set; }
    }

    private static ConcurrentDictionary<string, (IApiKey apiKey, DateTime dateTime)> Cache = new();

    public ApiKeysFeature()
    {
        Label = "API Key";
    }

    public string GenerateApiKey() => ApiKeyGenerator != null 
        ? ApiKeyGenerator()
        : (ApiKeyPrefix ?? "") + Guid.NewGuid().ToString("N");
    
    public void Register(IAppHost appHost)
    {
        appHost.GlobalRequestFiltersAsync.Insert(0, RequestFilterAsync);
        
        appHost.AddToAppMetadata(meta =>
        {
            meta.Plugins.ApiKey = new()
            {
                Label = Label.Localize(),
                HttpHeader = HttpHeader,
                Scopes = Scopes,
                Features = Features,
                ExpiresIn = ExpiresIn,
                Hide = Hide,
            };
        });
    }

    public ApiKeyInfo GetApiKeyInfo()
    {
        return new()
        {
            Label = Label.Localize(),
            HttpHeader = HttpHeader,
            Scopes = UserScopes,
            Features = UserFeatures,
            ExpiresIn = ExpiresIn,
            Hide = UserHide,
            RequestTypes = HostContext.Metadata.Operations
                .Where(x => x.RequiresApiKey)
                .Select(x => x.RequestType.Name)
                .OrderBy(x => x)
                .ToList(),
        };
    }

    public PartialApiKey ToPartialApiKey(ApiKey apiKey)
    {
        var to = apiKey.ConvertTo<PartialApiKey>();
        to.Scopes ??= [];
        to.Features ??= [];
        to.RestrictTo ??= [];
        to.Active = apiKey.CancelledDate == null && (apiKey.ExpiryDate == null || apiKey.ExpiryDate > DateTime.UtcNow);
        return to;
    }

    public async Task RequestFilterAsync(IRequest req, IResponse res, object requestDto)
    {
        var apiKeyToken = GetApiKeyToken(req); 
        if (apiKeyToken == null) return;
        
        var authSecret = HostContext.Config.AdminAuthSecret;
        if (authSecret != null && authSecret == apiKeyToken)
        {
            req.Items[Keywords.Session] = HostContext.Config.AuthSecretSession;
            return;
        }
        
        if (ApiKeyPrefix != null && !apiKeyToken.StartsWith(ApiKeyPrefix))
            return;
        if (CacheDuration != null && Cache.TryGetValue(apiKeyToken, out var entry))
        {
            if (entry.dateTime + CacheDuration > DateTime.UtcNow)
            {
                req.Items[Keywords.ApiKey] = entry.apiKey;
                if (entry.apiKey.HasScope(RoleNames.Admin))
                {
                    req.Items[Keywords.Session] = HostContext.Config.AuthSecretSession;
                }
                RecordUsage(entry.apiKey);
                return;
            }
            Cache.TryRemove(apiKeyToken, out _);
        }

        var source = req.Resolve<IApiKeySource>();
        var apiKey = await source.GetApiKeyAsync(apiKeyToken);
        if (apiKey != null)
        {
            req.Items[Keywords.ApiKey] = apiKey;
            if (apiKey.HasScope(RoleNames.Admin))
            {
                req.Items[Keywords.Session] = HostContext.Config.AuthSecretSession;
            }
            if (CacheDuration != null)
            {
                Cache[apiKeyToken] = (apiKey, DateTime.UtcNow);
            }
            RecordUsage(apiKey);
        }
    }

    public void RecordUsage(IApiKey apiKey)
    {
        if (apiKey is ApiKey x)
        {
            x.LastUsedDate = LastUsedApiKeys[x.Id] = DateTime.UtcNow;
        }
    }

    public string? GetApiKeyToken(IRequest req)
    {
        var to = (HttpHeader != null ? req.GetHeader(HttpHeader) : null) ?? req.GetBearerToken();
        if (string.IsNullOrEmpty(to))
        {
            var user = req.GetClaimsPrincipal();
            if (user.IsAuthenticated())
            {
                var apiKey = user.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.ApiKey)?.Value;
                if (!string.IsNullOrEmpty(apiKey))
                    return apiKey;
            }
            
            return null;
        }
        return to;
    }

    public void InitSchema()
    {
        using var db = OpenDb();
        InitSchema(db);
    }

    public void InitSchema(IDbConnection db)
    {
        db.CreateTableIfNotExists<ApiKey>();
    }
    
    public void DeleteSchema(IDbConnection db)
    {
        db.DropTable<ApiKey>();
    }

    public long ApiKeyCount(IDbConnection db) => db.Count<ApiKey>();
    public async Task<long> ApiKeyCountAsync(IDbConnection db) => await db.CountAsync<ApiKey>();

    public void InitKey(ApiKey to)
    {
        if (string.IsNullOrEmpty(to.Key))
            to.Key = GenerateApiKey();
        if (string.IsNullOrEmpty(to.VisibleKey))
            to.VisibleKey = to.Key[..(ApiKeyPrefix?.Length ?? 0)] + "***" + to.Key[^3..];
        if (string.IsNullOrEmpty(to.Name))
            to.Name = $"{DateTime.UtcNow.Year}-{DateTime.UtcNow.Month:00}";
        to.CreatedDate = DateTime.UtcNow;
        if (DefaultExpiry != null)
            to.ExpiryDate = DateTime.UtcNow.Add(DefaultExpiry.Value);
    }

    public ApiKey Insert(IDbConnection db, ApiKey apiKey)
    {
        InitKey(apiKey);
        apiKey.Id = (int)db.Insert(apiKey, selectIdentity:true);
        return apiKey;
    }

    public async Task<ApiKey> InsertAsync(IDbConnection db, ApiKey apiKey)
    {
        InitKey(apiKey);
        apiKey.Id = (int)await db.InsertAsync(apiKey, selectIdentity:true);
        return apiKey;
    }

    public void InsertAll(IDbConnection db, List<ApiKey> apiKeys)
    {
        apiKeys.ForEach(InitKey);
        db.InsertAll(apiKeys);
    }

    public async Task InsertAllAsync(IDbConnection db, List<ApiKey> apiKeys)
    {
        apiKeys.ForEach(InitKey);
        await db.InsertAllAsync(apiKeys);
    }
    
    public ApiKey? GetApiKey(IDbConnection db, string key) => db.Single<ApiKey>(x => x.Key == key);
    public async Task<ApiKey?> GetApiKeyAsync(IDbConnection db, string key) => await db.SingleAsync<ApiKey>(x => x.Key == key).ConfigAwait();

    public ApiKey? GetApiKeyById(IDbConnection db, int id) => db.SingleById<ApiKey>(id);
    public async Task<ApiKey?> GetApiKeyByIdAsync(IDbConnection db, int id) => await db.SingleByIdAsync<ApiKey>(id).ConfigAwait();

    public void Configure(IServiceCollection services)
    {
        services.TryAddSingleton<IApiKeySource>(c => new ApiKeysFeatureSource(this));
        services.TryAddSingleton<IApiKeyResolver>(_ => new ApiKeyResolver(this));
        
        foreach (var serviceType in RegisterServices)
        {
            services.RegisterService(serviceType);
        }
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        appHost.ConfigurePlugin<UiFeature>(feature => {
            feature.AddAdminLink(AdminUiFeature.Commands, new LinkInfo {
                Id = "apikeys",
                Label = "API Keys",
                Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Keys)),
                Show = $"role:{AdminRole}",
            });
        });
    }

    public void RemoveValidApiKeyById(int id)
    {
        var apiKey = ValidApiKeys.Values.Cast<ApiKey>().FirstOrDefault(x => x.Id == id);
        if (apiKey != null)
            ValidApiKeys.TryRemove(apiKey.Key, out _);
    }
}

public static class ApiKeysExtensions
{
    public static string? GetApiKeyToken(this IRequest? req) => req.GetApiKey()?.Key;

    public static string? GetApiKeyUser(this IRequest? req) =>
        req.GetApiKey() is ApiKeysFeature.ApiKey x 
            ? x.UserName ?? x.UserId
            : req.GetApiKey() is Auth.ApiKey y ? y.UserAuthId : null;
}


public class ApiKeyResolver(ApiKeysFeature feature) : IApiKeyResolver
{
    public string? GetApiKeyToken(IRequest req)
    {
        return feature.GetApiKeyToken(req);
    }
}
public class ApiKeysFeatureSource(ApiKeysFeature feature) : IApiKeySource
{
    public async Task<IApiKey?> GetApiKeyFromDbAsync(string key)
    {
        using var db = feature.OpenDb();
        var apiKey = await db.SingleAsync<ApiKeysFeature.ApiKey>(x => x.Key == key);
        if (apiKey == null) 
            return null;
        if (apiKey.CancelledDate != null)
            throw HttpError.Unauthorized(ErrorMessages.ApiKeyHasBeenCancelled.Localize());
        if (apiKey.ExpiryDate != null && apiKey.ExpiryDate < DateTime.UtcNow)
            throw HttpError.Unauthorized(ErrorMessages.ApiKeyHasExpired.Localize());
        return apiKey;
    }
    
    public async Task<IApiKey?> GetApiKeyAsync(string key)
    {
        if (!feature.ValidApiKeys.TryGetValue(key, out var apiKey))
        {
            apiKey = await GetApiKeyFromDbAsync(key);
            if (apiKey == null)
                return null;
            feature.ValidApiKeys[key] = apiKey;
        }
        feature.RecordUsage(apiKey);
        return apiKey;
    }

    public async Task<IApiKey?> GetApiKeyByIdAsync(int id)
    {
        using var db = feature.OpenDb();
        var apiKey = await db.SingleAsync<ApiKeysFeature.ApiKey>(x => x.Id == id);
        return apiKey;
    }

    public async Task<List<IApiKey>> GetApiKeysByUserIdAsync(string? userId)
    {
        using var db = feature.OpenDb();
        var apiKeys = await db.SelectAsync<ApiKeysFeature.ApiKey>(x => x.UserId == userId);
        var to = new List<IApiKey>();
        foreach (var apiKey in apiKeys)
        {
            if (apiKey.CancelledDate == null 
                && (apiKey.ExpiryDate == null || apiKey.ExpiryDate > DateTime.UtcNow))
            {
                to.Add(apiKey);
            }
        }
        return to;
    }
}

public class AdminApiKeysService : Service
{
    private async Task<ApiKeysFeature> AssertRequiredRole()
    {
        var feature = AssertPlugin<ApiKeysFeature>();
        await RequiredRoleAttribute.AssertRequiredRoleAsync(Request, feature.AdminRole);
        return feature;
    }
    
    public async Task<object> Get(AdminQueryApiKeys request)
    {
        var feature = await AssertRequiredRole().ConfigAwait();

        using var db = feature.OpenDb();
        var q = db.From<ApiKeysFeature.ApiKey>();
        if (request.Id != null)
            q.Where(x => x.Id == request.Id);
        if (!string.IsNullOrEmpty(request.ApiKey))
        {
            q.Where(x => x.Key == request.ApiKey);
        }
        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            q.Where(x => x.Name.ToLower().Contains(search) || x.Notes!.ToLower().Contains(search) || 
                    x.UserName!.ToLower().Contains(search) || x.UserId!.ToLower().Contains(search));
        }
        if (request.UserId != null)
            q.Where(x => x.UserId == request.UserId);
        if (request.UserName != null)
            q.Where(x => x.UserName == request.UserName);
        if (request.OrderBy != null)
            q.OrderByFields(request.OrderBy);
        if (request.Skip != null)
            q.Skip(request.Skip.Value);
        if (request.Take != null)
            q.Take(request.Take.Value);
        
        var results = await db.SelectAsync(q);
        var partialResults = results.ConvertAll(feature.ToPartialApiKey);
        foreach (var result in partialResults)
        {
            if (feature.LastUsedApiKeys.TryGetValue(result.Id, out var lastUsed))
                result.LastUsedDate = lastUsed;
        }
        return new AdminApiKeysResponse
        {
            Results = partialResults
        };
    }

    public async Task<object> Any(AdminCreateApiKey request)
    {
        var feature = await AssertRequiredRole().ConfigAwait();
        using var db = feature.OpenDb();

        var apiKey = request.ConvertTo<ApiKeysFeature.ApiKey>();
        await feature.InsertAllAsync(db, [apiKey]);
        
        return new AdminApiKeyResponse
        {
            Result = apiKey.Key
        };
    }

    public async Task<object> Any(AdminUpdateApiKey request)
    {
        var feature = await AssertRequiredRole().ConfigAwait();
        using var db = feature.OpenDb();

        var dict = request.ToObjectDictionary();
        var updateModel = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var reset = (request.Reset ?? []).ToSet(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in dict)
        {
            if (entry.Key == nameof(request.Reset)) continue;
            if (reset.Contains(entry.Key))
            {
                updateModel[entry.Key] = null;
            }
            else if (entry.Value != null)
            {
                if (entry.Value is List<string> { Count: 0 }) continue;
                updateModel[entry.Key] = entry.Value;
            }
        }

        if (updateModel.Count > 0)
        {
            await db.UpdateAsync<ApiKeysFeature.ApiKey>(updateModel, x => x.Id == request.Id);
            if (updateModel.ContainsKey(nameof(ApiKeysFeature.ApiKey.CancelledDate)))
            {
                feature.RemoveValidApiKeyById(request.Id);
            }
        }
        
        return new EmptyResponse();
    }

    public async Task<object> Any(AdminDeleteApiKey request)
    {
        var feature = await AssertRequiredRole().ConfigAwait();
        using var db = feature.OpenDb();

        await db.DeleteByIdAsync<ApiKeysFeature.ApiKey>(request.Id);
        feature.RemoveValidApiKeyById(request.Id.GetValueOrDefault());
        return new EmptyResponse();
    }
}


public class UserApiKeysService : Service
{
    private (string userId, string? userName) GetUserIdAndUserName()
    {
        var claimsPrincipal = Request.GetClaimsPrincipal();
        var userId = claimsPrincipal.GetUserId()
            ?? throw new ArgumentNullException(nameof(IdentityUser.Id));
        var userName = claimsPrincipal.GetUserName();
        return (userId, userName);
    }

    public async Task<object> Get(QueryUserApiKeys request)
    {
        var (userId, _) = GetUserIdAndUserName();
        var feature = AssertPlugin<ApiKeysFeature>();
        using var db = feature.OpenDb();

        var q = db.From<ApiKeysFeature.ApiKey>()
            .Where(x => x.UserId == userId);
        if (request.Id != null)
            q.Where(x => x.Id == request.Id);
        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            q.Where(x => x.Name.ToLower().Contains(search) || x.Notes!.ToLower().Contains(search));
        }
        if (request.OrderBy != null)
            q.OrderByFields(request.OrderBy);
        if (request.Skip != null)
            q.Skip(request.Skip.Value);
        if (request.Take != null)
            q.Take(request.Take.Value);
        
        var results = await db.SelectAsync(q);
        var partialResults = results.ConvertAll(feature.ToPartialApiKey);
        foreach (var result in partialResults)
        {
            if (feature.LastUsedApiKeys.TryGetValue(result.Id, out var lastUsed))
                result.LastUsedDate = lastUsed;
        }
        return new UserApiKeysResponse
        {
            Results = partialResults
        };
    }

    public async Task<object> Any(CreateUserApiKey request)
    {
        var feature = AssertPlugin<ApiKeysFeature>();
        using var db = feature.OpenDb();

        var (userId, userName) = GetUserIdAndUserName();
        var apiKey = request.ConvertTo<ApiKeysFeature.ApiKey>();
        apiKey.UserId = userId;
        apiKey.UserName = userName;
        if (request.Scopes is { Count: > 0 })
        {
            apiKey.Scopes = request.Scopes.Where(x => feature.UserScopes.Contains(x)).ToList();
        }
        if (request.Features is { Count: > 0 })
        {
            apiKey.Features = request.Features.Where(x => feature.UserFeatures.Contains(x)).ToList();
        }
        
        await feature.InsertAllAsync(db, [apiKey]);
        return new UserApiKeyResponse
        {
            Result = apiKey.Key
        };
    }

    public async Task<object> Any(UpdateUserApiKey request)
    {
        var (userId, _) = GetUserIdAndUserName();
        var feature = HostContext.AssertPlugin<ApiKeysFeature>();
        using var db = feature.OpenDb();

        var dict = request.ToObjectDictionary();
        var updateModel = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var reset = (request.Reset ?? []).ToSet(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in dict)
        {
            if (entry.Key == nameof(request.Reset)) continue;
            if (reset.Contains(entry.Key))
            {
                updateModel[entry.Key] = null;
            }
            else if (entry.Value != null)
            {
                if (entry.Value is List<string> { Count: 0 }) continue;
                updateModel[entry.Key] = entry.Value;
            }
        }
        if (request.Scopes is { Count: > 0 })
        {
            updateModel[nameof(request.Scopes)] = request.Scopes.Where(x => feature.UserScopes.Contains(x)).ToList();
        }
        if (request.Features is { Count: > 0 })
        {
            updateModel[nameof(request.Features)] = request.Features.Where(x => feature.UserFeatures.Contains(x)).ToList();
        }
        
        if (updateModel.Count > 0)
        {
            await db.UpdateAsync<ApiKeysFeature.ApiKey>(updateModel, 
                x => x.Id == request.Id && x.UserId == userId);

            if (updateModel.ContainsKey(nameof(ApiKeysFeature.ApiKey.CancelledDate)))
            {
                feature.RemoveValidApiKeyById(request.Id);
            }
        }
        return new EmptyResponse();
    }

    public async Task<object> Any(DeleteUserApiKey request)
    {
        var (userId, _) = GetUserIdAndUserName();
        var feature = AssertPlugin<ApiKeysFeature>();
        using var db = feature.OpenDb();

        await db.DeleteAsync<ApiKeysFeature.ApiKey>(x => x.Id == request.Id && x.UserId == userId);
        feature.RemoveValidApiKeyById(request.Id.GetValueOrDefault());
        return new EmptyResponse();
    }
}
#endif
