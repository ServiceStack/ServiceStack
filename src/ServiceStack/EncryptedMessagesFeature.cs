// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ServiceStack.Auth;
using ServiceStack.Text;
using System.Security.Cryptography;
using ServiceStack.Web;

namespace ServiceStack
{
    [DefaultRequest(typeof(GetPublicKey))]
    [Restrict(VisibilityTo = RequestAttributes.None)]
    public class EncryptedMessagesService : Service
    {
        public object Any(EncryptedMessage request)
        {
            throw new NotImplementedException("EncryptedMessages Service cannot be called directly");
        }

        public object Any(GetPublicKey request)
        {
            var rsaParameters = HostContext.GetPlugin<EncryptedMessagesFeature>().PrivateKey.Value;
            var publicKeyXml = rsaParameters.ToPublicKeyXml();
            Request.Response.ContentType = MimeTypes.Xml;
            Request.Response.AddHeader("X-PublicKey-Hash", publicKeyXml.ToSha256Hash());
            return publicKeyXml;
        }
    }

    public class EncryptedMessagesFeature : IPlugin
    {
        public static readonly string RequestItemsIv = "_encryptIv";
        public static readonly string RequestItemsCryptKey = "_encryptCryptKey";
        public static readonly string RequestItemsAuthKey = "_encryptAuthKey";
        public static readonly TimeSpan DefaultMaxMaxRequestAge = TimeSpan.FromMinutes(20);

        public static string ErrorInvalidMessage = "Invalid EncryptedMessage";
        public static string ErrorNonceSeen = "Nonce already seen";
        public static string ErrorRequestTooOld = "Request too old";
        public static string ErrorKeyNotFound = "Key with Id '{0}' was not found. Ensure you're using the latest Public Key from '{1}'";


        private readonly ConcurrentDictionary<byte[], DateTime> nonceCache = new ConcurrentDictionary<byte[], DateTime>(ByteArrayComparer.Instance);

        public RSAParameters? PrivateKey { get; set; }

        public List<RSAParameters> FallbackPrivateKeys { get; set; }

        protected Dictionary<string, RSAParameters> PrivateKeyModulusMap { get; set; }

        public string PublicKeyPath { get; set; }

        public TimeSpan MaxRequestAge { get; set; }

        public string PrivateKeyXml
        {
            get { return PrivateKey.Value.FromPrivateRSAParameters(); }
            set { PrivateKey = value.ToPrivateRSAParameters(); }
        }

        public EncryptedMessagesFeature()
        {
            PublicKeyPath = "/publickey";
            MaxRequestAge = DefaultMaxMaxRequestAge;
            FallbackPrivateKeys = new List<RSAParameters>();
        }

        public void Register(IAppHost appHost)
        {
            if (PrivateKey == null)
                PrivateKey = RsaUtils.CreatePrivateKeyParams();

            appHost.RegisterService(typeof(EncryptedMessagesService), PublicKeyPath);

            PrivateKeyModulusMap = new Dictionary<string, RSAParameters>
            {
                { Convert.ToBase64String(PrivateKey.Value.Modulus), PrivateKey.Value },
            };
            foreach (var fallbackKey in FallbackPrivateKeys)
            {
                PrivateKeyModulusMap[Convert.ToBase64String(fallbackKey.Modulus)] = fallbackKey;
            }

            appHost.RequestConverters.Add((req, requestDto) =>
            {
                var encRequest = requestDto as EncryptedMessage;
                if (encRequest == null)
                    return null;

                var cryptKey = new byte[AesUtils.KeySizeBytes];
                var authKey = new byte[AesUtils.KeySizeBytes];
                var iv = new byte[AesUtils.BlockSizeBytes];
                const int tagLength = HmacUtils.KeySizeBytes;
                try
                {
                    var privateKey = GetPrivateKey(encRequest.KeyId);
                    if (Equals(privateKey, default(RSAParameters)))
                    {
                        WriteUnencryptedError(req, HttpError.NotFound(ErrorKeyNotFound.Fmt(encRequest.KeyId, PublicKeyPath)), "KeyNotFoundException");
                        return null;
                    }

                    var authRsaEncCryptKey = Convert.FromBase64String(encRequest.EncryptedSymmetricKey);

                    var rsaEncCryptAuthKeys = new byte[authRsaEncCryptKey.Length - iv.Length - tagLength];

                    Buffer.BlockCopy(authRsaEncCryptKey, 0, iv, 0, iv.Length);
                    Buffer.BlockCopy(authRsaEncCryptKey, iv.Length, rsaEncCryptAuthKeys, 0, rsaEncCryptAuthKeys.Length);

                    var cryptAuthKeys = RsaUtils.Decrypt(rsaEncCryptAuthKeys, privateKey);

                    Buffer.BlockCopy(cryptAuthKeys, 0, cryptKey, 0, cryptKey.Length);
                    Buffer.BlockCopy(cryptAuthKeys, cryptKey.Length, authKey, 0, authKey.Length);

                    //Needs to be after cryptKey,authKey populated
                    if (nonceCache.ContainsKey(iv))
                        throw HttpError.Forbidden(ErrorNonceSeen);

                    var now = DateTime.UtcNow;
                    nonceCache.TryAdd(iv, now.Add(MaxRequestAge));

                    if (!HmacUtils.Verify(authRsaEncCryptKey, authKey))
                        throw new Exception("EncryptedSymmetricKey is Invalid");

                    var authEncryptedBytes = Convert.FromBase64String(encRequest.EncryptedBody);

                    if (!HmacUtils.Verify(authEncryptedBytes, authKey))
                        throw new Exception("EncryptedBody is Invalid");

                    var requestBodyBytes = HmacUtils.DecryptAuthenticated(authEncryptedBytes, cryptKey);
                    var requestBody = requestBodyBytes.FromUtf8Bytes();

                    if (string.IsNullOrEmpty(requestBody))
                        throw new ArgumentNullException("EncryptedBody");

                    var parts = requestBody.SplitOnFirst(' ');
                    var unixTime = int.Parse(parts[0]);

                    var minRequestDate = now.Subtract(MaxRequestAge);
                    if (unixTime.FromUnixTime() < minRequestDate)
                        throw HttpError.Forbidden(ErrorRequestTooOld);

                    DateTime expiredEntry;
                    nonceCache.Where(x => now > x.Value).ToList()
                        .Each(entry => nonceCache.TryRemove(entry.Key, out expiredEntry));

                    parts = parts[1].SplitOnFirst(' ');
                    req.Items[Keywords.InvokeVerb] = parts[0];

                    parts = parts[1].SplitOnFirst(' ');
                    var operationName = parts[0];
                    var requestJson = parts[1];

                    var requestType = appHost.Metadata.GetOperationType(operationName);
                    var request = JsonSerializer.DeserializeFromString(requestJson, requestType);

                    req.Items[RequestItemsCryptKey] = cryptKey;
                    req.Items[RequestItemsAuthKey] = authKey;
                    req.Items[RequestItemsIv] = iv;

                    return request;
                }
                catch (Exception ex)
                {
                    WriteEncryptedError(req, cryptKey, authKey, iv, ex, ErrorInvalidMessage);
                    return null;
                }
            });

            appHost.ResponseConverters.Add((req, response) =>
            {
                object oCryptKey, oAuthKey, oIv;
                if (!req.Items.TryGetValue(RequestItemsCryptKey, out oCryptKey) ||
                    !req.Items.TryGetValue(RequestItemsAuthKey, out oAuthKey) ||
                    !req.Items.TryGetValue(RequestItemsIv, out oIv))
                    return null;

                req.Response.ClearCookies();

                var ex = response as Exception;
                if (ex != null)
                {
                    WriteEncryptedError(req, (byte[])oCryptKey, (byte[])oAuthKey, (byte[])oIv, ex);
                    return null;
                }

                if (response == null) 
                    return new EncryptedMessageResponse();

                var responseBodyBytes = response.ToJson().ToUtf8Bytes();
                var encryptedBytes = AesUtils.Encrypt(responseBodyBytes, (byte[])oCryptKey, (byte[])oIv);
                var authEncryptedBytes = HmacUtils.Authenticate(encryptedBytes, (byte[])oAuthKey, (byte[])oIv);
                var encResponse = new EncryptedMessageResponse
                {
                    EncryptedBody = Convert.ToBase64String(authEncryptedBytes)
                };
                return encResponse;
            });
        }

        private RSAParameters GetPrivateKey(string useKey)
        {
            if (useKey.IsEmpty())
                return PrivateKey.Value;

            foreach (var entry in PrivateKeyModulusMap)
            {
                if (entry.Key.StartsWith(useKey))
                    return entry.Value;
            }

            return default(RSAParameters);
        }

        // Encrypted Messaging Errors before keys can be extracted have to be written unencrypted
        private static void WriteUnencryptedError(IRequest req, Exception ex, string description = null)
        {
            var errorResponse = new ErrorResponse {
                ResponseStatus = ex.ToResponseStatus()
            };

            var httpError = ex as IHttpError;
            req.Response.StatusCode = ex.ToStatusCode();
            req.Response.StatusDescription = description ?? (httpError != null ? httpError.ErrorCode : ex.GetType().Name);

            req.Response.ContentType = MimeTypes.Json;
            req.Response.Write(errorResponse.ToJson());
            req.Response.EndRequest();
        }

        public static void WriteEncryptedError(IRequest req, byte[] cryptKey, byte[] authKey, byte[] iv, Exception ex, string description = null)
        {
            var error = new ErrorResponse
            {
                ResponseStatus = ex.ToResponseStatus()
            };

            var responseBodyBytes = error.ToJson().ToUtf8Bytes();
            var encryptedBytes = AesUtils.Encrypt(responseBodyBytes, cryptKey, iv);
            var authEncryptedBytes = HmacUtils.Authenticate(encryptedBytes, authKey, iv);

            var httpError = ex as IHttpError;

            req.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            req.Response.StatusDescription = description ?? (httpError != null ? httpError.ErrorCode : ex.GetType().Name);

            var errorResponse = new EncryptedMessageResponse
            {
                EncryptedBody = Convert.ToBase64String(authEncryptedBytes)
            };

            req.Response.ContentType = MimeTypes.Json;
            req.Response.Write(errorResponse.ToJson());
            req.Response.EndRequest();
        }
    }

    public static class EncryptedMessagesFeatureExtensions
    {
        public static bool IsEncryptedMessage(this IRequest req)
        {
            return req.Items.ContainsKey(EncryptedMessagesFeature.RequestItemsCryptKey)
                && req.Items.ContainsKey(EncryptedMessagesFeature.RequestItemsIv);
        }
    }
}