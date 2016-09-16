using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    /// <summary>
    /// Enable access to protected Services using JWT Tokens
    /// </summary>
    public class JwtAuthProviderReader : AuthProvider, IAuthWithRequest, IAuthPlugin
    {
        public static RsaKeyLengths UseRsaKeyLength = RsaKeyLengths.Bit2048;

        public const string Name = AuthenticateService.JwtProvider;
        public const string Realm = "/auth/" + AuthenticateService.JwtProvider;

        public static HashSet<string> IgnoreForOperationTypes = new HashSet<string>
        {
            typeof(StaticFileHandler).Name,
        };

        /// <summary>
        /// Different HMAC Algorithms supported
        /// </summary>
        public static readonly Dictionary<string, Func<byte[], byte[], byte[]>> HmacAlgorithms = new Dictionary<string, Func<byte[], byte[], byte[]>>
        {
            { "HS256", (key, value) => { using (var sha = new HMACSHA256(key)) { return sha.ComputeHash(value); } } },
            { "HS384", (key, value) => { using (var sha = new HMACSHA384(key)) { return sha.ComputeHash(value); } } },
            { "HS512", (key, value) => { using (var sha = new HMACSHA512(key)) { return sha.ComputeHash(value); } } }
        };

        /// <summary>
        /// Different RSA Signing Algorithms supported
        /// </summary>
        public static readonly Dictionary<string, Func<RSAParameters, byte[], byte[]>> RsaSignAlgorithms = new Dictionary<string, Func<RSAParameters, byte[], byte[]>>
        {
            { "RS256", (key, value) => RsaUtils.Authenticate(value, key, "SHA256", UseRsaKeyLength) },
            { "RS384", (key, value) => RsaUtils.Authenticate(value, key, "SHA384", UseRsaKeyLength) },
            { "RS512", (key, value) => RsaUtils.Authenticate(value, key, "SHA512", UseRsaKeyLength) },
        };

        public static readonly Dictionary<string, Func<RSAParameters, byte[], byte[], bool>> RsaVerifyAlgorithms = new Dictionary<string, Func<RSAParameters, byte[], byte[], bool>>
        {
            { "RS256", (key, value, sig) => RsaUtils.Verify(value, sig, key, "SHA256", UseRsaKeyLength) },
            { "RS384", (key, value, sig) => RsaUtils.Verify(value, sig, key, "SHA384", UseRsaKeyLength) },
            { "RS512", (key, value, sig) => RsaUtils.Verify(value, sig, key, "SHA512", UseRsaKeyLength) },
        };

        /// <summary>
        /// Whether to only allow access via API Key from a secure connection. (default true)
        /// </summary>
        public bool RequireSecureConnection { get; set; }

        /// <summary>
        /// Run custom filter after JWT Header is created
        /// </summary>
        public Action<JsonObject, IAuthSession> CreateHeaderFilter { get; set; }

        /// <summary>
        /// Run custom filter after JWT Payload is created
        /// </summary>
        public Action<JsonObject, IAuthSession> CreatePayloadFilter { get; set; }

        /// <summary>
        /// Run custom filter after session is restored from a JWT Token
        /// </summary>
        public Action<IAuthSession, JsonObject, IRequest> PopulateSessionFilter { get; set; }

        /// <summary>
        /// Whether to encrypt JWE Payload (default false). 
        /// Uses RSA-OAEP for Key Encryption and AES/128/CBC HMAC SHA256 for Conent Encryption
        /// </summary>
        public bool EncryptPayload { get; set; }

        /// <summary>
        /// Which Hash Algorithm should be used to sign the JWT Token. (default HS256)
        /// </summary>
        public string HashAlgorithm { get; set; }

        /// <summary>
        /// Whether to only allow processing of JWT Tokens using the configured HashAlgorithm. (default true)
        /// </summary>
        public bool RequireHashAlgorithm { get; set; }

        /// <summary>
        /// The Issuer to embed in the token. (default ssjwt)
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// The Audience to embed in the token. (default null)
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// What Id to use to identify the Key used to sign the token. (default First 3 chars of Base64 Key)
        /// </summary>
        public string KeyId { get; set; }

        /// <summary>
        /// The AuthKey used to sign the JWT Token
        /// </summary>
        public byte[] AuthKey { get; set; }
        public string AuthKeyBase64
        {
            set { AuthKey = Convert.FromBase64String(value); }
        }

        /// <summary>
        /// Allow verification using multiple Auth keys
        /// </summary>
        public List<byte[]> FallbackAuthKeys { get; set; }

        /// <summary>
        /// The RSA Private Key used to Sign the JWT Token when RSA is used
        /// </summary>
        public RSAParameters? privateKey;
        public RSAParameters? PrivateKey
        {
            get { return privateKey; }
            set
            {
                privateKey = value;
                if (privateKey != null)
                    PublicKey = privateKey.Value.ToPublicRsaParameters();
            }
        }

        /// <summary>
        /// Convenient overload to intialize the Private Key via exported XML
        /// </summary>
        public string PrivateKeyXml
        {
            get { return PrivateKey?.FromPrivateRSAParameters(); }
            set { PrivateKey = value?.ToPrivateRSAParameters(); }
        }

        /// <summary>
        /// The RSA Public Key used to Verify the JWT Token when RSA is used
        /// </summary>
        public RSAParameters? PublicKey { get; set; }

        /// <summary>
        /// Convenient overload to intialize the Public Key via exported XML
        /// </summary>
        public string PublicKeyXml
        {
            get { return PublicKey?.FromPublicRSAParameters(); }
            set { PublicKey = value?.ToPublicRSAParameters(); }
        }

        /// <summary>
        /// Allow verification using multiple public keys
        /// </summary>
        public List<RSAParameters> FallbackPublicKeys { get; set; }

        /// <summary>
        /// How long should JWT Tokens be valid for. (default 14 days)
        /// </summary>
        public TimeSpan ExpireTokensIn { get; set; }

        /// <summary>
        /// Convenient overload to initialize ExpireTokensIn with an Integer
        /// </summary>
        public int ExpireTokensInDays
        {
            set
            {
                if (value > 0)
                    ExpireTokensIn = TimeSpan.FromDays(value);
            }
        }

        /// <summary>
        /// Whether to invalidate all JWT Tokens issued before a specified date.
        /// </summary>
        public DateTime? InvalidateTokensIssuedBefore { get; set; }

        /// <summary>
        /// Modify the registration of ConvertSessionToToken Service
        /// </summary>
        public Dictionary<Type, string[]> ServiceRoutes { get; set; }

        public JwtAuthProviderReader()
            : base(null, Realm, Name)
        {
            Init();
        }

        public JwtAuthProviderReader(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            Init(appSettings);
        }

        public virtual void Init(IAppSettings appSettings = null)
        {
            RequireSecureConnection = true;
            EncryptPayload = false;
            HashAlgorithm = "HS256";
            RequireHashAlgorithm = true;
            Issuer = "ssjwt";
            ExpireTokensIn = TimeSpan.FromDays(14);
            FallbackAuthKeys = new List<byte[]>();
            FallbackPublicKeys = new List<RSAParameters>();

            if (appSettings != null)
            {
                RequireSecureConnection = appSettings.Get("jwt.RequireSecureConnection", RequireSecureConnection);
                RequireHashAlgorithm = appSettings.Get("jwt.RequireHashAlgorithm", RequireHashAlgorithm);
                EncryptPayload = appSettings.Get("jwt.EncryptPayload", EncryptPayload);

                Issuer = appSettings.GetString("jwt.Issuer");
                Audience = appSettings.GetString("jwt.Audience");
                KeyId = appSettings.GetString("jwt.KeyId");

                var hashAlg = appSettings.GetString("jwt.HashAlgorithm");
                if (!string.IsNullOrEmpty(hashAlg))
                    HashAlgorithm = hashAlg;

                var privateKeyXml = appSettings.GetString("jwt.PrivateKeyXml");
                if (privateKeyXml != null)
                    PrivateKeyXml = privateKeyXml;

                var publicKeyXml = appSettings.GetString("jwt.PublicKeyXml");
                if (publicKeyXml != null)
                    PublicKeyXml = publicKeyXml;

                var base64 = appSettings.GetString("jwt.AuthKeyBase64");
                if (base64 != null)
                    AuthKeyBase64 = base64;

                var dateStr = appSettings.GetString("jwt.InvalidateTokensIssuedBefore");
                if (!string.IsNullOrEmpty(dateStr))
                    InvalidateTokensIssuedBefore = dateStr.FromJsv<DateTime>();

                ExpireTokensIn = appSettings.Get("jwt.ExpireTokensIn", ExpireTokensIn);

                var intStr = appSettings.GetString("jwt.ExpireTokensInDays");
                if (intStr != null)
                    ExpireTokensInDays = int.Parse(intStr);

                string base64Key;

                var i = 1;
                while ((base64Key = appSettings.GetString("jwt.PrivateKeyXml." + i++)) != null)
                {
                    var publicKey = base64Key.ToPublicRSAParameters();
                    FallbackPublicKeys.Add(publicKey);
                }

                i = 1;
                while ((base64Key = appSettings.GetString("jwt.PublicKeyXml." + i++)) != null)
                {
                    var publicKey = base64Key.ToPublicRSAParameters();
                    FallbackPublicKeys.Add(publicKey);
                }

                i = 1;
                while ((base64Key = appSettings.GetString("jwt.AuthKeyBase64." + i++)) != null)
                {
                    var authKey = Convert.FromBase64String(base64Key);
                    FallbackAuthKeys.Add(authKey);
                }
            }
        }

        public virtual string GetKeyId()
        {
            if (KeyId != null)
                return KeyId;

            if (HmacAlgorithms.ContainsKey(HashAlgorithm) && AuthKey != null)
                return Convert.ToBase64String(AuthKey).Substring(0, 3);
            if (RsaSignAlgorithms.ContainsKey(HashAlgorithm) && PublicKey != null)
                return Convert.ToBase64String(PublicKey.Value.Modulus).Substring(0, 3);

            return null;
        }

        public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null)
        {
            return session.FromToken && session.IsAuthenticated;
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            throw new NotImplementedException("JWT Authenticate() should not be called directly");
        }

        public void PreAuthenticate(IRequest req, IResponse res)
        {
            if (req.OperationName != null && IgnoreForOperationTypes.Contains(req.OperationName))
                return;

            var bearerToken = req.GetBearerToken()
                ?? req.GetCookieValue(Keywords.TokenCookie);

            if (bearerToken != null)
            {
                var parts = bearerToken.Split('.');
                if (parts.Length == 3)
                {
                    if (RequireSecureConnection && !req.IsSecureConnection)
                        throw HttpError.Forbidden(ErrorMessages.JwtRequiresSecureConnection);

                    var header = parts[0];
                    var payload = parts[1];
                    var signatureBytes = parts[2].FromBase64UrlSafe();

                    var headerJson = header.FromBase64UrlSafe().FromUtf8Bytes();
                    var payloadBytes = payload.FromBase64UrlSafe();

                    var headerData = headerJson.FromJson<Dictionary<string, string>>();

                    var bytesToSign = string.Concat(header, ".", payload).ToUtf8Bytes();

                    var algorithm = headerData["alg"];

                    //Potential Security Risk for relying on user-specified algorithm: https://auth0.com/blog/2015/03/31/critical-vulnerabilities-in-json-web-token-libraries/
                    if (RequireHashAlgorithm && algorithm != HashAlgorithm)
                        throw new NotSupportedException("Invalid algoritm '{0}', expected '{1}'".Fmt(algorithm, HashAlgorithm));

                    if (!VerifyPayload(algorithm, bytesToSign, signatureBytes))
                        return;

                    var payloadJson = payloadBytes.FromUtf8Bytes();
                    var jwtPayload = JsonObject.Parse(payloadJson);

                    var session = CreateSessionFromPayload(req, jwtPayload);
                    req.Items[Keywords.Session] = session;
                }
                else if (parts.Length == 5) //Encrypted JWE Token
                {
                    if (RequireSecureConnection && !req.IsSecureConnection)
                        throw HttpError.Forbidden(ErrorMessages.JwtRequiresSecureConnection);

                    if (PrivateKey == null || PublicKey == null)
                        throw new NotSupportedException("PrivateKey is required to DecryptPayload");

                    var jweHeaderBase64Url = parts[0];
                    var jweEncKeyBase64Url = parts[1];
                    var ivBase64Url = parts[2];
                    var cipherTextBase64Url = parts[3];
                    var tagBase64Url = parts[4];

                    var sentTag = tagBase64Url.FromBase64UrlSafe();
                    var aadBytes = (jweHeaderBase64Url + "." + jweEncKeyBase64Url).ToUtf8Bytes();
                    var iv = ivBase64Url.FromBase64UrlSafe();
                    var cipherText = cipherTextBase64Url.FromBase64UrlSafe();

                    var jweEncKey = jweEncKeyBase64Url.FromBase64UrlSafe();
                    var cryptAuthKeys256 = RsaUtils.Decrypt(jweEncKey, PrivateKey.Value, UseRsaKeyLength);

                    var authKey = new byte[128 / 8];
                    var cryptKey = new byte[128 / 8];
                    Buffer.BlockCopy(cryptAuthKeys256, 0, authKey, 0, authKey.Length);
                    Buffer.BlockCopy(cryptAuthKeys256, authKey.Length, cryptKey, 0, cryptKey.Length);

                    using (var hmac = new HMACSHA256(authKey))
                    using (var encryptedStream = new MemoryStream())
                    {
                        using (var writer = new BinaryWriter(encryptedStream))
                        {
                            writer.Write(aadBytes);
                            writer.Write(iv);
                            writer.Write(cipherText);
                            writer.Flush();

                            var calcTag = hmac.ComputeHash(encryptedStream.ToArray());

                            if (!calcTag.EquivalentTo(sentTag))
                                return;
                        }
                    }

                    JsonObject jwtPayload;
                    var aes = Aes.Create();
                    aes.KeySize = 128;
                    aes.BlockSize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (aes)
                    using (var decryptor = aes.CreateDecryptor(cryptKey, iv))
                    using (var ms = MemoryStreamFactory.GetStream(cipherText))
                    using (var cryptStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        var jwtPayloadBytes = cryptStream.ReadFully();
                        jwtPayload = JsonObject.Parse(jwtPayloadBytes.FromUtf8Bytes());
                    }

                    var session = CreateSessionFromPayload(req, jwtPayload);
                    req.Items[Keywords.Session] = session;
                }
            }
        }

        private IAuthSession CreateSessionFromPayload(IRequest req, JsonObject jwtPayload)
        {
            var expiresAt = GetUnixTime(jwtPayload, "exp");
            var secondsSinceEpoch = DateTime.UtcNow.ToUnixTime();
            if (secondsSinceEpoch >= expiresAt)
                throw new TokenException(ErrorMessages.TokenExpired);

            if (InvalidateTokensIssuedBefore != null)
            {
                var issuedAt = GetUnixTime(jwtPayload, "iat");
                if (issuedAt == null || issuedAt < InvalidateTokensIssuedBefore.Value.ToUnixTime())
                    throw new TokenException(ErrorMessages.TokenInvalidated);
            }

            string audience;
            if (jwtPayload.TryGetValue("aud", out audience))
            {
                if (audience != Audience)
                    throw new TokenException("Invalid Audience: " + audience);
            }

            var sessionId = jwtPayload.GetValue("jid", SessionExtensions.CreateRandomSessionId);
            var session = SessionFeature.CreateNewSession(req, sessionId);

            session.PopulateFromMap(jwtPayload);

            PopulateSessionFilter?.Invoke(session, jwtPayload, req);

            HostContext.AppHost.OnSessionFilter(session, sessionId);
            return session;
        }

        public bool VerifyPayload(string algorithm, byte[] bytesToSign, byte[] sentSignatureBytes)
        {
            var isHmac = HmacAlgorithms.ContainsKey(algorithm);
            var isRsa = RsaSignAlgorithms.ContainsKey(algorithm);
            if (!isHmac && !isRsa)
                throw new NotSupportedException("Invalid algoritm: " + algorithm);

            if (isHmac)
            {
                if (AuthKey == null)
                    throw new NotSupportedException("AuthKey required to use: " + HashAlgorithm);

                var authKeys = new List<byte[]> { AuthKey };
                authKeys.AddRange(FallbackAuthKeys);
                foreach (var authKey in authKeys)
                {
                    var calcSignatureBytes = HmacAlgorithms[algorithm](authKey, bytesToSign);
                    if (calcSignatureBytes.EquivalentTo(sentSignatureBytes))
                        return true;
                }
            }
            else
            {
                if (PublicKey == null)
                    throw new NotSupportedException("PublicKey required to use: " + HashAlgorithm);

                var publicKeys = new List<RSAParameters> { PublicKey.Value };
                publicKeys.AddRange(FallbackPublicKeys);
                foreach (var publicKey in publicKeys)
                {
                    var verified = RsaVerifyAlgorithms[algorithm](publicKey, bytesToSign, sentSignatureBytes);
                    if (verified)
                        return true;
                }
            }

            return false;
        }

        static int? GetUnixTime(Dictionary<string, string> jwtPayload, string key)
        {
            string value;
            if (jwtPayload.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
            {
                try
                {
                    return int.Parse(value);
                }
                catch (Exception)
                {
                    throw new TokenException($"Claim '{key}' must be a Unix Timestamp");
                }
            }
            return null;
        }

        public void Register(IAppHost appHost, AuthFeature feature)
        {
            var isHmac = HmacAlgorithms.ContainsKey(HashAlgorithm);
            var isRsa = RsaSignAlgorithms.ContainsKey(HashAlgorithm);
            if (!isHmac && !isRsa)
                throw new NotSupportedException("Invalid algoritm: " + HashAlgorithm);

            if (isHmac && AuthKey == null)
                throw new ArgumentNullException("AuthKey", "An AuthKey is Required to use JWT, e.g: new JwtAuthProvider { AuthKey = AesUtils.CreateKey() }");
            else if (isRsa && PrivateKey == null && PublicKey == null)
                throw new ArgumentNullException("PrivateKey", "PrivateKey is Required to use JWT with " + HashAlgorithm);

            if (KeyId == null)
                KeyId = GetKeyId();

            foreach (var registerService in ServiceRoutes)
            {
                appHost.RegisterService(registerService.Key, registerService.Value);
            }

            feature.AuthResponseDecorator = AuthenticateResponseDecorator;
        }

        public object AuthenticateResponseDecorator(IServiceBase authService, Authenticate request, AuthenticateResponse authResponse)
        {
            if (authResponse.BearerToken == null || request.UseTokenCookie != true)
                return authResponse;

            authService.Request.RemoveSession(authService.GetSessionId());

            return new HttpResult(authResponse)
            {
                Cookies = {
                    new Cookie(Keywords.TokenCookie, authResponse.BearerToken, Cookies.RootPath) {
                        HttpOnly = true,
                        Secure = authService.Request.IsSecureConnection,
                        Expires = DateTime.UtcNow.Add(ExpireTokensIn),
                    }
                }
            };
        }
    }
}