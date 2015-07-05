// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Concurrent;
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
        public static readonly string RequestItemsAesKey = "_encryptKey";
        public static readonly string RequestItemsIv = "_encryptIv";
        public static readonly TimeSpan DefaultMaxMaxRequestAge = TimeSpan.FromMinutes(20);

        public static string ErrorInvalidMessage = "Invalid EncryptedMessage";
        public static string ErrorNonceSeen = "Nonce already seen";
        public static string ErrorRequestTooOld = "Request too old";

        private readonly ConcurrentDictionary<byte[], DateTime> nonceCache = new ConcurrentDictionary<byte[], DateTime>(ByteArrayComparer.Instance);

        public RSAParameters? PrivateKey { get; set; }

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
        }

        public void Register(IAppHost appHost)
        {
            if (PrivateKey == null)
                PrivateKey = RsaUtils.CreatePrivateKeyParams();

            appHost.RegisterService(typeof(EncryptedMessagesService), PublicKeyPath);

            appHost.RequestConverters.Add((req, requestDto) =>
            {
                var encRequest = requestDto as EncryptedMessage;
                if (encRequest == null)
                    return null;

                byte[] aesKey = null;
                byte[] iv = null;
                try
                {
                    var rsaEncAesKeyBytes = RsaUtils.Decrypt(Convert.FromBase64String(encRequest.EncryptedSymmetricKey), PrivateKey.Value);

                    aesKey = new byte[AesUtils.KeySize / 8];
                    iv = new byte[AesUtils.IvSize / 8];

                    Buffer.BlockCopy(rsaEncAesKeyBytes, 0, aesKey, 0, aesKey.Length);
                    Buffer.BlockCopy(rsaEncAesKeyBytes, aesKey.Length, iv, 0, iv.Length);

                    if (nonceCache.ContainsKey(iv))
                        throw HttpError.Forbidden(ErrorNonceSeen);

                    var now = DateTime.UtcNow;
                    nonceCache.TryAdd(iv, now.Add(MaxRequestAge));

                    var requestBodyBytes = AesUtils.Decrypt(Convert.FromBase64String(encRequest.EncryptedBody), aesKey, iv);
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

                    req.Items[RequestItemsAesKey] = aesKey;
                    req.Items[RequestItemsIv] = iv;

                    return request;
                }
                catch (Exception ex)
                {
                    WriteEncryptedError(req, aesKey, iv, ex, ErrorInvalidMessage);
                    return null;
                }
            });

            appHost.ResponseConverters.Add((req, response) =>
            {
                object oAesKey;
                object oIv;
                if (!req.Items.TryGetValue(RequestItemsAesKey, out oAesKey) ||
                    !req.Items.TryGetValue(RequestItemsIv, out oIv))
                    return null;

                req.Response.ClearCookies();

                var ex = response as Exception;
                if (ex != null)
                {
                    WriteEncryptedError(req, (byte[])oAesKey, (byte[])oIv, ex);
                    return null;
                }

                var responseBody = response.ToJson();
                var encryptedBody = AesUtils.Encrypt(responseBody, (byte[])oAesKey, (byte[])oIv);
                return new EncryptedMessageResponse
                {
                    EncryptedBody = encryptedBody
                };
            });
        }

        public static void WriteEncryptedError(IRequest req, byte[] aesKey, byte[] iv, Exception ex, string description = null)
        {
            var error = new ErrorResponse {
                ResponseStatus = ex.ToResponseStatus()
            };

            var responseBody = error.ToJson();
            var encryptedBody = AesUtils.Encrypt(responseBody, aesKey, iv);

            var httpError = ex as IHttpError;

            req.Response.StatusCode = (int) HttpStatusCode.BadRequest;
            req.Response.StatusDescription = description ?? (httpError != null ? httpError.ErrorCode : ex.GetType().Name);

            var errorResponse = new EncryptedMessageResponse
            {
                EncryptedBody = encryptedBody
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
            return req.Items.ContainsKey(EncryptedMessagesFeature.RequestItemsAesKey)
                && req.Items.ContainsKey(EncryptedMessagesFeature.RequestItemsIv);
        }
    }
}