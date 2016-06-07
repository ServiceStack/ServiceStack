using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using ServiceStack.Configuration;
using ServiceStack.Host;
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
            { "RS512", (key, value) => RsaUtils.Authenticate(value, key, "SHA512", UseRsaKeyLength) },
        };

        public static readonly Dictionary<string, Func<RSAParameters, byte[], byte[], bool>> RsaVerifyAlgorithms = new Dictionary<string, Func<RSAParameters, byte[], byte[], bool>>
        {
            { "RS256", (key, value, sig) => RsaUtils.Verify(value, sig, key, "SHA256", UseRsaKeyLength) },
            { "RS512", (key, value, sig) => RsaUtils.Verify(value, sig, key, "SHA512", UseRsaKeyLength) },
        };

        /// <summary>
        /// Whether to only allow access via API Key from a secure connection. (default true)
        /// </summary>
        public bool RequireSecureConnection { get; set; }

        /// <summary>
        /// Run custom filter after JWT Header is created
        /// </summary>
        public Action<JsonObject> CreateHeaderFilter { get; set; }

        /// <summary>
        /// Run custom filter after JWT Payload is created
        /// </summary>
        public Action<JsonObject> CreatePayloadFilter { get; set; }

        /// <summary>
        /// Run custom filter after session is restored from a JWT Token
        /// </summary>
        public Action<IAuthSession, JsonObject, IRequest> PopulateSessionFilter { get; set; }

        /// <summary>
        /// Whether to encrypt JWT Payload with supplied AES Crypt Key and IV/ (default false)
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
            get { return PrivateKey != null ? PrivateKey.Value.FromPrivateRSAParameters() : null; }
            set { PrivateKey = value != null ? value.ToPrivateRSAParameters() : (RSAParameters?) null; }
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
            get { return PublicKey != null ? PublicKey.Value.FromPublicRSAParameters() : null; }
            set { PublicKey = value != null ? value.ToPublicRSAParameters() : (RSAParameters?)null; }
        }

        /// <summary>
        /// How long should JWT Tokens be valid for. (default 14 days)
        /// </summary>
        public TimeSpan ExpireTokensIn { get; set; }

        /// <summary>
        /// Whether to invalidate all JWT Tokens issued before a specified date.
        /// </summary>
        public DateTime? InvalidateTokensIssuedBefore { get; set; }

        /// <summary>
        /// Modify the registration of ConvertSessionToToken Service
        /// </summary>
        public Dictionary<Type, string[]> ServiceRoutes { get; set; }

        public JwtAuthProviderReader()
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
            AuthKey = AesUtils.CreateKey();
            HashAlgorithm = "HS256";
            RequireHashAlgorithm = true;
            Issuer = "ssjwt";
            ExpireTokensIn = TimeSpan.FromDays(14);

            if (appSettings != null)
            {
                RequireSecureConnection = appSettings.Get("jwt.RequireSecureConnection", RequireSecureConnection);

                var hashAlg = appSettings.GetString("jwt.HashAlgorithm");
                if (!string.IsNullOrEmpty(hashAlg))
                    HashAlgorithm = hashAlg;

                RequireHashAlgorithm = appSettings.Get("jwt.RequireHashAlgorithm", RequireSecureConnection);

                PrivateKeyXml = appSettings.GetString("jwt.PrivateKeyXml");

                PublicKeyXml = appSettings.GetString("jwt.PublicKeyXml");

                var base64 = appSettings.GetString("jwt.AuthKeyBase64");
                if (base64 != null)
                    AuthKeyBase64 = base64;

                var issuer = appSettings.GetString("jwt.Issuer");
                if (!string.IsNullOrEmpty(issuer))
                    Issuer = issuer;

                ExpireTokensIn = appSettings.Get("jwt.ExpireTokensIn", ExpireTokensIn);

                var dateStr = appSettings.GetString("jwt.InvalidateTokensIssuedBefore");
                if (!string.IsNullOrEmpty(dateStr))
                    InvalidateTokensIssuedBefore = dateStr.FromJsv<DateTime>();

                KeyId = appSettings.GetString("jwt.KeyId");
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
            var bearerToken = req.GetBearerToken()
                ?? req.GetCookieValue(Keywords.JwtSessionToken);

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

                    VerifyPayload(algorithm, bytesToSign, signatureBytes);

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

                    using (var hmac = new HMACSHA256(AuthKey))
                    using (var encryptedStream = new MemoryStream())
                    {
                        using (var writer = new BinaryWriter(encryptedStream))
                        {
                            writer.Write(aadBytes);
                            writer.Write(iv);
                            writer.Write(cipherText);
                            writer.Flush();

                            var calcTag = hmac.ComputeHash(encryptedStream.ToArray());

                            if (sentTag.Length != calcTag.Length)
                                throw new ArgumentException("Invalid JWE Authentication Tag");

                            var compare = 0;
                            for (var i = 0; i < sentTag.Length; i++)
                                compare |= sentTag[i] ^ calcTag[i];

                            if (compare != 0)
                                throw new ArgumentException("Invalid JWE Authentication Tag");
                        }
                    }

                    var jweEncKey = jweEncKeyBase64Url.FromBase64UrlSafe();
                    var cryptKey = RsaUtils.Decrypt(jweEncKey, PrivateKey.Value, UseRsaKeyLength);

                    JsonObject jwtPayload;
                    using (var aes = new AesManaged
                    {
                        KeySize = 128,
                        BlockSize = 128,
                        Mode = CipherMode.CBC,
                        Padding = PaddingMode.PKCS7
                    })
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

            var sessionId = jwtPayload.GetValue("jid", SessionExtensions.CreateRandomSessionId);
            var session = SessionFeature.CreateNewSession(req, sessionId);

            session.PopulateFromMap(jwtPayload);

            if (PopulateSessionFilter != null)
                PopulateSessionFilter(session, jwtPayload, req);

            HostContext.AppHost.OnSessionFilter(session, sessionId);
            return session;
        }

        public void VerifyPayload(string algorithm, byte[] bytesToSign, byte[] sentSignatureBytes)
        {
            var isHmac = HmacAlgorithms.ContainsKey(algorithm);
            var isRsa = RsaSignAlgorithms.ContainsKey(algorithm);
            if (!isHmac && !isRsa)
                throw new NotSupportedException("Invalid algoritm: " + algorithm);

            if (isHmac)
            {
                var calcSignatureBytes = HmacAlgorithms[algorithm](AuthKey, bytesToSign);

                if (!calcSignatureBytes.EquivalentTo(sentSignatureBytes))
                    throw new TokenException(ErrorMessages.InvalidSignature);
            }
            else
            {
                if (PublicKey == null)
                    throw new NotSupportedException("Invalid algoritm: " + algorithm);

                var verified = RsaVerifyAlgorithms[algorithm](PublicKey.Value, bytesToSign, sentSignatureBytes);
                if (!verified)
                    throw new TokenException(ErrorMessages.InvalidSignature);
            }
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
                    throw new TokenException("Claim '{0}' must be a Unix Timestamp".Fmt(key));
                }
            }
            return null;
        }

        public void Register(IAppHost appHost, AuthFeature feature)
        {
            if (KeyId == null)
                KeyId = GetKeyId();

            foreach (var registerService in ServiceRoutes)
            {
                appHost.RegisterService(registerService.Key, registerService.Value);
            }
        }
    }
}