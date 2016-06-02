using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public class JwtAuthProvider : AuthProvider, IAuthWithRequest, IAuthResponseFilter
    {
        public static RsaKeyLengths UseRsaKeyLength = RsaKeyLengths.Bit2048;

        public const string Name = AuthenticateService.JwtProvider;
        public const string Realm = "/auth/" + AuthenticateService.JwtProvider;

        public static readonly Dictionary<string, Func<byte[], byte[], byte[]>> HmacAlgorithms = new Dictionary<string, Func<byte[], byte[], byte[]>>
        {
            { "HS256", (key, value) => { using (var sha = new HMACSHA256(key)) { return sha.ComputeHash(value); } } },
            { "HS384", (key, value) => { using (var sha = new HMACSHA384(key)) { return sha.ComputeHash(value); } } },
            { "HS512", (key, value) => { using (var sha = new HMACSHA512(key)) { return sha.ComputeHash(value); } } }
        };

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

        public bool RequireSecureConnection { get; set; }

        public Action<Dictionary<string, string>> JwtHeaderFilter { get; set; }

        public Action<Dictionary<string, string>> JwtPayloadFilter { get; set; }

        public Action<IAuthSession, Dictionary<string, string>> JwtSessionFilter { get; set; }

        public bool EncryptPayload { get; set; }

        public string HashAlgorithm { get; set; }

        public bool RequireHashAlgorithm { get; set; }

        public string Issuer { get; set; }

        public byte[] HmacAuthKey { get; set; }

        public string HmacAuthKeyBase64
        {
            set { HmacAuthKey = Convert.FromBase64String(value); }
        }

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

        public string PrivateKeyXml
        {
            get { return PrivateKey.Value.FromPrivateRSAParameters(); }
            set { PrivateKey = value.ToPrivateRSAParameters(); }
        }

        public RSAParameters? PublicKey { get; set; }

        public TimeSpan ExpireTokensIn { get; set; }

        public DateTime? InvalidateTokensIssuedBefore { get; set; }

        public JwtAuthProvider()
        {
            Init();
        }

        public JwtAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name)
        {
            Init(appSettings);
        }

        public void Init(IAppSettings appSettings = null)
        {
            RequireSecureConnection = true;
            HmacAuthKey = AesUtils.CreateKey();
            HashAlgorithm = "HS256";
            RequireHashAlgorithm = true;
            Issuer = "ssjwt";
            ExpireTokensIn = TimeSpan.FromDays(14);

            if (appSettings != null)
            {
                RequireSecureConnection = appSettings.Get("jwt.RequireSecureConnection", RequireSecureConnection);

                var base64 = appSettings.GetString("jwt.HmacAuthKeyBase64");
                if (base64 != null)
                    HmacAuthKeyBase64 = base64;

                var hashAlg = appSettings.GetString("jwt.HashAlgorithm");
                if (!string.IsNullOrEmpty(hashAlg))
                    HashAlgorithm = hashAlg;

                RequireHashAlgorithm = appSettings.Get("jwt.RequireHashAlgorithm", RequireSecureConnection);

                var issuer = appSettings.GetString("jwt.Issuer");
                if (!string.IsNullOrEmpty(issuer))
                    Issuer = issuer;

                ExpireTokensIn = appSettings.Get("jwt.ExpireTokensIn", ExpireTokensIn);

                var dateStr = appSettings.GetString("jwt.InvalidateTokensIssuedBefore");
                if (!string.IsNullOrEmpty(dateStr))
                    InvalidateTokensIssuedBefore = dateStr.FromJsv<DateTime>();
            }
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
                    var signatureBytes = Base64UrlDecode(parts[2]);

                    var headerJson = Base64UrlDecode(header).FromUtf8Bytes();
                    var payloadBytes = Base64UrlDecode(payload);

                    var headerData = headerJson.FromJson<Dictionary<string, string>>();

                    var bytesToSign = string.Concat(header, ".", payload).ToUtf8Bytes();

                    var algorithm = headerData["alg"];

                    //Potential Security Risk for relying on user-specified algorithm: https://auth0.com/blog/2015/03/31/critical-vulnerabilities-in-json-web-token-libraries/
                    if (RequireHashAlgorithm && algorithm != HashAlgorithm)
                        throw new NotSupportedException("Invalid algoritm '{0}', expected '{1}'".Fmt(algorithm, HashAlgorithm));

                    VerifyPayload(algorithm, bytesToSign, signatureBytes);

                    if (EncryptPayload)
                    {
                        if (PrivateKey == null)
                            throw new NotSupportedException("PrivateKey required to Decrypt Payload");

                        payloadBytes = RsaUtils.Decrypt(payloadBytes, PrivateKey.Value, UseRsaKeyLength);
                    }

                    var payloadJson = payloadBytes.FromUtf8Bytes();
                    var jwtPayload = payloadJson.FromJson<Dictionary<string, string>>();

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
                    session.IsAuthenticated = true;
                    session.FromToken = true;
                    session.UserAuthId = jwtPayload["sub"];
                    session.Email = jwtPayload.GetValueOrDefault("email");
                    session.UserName = jwtPayload.GetValueOrDefault("preferred_username");
                    session.DisplayName = jwtPayload.GetValueOrDefault("name");
                    session.ProfileUrl = jwtPayload.GetValueOrDefault("picture");

                    var roles = jwtPayload.GetValueOrDefault("role");
                    if (roles != null)
                        session.Roles = roles.Split(',').ToList();

                    var perms = jwtPayload.GetValueOrDefault("perm");
                    if (perms != null)
                        session.Permissions = perms.Split(',').ToList();

                    HostContext.AppHost.OnSessionFilter(session, sessionId);

                    req.Items[Keywords.Session] = session;
                }
            }
        }

        public void VerifyPayload(string algorithm, byte[] bytesToSign, byte[] sentSignatureBytes)
        {
            var isHmac = HmacAlgorithms.ContainsKey(algorithm);
            var isRsa = RsaSignAlgorithms.ContainsKey(algorithm);
            if (!isHmac && !isRsa)
                throw new NotSupportedException("Invalid algoritm: " + algorithm);

            if (isHmac)
            {
                var calcSignatureBytes = HmacAlgorithms[algorithm](HmacAuthKey, bytesToSign);

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

        public AuthenticateResponse Execute(IServiceBase authService, IAuthProvider authProvider, IAuthSession session, AuthenticateResponse response)
        {
            if (response.BearerToken == null && session.IsAuthenticated)
            {
                if (!RequireSecureConnection || authService.Request.IsSecureConnection)
                    response.BearerToken = CreateJwtBearerToken(session);
            }

            return response;
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

        public string CreateJwtBearerToken(IAuthSession session)
        {
            var jwtHeader = CreateJwtHeader(HashAlgorithm);
            if (JwtHeaderFilter != null)
                JwtHeaderFilter(jwtHeader);

            var jwtPayload = CreateJwtPayload(session, Issuer, ExpireTokensIn);
            if (JwtPayloadFilter != null)
                JwtPayloadFilter(jwtPayload);

            Func<byte[], byte[]> hashAlgoritm = null;

            Func<byte[], byte[], byte[]> hmac;
            if (HmacAlgorithms.TryGetValue(HashAlgorithm, out hmac))
                hashAlgoritm = data => hmac(HmacAuthKey, data);

            Func<RSAParameters, byte[], byte[]> rsa;
            if (RsaSignAlgorithms.TryGetValue(HashAlgorithm, out rsa))
            {
                if (PrivateKey == null)
                    throw new NotSupportedException("PrivateKey required to use: " + HashAlgorithm);

                hashAlgoritm = data => rsa(PrivateKey.Value, data);
            }

            if (hashAlgoritm == null)
                throw new NotSupportedException("Invalid algoritm: " + HashAlgorithm);

            if (EncryptPayload && PublicKey == null)
                throw new NotSupportedException("PublicKey required to EncryptPayload");

            var encryptWithKey = EncryptPayload ? PublicKey : null;

            var bearerToken = CreateJwtBearerToken(jwtHeader, jwtPayload, hashAlgoritm, encryptWithKey);
            return bearerToken;
        }

        public static string CreateJwtBearerToken(
            Dictionary<string, string> jwtHeader,
            Dictionary<string, string> jwtPayload,
            Func<byte[], byte[]> signData,
            RSAParameters? encryptWithPublicKey = null)
        {
            var headerBytes = jwtHeader.ToJson().ToUtf8Bytes();
            var payloadBytes = jwtPayload.ToJson().ToUtf8Bytes();

            if (encryptWithPublicKey != null)
            {
                payloadBytes = RsaUtils.Encrypt(payloadBytes, encryptWithPublicKey.Value, UseRsaKeyLength);
            }

            var base64Header = Base64UrlEncode(headerBytes);
            var base64Payload = Base64UrlEncode(payloadBytes);

            var stringToSign = base64Header + "." + base64Payload;
            var signature = signData(stringToSign.ToUtf8Bytes());

            var bearerToken = base64Header + "." + base64Payload + "." + Base64UrlEncode(signature);
            return bearerToken;
        }

        public static Dictionary<string, string> CreateJwtHeader(string algorithm)
        {
            var header = new Dictionary<string, string>
            {
                { "typ", "JWT" },
                { "alg", algorithm }
            };
            return header;
        }

        public static Dictionary<string, string> CreateJwtPayload(IAuthSession session, string issuer, TimeSpan expireIn)
        {
            var now = DateTime.UtcNow;
            var jwtPayload = new Dictionary<string, string>
            {
                {"iss", issuer},
                {"sub", session.UserAuthId},
                {"iat", now.ToUnixTime().ToString()},
                {"exp", now.Add(expireIn).ToUnixTime().ToString()},
            };

            if (!string.IsNullOrEmpty(session.Email))
                jwtPayload["email"] = session.Email;
            if (!string.IsNullOrEmpty(session.DisplayName))
                jwtPayload["name"] = session.DisplayName;

            if (!string.IsNullOrEmpty(session.UserName))
                jwtPayload["preferred_username"] = session.UserName;
            else if (!string.IsNullOrEmpty(session.UserAuthName) && !session.UserAuthName.Contains("@"))
                jwtPayload["preferred_username"] = session.UserAuthName;

            var profileUrl = session.GetProfileUrl();
            if (profileUrl != null && profileUrl != AuthMetadataProvider.DefaultNoProfileImgUrl)
                jwtPayload["picture"] = profileUrl;

            var authRepo = HostContext.TryResolve<IAuthRepository>().AsUserAuthRepository();
            var manageRoles = authRepo as IManageRoles;

            if (manageRoles != null)
            {
                var roles = manageRoles.GetRoles(session.UserAuthId);
                if (!roles.IsEmpty())
                    jwtPayload["role"] = string.Join(",", roles);

                var perms = manageRoles.GetPermissions(session.UserAuthId);
                if (!perms.IsEmpty())
                    jwtPayload["perm"] = string.Join(",", perms);
            }
            else
            {
                if (!session.Roles.IsEmpty())
                    jwtPayload["role"] = string.Join(",", session.Roles);

                if (!session.Permissions.IsEmpty())
                    jwtPayload["perm"] = string.Join(",", session.Permissions);
            }

            return jwtPayload;
        }

        // from JWT spec
        public static string Base64UrlEncode(byte[] input)
        {
            var output = Convert.ToBase64String(input);
            output = output.Split('=')[0]; // Remove any trailing '='s
            output = output.Replace('+', '-'); // 62nd char of encoding
            output = output.Replace('/', '_'); // 63rd char of encoding
            return output;
        }

        // from JWT spec
        public static byte[] Base64UrlDecode(string input)
        {
            var output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: output += "=="; break; // Two pad chars
                case 3: output += "="; break;  // One pad char
                default: throw new Exception("Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
        }
    }

}