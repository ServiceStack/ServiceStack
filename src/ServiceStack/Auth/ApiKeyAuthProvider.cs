﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    /// <summary>
    /// The Interface Auth Repositories need to implement to support API Keys
    /// </summary>
    public interface IManageApiKeys
    {
        void InitApiKeySchema();

        bool ApiKeyExists(string apiKey);

        ApiKey GetApiKey(string apiKey);

        List<ApiKey> GetUserApiKeys(string userId);

        void StoreAll(IEnumerable<ApiKey> apiKeys);
    }

    public interface IManageApiKeysAsync
    {
        void InitApiKeySchema();

        Task<bool> ApiKeyExistsAsync(string apiKey, CancellationToken token=default);

        Task<ApiKey> GetApiKeyAsync(string apiKey, CancellationToken token=default);

        Task<List<ApiKey>> GetUserApiKeysAsync(string userId, CancellationToken token=default);

        Task StoreAllAsync(IEnumerable<ApiKey> apiKeys, CancellationToken token=default);
    }

    /// <summary>
    /// The POCO Table used to persist API Keys
    /// </summary>
    public class ApiKey : IMeta
    {
        public string Id { get; set; }
        public string UserAuthId { get; set; }

        public string Environment { get; set; }
        public string KeyType { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime? CancelledDate { get; set; }
        public string Notes { get; set; }

        //Custom Reference Data
        public int? RefId { get; set; }
        public string RefIdStr { get; set; }
        public Dictionary<string, string> Meta { get; set; }
    }

    public delegate string CreateApiKeyDelegate(string environment, string keyType, int keySizeBytes);

    /// <summary>
    /// Enable access to protected Services using API Keys
    /// </summary>
    public class ApiKeyAuthProvider : AuthProvider, IAuthWithRequest, IAuthPlugin
    {
        public override string Type => "Bearer";
        public const string Name = AuthenticateService.ApiKeyProvider;
        public const string Realm = "/auth/" + AuthenticateService.ApiKeyProvider;

        public static string[] DefaultTypes = new[] { "secret" };
        public static string[] DefaultEnvironments = new[] { "live", "test" };
        public static int DefaultKeySizeBytes = 24;

        /// <summary>
        /// Modify the registration of GetApiKeys and RegenerateApiKeys Services
        /// </summary>
        public Dictionary<Type, string[]> ServiceRoutes { get; set; }

        /// <summary>
        /// How much entropy should the generated keys have. (default 24)
        /// </summary>
        public int KeySizeBytes { get; set; }

        /// <summary>
        /// Generate different keys for different environments. (default live,test)
        /// </summary>
        public string[] Environments { get; set; }

        /// <summary>
        /// Different types of Keys each user can have. (default secret)
        /// </summary>
        public string[] KeyTypes { get; set; }

        /// <summary>
        /// Whether to automatically expire keys. (default no expiry)
        /// </summary>
        public TimeSpan? ExpireKeysAfter { get; set; }

        /// <summary>
        /// Automatically create the ApiKey Table for AuthRepositories which need it. (default true)
        /// </summary>
        public bool InitSchema { get; set; }

        /// <summary>
        /// Whether to only allow access via API Key from a secure connection. (default true)
        /// </summary>
        public bool RequireSecureConnection { get; set; }

        /// <summary>
        /// Change how API Key is generated
        /// </summary>
        public CreateApiKeyDelegate GenerateApiKey { get; set; }

        /// <summary>
        /// Run custom filter after API Key is created
        /// </summary>
        public Action<ApiKey> CreateApiKeyFilter { get; set; }

        /// <summary>
        /// Cache the User Session so it can be reused between subsequent API Key Requests
        /// </summary>
        public TimeSpan? SessionCacheDuration { get; set; }

        /// <summary>
        /// Whether to allow API Keys in 'apikey' QueryString or FormData
        /// </summary>
        public bool AllowInHttpParams { get; set; }

        public ApiKeyAuthProvider()
            : base(null, Realm, Name)
        {
            Init();
        }

        public ApiKeyAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            Init(appSettings);
        }

        protected virtual void Init(IAppSettings appSettings = null)
        {
            InitSchema = true;
            RequireSecureConnection = true;
            Environments = DefaultEnvironments;
            KeyTypes = DefaultTypes;
            KeySizeBytes = DefaultKeySizeBytes;
            GenerateApiKey = CreateApiKey;

            if (appSettings != null)
            {
                InitSchema = appSettings.Get("apikey.InitSchema", true);
                RequireSecureConnection = appSettings.Get("apikey.RequireSecureConnection", true);
                AllowInHttpParams = appSettings.Get("apikey.AllowInHttpParams", false);

                var env = appSettings.GetString("apikey.Environments");
                if (env != null)
                    Environments = env.Split(ConfigUtils.ItemSeperator);

                var type = appSettings.GetString("apikey.KeyTypes");
                if (type != null)
                    KeyTypes = type.Split(ConfigUtils.ItemSeperator);

                var keySize = appSettings.GetString("apikey.KeySizeBytes");
                if (keySize != null)
                    KeySizeBytes = int.Parse(keySize);

                var timespan = appSettings.GetString("apikey.ExpireKeysAfter");
                if (timespan != null)
                    ExpireKeysAfter = timespan.FromJsv<TimeSpan>();

                timespan = appSettings.GetString("apikey.SessionCacheDuration");
                if (timespan != null)
                    SessionCacheDuration = timespan.FromJsv<TimeSpan>();
            }

            ServiceRoutes = new Dictionary<Type, string[]>
            {
                { typeof(GetApiKeysService), new[] { "/apikeys", "/apikeys/{Environment}" } },
                { typeof(RegenerateApiKeysService), new [] { "/apikeys/regenerate", "/apikeys/regenerate/{Environment}" } },
            };
        }

        [ThreadStatic] private static byte[] CachedBytes;

        public virtual string CreateApiKey(string environment, string keyType, int sizeBytes)
        {
            if (CachedBytes == null)
                CachedBytes = new byte[sizeBytes];

            SessionExtensions.PopulateWithSecureRandomBytes(CachedBytes);
            return CachedBytes.ToBase64UrlSafe();
        }

        public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null)
        {
            return session != null && session.IsAuthenticated && !session.UserAuthName.IsNullOrEmpty();
        }

        public override async Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token=default)
        {
            var authRepo = HostContext.AppHost.GetAuthRepositoryAsync(authService.Request);
#if NET472 || NETSTANDARD2_0
            await using (authRepo as IAsyncDisposable)
#else
            using (authRepo as IDisposable)
#endif
            {
                var apiKey = GetApiKey(authService.Request, request.Password);
                ValidateApiKey(authService.Request, apiKey);
                
                if (string.IsNullOrEmpty(apiKey.UserAuthId))
                    throw HttpError.Conflict(ErrorMessages.ApiKeyIsInvalid.Localize(authService.Request));

                var userAuth = await authRepo.GetUserAuthAsync(apiKey.UserAuthId, token).ConfigAwait();
                if (userAuth == null)
                    throw HttpError.Unauthorized(ErrorMessages.UserForApiKeyDoesNotExist.Localize(authService.Request));

                if (await IsAccountLockedAsync(authRepo, userAuth, token: token).ConfigAwait())
                    throw new AuthenticationException(ErrorMessages.UserAccountLocked.Localize(authService.Request));

                await session.PopulateSessionAsync(userAuth, authRepo, token).ConfigAwait();

                if (session.UserAuthName == null)
                    session.UserAuthName = userAuth.UserName ?? userAuth.Email;

                var response = await OnAuthenticatedAsync(authService, session, null, null, token).ConfigAwait();
                if (response != null)
                    return response;

                authService.Request.Items[Keywords.ApiKey] = apiKey;

                return new AuthenticateResponse
                {
                    UserId = session.UserAuthId,
                    UserName = session.UserName,
                    SessionId = session.Id,
                    DisplayName = session.DisplayName
                        ?? session.UserName
                        ?? $"{session.FirstName} {session.LastName}".Trim(),
                    ReferrerUrl = authService.Request.GetReturnUrl(),
                };
            }
        }

        public virtual async Task PreAuthenticateAsync(IRequest req, IResponse res)
        {
            //The API Key is sent in the Basic Auth Username and Password is Empty
            var userPass = req.GetBasicAuthUserAndPassword();
            if (userPass != null && string.IsNullOrEmpty(userPass.Value.Value))
            {
                var apiKey = GetApiKey(req, userPass.Value.Key);
                await PreAuthenticateWithApiKeyAsync(req, res, apiKey).ConfigAwait();
            }
            var bearerToken = req.GetBearerToken();
            if (bearerToken != null)
            {
                var apiKey = GetApiKey(req, bearerToken);
                if (apiKey != null)
                {
                    await PreAuthenticateWithApiKeyAsync(req, res, apiKey).ConfigAwait();
                }
            }

            if (AllowInHttpParams)
            {
                var apiKey = req.QueryString[Keywords.ApiKeyParam] ?? req.FormData[Keywords.ApiKeyParam];
                if (apiKey != null)
                {
                    await PreAuthenticateWithApiKeyAsync(req, res, GetApiKey(req, apiKey)).ConfigAwait();
                }
            }
        }

        protected virtual ApiKey GetApiKey(IRequest req, string apiKey)
        {
            var authRepo = (IManageApiKeys)HostContext.AppHost.GetAuthRepository(req);
            using (authRepo as IDisposable)
            {
                return authRepo.GetApiKey(apiKey);
            }
        }

        public virtual void ValidateApiKey(IRequest req, ApiKey apiKey)
        {
            if (apiKey == null)
                throw HttpError.NotFound(ErrorMessages.ApiKeyDoesNotExist.Localize(req));

            if (apiKey.CancelledDate != null)
                throw HttpError.Forbidden(ErrorMessages.ApiKeyHasBeenCancelled.Localize(req));

            if (apiKey.ExpiryDate != null && DateTime.UtcNow > apiKey.ExpiryDate.Value)
                throw HttpError.Forbidden(ErrorMessages.ApiKeyHasExpired.Localize(req));
        }

        public virtual async Task PreAuthenticateWithApiKeyAsync(IRequest req, IResponse res, ApiKey apiKey)
        {
            if (RequireSecureConnection && !req.IsSecureConnection)
                throw HttpError.Forbidden(ErrorMessages.ApiKeyRequiresSecureConnection.Localize(req));

            ValidateApiKey(req, apiKey);

            var apiSessionKey = GetSessionKey(apiKey.Id);
            if (await HasCachedSessionAsync(req, apiSessionKey).ConfigAwait())
            {
                req.Items[Keywords.ApiKey] = apiKey;
                return;
            }

            //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)			
            SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

            using var authService = HostContext.ResolveService<AuthenticateService>(req);
            var response = await authService.PostAsync(new Authenticate
            {
                provider = Name,
                UserName = "ApiKey",
                Password = apiKey.Id,
            }).ConfigAwait();

            await CacheSessionAsync(req, apiSessionKey);
        }

        public virtual async Task<bool> HasCachedSessionAsync(IRequest req, string apiSessionKey)
        {
            if (SessionCacheDuration != null)
            {
                var session = await req.GetCacheClientAsync().GetAsync<IAuthSession>(apiSessionKey).ConfigAwait();

                if (session != null)
                    session = HostContext.AppHost.OnSessionFilter(req, session, session.Id);

                if (session != null)
                {
                    req.Items[Keywords.Session] = session;
                    return true;
                }
            }
            return false;
        }

        public virtual async Task CacheSessionAsync(IRequest req, string apiSessionKey)
        {
            if (SessionCacheDuration != null)
            {
                var session = await req.GetSessionAsync().ConfigAwait();
                await req.GetCacheClientAsync().SetAsync(apiSessionKey, session, SessionCacheDuration).ConfigAwait();
            }
        }

        public static string GetSessionKey(string apiKey) => "key:sess:" + apiKey;

        public override void Register(IAppHost appHost, AuthFeature feature)
        {
            base.Register(appHost, feature);
            var authRepo = HostContext.AppHost.GetAuthRepository();
            if (authRepo == null)
                throw new NotSupportedException("ApiKeyAuthProvider requires a registered IAuthRepository");

            if (!(authRepo is IManageApiKeys apiRepo))
                throw new NotSupportedException(authRepo.GetType().Name + " does not implement IManageApiKeys");

            appHost.RegisterServices(ServiceRoutes);

            feature.AuthEvents.Add(new ApiKeyAuthEvents(this));

            if (InitSchema)
            {
                using (apiRepo as IDisposable)
                {
                    apiRepo.InitApiKeySchema();
                }
            }
        }

        public override Task OnFailedAuthentication(IAuthSession session, IRequest httpReq, IResponse httpRes)
        {
            httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
            //Needs to be 'Basic ' in order for HttpWebRequest to accept challenge and send NetworkCredentials
            httpRes.AddHeader(HttpHeaders.WwwAuthenticate, $"Basic realm=\"{this.AuthRealm}\"");
            return HostContext.AppHost.HandleShortCircuitedErrors(httpReq, httpRes, httpReq.Dto);
        }

        public List<ApiKey> GenerateNewApiKeys(string userId, params string[] environments)
        {
            var now = DateTime.UtcNow;
            var apiKeys = new List<ApiKey>();

            if (environments.Length == 0)
                environments = Environments;

            foreach (var env in environments)
            {
                foreach (var keyType in KeyTypes)
                {
                    var key = GenerateApiKey(env, keyType, KeySizeBytes);

                    var apiKey = new ApiKey
                    {
                        UserAuthId = userId,
                        Environment = env,
                        KeyType = keyType,
                        Id = key,
                        CreatedDate = now,
                        ExpiryDate = ExpireKeysAfter != null ? now.Add(ExpireKeysAfter.Value) : (DateTime?) null
                    };

                    CreateApiKeyFilter?.Invoke(apiKey);

                    apiKeys.Add(apiKey);
                }
            }
            return apiKeys;
        }
    }

    internal class ApiKeyAuthEvents : AuthEvents
    {
        private readonly ApiKeyAuthProvider apiKeyProvider;

        public ApiKeyAuthEvents(ApiKeyAuthProvider apiKeyProvider)
        {
            this.apiKeyProvider = apiKeyProvider;
        }

        public override void OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase registrationService)
        {
            var apiKeys = apiKeyProvider.GenerateNewApiKeys(session.UserAuthId);
            var authRepo = (IManageApiKeys)HostContext.AppHost.GetAuthRepository(httpReq);
            using (authRepo as IDisposable)
            {
                authRepo.StoreAll(apiKeys);
            }
        }
    }

    [Authenticate]
    [DefaultRequest(typeof(GetApiKeys))]
    public class GetApiKeysService : Service
    {
        public object Any(GetApiKeys request)
        {
            var apiKeyAuth = this.Request.AssertValidApiKeyRequest();
            if (string.IsNullOrEmpty(request.Environment) && apiKeyAuth.Environments.Length != 1)
                throw new ArgumentNullException("Environment");

            var env = request.Environment ?? apiKeyAuth.Environments[0];

            var apiRepo = (IManageApiKeys)HostContext.AppHost.GetAuthRepository(base.Request);
            using (apiRepo as IDisposable)
            {
                return new GetApiKeysResponse
                {
                    Results = apiRepo.GetUserApiKeys(GetSession().UserAuthId)
                        .Where(x => x.Environment == env)
                        .Map(k => new UserApiKey {
                            Key = k.Id,
                            KeyType = k.KeyType,
                            ExpiryDate = k.ExpiryDate,
                        })
                };
            }
        }
    }

    [Authenticate]
    [DefaultRequest(typeof(RegenerateApiKeys))]
    public class RegenerateApiKeysService : Service
    {
        public object Any(RegenerateApiKeys request)
        {
            var apiKeyAuth = this.Request.AssertValidApiKeyRequest();
            if (string.IsNullOrEmpty(request.Environment) && apiKeyAuth.Environments.Length != 1)
                throw new ArgumentNullException("Environment");

            var env = request.Environment ?? apiKeyAuth.Environments[0];

            var apiRepo = (IManageApiKeys)HostContext.AppHost.GetAuthRepository(base.Request);
            using (apiRepo as IDisposable)
            {
                var userId = GetSession().UserAuthId;
                var updateKeys = apiRepo.GetUserApiKeys(userId)
                    .Where(x => x.Environment == env)
                    .ToList();

                updateKeys.Each(x => x.CancelledDate = DateTime.UtcNow);

                var newKeys = apiKeyAuth.GenerateNewApiKeys(userId, env);
                updateKeys.AddRange(newKeys);

                apiRepo.StoreAll(updateKeys);

                return new RegenerateApiKeysResponse
                {
                    Results = newKeys.Map(k => new UserApiKey
                    {
                        Key = k.Id,
                        KeyType = k.KeyType,
                        ExpiryDate = k.ExpiryDate,
                    })
                };
            }
        }
    }
}

namespace ServiceStack
{
    public static class ApiKeyAuthProviderExtensions
    {
        public static ApiKey GetApiKey(this IRequest req)
        {
            if (req == null)
                return null;

            return req.Items.TryGetValue(Keywords.ApiKey, out var oApiKey)
                ? oApiKey as ApiKey
                : null;
        }

        internal static ApiKeyAuthProvider AssertValidApiKeyRequest(this IRequest req)
        {
            var apiKeyAuth = (ApiKeyAuthProvider)AuthenticateService.GetAuthProvider(AuthenticateService.ApiKeyProvider);
            if (apiKeyAuth.RequireSecureConnection && !req.IsSecureConnection)
                throw HttpError.Forbidden(ErrorMessages.ApiKeyRequiresSecureConnection.Localize(req));

            return apiKeyAuth;
        }
    }
}