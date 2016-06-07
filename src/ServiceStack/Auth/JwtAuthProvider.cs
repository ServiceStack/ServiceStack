using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    /// <summary>
    /// Used to Issue and process JWT Tokens and registers ConvertSessionToToken Service to convert Sessions to JWT Tokens
    /// </summary>
    public class JwtAuthProvider : JwtAuthProviderReader, IAuthResponseFilter
    {
        public JwtAuthProvider() { }

        public JwtAuthProvider(IAppSettings appSettings) : base(appSettings) { }

        public override void Init(IAppSettings appSettings = null)
        {
            ServiceRoutes = new Dictionary<Type, string[]>
            {
                { typeof(ConvertSessionToTokenService), new[] { "/session-to-token" } },
            };

            base.Init(appSettings);
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

        public string CreateJwtBearerToken(IAuthSession session)
        {
            var jwtPayload = CreateJwtPayload(session, Issuer, ExpireTokensIn);
            if (CreatePayloadFilter != null)
                CreatePayloadFilter(jwtPayload);

            if (EncryptPayload)
            {
                if (PrivateKey == null || PublicKey == null)
                    throw new NotSupportedException("PrivateKey is required to EncryptPayload");

                return CreateEncryptedJweToken(jwtPayload, PublicKey.Value, AuthKey);
            }

            var jwtHeader = CreateJwtHeader(HashAlgorithm, GetKeyId());
            if (CreateHeaderFilter != null)
                CreateHeaderFilter(jwtHeader);

            Func<byte[], byte[]> hashAlgoritm = null;

            Func<byte[], byte[], byte[]> hmac;
            if (HmacAlgorithms.TryGetValue(HashAlgorithm, out hmac))
                hashAlgoritm = data => hmac(AuthKey, data);

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

        public static string CreateEncryptedJweToken(JsonObject jwtPayload, RSAParameters publicKey, byte[] authKey)
        {
            //From: http://self-issued.info/docs/draft-ietf-jose-json-web-encryption-09.html#RSACBCExample
            var jweHeader = new JsonObject
            {
                { "alg", "RSA1_5" },
                { "enc", "A128CBC-HS256" },
                { "kid", Convert.ToBase64String(publicKey.Modulus).Substring(0,3) },
            };

            var jweHeaderBase64Url = jweHeader.ToJson().ToUtf8Bytes().ToBase64UrlSafe();

            using (var aes = new AesManaged
            {
                KeySize = 128,
                BlockSize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            })
            {
                aes.GenerateIV();
                var iv = aes.IV;
                var cryptKey = aes.Key;

                var jweEncKey = RsaUtils.Encrypt(cryptKey, publicKey, UseRsaKeyLength);
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

        public static JsonObject CreateJwtPayload(IAuthSession session, string issuer, TimeSpan expireIn)
        {
            var now = DateTime.UtcNow;
            var jwtPayload = new JsonObject
            {
                {"iss", issuer},
                {"sub", session.UserAuthId},
                {"iat", now.ToUnixTime().ToString()},
                {"exp", now.Add(expireIn).ToUnixTime().ToString()},
            };

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

            var authRepo = HostContext.TryResolve<IAuthRepository>().AsUserAuthRepository();
            var manageRoles = authRepo as IManageRoles;

            var roles = session.Roles ?? new List<string>();
            var perms = session.Permissions ?? new List<string>();

            if (manageRoles != null)
            {
                roles.AddRange(manageRoles.GetRoles(session.UserAuthId) ?? TypeConstants.EmptyStringArray);
                perms.AddRange(manageRoles.GetPermissions(session.UserAuthId) ?? TypeConstants.EmptyStringArray);
            }

            if (roles.Count > 0)
                jwtPayload["roles"] = roles.ToJson();

            if (perms.Count > 0)
                jwtPayload["perms"] = perms.ToJson();

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
            var response = new ConvertSessionToTokenResponse
            {
                BearerToken = jwtAuthProvider.CreateJwtBearerToken(session)
            };

            if (!request.PreserveSession)
                Request.RemoveSession(session.Id);

            if (request.SkipCookie)
                return response;

            return new HttpResult(response)
            {
                Cookies = {
                    new Cookie(Keywords.JwtSessionToken, response.BearerToken) {
                        HttpOnly = true,
                        Secure = Request.IsSecureConnection,
                        Expires = DateTime.UtcNow.Add(jwtAuthProvider.ExpireTokensIn),
                    }
                }
            };
        }
    }
}