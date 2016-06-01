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
        public const string Name = AuthenticateService.JwtProvider;
        public const string Realm = "/auth/" + AuthenticateService.JwtProvider;

        public static readonly Dictionary<string, Func<byte[], byte[], byte[]>> HashAlgorithms = new Dictionary<string, Func<byte[], byte[], byte[]>>
        {
            { "HS256", (key, value) => { using (var sha = new HMACSHA256(key)) { return sha.ComputeHash(value); } } },
            { "HS384", (key, value) => { using (var sha = new HMACSHA384(key)) { return sha.ComputeHash(value); } } },
            { "HS512", (key, value) => { using (var sha = new HMACSHA512(key)) { return sha.ComputeHash(value); } } }
        };

        public Action<Dictionary<string, string>> JwtHeaderFilter { get; set; }
        public Action<Dictionary<string, string>> JwtPayloadFilter { get; set; }
        public Action<IAuthSession, Dictionary<string, string>> JwtSessionFilter { get; set; }

        public bool EncryptPayload { get; set; }
        public string HashAlgorithm { get; set; }
        public string Issuer { get; set; }

        public byte[] AuthKey { get; set; }
        public string AuthKeyBase64
        {
            set { AuthKey = Convert.FromBase64String(value); }
        }

        public byte[] CryptKey { get; set; }
        public string CryptKeyBase64
        {
            set { CryptKey = Convert.FromBase64String(value); }
        }

        public byte[] Iv { get; set; }
        public string IvBase64
        {
            set { Iv = Convert.FromBase64String(value); }
        }

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
            AuthKey = AesUtils.CreateKey();
            HashAlgorithm = "HS256";
            Issuer = "ssjwt";
            ExpireTokensIn = TimeSpan.FromDays(14);

            if (appSettings != null)
            {
                var base64 = appSettings.GetString("jwt.AuthKeyBase64");
                if (base64 != null)
                    AuthKeyBase64 = base64;

                base64 = appSettings.GetString("jwt.CryptKeyBase64");
                if (!string.IsNullOrEmpty(base64))
                    CryptKeyBase64 = base64;

                base64 = appSettings.GetString("jwt.IvBase64");
                if (!string.IsNullOrEmpty(base64))
                    IvBase64 = base64;

                var hashAlg = appSettings.GetString("jwt.HashAlgorithm");
                if (!string.IsNullOrEmpty(hashAlg))
                    HashAlgorithm = hashAlg;

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
            var bearerToken = req.GetBearerToken();
            if (bearerToken != null)
            {
                var parts = bearerToken.Split('.');
                if (parts.Length == 3)
                {
                    var header = parts[0];
                    var payload = parts[1];
                    var sentSignatureBytes = Base64UrlDecode(parts[2]);

                    var headerJson = Base64UrlDecode(header).FromUtf8Bytes();
                    var payloadBytes = Base64UrlDecode(payload);

                    var headerData = headerJson.FromJson<Dictionary<string, string>>();

                    var bytesToSign = string.Concat(header, ".", payload).ToUtf8Bytes();
                    var algorithm = headerData["alg"];

                    var calcSignatureBytes = HashAlgorithms[algorithm](AuthKey, bytesToSign);

                    if (!calcSignatureBytes.EquivalentTo(sentSignatureBytes))
                        throw new TokenException("Invalid signature");

                    if (CryptKey != null && Iv != null)
                    {
                        payloadBytes = AesUtils.Decrypt(payloadBytes, CryptKey, Iv);
                    }

                    var payloadJson = payloadBytes.FromUtf8Bytes();
                    var jwtPayload = payloadJson.FromJson<Dictionary<string, string>>();

                    var expiresAt = GetUnixTime(jwtPayload, "exp");
                    var secondsSinceEpoch = DateTime.UtcNow.ToUnixTime();
                    if (secondsSinceEpoch >= expiresAt)
                        throw new TokenException("Token has expired");

                    if (InvalidateTokensIssuedBefore != null)
                    {
                        var issuedAt = GetUnixTime(jwtPayload, "iat");
                        if (issuedAt == null || issuedAt < InvalidateTokensIssuedBefore.Value.ToUnixTime())
                            throw new TokenException("Token has been invalidated");
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

        public static int? GetUnixTime(Dictionary<string, string> jwtPayload, string key)
        {
            string value;
            if (jwtPayload.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
            {
                try
                {
                    return Convert.ToInt32(value);
                }
                catch (Exception)
                {
                    throw new TokenException("Claim '{0}' must be a Unix Timestamp".Fmt(key));
                }
            }
            return null;
        }

        public AuthenticateResponse Execute(IServiceBase authService, IAuthProvider authProvider, IAuthSession session, AuthenticateResponse response)
        {
            if (response.BearerToken == null && session.IsAuthenticated)
                response.BearerToken = CreateJwtBearerToken(session);

            return response;
        }

        public string CreateJwtBearerToken(IAuthSession session)
        {
            var jwtHeader = CreateJwtHeader();
            if (JwtHeaderFilter != null)
                JwtHeaderFilter(jwtHeader);

            var jwtPayload = CreateJwtPayload(session, Issuer, ExpireTokensIn);
            if (JwtPayloadFilter != null)
                JwtPayloadFilter(jwtPayload);

            var bearerToken = CreateJwtBearerToken(jwtHeader, jwtPayload, HashAlgorithms[HashAlgorithm], AuthKey, CryptKey, Iv);
            return bearerToken;
        }

        public static string CreateJwtBearerToken(
            Dictionary<string, string> jwtHeader,
            Dictionary<string, string> jwtPayload,
            Func<byte[], byte[], byte[]> hashAlgorithm,
            byte[] authKey,
            byte[] cryptKey = null, byte[] iv = null)
        {
            var headerBytes = jwtHeader.ToJson().ToUtf8Bytes();
            var payloadBytes = jwtPayload.ToJson().ToUtf8Bytes();

            if (cryptKey != null && iv != null)
            {
                payloadBytes = AesUtils.Encrypt(payloadBytes, cryptKey, iv);
            }

            var base64Header = Base64UrlEncode(headerBytes);
            var base64Payload = Base64UrlEncode(payloadBytes);

            var stringToSign = base64Header + "." + base64Payload;
            var signature = hashAlgorithm(authKey, stringToSign.ToUtf8Bytes());

            var bearerToken = base64Header + "." + base64Payload + "." + Base64UrlEncode(signature);
            return bearerToken;
        }

        public static Dictionary<string, string> CreateJwtHeader()
        {
            var header = new Dictionary<string, string>
            {
                {"typ", "JWT"},
                {"alg", "HS256"}
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
                //{ "jti", SessionExtensions.CreateRandomSessionId() },
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

    public class TokenException : Exception
    {
        public TokenException(string message) : base(message) { }
    }
}