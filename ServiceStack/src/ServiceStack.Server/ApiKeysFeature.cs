#nullable enable
#if NET8_0_OR_GREATER

using System;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.OrmLite;
using ServiceStack.Web;

namespace ServiceStack;

public class ApiKeysFeature : IPlugin, IConfigureServices, IRequiresSchema, Model.IHasStringId, IPreInitPlugin
{
    public string Id { get; set; } = Plugins.ApiKeys;
    public string AdminRole { get; set; } = RoleNames.Admin;

    public string? ApiKeyPrefix = "ak-";
    public string? HttpHeader = "x-api-key";
    public TimeSpan? CacheDuration = TimeSpan.FromMinutes(10);
    public Func<string>? ApiKeyGenerator { get; set; }
    public TimeSpan? DefaultExpiry { get; set; }
    public Dictionary<int, DateTime> LastUsedApiKeys { get; set; } = new();

    public List<string> Scopes { get; set; } =
    [
        RoleNames.Admin
    ];

    public List<string> Features { get; set; } = [];
    
    public List<KeyValuePair<string, string>> ExpiresIn { get; set; } = new()
    {
        new("", "Never"),
        new("1", "1 day"),
        new("7", "7 days"),
        new("30", "30 days"),
        new("90", "90 days"),
        new("180", "180 days"),
        new("365", "365 days"),
        new("730", "2 years"),
        new("1825", "5 years"),
        new("3650", "10 years"),
    };

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

        public string? Environment { get; set; }

        public string? Notes { get; set; }

        //Custom Reference Data
        public int? RefId { get; set; }
        public string? RefIdStr { get; set; }
        
        public bool HasScope(string scope) => Scopes.Contains(scope);
        public bool HasFeature(string feature) => Features.Contains(feature);

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
            };
        });
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
            return null;
        return to;
    }

    public void InitSchema()
    {
        using var db = HostContext.AppHost.GetDbConnection();
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

    public void Configure(IServiceCollection services)
    {
        services.AddSingleton<IApiKeySource>(c => new ApiKeysFeatureSource(this, c.GetRequiredService<IDbConnectionFactory>()));
        services.AddSingleton<IApiKeyResolver>(_ => new ApiKeyResolver(this));
        services.RegisterService<AdminApiKeysService>();
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
}

public static class ApiKeysExtensions
{
    public static string? GetApiKeyToken(this IRequest? req) => req.GetApiKey()?.Key;

    public static string? GetApiKeyUser(this IRequest? req) =>
        req.GetApiKey() is ApiKeysFeature.ApiKey x 
            ? x.UserName ?? x.UserId
            : req.GetApiKey() is Auth.ApiKey y ? y.UserAuthId : null;
}


class ApiKeyResolver(ApiKeysFeature feature) : IApiKeyResolver
{
    public string? GetApiKeyToken(IRequest req)
    {
        return feature.GetApiKeyToken(req);
    }
}
class ApiKeysFeatureSource(ApiKeysFeature feature, IDbConnectionFactory dbFactory) : IApiKeySource
{
    public async Task<IApiKey?> GetApiKeyAsync(string key)
    {
        using var db = dbFactory.OpenDbConnection();
        var apiKey = await db.SingleAsync<ApiKeysFeature.ApiKey>(x => x.Key == key);
        if (apiKey == null) 
            return null;
        if (apiKey.CancelledDate != null)
            throw HttpError.Unauthorized(ErrorMessages.ApiKeyHasBeenCancelled.Localize());
        if (apiKey.ExpiryDate != null && apiKey.ExpiryDate < DateTime.UtcNow)
            throw HttpError.Unauthorized(ErrorMessages.ApiKeyHasExpired.Localize());
        feature.RecordUsage(apiKey);
        return apiKey;
    }
}

public class AdminApiKeysService(IDbConnectionFactory dbFactory) : Service
{
    public async Task<object> Get(AdminQueryApiKeys request)
    {
        using var db = dbFactory.OpenDbConnection();
        var q = db.From<ApiKeysFeature.ApiKey>();
        if (request.Id != null)
            q.Where(x => x.Id == request.Id);
        if (!string.IsNullOrEmpty(request.Search))
        {
            var search = request.Search.ToLower();
            q.Where(x => x.Name.ToLower().Contains(search) || x.Notes.ToLower().Contains(search) || 
                         x.UserName.ToLower().Contains(search) || x.UserId.ToLower().Contains(search));
        }
        if (request.UserId != null)
            q.Where(x => x.UserId == request.UserId);
        if (request.UserName != null)
            q.Where(x => x.UserName == request.UserName);
        if (request.OrderBy != null)
            q.OrderBy(request.OrderBy);
        if (request.Skip != null)
            q.Skip(request.Skip.Value);
        if (request.Take != null)
            q.Take(request.Take.Value);
        
        var results = await db.SelectAsync(q);
        var partialResults = results.ConvertAll(x => x.ConvertTo<PartialApiKey>());
        var feature = HostContext.AssertPlugin<ApiKeysFeature>();
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
        var feature = HostContext.AssertPlugin<ApiKeysFeature>();

        var apiKey = request.ConvertTo<ApiKeysFeature.ApiKey>();
        await feature.InsertAllAsync(Db, [apiKey]);
        
        return new AdminApiKeyResponse
        {
            Result = apiKey.Key
        };
    }

    public async Task<object> Any(AdminUpdateApiKey request)
    {
        var dict = request.ToObjectDictionary();
        var updateModel = new Dictionary<string, object?>();
        var reset = (request.Reset ?? []).ToSet(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in dict)
        {
            if (entry.Key == nameof(request.Reset)) continue;
            if (entry.Value != null || reset.Contains(entry.Key))
            {
                updateModel[entry.Key] = entry.Value;
            }
        }

        if (updateModel.Count > 0)
        {
            await Db.UpdateAsync<ApiKeysFeature.ApiKey>(updateModel, x => x.Id == request.Id);
        }
        
        return new EmptyResponse();
    }

    public async Task<object> Any(AdminDeleteApiKey request)
    {
        await Db.DeleteByIdAsync<ApiKeysFeature.ApiKey>(request.Id);
        return new EmptyResponse();
    }
}

#endif
