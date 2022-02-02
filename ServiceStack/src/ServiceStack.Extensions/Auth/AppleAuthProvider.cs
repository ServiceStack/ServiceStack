using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    /// <summary>
    /// Required to use Sign In with Apple:
    ///  - Membership Team ID from https://developer.apple.com/account/#/membership/
    ///  - Create &amp; configure App ID from https://developer.apple.com/account/resources/identifiers/list
    ///  - Use App Id to create &amp; configure Service ID from https://developer.apple.com/account/resources/identifiers/list/serviceId
    ///  - Use App Id to create &amp; configure Private Key from https://developer.apple.com/account/resources/authkeys/list
    ///  Service ID must be configured with non-localhost trusted domain and HTTPS callback URL, for development can use:
    ///   - Domain: local.servicestack.com
    ///   - Callback URL: https://local.servicestack.com:5001/auth/apple
    /// </summary>
    public class AppleAuthProvider : OAuth2Provider, IAuthPlugin
    {
        public const string Name = "apple";
        public static string Realm = DefaultAuthorizeUrl;

        public const string DefaultAudience = "https://appleid.apple.com";
        public const string DefaultAuthorizeUrl = "https://appleid.apple.com/auth/authorize";
        public const string DefaultAccessTokenUrl = "https://appleid.apple.com/auth/token";
        public const string DefaultIssuerSigningKeysUrl = "https://appleid.apple.com/auth/keys";
        
        /// <summary>
        /// The audience used in JWT Client Secret.
        /// Default: https://appleid.apple.com
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// Apple Developer Membership Team ID
        /// </summary>
        public string TeamId { get; set; }
        
        /// <summary>
        /// Service ID
        /// </summary>
        public string ClientId { get; set; }
        
        /// <summary>
        /// Bundle ID
        /// </summary>
        public string BundleId { get; set; }

        /// <summary>
        /// The Private Key ID
        /// </summary>
        public string KeyId { get; set; }
        
        /// <summary>
        /// Path to .p8 Private Key 
        /// </summary>
        public string KeyPath { get; set; }

        /// <summary>
        /// Base64 of .p8 Private Key bytes 
        /// </summary>
        public string KeyBase64
        {
            set => KeyBytes = ConvertPrivateKeyToBytes(value);
        }
        
        /// <summary>
        /// .p8 Private Key bytes 
        /// </summary>
        public byte[] KeyBytes { get; set; }
        
        /// <summary>
        /// Customize ClientSecret JWT
        /// </summary>
        public Func<AppleAuthProvider, string> ClientSecretFactory { get; set; }
        
        /// <summary>
        /// When JWT Client Secret expires, defaults to Apple Max 6 Month Expiry 
        /// </summary>
        public TimeSpan ClientSecretExpiry { get; set; } = TimeSpan.FromSeconds(15777000); // 6 months in secs 
        
        /// <summary>
        /// Optional: static list of Apple's public keys, defaults to fetching from https://appleid.apple.com/auth/keys
        /// </summary>
        public string IssuerSigningKeysJson { get; set; }

        /// <summary>
        /// Whether to cache private Key if loading from KeyPath, defaults: true
        /// </summary>
        public bool CacheKey { get; set; }

        /// <summary>
        /// Whether to cache Apple's public keys, defaults: true
        /// </summary>
        public bool CacheIssuerSigningKeys { get; set; }
        
        /// <summary>
        /// How long before re-validating Sign in RefreshToken, default: 1 day.
        /// Set to null to disable RefreshToken validation.
        /// </summary>
        public TimeSpan? ValidateRefreshTokenExpiry { get; set; } = TimeSpan.FromDays(1);

        public AppleAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name, "ClientId", "ClientSecret")
        {
            ResponseMode = "form_post";
            RestoreSessionFromState = true;
            VerifyAccessTokenAsync = OnVerifyAccessTokenAsync;
            ResolveUnknownDisplayName = DefaultResolveUnknownDisplayName;
            
            ClientId = appSettings.GetString($"oauth.{Name}.{nameof(ClientId)}");
            BundleId = appSettings.GetString($"oauth.{Name}.{nameof(BundleId)}");
            Audience = appSettings.Get($"oauth.{Name}.{nameof(Audience)}", DefaultAudience);
            TeamId = appSettings.GetString($"oauth.{Name}.{nameof(TeamId)}");
            KeyId = appSettings.GetString($"oauth.{Name}.{nameof(KeyId)}");
            KeyPath = appSettings.GetString($"oauth.{Name}.{nameof(KeyPath)}");
            KeyBase64 = appSettings.GetString($"oauth.{Name}.{nameof(KeyBase64)}");
            AuthorizeUrl = appSettings.Get($"oauth.{Name}.{nameof(AuthorizeUrl)}", DefaultAuthorizeUrl);
            AccessTokenUrl = appSettings.Get($"oauth.{Name}.{nameof(AccessTokenUrl)}", DefaultAccessTokenUrl);
            IssuerSigningKeysUrl = appSettings.Get($"oauth.{Name}.{nameof(IssuerSigningKeysUrl)}", DefaultIssuerSigningKeysUrl);
            IssuerSigningKeysJson = appSettings.GetString($"oauth.{Name}.{nameof(IssuerSigningKeysJson)}");
            Scopes = appSettings.Get($"oauth.{Name}.{nameof(Scopes)}", new[] { "name", "email" });
            
            var clientSecretExpiry = appSettings.GetString($"oauth.{Name}.{nameof(ClientSecretExpiry)}");
            if (!string.IsNullOrEmpty(clientSecretExpiry))
                ClientSecretExpiry = clientSecretExpiry.FromJsv<TimeSpan>();

            CacheKey = appSettings.Get($"oauth.{Name}.{nameof(CacheKey)}", true);
            CacheIssuerSigningKeys = appSettings.Get($"oauth.{Name}.{nameof(CacheIssuerSigningKeys)}", true);

            NavItem = new NavItem {
                Href = "/auth/" + Name,
                Label = "Sign In with Apple",
                Id = "btn-" + Name,
                ClassName = "btn-social btn-apple",
                IconClass = "fab svg-apple",
            };
        }

        public override void Register(IAppHost appHost, AuthFeature feature)
        {
            base.Register(appHost, feature);
            appHost.Register(new CryptoProviderFactory { CacheSignatureProviders = false });
            appHost.Register(new JwtSecurityTokenHandler());
            // UserName validation Regex to increase to allow for Apple's 44 char userid
            feature.ValidUserNameRegEx= new Regex(@"^(?=.{3,44}$)([A-Za-z0-9][._-]?)*$", RegexOptions.Compiled);
        }

        public virtual async Task<bool> OnVerifyAccessTokenAsync(string idToken, AuthContext ctx)
        {
            try
            {
                ValidateIdentityToken(idToken);
                var idTokenAuthInfo = await CreateAuthInfoAsync(idToken).ConfigAwait();

                var authRepo = GetAuthRepositoryAsync(ctx.Request);
                await using (authRepo as IAsyncDisposable)
                {
                    // If the User doesn't exist this successful Apple SignIn attempt should create the user
                    var userName = idTokenAuthInfo.Get("sub");
                    var userAuth = await authRepo.GetUserAuthByUserNameAsync(userName);
                    if (userAuth == null)
                    {
                        // Look for 1st time info (pass through from App) in Meta dictionary or ?QueryString / FormData
                        ctx.AuthInfo = (ctx.Request.Dto as Authenticate)?.Meta;
                        if (ctx.AuthInfo == null || ctx.AuthInfo.Count == 0)
                        {
                            ctx.AuthInfo = new Dictionary<string, string> {
                                ["authorizationCode"] = ctx.Request.GetQueryStringOrForm("authorizationCode"),
                                ["givenName"] = ctx.Request.GetQueryStringOrForm("givenName"),
                                ["familyName"] = ctx.Request.GetQueryStringOrForm("familyName"),
                            };
                        }
                        
                        if (!ctx.AuthInfo.TryGetValue("authorizationCode", out var code) || string.IsNullOrEmpty(code))
                            throw new Exception("authorizationCode is required for new Users");

                        // Validates the authorizationCode & Retrieves the RefreshToken for the user
                        var clientId = idTokenAuthInfo.Get("aud");
                        var clientSecret = GetClientSecret(clientId);
                        var accessTokenUrl = $"{AccessTokenUrl}?code={code}&client_id={clientId}&client_secret={clientSecret}&grant_type=authorization_code";
                        var contents = await AccessTokenUrlFilter(ctx, accessTokenUrl).PostToUrlAsync("").ConfigAwait();
                        
                        var authInfo = (Dictionary<string,object>)JSON.parse(contents);
                        foreach (var entry in authInfo.ToStringDictionary())
                        {
                            ctx.AuthInfo[entry.Key] = entry.Value;
                        }

                        return true; // Calls AuthenticateWithAccessTokenAsync to register user
                    }

                    // Whether to validate the Users Refresh Token + validate User is still in good standing 
                    if (ValidateRefreshTokenExpiry != null)
                    {
                        var userAuthId = userAuth.Id.ToString();
                        var validateCacheKey = $"apple:validate:refresh:{userAuthId}";
                        var cache = ctx.Request.GetCacheClientAsync();
                        var cacheExpiry = ValidateRefreshTokenExpiry.GetValueOrDefault();
                        var contents = await cache.GetOrCreateAsync(validateCacheKey, cacheExpiry, async () => {
                            var userAuthDetails = await authRepo.GetUserAuthDetailsAsync(userAuthId);
                            var appleAuthDetails = userAuthDetails.FirstOrDefault(x => x.Provider == Name);
                            if (appleAuthDetails?.RefreshToken == null)
                                throw new Exception($"User {userAuthId} is missing '{Name}' RefreshToken");

                            var clientId = appleAuthDetails.Items.Get("aud") ?? ClientId;
                            var contents = await ValidateRefreshToken(appleAuthDetails.RefreshToken, clientId, ctx); //expensive
                            return contents;
                        });
                        
                        var authInfo = (Dictionary<string, object>) JSON.parse(contents);
                        ctx.AuthInfo = authInfo.ToStringDictionary();
                    }
                    else
                    {
                        // ctx.AuthInfo is used & required in AuthenticateWithAccessTokenAsync()
                        ctx.AuthInfo = new Dictionary<string, string> {
                            ["id_token"] = idToken
                        };
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"OnVerifyAccessTokenAsync(): Could not validate Apple ID Token '{idToken}': {ex.Message}", ex);
                return false;
            }
        }

        public async Task<string> ValidateRefreshToken(string refreshToken, string clientId, AuthContext ctx)
        {
            var redirectUri = GetRedirectUri(ctx);
            var clientSecret = GetClientSecret(clientId);
            var accessTokenUrl = $"{AccessTokenUrl}?client_id={clientId}&client_secret={clientSecret}&redirect_uri={redirectUri.UrlEncode()}&grant_type=refresh_token&refresh_token={refreshToken}";
            var contents = await AccessTokenUrlFilter(ctx, accessTokenUrl).PostToUrlAsync("").ConfigAwait(); //expensive
            return contents;
        }

        protected override void AssertValidState()
        {
            base.AssertValidState();

            if (string.IsNullOrEmpty(TeamId))
                throw new Exception($"oauth.{Provider}.{nameof(TeamId)} is required");
            if (string.IsNullOrEmpty(Audience))
                throw new Exception($"oauth.{Provider}.{nameof(Audience)} is required");
            if (string.IsNullOrEmpty(Audience))
                throw new Exception($"oauth.{Provider}.{nameof(Audience)} is required");
        }

        protected override void AssertConsumerSecret()
        {
            if (string.IsNullOrEmpty(ConsumerSecret) && KeyBytes == null && KeyPath == null && ClientSecretFactory == null)
                throw new Exception($"{ConsumerSecretName} is required otherwise configure either Private Key or ClientSecretFactory");
        }

        protected virtual string GetClientSecret(string clientId)
        {
            if (ClientSecretFactory != null)
                return ClientSecretFactory(this);

            var keyBytes = GetPrivateKeyBytes();
            
            using var algorithm = ECDsa.Create();
            Debug.Assert(algorithm != null, nameof(algorithm) + " != null");
            algorithm.ImportPkcs8PrivateKey(keyBytes, out _);
            var key = new ECDsaSecurityKey(algorithm) { KeyId = KeyId };
            var appHost = HostContext.AssertAppHost();
            var tokenDescriptor = new SecurityTokenDescriptor {
                Audience = Audience,
                Expires = DateTime.UtcNow.Add(ClientSecretExpiry),
                Issuer = TeamId,
                Subject = new ClaimsIdentity(new[] { new Claim("sub", clientId) }),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256Signature) {
                    CryptoProviderFactory = appHost.Resolve<CryptoProviderFactory>(),
                },
            };

            var tokenHandler = appHost.Resolve<JwtSecurityTokenHandler>();
            var clientSecret = tokenHandler.CreateEncodedJwt(tokenDescriptor);
            return clientSecret;
        }

        protected virtual byte[] GetPrivateKeyBytes()
        {
            var privateKeyBytes = KeyBytes;
            if (privateKeyBytes == null)
            {
                var privateKeyFile = HostContext.AppHost.VirtualFiles.GetFile(KeyPath);
                if (privateKeyFile == null)
                    throw new Exception($"Could not find '{KeyPath}' in AppHost.VirtualFiles");
                var ret = ConvertPrivateKeyToBytes(privateKeyFile.ReadAllText());
                if (CacheKey)
                    KeyBytes = ret;
                return ret;
            }
            return privateKeyBytes;
        }

        private static byte[] ConvertPrivateKeyToBytes(string keyText)
        {
            if (string.IsNullOrEmpty(keyText))
                return null;
            if (keyText.StartsWith("-----BEGIN PRIVATE KEY-----", StringComparison.Ordinal))
            {
                var keyBase64 = keyText.AsSpan().Trim().RightPart('\n').LastLeftPart('\n');
                return MemoryProvider.Instance.ParseBase64(keyBase64);
            }
            return Convert.FromBase64String(keyText);
        }

        protected override async Task<string> GetAccessTokenJsonAsync(string code, AuthContext ctx, CancellationToken token=default)
        {
            var redirectUri = GetRedirectUri(ctx);
            var clientSecret = GetClientSecret(ClientId);
            var accessTokenUrl = $"{AccessTokenUrl}?code={code}&client_id={ClientId}&client_secret={clientSecret}&redirect_uri={redirectUri.UrlEncode()}&grant_type=authorization_code";
            var contents = await AccessTokenUrlFilter(ctx, accessTokenUrl).PostToUrlAsync("").ConfigAwait();
            return contents;
        }

        protected virtual string GetRedirectUri(AuthContext ctx)
        {
            var redirectUri = this.CallbackUrl;
            var returnUrl = ctx.Request.GetQueryStringOrForm(Keywords.ReturnUrl);
            if (!string.IsNullOrEmpty(returnUrl))
            {
                // When authenticating from an Android App we need to redirect back to an App's intent,
                // The ReturnUrl is used to identify which app to redirect to, e.g:
                //  - /auth/apple?ReturnUrl=android:com.example.fluweb
                // The same Callback URL also needs to be used when  
                redirectUri = redirectUri.AddQueryParam(Keywords.ReturnUrl, returnUrl, encode: false);
            }
            return redirectUri;
        }

        protected virtual string GetIssuerSigningKeysJson()
        {
            if (!string.IsNullOrEmpty(IssuerSigningKeysJson))
                return IssuerSigningKeysJson;

            var jsonKeys = IssuerSigningKeysUrl.GetJsonFromUrl();
            if (CacheIssuerSigningKeys)
                IssuerSigningKeysJson = jsonKeys;
            return jsonKeys;
        }
        
        /*  Good Reference: https://developer.okta.com/blog/2019/06/04/what-the-heck-is-sign-in-with-apple
            FormData:
                code:  OAuth code
                state: OAuth state 
                user: {"name":{"firstName":"First","lastName":"Last"},"email":"random@privaterelay.appleid.com"}
                    * name is only sent first time on posted redirect

            authInfo:
            {
                "access_token": "...",
                "token_type": "Bearer",
                "expires_in": 3600,
                "refresh_token": "...",
                "id_token": "{JWT}"
            }

            id_token:
                iss: https://appleid.apple.com
                aud: net.servicestack.myappid
                exp: 1598882035
                iat: 1598881435
                sub: 000000.1bf3c1f830af4fd6b6760ceb1eeee295.1234
                at_hash: SfRXQicLrN-dWqsCaQ_fBg
                email: user@gmail.com
                email_verified: true
                auth_time: 1598881434
                nonce_supported: true
         */

        protected override async Task<object> AuthenticateWithAccessTokenAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens,
            string accessToken, Dictionary<string, string> authInfo = null, CancellationToken token = default)
        {
            if (authInfo == null)
                throw new ArgumentNullException(nameof(authInfo));
            
            tokens.AccessToken = accessToken;

            tokens.Items ??= new Dictionary<string, string>();
            foreach (var entry in authInfo)
            {
                if (entry.Key == "refresh_token")
                {
                    tokens.RefreshToken = entry.Value;
                }
                else if (entry.Key == "expires_in")
                {
                    tokens.RefreshTokenExpiry = DateTime.UtcNow.AddSeconds(entry.Value.ConvertTo<int>());
                }
                else if (entry.Key == "access_token") { /*ignore*/ }
                else
                {
                    tokens.Items[entry.Key] = entry.Value;
                }
            }

            var idToken = authInfo["id_token"];

            try
            {
                ValidateIdentityToken(idToken);
            }
            catch (Exception ex)
            {
                Log.Error($"Could not validate Apple ID Token '{idToken}': {ex.Message}", ex);
                throw;
            }

            var idTokenAuthInfo = await CreateAuthInfoAsync(idToken, token).ConfigAwait();
            
            // User Info only on first time by ?user for Web OAuth or givenName/familyName by Native App
            var userJson = authService.Request.GetQueryStringOrForm("user");
            if (userJson != null)
            {
                if (JSON.parse(userJson) is Dictionary<string, object> userObj)
                {
                    if (userObj.TryGetValue("name", out var oName) && oName is Dictionary<string, object> name)
                    {
                        foreach (var entry in name.ToStringDictionary())
                        {
                            idTokenAuthInfo[entry.Key] = entry.Value;
                        }
                    }
                }
            }
            else
            {
                // Use userIdentifier/email from signed/verified JWT instead
                authInfo.TryGetValue("givenName", out var firstName);
                if (string.IsNullOrEmpty(firstName))
                    authInfo.TryGetValue("firstName", out firstName);
                if (!string.IsNullOrEmpty(firstName))
                    idTokenAuthInfo["firstName"] = firstName;

                authInfo.TryGetValue("familyName", out var lastName);
                if (string.IsNullOrEmpty(lastName))
                    authInfo.TryGetValue("lastName", out lastName);
                if (!string.IsNullOrEmpty(lastName))
                    idTokenAuthInfo["lastName"] = lastName;
            }

            session.IsAuthenticated = true;
            return await OnAuthenticatedAsync(authService, session, tokens, idTokenAuthInfo, token).ConfigAwait();
        }

        public void ValidateIdentityToken(string idToken)
        {
            var appHost = HostContext.AssertAppHost();
            var tokenHandler = appHost.Resolve<JwtSecurityTokenHandler>();

            var jsonKeys = GetIssuerSigningKeysJson();
            var keySet = JsonWebKeySet.Create(jsonKeys);
            
            // Uses BundleId when authenticating via Native App or ClientId/ServicesID via Web/OAuth flows 
            var idTokenPayload = JwtAuthProviderReader.ExtractPayload(idToken);
            var useAudience = idTokenPayload.TryGetValue("aud", out var oAud) && oAud is string aud && aud == BundleId
                ? BundleId
                : ClientId;

            var parameters = new TokenValidationParameters {
                CryptoProviderFactory = appHost.Resolve<CryptoProviderFactory>(),
                IssuerSigningKeys = keySet.Keys,
                ValidAudience = useAudience,
                ValidIssuer = Audience,
            };

            tokenHandler.ValidateToken(idToken, parameters, out _);
        }

        protected override Task<Dictionary<string, string>> CreateAuthInfoAsync(string idToken, CancellationToken token = default)
        {
            var idTokenPayload = JwtAuthProviderReader.ExtractPayload(idToken);
            return idTokenPayload.ToStringDictionary().InTask();
        }

        public static string DefaultResolveUnknownDisplayName(IAuthSession authSession, IAuthTokens tokens)
        {
            var email = authSession.Email;
            if (email.EndsWith("appleid.com")) //private email
            {
                var partialId = email.LeftPart('@');
                return $"Apple {partialId}";
            }
            return email.LeftPart('@').Replace('.', ' ').Replace('_', ' ');
        }

        protected override Task LoadUserAuthInfoAsync(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token = default)
        {
            tokens.UserId = tokens.UserName = authInfo.Get("sub");
            tokens.Email = authInfo.Get("email");

            var firstName = authInfo.Get("firstName"); // 1st Sign In only
            var lastName = authInfo.Get("lastName");   // 1st Sign In only
            if (!string.IsNullOrEmpty(firstName))
                tokens.FirstName = firstName;
            if (!string.IsNullOrEmpty(lastName))
                tokens.LastName = lastName;
            if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                tokens.DisplayName = firstName + " " + lastName;
                
            userSession.UserAuthName = tokens.UserName ?? tokens.Email;

            LoadUserOAuthProvider(userSession, tokens);
            return TypeConstants.EmptyTask;
        }
    }

    public enum AppleAuthFeature
    {
        /// <summary>
        /// Android support for https://pub.dev/packages/sign_in_with_apple
        /// </summary>
        FlutterSignInWithApple,
    }

    public static class AppleAuthProviderExtensions
    {
        public static string SignInWithAppleUrlFilter(AuthContext ctx, string url)
        {
            if (url.StartsWith("android:"))
            {
                var packageId = url.RightPart(':');
                string hashParams = null;
                if (packageId.IndexOf('#') >= 0)
                {
                    hashParams = packageId.RightPart('#');
                    packageId = packageId.LeftPart('#');
                }

                var sb = StringBuilderCache.Allocate();
                if (hashParams?.StartsWith("f=") == true) //error
                {
                    sb.Append(hashParams.Replace('/', '&'));
                }
                else
                {
                    var reqParams = ctx.Request.GetRequestParams();
                    foreach (var entry in reqParams)
                    {
                        if (sb.Length > 0)
                            sb.Append('&');
                        sb.Append(entry.Key).Append('=').Append(entry.Value.UrlEncode());
                    }
                }
                
                url = $"intent://callback?{sb}#Intent;package={packageId};scheme=signinwithapple;end";
            }
            return url;
        }
        
        public static AppleAuthProvider Use(this AppleAuthProvider provider, AppleAuthFeature feature)
        {
            if (feature == AppleAuthFeature.FlutterSignInWithApple)
            {
                provider.SuccessRedirectUrlFilter = SignInWithAppleUrlFilter;
                provider.FailedRedirectUrlFilter = SignInWithAppleUrlFilter;
            }
            return provider;
        }
    }
}
