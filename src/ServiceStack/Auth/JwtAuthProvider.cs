using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    /// <summary>
    /// Used to Issue and process JWT Tokens and registers ConvertSessionToToken Service to convert Sessions to JWT Tokens
    /// </summary>
    public class JwtAuthProvider : JwtAuthProviderReader, IAuthResponseFilter
    {
        /// <summary>
        /// Whether to populate the Bearer Token in the AuthenticateResponse
        /// </summary>
        public bool SetBearerTokenOnAuthenticateResponse { get; set; }

        public JwtAuthProvider() {}

        public JwtAuthProvider(IAppSettings appSettings) : base(appSettings) { }

        public override void Init(IAppSettings appSettings = null)
        {
            this.SetBearerTokenOnAuthenticateResponse = appSettings == null 
                || appSettings.Get("jwt.SetBearerTokenOnAuthenticateResponse", true);

            ServiceRoutes = new Dictionary<Type, string[]>
            {
                { typeof(ConvertSessionToTokenService), new[] { "/session-to-token" } },
                { typeof(GetAccessTokenService), new[] { "/access-token" } },
            };

            base.Init(appSettings);
        }

        public void Execute(AuthFilterContext authContext)
        {
            var session = authContext.Session;
            var authService = authContext.AuthService;

            var shouldReturnTokens = authContext.DidAuthenticate;
            if (shouldReturnTokens && SetBearerTokenOnAuthenticateResponse && authContext.AuthResponse.BearerToken == null && session.IsAuthenticated)
            {
                if (!RequireSecureConnection || authService.Request.IsSecureConnection)
                {
                    IEnumerable<string> roles = null, perms = null;
                    if (HostContext.AppHost.GetAuthRepository(authService.Request) is IManageRoles authRepo && session.UserAuthId != null)
                    {
                        roles = authRepo.GetRoles(session.UserAuthId);
                        perms = authRepo.GetPermissions(session.UserAuthId);
                    }

                    authContext.AuthResponse.BearerToken = CreateJwtBearerToken(authContext.AuthService.Request, session, roles, perms);
                    authContext.AuthResponse.RefreshToken = EnableRefreshToken()
                        ? CreateJwtRefreshToken(authService.Request, session.UserAuthId, ExpireRefreshTokensIn)
                        : null;
                }
            }
        }

        public Func<byte[], byte[]> GetHashAlgorithm() => GetHashAlgorithm(null);

        public Func<byte[], byte[]> GetHashAlgorithm(IRequest req)
        {
            Func<byte[], byte[]> hashAlgorithm = null;

            if (HmacAlgorithms.TryGetValue(HashAlgorithm, out var hmac))
            {
                var authKey = GetAuthKey(req);
                if (authKey == null)
                    throw new NotSupportedException("AuthKey required to use: " + HashAlgorithm);

                hashAlgorithm = data => hmac(authKey, data);
            }

            if (RsaSignAlgorithms.TryGetValue(HashAlgorithm, out var rsa))
            {
                var privateKey = GetPrivateKey(req);
                if (privateKey == null)
                    throw new NotSupportedException("PrivateKey required to use: " + HashAlgorithm);

                hashAlgorithm = data => rsa(privateKey.Value, data);
            }

            if (hashAlgorithm == null)
                throw new NotSupportedException("Invalid algorithm: " + HashAlgorithm);

            return hashAlgorithm;
        }

        public string CreateJwtBearerToken(IAuthSession session, IEnumerable<string> roles = null, IEnumerable<string> perms = null) =>
            CreateJwtBearerToken(null, session, roles, perms);

        public string CreateJwtBearerToken(IRequest req, IAuthSession session, IEnumerable<string> roles = null, IEnumerable<string> perms = null)
        {
            var jwtPayload = CreateJwtPayload(session, Issuer, ExpireTokensIn, Audiences, roles, perms);
            CreatePayloadFilter?.Invoke(jwtPayload, session);

            if (EncryptPayload)
            {
                var publicKey = GetPublicKey(req);
                if (publicKey == null)
                    throw new NotSupportedException("PublicKey is required to EncryptPayload");

                return CreateEncryptedJweToken(jwtPayload, publicKey.Value);
            }

            var jwtHeader = CreateJwtHeader(HashAlgorithm, GetKeyId(req));
            CreateHeaderFilter?.Invoke(jwtHeader, session);

            var hashAlgorithm = GetHashAlgorithm(req);
            var bearerToken = CreateJwt(jwtHeader, jwtPayload, hashAlgorithm);
            return bearerToken;
        }

        public string CreateJwtRefreshToken(string userId, TimeSpan expireRefreshTokenIn) => CreateJwtRefreshToken(null, userId, expireRefreshTokenIn);
        public string CreateJwtRefreshToken(IRequest req, string userId, TimeSpan expireRefreshTokenIn)
        {
            var jwtHeader = new JsonObject
            {
                {"typ", "JWTR"}, //RefreshToken
                {"alg", HashAlgorithm}
            };

            var keyId = GetKeyId(req);
            if (keyId != null)
                jwtHeader["kid"] = keyId;

            var now = DateTime.UtcNow;
            var jwtPayload = new JsonObject
            {
                {"sub", userId},
                {"iat", now.ToUnixTime().ToString()},
                {"exp", now.Add(expireRefreshTokenIn).ToUnixTime().ToString()},
            };

            jwtPayload.SetAudience(Audiences);

            var hashAlgorithm = GetHashAlgorithm(req);
            var refreshToken = CreateJwt(jwtHeader, jwtPayload, hashAlgorithm);
            return refreshToken;
        }

        protected virtual bool EnableRefreshToken()
        {
            var userSessionSource = AuthenticateService.GetUserSessionSource();
            if (userSessionSource != null)
                return true;

            var authRepo = HostContext.AppHost?.TryResolve<IAuthRepository>();
            if (authRepo == null)
                return false;

            using (authRepo as IDisposable)
            {
                return authRepo is IUserAuthRepository;
            }
        }

        public static string CreateEncryptedJweToken(JsonObject jwtPayload, RSAParameters publicKey)
        {
            //From: http://self-issued.info/docs/draft-ietf-jose-json-web-encryption-09.html#RSACBCExample
            var jweHeader = new JsonObject
            {
                { "alg", "RSA-OAEP" },
                { "enc", "A128CBC-HS256" },
                { "kid", Convert.ToBase64String(publicKey.Modulus).Substring(0,3) },
            };

            var jweHeaderBase64Url = jweHeader.ToJson().ToUtf8Bytes().ToBase64UrlSafe();

            var authKey = new byte[128 / 8];
            var cryptKey = new byte[128 / 8];
            var cryptAuthKeys256 = AesUtils.CreateKey();

            Buffer.BlockCopy(cryptAuthKeys256, 0, authKey, 0, authKey.Length);
            Buffer.BlockCopy(cryptAuthKeys256, authKey.Length, cryptKey, 0, cryptKey.Length);

            var aes = Aes.Create();
            aes.KeySize = 128;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using (aes)
            {
                aes.GenerateIV();
                var iv = aes.IV;
                aes.Key = cryptKey;

                var jweEncKey = RsaUtils.Encrypt(cryptAuthKeys256, publicKey, UseRsaKeyLength);
                var jweEncKeyBase64Url = jweEncKey.ToBase64UrlSafe();
                var ivBase64Url = iv.ToBase64UrlSafe();

                var aad = jweHeaderBase64Url + "." + jweEncKeyBase64Url;
                var aadBytes = aad.ToUtf8Bytes();
                var payloadBytes = jwtPayload.ToJson().ToUtf8Bytes();

                using (var cipherStream = MemoryStreamFactory.GetStream())
                using (var encrypter = aes.CreateEncryptor(cryptKey, iv))
                using (var cryptoStream = new CryptoStream(cipherStream, encrypter, CryptoStreamMode.Write))
                using (var writer = new BinaryWriter(cryptoStream))
                {
                    writer.Write(payloadBytes);
                    cryptoStream.FlushFinalBlock();
                    
                    using (var hmac = new HMACSHA256(authKey))
                    using (var encryptedStream = MemoryStreamFactory.GetStream())
                    using (var bw = new BinaryWriter(encryptedStream))
                    {
                        bw.Write(aadBytes);
                        bw.Write(iv);
                        bw.Write(cipherStream.GetBuffer(), 0, (int)cipherStream.Length);
                        bw.Flush();
    
                        var  tag = hmac.ComputeHash(encryptedStream.GetBuffer(), 0, (int) encryptedStream.Length);

                        var cipherTextBase64Url = cipherStream.ToBase64UrlSafe();
                        var tagBase64Url = tag.ToBase64UrlSafe();
        
                        var jweToken = jweHeaderBase64Url + "."
                            + jweEncKeyBase64Url + "." 
                            + ivBase64Url + "."
                            + cipherTextBase64Url + "."
                            + tagBase64Url;
        
                        return jweToken;
                    }
                }
            }
        }

        public static string CreateJwt(
            JsonObject jwtHeader,
            JsonObject jwtPayload,
            Func<byte[], byte[]> signData)
        {
            var headerBytes = jwtHeader.ToJson().ToUtf8Bytes();
            var payloadBytes = jwtPayload.ToJson().ToUtf8Bytes();

            var base64Header = headerBytes.ToBase64UrlSafe();
            var base64Payload = payloadBytes.ToBase64UrlSafe();

            var stringToSign = base64Header + "." + base64Payload;
            var signature = signData(stringToSign.ToUtf8Bytes());

            var bearerToken = base64Header + "." + base64Payload + "." + signature.ToBase64UrlSafe();
            return bearerToken;
        }

        public static JsonObject CreateJwtHeader(string algorithm, string keyId = null)
        {
            var header = new JsonObject
            {
                { "typ", "JWT" },
                { "alg", algorithm }
            };

            if (keyId != null)
                header["kid"] = keyId;

            return header;
        }

        public static JsonObject CreateJwtPayload(
            IAuthSession session, string issuer, TimeSpan expireIn, 
            IEnumerable<string> audiences=null,
            IEnumerable<string> roles=null,
            IEnumerable<string> permissions =null)
        {
            var now = DateTime.UtcNow;
            var jwtPayload = new JsonObject
            {
                {"iss", issuer},
                {"sub", session.UserAuthId},
                {"iat", now.ToUnixTime().ToString()},
                {"exp", now.Add(expireIn).ToUnixTime().ToString()},
            };

            jwtPayload.SetAudience(audiences?.ToList());

            if (!string.IsNullOrEmpty(session.Email))
                jwtPayload["email"] = session.Email;
            if (!string.IsNullOrEmpty(session.FirstName))
                jwtPayload["given_name"] = session.FirstName;
            if (!string.IsNullOrEmpty(session.LastName))
                jwtPayload["family_name"] = session.LastName;
            if (!string.IsNullOrEmpty(session.DisplayName))
                jwtPayload["name"] = session.DisplayName;

            if (!string.IsNullOrEmpty(session.UserName))
                jwtPayload["preferred_username"] = session.UserName;
            else if (!string.IsNullOrEmpty(session.UserAuthName) && !session.UserAuthName.Contains("@"))
                jwtPayload["preferred_username"] = session.UserAuthName;

            var profileUrl = session.GetProfileUrl();
            if (profileUrl != null && profileUrl != Svg.GetDataUri(Svg.Icons.DefaultProfile))
                jwtPayload["picture"] = profileUrl;

            var combinedRoles = new List<string>(session.Roles.Safe());
            var combinedPerms = new List<string>(session.Permissions.Safe());

            roles.Each(x => combinedRoles.AddIfNotExists(x));
            permissions.Each(x => combinedPerms.AddIfNotExists(x));

            if (combinedRoles.Count > 0)
                jwtPayload["roles"] = combinedRoles.ToJson();

            if (combinedPerms.Count > 0)
                jwtPayload["perms"] = combinedPerms.ToJson();

            return jwtPayload;
        }
    }

    [Authenticate]
    [DefaultRequest(typeof(ConvertSessionToToken))]
    public class ConvertSessionToTokenService : Service
    {
        public object Any(ConvertSessionToToken request)
        {
            var jwtAuthProvider = (JwtAuthProvider)AuthenticateService.GetRequiredJwtAuthProvider();

            if (jwtAuthProvider.RequireSecureConnection && !Request.IsSecureConnection)
                throw HttpError.Forbidden(ErrorMessages.JwtRequiresSecureConnection.Localize(Request));

            if (Request.ResponseContentType.MatchesContentType(MimeTypes.Html))
                Request.ResponseContentType = MimeTypes.Json;

            var token = Request.GetJwtToken();
            IAuthSession session = null;
            var includeTokensInResponse = jwtAuthProvider.IncludeJwtInConvertSessionToTokenResponse;
            var createFromSession = string.IsNullOrEmpty(token);
            if (createFromSession || includeTokensInResponse)
            {
                session = Request.GetSession();

                if (createFromSession)
                    token = jwtAuthProvider.CreateJwtBearerToken(Request, session);

                if (!request.PreserveSession)
                    Request.RemoveSession(session.Id);
            }

            return new HttpResult(new ConvertSessionToTokenResponse {
                AccessToken = includeTokensInResponse
                    ? token
                    : null,
                RefreshToken = createFromSession && includeTokensInResponse && !request.PreserveSession
                    ? jwtAuthProvider.CreateJwtRefreshToken(Request, session.UserAuthId, jwtAuthProvider.ExpireRefreshTokensIn)
                    : null
            })
            {
                Cookies = {
                    new Cookie(Keywords.TokenCookie, token, Cookies.RootPath) {
                        HttpOnly = true,
                        Secure = Request.IsSecureConnection,
                        Expires = DateTime.UtcNow.Add(jwtAuthProvider.ExpireTokensIn),
                    }
                }
            };
        }
    }
    
    [DefaultRequest(typeof(GetAccessToken))]
    public class GetAccessTokenService : Service
    {
        public async Task<object> Any(GetAccessToken request)
        {
            var jwtAuthProvider = (JwtAuthProvider)AuthenticateService.GetRequiredJwtAuthProvider();

            if (jwtAuthProvider.RequireSecureConnection && !Request.IsSecureConnection)
                throw HttpError.Forbidden(ErrorMessages.JwtRequiresSecureConnection.Localize(Request));

            if (string.IsNullOrEmpty(request.RefreshToken))
                throw new ArgumentNullException(nameof(request.RefreshToken));

            JsonObject jwtPayload;
            try
            {
                jwtPayload = jwtAuthProvider.GetVerifiedJwtPayload(Request, request.RefreshToken.Split('.'));
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }

            if (jwtPayload == null)
                throw new ArgumentException(ErrorMessages.TokenInvalid.Localize(Request));

            jwtAuthProvider.AssertJwtPayloadIsValid(jwtPayload);

            if (jwtAuthProvider.ValidateRefreshToken != null && !jwtAuthProvider.ValidateRefreshToken(jwtPayload, Request))
                throw new ArgumentException(ErrorMessages.RefreshTokenInvalid.Localize(Request), nameof(request.RefreshToken));

            var userId = jwtPayload["sub"];

            var result = await Request.GetSessionFromSourceAsync(userId, async (authRepo, userAuth) => {
                if (await jwtAuthProvider.IsAccountLockedAsync(authRepo, userAuth))
                    throw new AuthenticationException(ErrorMessages.UserAccountLocked.Localize(Request));
            }).ConfigAwait();

            if (result == null)
                throw new NotSupportedException("JWT RefreshTokens requires a registered IUserAuthRepository or an AuthProvider implementing IUserSessionSource");
            
            var accessToken = jwtAuthProvider.CreateJwtBearerToken(Request, 
                session:result.Session, roles:result.Roles, perms:result.Permissions);

            var response = new GetAccessTokenResponse
            {
                AccessToken = accessToken
            };

            if (request.UseTokenCookie != true)
                return response;
            
            return new HttpResult(new GetAccessTokenResponse())
            {
                Cookies = {
                    new Cookie(Keywords.TokenCookie, accessToken, Cookies.RootPath) {
                        HttpOnly = true,
                        Secure = Request.IsSecureConnection,
                        Expires = DateTime.UtcNow.Add(jwtAuthProvider.ExpireTokensIn),
                    }
                }
            };
        }
    }

    internal static class JwtAuthProviderUtils
    {
        internal static void SetAudience(this JsonObject jwtPayload, List<string> audiences)
        {
            if (audiences?.Count > 0)
            {
                jwtPayload["aud"] = audiences.Count == 1
                    ? audiences[0]
                    : audiences.ToJson();
            }
        }
    }
}