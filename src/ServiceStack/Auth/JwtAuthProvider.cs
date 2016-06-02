using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using ServiceStack.Configuration;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public class JwtAuthProvider : JwtAuthProviderReader, IAuthResponseFilter
    {
        public JwtAuthProvider() {}

        public JwtAuthProvider(IAppSettings appSettings) : base(appSettings) {}

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

            var base64Header = headerBytes.ToBase64UrlSafe();
            var base64Payload = payloadBytes.ToBase64UrlSafe();

            var stringToSign = base64Header + "." + base64Payload;
            var signature = signData(stringToSign.ToUtf8Bytes());

            var bearerToken = base64Header + "." + base64Payload + "." + signature.ToBase64UrlSafe();
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
    }
}