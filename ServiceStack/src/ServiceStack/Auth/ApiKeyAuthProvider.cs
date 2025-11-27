using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.Html;

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
    public class ApiKey : IApiKey
    {
        string IApiKey.Key => Id;
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
        public bool HasScope(string scope) => false;
        public bool HasFeature(string feature) => false;
        public bool CanAccess(Type requestType) => false;
    }

    public delegate string CreateApiKeyDelegate(string environment, string keyType, int keySizeBytes);

    /// <summary>
    /// Enable access to protected Services using API Keys
    /// </summary>
    public class ApiKeyAuthProvider : AuthProvider, IAuthWithRequest
    {
        public override string Type => "Bearer";
        public const string Name = AuthenticateService.ApiKeyProvider;
        public const string Realm = "/auth/" + AuthenticateService.ApiKeyProvider;

        public static string[] DefaultTypes = ["secret"];
        public static string[] DefaultEnvironments = ["live", "test"];
        public static int DefaultKeySizeBytes = 24;

        /// <summary>
        /// Modify the registration of GetApiKeys and RegenerateApiKeys Services
        /// </summary>
        public Dictionary<Type, string[]> ServiceRoutes { get; set; } = new();

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
            Label = "API Key";
            FormLayout = new() {
                new InputInfo(nameof(IHasBearerToken.BearerToken), Input.Types.Textarea) {
                    Label = "API Key",
                    Placeholder = "",
                    Required = true,
                },
            };
            
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
            await using (authRepo as IAsyncDisposable)
            {
                var apiKey = await GetApiKeyAsync(authService.Request, request.Password).ConfigAwait();
                ValidateApiKey(authService.Request, apiKey);
                
                if (string.IsNullOrEmpty(apiKey.UserAuthId))
                    throw HttpError.Conflict(ErrorMessages.ApiKeyInvalid.Localize(authService.Request));

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

                authService.Request.SetItem(Keywords.ApiKey, apiKey);

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
                var apiKey = await GetApiKeyAsync(req, userPass.Value.Key).ConfigAwait();
                await PreAuthenticateWithApiKeyAsync(req, res, apiKey).ConfigAwait();
            }
            var bearerToken = req.GetBearerToken();
            if (bearerToken != null)
            {
                var apiKey = await GetApiKeyAsync(req, bearerToken).ConfigAwait();
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
                    await PreAuthenticateWithApiKeyAsync(req, res, await GetApiKeyAsync(req, apiKey).ConfigAwait()).ConfigAwait();
                }
            }
        }

        protected virtual async Task<ApiKey> GetApiKeyAsync(IRequest req, string apiKey)
        {
            var manageApiKeys = HostContext.AppHost.AssertManageApiKeysAsync(req);
            using (manageApiKeys as IDisposable)
            {
                return await manageApiKeys.GetApiKeyAsync(apiKey).ConfigAwait();
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
            if (!req.AllowConnection(RequireSecureConnection))
                throw HttpError.Forbidden(ErrorMessages.ApiKeyRequiresSecureConnection.Localize(req));

            ValidateApiKey(req, apiKey);

            var apiSessionKey = GetSessionKey(apiKey.Id);
            if (await HasCachedSessionAsync(req, apiSessionKey).ConfigAwait())
            {
                req.SetItem(Keywords.ApiKey, apiKey);
                return;
            }

            //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)			
            SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

            await using var authService = HostContext.ResolveService<AuthenticateService>(req);
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
                    req.SetItem(Keywords.Session, session);
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

        public override void Configure(IServiceCollection services, AuthFeature feature)
        {
            base.Configure(services, feature);
            services.RegisterServices(ServiceRoutes);
        }

        public override void Register(IAppHost appHost, AuthFeature feature)
        {
            base.Register(appHost, feature);
            var manageApiKeys = HostContext.AppHost.AssertManageApiKeysAsync();
            using (manageApiKeys as IDisposable)
            {
                if (InitSchema)
                    manageApiKeys.InitApiKeySchema();
            }

            feature.AuthEvents.Add(new ApiKeyAuthEvents(this));
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

        public override async Task OnRegisteredAsync(IRequest httpReq, IAuthSession session, IServiceBase registrationService,
            CancellationToken token = default)
        {
            var apiKeys = apiKeyProvider.GenerateNewApiKeys(session.UserAuthId);
            var manageApiKeys = HostContext.AppHost.AssertManageApiKeysAsync(httpReq);
            using (manageApiKeys as IDisposable)
            {
                await manageApiKeys.StoreAllAsync(apiKeys, token).ConfigAwait();
            }
        }
    }

    [Authenticate]
    [DefaultRequest(typeof(GetApiKeys))]
    public class GetApiKeysService : Service
    {
        public async Task<object> Any(GetApiKeys request)
        {
            var apiKeyAuth = this.Request.AssertValidApiKeyRequest();
            if (string.IsNullOrEmpty(request.Environment) && apiKeyAuth.Environments.Length != 1)
                throw new ArgumentNullException(nameof(request.Environment));

            var env = request.Environment ?? apiKeyAuth.Environments[0];

            var manageApiKeys = HostContext.AppHost.AssertManageApiKeysAsync(Request);
            using (manageApiKeys as IDisposable)
            {
                var userId = (await GetSessionAsync().ConfigAwait()).UserAuthId;
                return new GetApiKeysResponse
                {
                    Results = (await manageApiKeys.GetUserApiKeysAsync(userId).ConfigAwait())
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
        public async Task<object> Any(RegenerateApiKeys request)
        {
            var apiKeyAuth = this.Request.AssertValidApiKeyRequest();
            if (string.IsNullOrEmpty(request.Environment) && apiKeyAuth.Environments.Length != 1)
                throw new ArgumentNullException("Environment");

            var env = request.Environment ?? apiKeyAuth.Environments[0];

            var manageApiKeys = HostContext.AppHost.AssertManageApiKeysAsync(Request);
            using (manageApiKeys as IDisposable)
            {
                var userId = (await GetSessionAsync().ConfigAwait()).UserAuthId;
                var updateKeys = (await manageApiKeys.GetUserApiKeysAsync(userId).ConfigAwait())
                    .Where(x => x.Environment == env)
                    .ToList();

                updateKeys.Each(x => x.CancelledDate = DateTime.UtcNow);

                var newKeys = apiKeyAuth.GenerateNewApiKeys(userId, env);
                updateKeys.AddRange(newKeys);

                await manageApiKeys.StoreAllAsync(updateKeys).ConfigAwait();

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
        public static IApiKey GetApiKey(this IRequest req)
        {
            if (req == null)
                return null;

            return req.Items.TryGetValue(Keywords.ApiKey, out var oApiKey)
                ? oApiKey as IApiKey
                : null;
        }

        internal static ApiKeyAuthProvider AssertValidApiKeyRequest(this IRequest req)
        {
            var apiKeyAuth = (ApiKeyAuthProvider)AuthenticateService.GetAuthProvider(AuthenticateService.ApiKeyProvider);
            if (!req.AllowConnection(apiKeyAuth.RequireSecureConnection))
                throw HttpError.Forbidden(ErrorMessages.ApiKeyRequiresSecureConnection.Localize(req));

            return apiKeyAuth;
        }

        public static IManageApiKeysAsync AssertManageApiKeysAsync(this ServiceStackHost appHost, IRequest req=null)
        {
            var authRepo = appHost.GetAuthRepositoryAsync(req);
            if (authRepo == null)
                throw new NotSupportedException("ApiKeyAuthProvider requires a registered IAuthRepository");
            if (authRepo is IManageApiKeysAsync manageApiKeysAsync)
                return manageApiKeysAsync;
            if (authRepo is IManageApiKeys manageApiKeys)
                return new ManageApiKeysAsyncWrapper(manageApiKeys);

            if (authRepo is UserAuthRepositoryAsyncWrapper wrapper)
            {
                if (wrapper.AuthRepo is IManageApiKeysAsync wrapperApiKeysAsync)
                    return wrapperApiKeysAsync;
                if (wrapper.AuthRepo is IManageApiKeys wrapperApiKeys)
                    return new ManageApiKeysAsyncWrapper(wrapperApiKeys);
            }
            
            throw new NotSupportedException(authRepo.GetType().Name + " does not implement IManageApiKeys");
        }
    }

    public class ManageApiKeysAsyncWrapper(IManageApiKeys manageApiKeys) : IManageApiKeysAsync
    {
        public void InitApiKeySchema() => manageApiKeys.InitApiKeySchema();

        public Task<bool> ApiKeyExistsAsync(string apiKey, CancellationToken token = default) => 
            Task.FromResult(manageApiKeys.ApiKeyExists(apiKey));
        public Task<ApiKey> GetApiKeyAsync(string apiKey, CancellationToken token = default) => 
            Task.FromResult(manageApiKeys.GetApiKey(apiKey));
        public Task<List<ApiKey>> GetUserApiKeysAsync(string userId, CancellationToken token = default) => 
            Task.FromResult(manageApiKeys.GetUserApiKeys(userId));
        public Task StoreAllAsync(IEnumerable<ApiKey> apiKeys, CancellationToken token = default)
        {
            manageApiKeys.StoreAll(apiKeys);
            return Task.CompletedTask;
        }
    }
}