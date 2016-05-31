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
        public Action<IAuthSession, Dictionary<string,string>> JwtSessionFilter { get; set; }

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

        public byte[] IV { get; set; }
        public string IVBase64
        {
            set { IV = Convert.FromBase64String(value); }
        }

        public TimeSpan ExpireTokensAfter { get; set; }

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
            ExpireTokensAfter = TimeSpan.FromDays(14);

            if (appSettings != null)
            {
                var base64 = appSettings.GetString("jwt.AuthKeyBase64");
                if (base64 != null)
                    AuthKeyBase64 = base64;

                base64 = appSettings.GetString("jwt.CryptKeyBase64");
                if (!string.IsNullOrEmpty(base64))
                    CryptKeyBase64 = base64;

                base64 = appSettings.GetString("jwt.IVBase64");
                if (!string.IsNullOrEmpty(base64))
                    IVBase64 = base64;

                var hashAlg = appSettings.GetString("jwt.HashAlgorithm");
                if (!string.IsNullOrEmpty(hashAlg))
                    HashAlgorithm = hashAlg;

                var issuer = appSettings.GetString("jwt.Issuer");
                if (!string.IsNullOrEmpty(issuer))
                    Issuer = issuer;

                ExpireTokensAfter = appSettings.Get("jwt.ExpireTokensAfter", ExpireTokensAfter);
            }
        }

        public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null)
        {
            return session.IsPartial && session.IsAuthenticated;
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            throw new System.NotImplementedException("JWT should not be Authenticated directly");
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
                        throw new SignatureVerificationException("Invalid signature");

                    if (CryptKey != null && IV != null)
                    {
                        payloadBytes = AesUtils.Decrypt(payloadBytes, CryptKey, IV);
                    }

                    var payloadJson = payloadBytes.FromUtf8Bytes();
                    var jwtPayload = payloadJson.FromJson<Dictionary<string, string>>();

                    if (jwtPayload.ContainsKey("exp") && !string.IsNullOrEmpty(jwtPayload["exp"]))
                    {
                        int exp;
                        try
                        {
                            exp = Convert.ToInt32(jwtPayload["exp"]);
                        }
                        catch (Exception)
                        {
                            throw new SignatureVerificationException("Claim 'exp' must be a Unix Timestamp.");
                        }

                        var secondsSinceEpoch = DateTime.UtcNow.ToUnixTime();
                        if (secondsSinceEpoch >= exp)
                            throw new SignatureVerificationException("Token has expired.");
                    }

                    var sessionId = jwtPayload.GetValue("jid", SessionExtensions.CreateRandomSessionId);
                    var session = SessionFeature.CreateNewSession(req, sessionId);
                    session.IsAuthenticated = true;
                    session.IsPartial = true;
                    session.UserAuthId = jwtPayload["sub"];
                    session.Email = jwtPayload.GetValueOrDefault("email");
                    session.DisplayName = jwtPayload.GetValueOrDefault("name");
                    session.UserName = jwtPayload.GetValueOrDefault("preferred_username");
                    session.ProfileUrl = jwtPayload.GetValueOrDefault("picture");

                    var roles = jwtPayload.GetValueOrDefault("roles");
                    if (roles != null)
                        session.Roles = roles.Split(',').ToList();

                    var perms = jwtPayload.GetValueOrDefault("perms");
                    if (perms != null)
                        session.Permissions = perms.Split(',').ToList();

                    HostContext.AppHost.OnSessionFilter(session, sessionId);

                    req.Items[Keywords.Session] = session;
                }
            }
        }

        public AuthenticateResponse Execute(IServiceBase authService, IAuthProvider authProvider, IAuthSession session, AuthenticateResponse response)
        {
            if (response.BearerToken == null && session.IsAuthenticated)
            {
                var now = DateTime.UtcNow;
                var jwtPayload = new Dictionary<string, string>
                {
                    { "iss", Issuer },
                    { "sub", session.UserAuthId },
                    { "iat", now.ToUnixTime().ToString() },
                    { "exp", now.Add(ExpireTokensAfter).ToUnixTime().ToString() },
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

                var authRepo = authService.TryResolve<IAuthRepository>().AsUserAuthRepository(authService.GetResolver());
                var manageRoles = authRepo as IManageRoles;

                if (manageRoles != null)
                {
                    var roles = manageRoles.GetRoles(session.UserAuthId);
                    if (!roles.IsEmpty())
                        jwtPayload["roles"] = string.Join(",", roles);

                    var perms = manageRoles.GetPermissions(session.UserAuthId);
                    if (!perms.IsEmpty())
                        jwtPayload["perms"] = string.Join(",", perms);
                }
                else
                {
                    if (!session.Roles.IsEmpty())
                        jwtPayload["roles"] = string.Join(",", session.Roles);

                    if (!session.Permissions.IsEmpty())
                        jwtPayload["perms"] = string.Join(",", session.Permissions);
                }

                var header = new Dictionary<string, string>
                {
                    { "typ", "JWT" },
                    { "alg", "HS256" }
                };

                if (JwtHeaderFilter != null)
                    JwtHeaderFilter(header);

                if (JwtPayloadFilter != null)
                    JwtPayloadFilter(jwtPayload);

                var headerBytes = header.ToJson().ToUtf8Bytes();
                var payloadBytes = jwtPayload.ToJson().ToUtf8Bytes();

                if (CryptKey != null && IV != null)
                {
                    payloadBytes = AesUtils.Encrypt(payloadBytes, CryptKey, IV);
                }

                var base64Header = Base64UrlEncode(headerBytes);
                var base64Payload = Base64UrlEncode(payloadBytes);

                var stringToSign = base64Header + "." + base64Payload;
                var signature = HashAlgorithms[HashAlgorithm](AuthKey, stringToSign.ToUtf8Bytes());

                response.BearerToken = base64Header + "." + base64Payload + "." + Base64UrlEncode(signature);
            }

            return response;
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

    public class SignatureVerificationException : Exception
    {
        public SignatureVerificationException(string message) : base(message) { }
    }
}