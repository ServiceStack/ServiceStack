#nullable enable
#if NET8_0_OR_GREATER

using System;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Data;
using ServiceStack.Host;
using ServiceStack.OrmLite;
using ServiceStack.Script;
using ServiceStack.Web;

namespace ServiceStack;

public class ApiKeysFeature : IPlugin, IConfigureServices, IRequiresSchema
{
    public string? ApiKeyPrefix = "ak-";
    public string? HttpHeaderName = "x-api-key";
    public TimeSpan? CacheDuration = TimeSpan.FromMinutes(10);
    public Func<string>? ApiKeyGenerator { get; set; }
    public TimeSpan? DefaultExpiry { get; set; }

    public class ApiKey : IApiKey
    {
        /// <summary>
        /// The API Key
        /// </summary>
        public string Id { get; set; }

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

        /// <summary>
        /// If supporting API Keys for multiple Environments
        /// </summary>
        public string? Environment { get; set; }
    
        public DateTime CreatedDate { get; set; }
    
        public DateTime? ExpiryDate { get; set; }
    
        public DateTime? CancelledDate { get; set; }

        public List<string> Scopes { get; set; } = new();
    
        public string? Notes { get; set; }

        //Custom Reference Data
        public int? RefId { get; set; }
        public string? RefIdStr { get; set; }
        public Dictionary<string, string>? Meta { get; set; }
    }

    private static ConcurrentDictionary<string, (ApiKey apiKey, DateTime dateTime)> Cache = new();

    public string GenerateApiKey() => ApiKeyGenerator != null 
        ? ApiKeyGenerator()
        : (ApiKeyPrefix ?? "") + Guid.NewGuid().ToString("N");

    public void Register(IAppHost appHost)
    {
        appHost.GlobalRequestFiltersAsync.Add(RequestFilterAsync);
    }

    public async Task RequestFilterAsync(IRequest req, IResponse res, object requestDto)
    {
        var apiKeyId = (HttpHeaderName != null ? req.GetHeader(HttpHeaderName) : null) ?? req.GetBearerToken();
        if (string.IsNullOrEmpty(apiKeyId)) 
            return;
        if (ApiKeyPrefix != null && !apiKeyId.StartsWith(ApiKeyPrefix))
            return;
        if (CacheDuration != null && Cache.TryGetValue(apiKeyId, out var entry))
        {
            if (entry.dateTime + CacheDuration > DateTime.UtcNow)
            {
                req.Items[Keywords.ApiKey] = entry.apiKey;
                return;
            }
            Cache.TryRemove(apiKeyId, out _);
        }

        using var db = await req.TryResolve<IDbConnectionFactory>().OpenDbConnectionAsync();
        var apiKey = await db.SingleByIdAsync<ApiKey>(apiKeyId);
        if (apiKey != null)
        {
            req.Items[Keywords.ApiKey] = apiKey;
            if (CacheDuration != null)
            {
                Cache[apiKeyId] = (apiKey, DateTime.UtcNow);
            }
        }
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
        if (string.IsNullOrEmpty(to.Id))
            to.Id = GenerateApiKey();
        if (string.IsNullOrEmpty(to.VisibleKey))
            to.VisibleKey = to.Id[..(ApiKeyPrefix?.Length ?? 0)] + "***" + to.Id[^3..];
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
        ServiceStackHost.InitOptions.ScriptContext.ScriptMethods.Add(new ApiKeysFeatureScriptMethods());
    }
}

public static class ApiKeysExtensions
{
    public static string? GetApiKeyUser(this IRequest? req) => X.Map(req.GetApiKey() as ApiKeysFeature.ApiKey, x => x.UserName ?? x.UserId);
}

public class ApiKeysFeatureScriptMethods : ScriptMethods
{
    public ITypeValidator ApiKey() => ApiKeyValidator.Instance;
    public ITypeValidator ApiKey(string scope) => new ApiKeyValidator(scope);
}

public class ApiKeyValidator()
    : TypeValidator(nameof(HttpStatusCode.Unauthorized), DefaultErrorMessage, 401), IAuthTypeValidator
{
    public static string DefaultErrorMessage { get; set; } = ErrorMessages.NotAuthenticated;
    public static ApiKeyValidator Instance { get; } = new();
    ConcurrentDictionary<string, ApiKeysFeature.ApiKey> ValidApiKeys = new();

    private string? Scope { get; }
    public ApiKeyValidator(string scope) : this()
    {
        Scope = scope;
        this.ContextArgs = new Dictionary<string, object> {
            [nameof(Scope)] = Scope,
        };
    }

    public override async Task<bool> IsValidAsync(object dto, IRequest request)
    {
        var authSecret = request.GetAuthSecret();
        if (authSecret != null && authSecret == HostContext.AppHost.Config.AdminAuthSecret)
            return true;
        
        var bearerToken = request.GetBearerToken();
        if (bearerToken != null)
        {
            if (ValidApiKeys.TryGetValue(bearerToken, out var apiKey))
            {
                request.Items[Keywords.ApiKey] = apiKey;
                return true;
            }
            
            using var db = request.TryResolve<IDbConnectionFactory>().OpenDbConnection();
            apiKey = await db.SingleByIdAsync<ApiKeysFeature.ApiKey>(bearerToken);
            if (apiKey != null)
            {
                if (Scope != null && apiKey.Scopes?.Contains(Scope) != true)
                    return false;
                
                ValidApiKeys[bearerToken] = apiKey;
                request.Items[Keywords.ApiKey] = apiKey;
                return true;
            }
        }
        return false;
    }
}

#endif
