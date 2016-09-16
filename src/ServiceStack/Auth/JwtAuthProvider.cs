using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
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
            };

            base.Init(appSettings);
        }

        public AuthenticateResponse Execute(IServiceBase authService, IAuthProvider authProvider, IAuthSession session, AuthenticateResponse response)
        {
            if (SetBearerTokenOnAuthenticateResponse && response.BearerToken == null && session.IsAuthenticated)
            {
                if (!RequireSecureConnection || authService.Request.IsSecureConnection)
                {
                    IEnumerable<string> roles = null, perms = null;
                    var authRepo = HostContext.AppHost.GetAuthRepository(authService.Request) as IManageRoles;
                    if (authRepo != null)
                    {
                        roles = authRepo.GetRoles(session.UserAuthId);
                        perms = authRepo.GetPermissions(session.UserAuthId);
                    }

                    response.BearerToken = CreateJwtBearerToken(session, roles, perms);
                }
            }

            return response;
        }

        public string CreateJwtBearerToken(IAuthSession session, IEnumerable<string> roles = null, IEnumerable<string> perms = null)
        {
            var jwtPayload = CreateJwtPayload(session, Issuer, ExpireTokensIn, Audience, roles, perms);
            CreatePayloadFilter?.Invoke(jwtPayload, session);

            if (EncryptPayload)
            {
                if (PrivateKey == null || PublicKey == null)
                    throw new NotSupportedException("PrivateKey is required to EncryptPayload");

                return CreateEncryptedJweToken(jwtPayload, PublicKey.Value);
            }

            var jwtHeader = CreateJwtHeader(HashAlgorithm, GetKeyId());
            CreateHeaderFilter?.Invoke(jwtHeader, session);

            Func<byte[], byte[]> hashAlgoritm = null;

            Func<byte[], byte[], byte[]> hmac;
            if (HmacAlgorithms.TryGetValue(HashAlgorithm, out hmac))
            {
                if (AuthKey == null)
                    throw new NotSupportedException("AuthKey required to use: " + HashAlgorithm);

                hashAlgoritm = data => hmac(AuthKey, data);
            }

            Func<RSAParameters, byte[], byte[]> rsa;
            if (RsaSignAlgorithms.TryGetValue(HashAlgorithm, out rsa))
            {
                if (PrivateKey == null)
                    throw new NotSupportedException("PrivateKey required to use: " + HashAlgorithm);

                hashAlgoritm = data => rsa(PrivateKey.Value, data);
            }

            if (hashAlgoritm == null)
                throw new NotSupportedException("Invalid algoritm: " + HashAlgorithm);

            var bearerToken = CreateJwtBearerToken(jwtHeader, jwtPayload, hashAlgoritm);
            return bearerToken;
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

                byte[] cipherText, tag;
                using (var encrypter = aes.CreateEncryptor(cryptKey, iv))
                using (var cipherStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(cipherStream, encrypter, CryptoStreamMode.Write))
                    using (var writer = new BinaryWriter(cryptoStream))
                    {
                        writer.Write(payloadBytes);
                    }

                    cipherText = cipherStream.ToArray();
                }

                using (var hmac = new HMACSHA256(authKey))
                using (var encryptedStream = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(encryptedStream))
                    {
                        writer.Write(aadBytes);
                        writer.Write(iv);
                        writer.Write(cipherText);
                        writer.Flush();

                        tag = hmac.ComputeHash(encryptedStream.ToArray());
                    }
                }

                var cipherTextBase64Url = cipherText.ToBase64UrlSafe();
                var tagBase64Url = tag.ToBase64UrlSafe();

                var jweToken = jweHeaderBase64Url + "."
                    + jweEncKeyBase64Url + "." 
                    + ivBase64Url + "."
                    + cipherTextBase64Url + "."
                    + tagBase64Url;

                return jweToken;
            }
        }

        public static string CreateJwtBearerToken(
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
            string audience=null,
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

            if (audience != null)
                jwtPayload["aud"] = audience;

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
            if (profileUrl != null && profileUrl != AuthMetadataProvider.DefaultNoProfileImgUrl)
                jwtPayload["picture"] = profileUrl;

            var combinedRoles = new List<string>(session.Roles.Safe());
            var combinedPerms = new List<string>(session.Permissions.Safe());

            if (roles != null)
                combinedRoles.AddRange(roles);
            if (permissions != null)
                combinedPerms.AddRange(permissions);

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
            var jwtAuthProvider = AuthenticateService.GetAuthProvider(JwtAuthProvider.Name) as JwtAuthProvider;
            if (jwtAuthProvider == null)
                throw new NotSupportedException("JwtAuthProvider is not registered");

            if (jwtAuthProvider.RequireSecureConnection && !Request.IsSecureConnection)
                throw HttpError.Forbidden(ErrorMessages.JwtRequiresSecureConnection);

            var session = Request.GetSession();
            if (session.FromToken)
                return new ConvertSessionToTokenResponse();

            var token = jwtAuthProvider.CreateJwtBearerToken(session);

            if (!request.PreserveSession)
                Request.RemoveSession(session.Id);

            return new HttpResult(new ConvertSessionToTokenResponse())
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
}