#nullable enable
#if NET8_0_OR_GREATER

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Html;
using ServiceStack.Web;

namespace ServiceStack;

public class ApiKeyCredentialsProvider : AuthProvider
{
    public override string Type => AuthenticateService.CredentialsProvider;
    public ConcurrentDictionary<string, IApiKey> ValidApiKeys { get; private set; } = new();
    
    public ApiKeyCredentialsProvider()
    {
        Provider = AuthenticateService.CredentialsProvider;
        Sort = -1;
        Label = Provider.ToPascalCase();
        FormLayout =
        [
            Input.For<Authenticate>(x => x.UserName, c =>
            {
                c.Label = "Display Name";
                c.Required = true;
            }),

            Input.For<Authenticate>(x => x.Password, c =>
            {
                c.Label = "API Key";
                c.Type = "Password";
                c.Required = true;
            }),

            Input.For<Authenticate>(x => x.RememberMe)
        ];
    }

    public override void Configure(IServiceCollection services, AuthFeature authFeature)
    {
    }

    public override void Register(IAppHost appHost, AuthFeature authFeature)
    {
        var feature = appHost.GetPlugin<ApiKeysFeature>();
        if (feature != null)
        {
            ValidApiKeys = feature.ValidApiKeys;
        }
        
        appHost.PreRequestFilters.Add((req, res) =>
        {
            var session = req.GetSession();
            if (session.IsAuthenticated && session is AuthUserSession { RequestTokenSecret: not null } authSession)
            {
                if (appHost.Config.AdminAuthSecret == authSession.RequestTokenSecret)
                {
                    req.SetItem(Keywords.AuthSecret, appHost.Config.AdminAuthSecret);
                    req.SetItem(Keywords.Authorization, "Bearer " + appHost.Config.AdminAuthSecret);
                }
                if (ValidApiKeys.TryGetValue(authSession.RequestTokenSecret, out var _))
                {
                    req.SetItem(Keywords.Authorization, "Bearer " + authSession.RequestTokenSecret);
                }
            }
        });
    }
    
    public override async Task<object?> AuthenticateAsync(IServiceBase authService, IAuthSession? session, Authenticate request, CancellationToken token = new())
    {
        var req = authService.Request;
        var authSecret = request.Password;
        var sessionId = session?.Id ?? Guid.NewGuid().ToString("n");
        session = null;
        if (HostContext.Config.AdminAuthSecret != null && HostContext.Config.AdminAuthSecret == authSecret)
        {
            var authSession = HostContext.AssertPlugin<AuthFeature>().AuthSecretSession; 
            session = new AuthUserSession {
                Id = sessionId,
                DisplayName = authSession.DisplayName,
                UserName = authSession.UserName,
                UserAuthName = authSession.UserAuthName,
                AuthProvider = AuthenticateService.ApiKeyProvider,
                IsAuthenticated = authSession.IsAuthenticated,
                Roles = authSession.Roles,
                Permissions = authSession.Permissions,
                UserAuthId = authSession.UserAuthId,
                RequestTokenSecret = authSecret,
            };
        }
        
        var apiKey = await GetValidApiKeyAsync(authSecret, req);
        if (apiKey != null)
        {
            var dbApiKey = (ApiKeysFeature.ApiKey)apiKey;
            session = new AuthUserSession 
            {
                Id = sessionId,
                DisplayName = request.UserName,
                UserName = dbApiKey.Name,
                UserAuthName = dbApiKey.Name,
                AuthProvider = AuthenticateService.ApiKeyProvider,
                IsAuthenticated = true,
                Roles = dbApiKey.Scopes,
                Permissions = [],
                UserAuthId = $"{dbApiKey.Id}",
                RequestTokenSecret = apiKey.Key,
            };
        }

        if (session != null)
        {
            session.IsAuthenticated = true;
            await SaveSessionAsync(session, req, token:token);
            
            var response = new AuthenticateResponse
            {
                UserId = session.UserAuthId,
                UserName = session.UserName,
                SessionId = session.Id,
                DisplayName = session.DisplayName,
                ReferrerUrl = session.ReferrerUrl,
                AuthProvider = session.AuthProvider,
            };
            return response;
        }

        throw HttpError.Unauthorized(ErrorMessages.ApiKeyInvalid.Localize(authService.Request));
    }

    public async Task<IApiKey?> GetValidApiKeyAsync(string token, IRequest request)
    {
        if (string.IsNullOrEmpty(token))
            return null;
        
        var source = request.TryResolve<IApiKeySource>();
        if (ValidApiKeys.TryGetValue(token, out var apiKey))
            return apiKey;

        apiKey = await source.GetApiKeyAsync(token);
        if (apiKey != null)
        {
            ValidApiKeys[token] = apiKey;

            if (apiKey.HasScope(RoleNames.Admin))
                return apiKey;
                
            return apiKey;
        }
        
        return null;
    }

    async Task SaveSessionAsync(IAuthSession session, IRequest httpReq, CancellationToken token)
    {
        session.LastModified = DateTime.UtcNow;
        httpReq.SetItem(Keywords.Session, session);
        
        var sessionKey = SessionFeature.GetSessionKey(session.Id ?? httpReq.GetOrCreateSessionId());
        await httpReq.GetCacheClientAsync().CacheSetAsync(sessionKey, session, SessionFeature.DefaultSessionExpiry, token);
    }
    
    public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate? request = null)
    {
        if (session is AuthUserSession { RequestTokenSecret: not null } userSession)
            return HostContext.Config.AdminAuthSecret == userSession.RequestTokenSecret 
                   || ValidApiKeys.ContainsKey(userSession.RequestTokenSecret);
        return false;
    }
}

#endif
