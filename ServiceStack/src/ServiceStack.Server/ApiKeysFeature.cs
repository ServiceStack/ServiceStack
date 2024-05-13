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

public class ApiKeysFeature : IPlugin, IConfigureServices, IRequiresSchema
{
    public string? ApiKeyPrefix = "ak-";
    public string? HttpHeaderName = "x-api-key";
    public TimeSpan? CacheDuration = TimeSpan.FromMinutes(10);
    public Func<string>? ApiKeyGenerator { get; set; }
    public TimeSpan? DefaultExpiry { get; set; }

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

        /// <summary>
        /// If supporting API Keys for multiple Environments
        /// </summary>
        public string? Environment { get; set; }
    
        public DateTime CreatedDate { get; set; }
    
        public DateTime? ExpiryDate { get; set; }
    
        public DateTime? CancelledDate { get; set; }

        public List<string> Scopes { get; set; } = [];
    
        public string? Notes { get; set; }

        //Custom Reference Data
        public int? RefId { get; set; }
        public string? RefIdStr { get; set; }
        
        public bool HasScope(string scope) => Scopes.Contains(scope);
        public Dictionary<string, string>? Meta { get; set; }
    }

    private static ConcurrentDictionary<string, (IApiKey apiKey, DateTime dateTime)> Cache = new();

    public string GenerateApiKey() => ApiKeyGenerator != null 
        ? ApiKeyGenerator()
        : (ApiKeyPrefix ?? "") + Guid.NewGuid().ToString("N");

    public void Register(IAppHost appHost)
    {
        appHost.GlobalRequestFiltersAsync.Insert(0, RequestFilterAsync);
    }

    public async Task RequestFilterAsync(IRequest req, IResponse res, object requestDto)
    {
        var apiKeyToken = (HttpHeaderName != null ? req.GetHeader(HttpHeaderName) : null) ?? req.GetBearerToken();
        if (string.IsNullOrEmpty(apiKeyToken)) 
            return;
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
        services.AddSingleton<IApiKeySource, ApiKeysFeatureSource>();
    }
}

public static class ApiKeysExtensions
{
    public static string? GetApiKeyId(this IRequest? req) =>
        req.GetApiKey()?.Key;
    
    public static string? GetApiKeyUser(this IRequest? req) =>
        req.GetApiKey() is ApiKeysFeature.ApiKey x 
            ? x.UserName ?? x.UserId
            : req.GetApiKey() is Auth.ApiKey y ? y.UserAuthId : null;
}

public class ApiKeysFeatureSource(IDbConnectionFactory dbFactory) : IApiKeySource
{
    public async Task<IApiKey?> GetApiKeyAsync(string key)
    {
        using var db = dbFactory.OpenDbConnection();
        var apiKey = await db.SingleAsync<ApiKeysFeature.ApiKey>(x => x.Key == key);
        if (apiKey == null) 
            return null;
        if (apiKey.CancelledDate != null)
            throw HttpError.Unauthorized("API Key has been cancelled");
        if (apiKey.ExpiryDate != null && apiKey.ExpiryDate > DateTime.UtcNow)
            throw HttpError.Unauthorized("API Key has expired");
        return apiKey;
    }
}

#endif
