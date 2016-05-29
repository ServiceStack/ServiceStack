using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public interface IManageApiKeys
    {
        void InitApiKeySchema();

        bool ApiKeyExists(string apiKey);

        ApiKey GetApiKey(string apiKey);

        List<ApiKey> GetUserApiKeys(string userId);

        void StoreAll(IEnumerable<ApiKey> apiKeys);
    }

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

    public class ApiKeyAuthProvider : AuthProvider, IAuthWithRequest, IAuthPlugin
    {
        public const string Name = AuthenticateService.ApiKeyProvider;
        public const string Realm = "/auth/" + AuthenticateService.ApiKeyProvider;

        public static string[] DefaultTypes = new[] { "secret" };
        public static string[] DefaultEnvironments = new[] { "live", "test" };
        public static int DefaultKeySizeBytes = 16;

        public Dictionary<Type, string[]> ServiceRoutes { get; set; }

        public int KeySizeBytes { get; set; }
        public string[] Environments { get; set; }
        public string[] KeyTypes { get; set; }
        public TimeSpan? ExpireKeysAfter { get; set; }
        public bool InitSchema { get; set; }
        public bool RequireSecureConnection { get; set; }

        public CreateApiKeyDelegate CreateApiKeyFn { get; set; }
        public Action<ApiKey> ApiKeyFilterFn { get; set; }

        public ApiKeyAuthProvider()
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
            CreateApiKeyFn = CreateApiKey;

            if (appSettings != null)
            {
                InitSchema = appSettings.Get("apikey.InitSchema", true);
                RequireSecureConnection = appSettings.Get("apikey.RequireSecureConnection", true);

                var env = appSettings.GetString("apikey.Environments");
                if (env != null)
                    Environments = env.Split(ConfigUtils.ItemSeperator);

                var type = appSettings.GetString("apikey.KeyTypes");
                if (type != null)
                    KeyTypes = type.Split(ConfigUtils.ItemSeperator);

                var keySize = appSettings.GetString("apikey.KeySizeBytes");
                if (keySize != null)
                    KeySizeBytes = int.Parse(keySize);
            }

            ServiceRoutes = new Dictionary<Type, string[]>
            {
                { typeof(GetApiKeysService), new[] { "/apikeys", "/apikeys/{Environment}" } },
                { typeof(RegenrateApiKeysService), new [] { "/apikeys/regenerate", "/apikeys/regenerate/{Environment}" } },
            };
        }

        public virtual string CreateApiKey(string environment, string keyType, int sizeBytes)
        {
            return SessionExtensions.CreateRandomBase62Id(sizeBytes);
        }

        public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null)
        {
            return session != null && session.IsAuthenticated && !session.UserAuthName.IsNullOrEmpty();
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var authRepo = authService.TryResolve<IAuthRepository>().AsUserAuthRepository(authService.GetResolver());
            var apiRepo = (IManageApiKeys)authRepo;

            var apiKey = apiRepo.GetApiKey(request.Password);
            if (apiKey == null)
                throw HttpError.NotFound("ApiKey does not exist");

            if (apiKey.CancelledDate != null)
                throw HttpError.Forbidden("ApiKey has been cancelled");

            if (apiKey.ExpiryDate != null && DateTime.UtcNow > apiKey.ExpiryDate.Value)
                throw HttpError.Forbidden("ApiKey has expired");

            var userAuth = authRepo.GetUserAuth(apiKey.UserAuthId);
            if (userAuth == null)
                throw HttpError.Unauthorized("User for ApiKey does not exist");

            if (IsAccountLocked(authRepo, userAuth))
                throw new AuthenticationException(ErrorMessages.UserAccountLocked);

            PopulateSession(authRepo, userAuth, session);

            if (session.UserAuthName == null)
                session.UserAuthName = userAuth.UserName ?? userAuth.Email;

            var response = OnAuthenticated(authService, session, null, null);
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
                    ?? "{0} {1}".Fmt(session.FirstName, session.LastName).Trim(),
                ReferrerUrl = request.Continue,
            };
        }

        public void PreAuthenticate(IRequest req, IResponse res)
        {
            //The API Key is sent in the Basic Auth Username and Password is Empty
            var userPass = req.GetBasicAuthUserAndPassword();
            if (userPass != null && string.IsNullOrEmpty(userPass.Value.Value))
            {
                if (RequireSecureConnection && !req.IsSecureConnection)
                    throw HttpError.Forbidden(ErrorMessages.ApiKeyRequiresSecureConnection);

                var apiKey = userPass.Value.Key;

                PreAuthenticateWithApiKey(req, res, apiKey);
            }
            var bearerToken = req.GetBearerToken();
            if (bearerToken != null)
            {
                var authRepo = (IManageApiKeys)req.TryResolve<IAuthRepository>().AsUserAuthRepository(req);
                if (authRepo.ApiKeyExists(bearerToken))
                {
                    if (RequireSecureConnection && !req.IsSecureConnection)
                        throw HttpError.Forbidden(ErrorMessages.ApiKeyRequiresSecureConnection);

                    PreAuthenticateWithApiKey(req, res, bearerToken);
                }
            }
        }

        private static void PreAuthenticateWithApiKey(IRequest req, IResponse res, string apiKey)
        {
            //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)			
            SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

            using (var authService = HostContext.ResolveService<AuthenticateService>(req))
            {
                var response = authService.Post(new Authenticate
                {
                    provider = Name,
                    UserName = "ApiKey",
                    Password = apiKey,
                });
            }
        }

        public void Register(IAppHost appHost, AuthFeature feature)
        {
            var authRepo = appHost.TryResolve<IAuthRepository>().AsUserAuthRepository(appHost);
            var apiRepo = authRepo as IManageApiKeys;
            if (apiRepo == null)
                throw new NotSupportedException(apiRepo.GetType().Name + " does not implement IManageApiKeys");

            foreach (var registerService in ServiceRoutes)
            {
                appHost.RegisterService(registerService.Key, registerService.Value);
            }

            feature.AuthEvents.Add(new ApiKeyAuthEvents(this));

            if (InitSchema)
            {
                apiRepo.InitApiKeySchema();
            }
        }

        public override void OnFailedAuthentication(IAuthSession session, IRequest httpReq, IResponse httpRes)
        {
            httpRes.StatusCode = (int)HttpStatusCode.Unauthorized;
            //Needs to be 'Basic ' in order for HttpWebRequest to accept challenge and send NetworkCredentials
            httpRes.AddHeader(HttpHeaders.WwwAuthenticate, "Basic realm=\"{0}\"".Fmt(this.AuthRealm));
            httpRes.EndRequest();
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
                    var key = CreateApiKeyFn(env, keyType, KeySizeBytes);

                    var apiKey = new ApiKey
                    {
                        UserAuthId = userId,
                        Environment = env,
                        KeyType = keyType,
                        Id = key,
                        CreatedDate = now,
                        ExpiryDate = ExpireKeysAfter != null ? now.Add(ExpireKeysAfter.Value) : (DateTime?) null
                    };

                    if (ApiKeyFilterFn != null)
                        ApiKeyFilterFn(apiKey);

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
            var authRepo = (IManageApiKeys)httpReq.TryResolve<IAuthRepository>().AsUserAuthRepository(httpReq);
            authRepo.StoreAll(apiKeys);
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

            var authRepo = (IManageApiKeys)TryResolve<IAuthRepository>().AsUserAuthRepository(Request);
            return new GetApiKeysResponse
            {
                Results = authRepo.GetUserApiKeys(GetSession().UserAuthId)
                    .Where(x => x.Environment == env)
                    .Map(k => new UserApiKey {
                            Key = k.Id,
                            KeyType = k.KeyType,
                            ExpiryDate = k.ExpiryDate,
                        })
            };
        }
    }

    [Authenticate]
    [DefaultRequest(typeof(RegenrateApiKeys))]
    public class RegenrateApiKeysService : Service
    {
        public object Any(RegenrateApiKeys request)
        {
            var apiKeyAuth = this.Request.AssertValidApiKeyRequest();
            if (string.IsNullOrEmpty(request.Environment) && apiKeyAuth.Environments.Length != 1)
                throw new ArgumentNullException("Environment");

            var env = request.Environment ?? apiKeyAuth.Environments[0];

            var authRepo = (IManageApiKeys)TryResolve<IAuthRepository>().AsUserAuthRepository(Request);

            var userId = GetSession().UserAuthId;
            var updateKeys = authRepo.GetUserApiKeys(userId)
                .Where(x => x.Environment == env)
                .ToList();

            updateKeys.Each(x => x.CancelledDate = DateTime.UtcNow);

            var newKeys = apiKeyAuth.GenerateNewApiKeys(userId, env);
            updateKeys.AddRange(newKeys);

            authRepo.StoreAll(updateKeys);
            
            return new RegenrateApiKeysResponse
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

namespace ServiceStack
{
    public static class ApiKeyAuthProviderExtensions
    {
        public static ApiKey GetApiKey(this IRequest req)
        {
            if (req == null)
                return null;

            object oApiKey;
            return req.Items.TryGetValue(Keywords.ApiKey, out oApiKey)
                ? oApiKey as ApiKey
                : null;
        }

        internal static ApiKeyAuthProvider AssertValidApiKeyRequest(this IRequest req)
        {
            var apiKeyAuth = (ApiKeyAuthProvider)AuthenticateService.GetAuthProvider(AuthenticateService.ApiKeyProvider);
            if (apiKeyAuth.RequireSecureConnection && !req.IsSecureConnection)
                throw HttpError.Forbidden(ErrorMessages.ApiKeyRequiresSecureConnection);

            return apiKeyAuth;
        }
    }
}