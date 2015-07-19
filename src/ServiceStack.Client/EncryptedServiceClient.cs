﻿// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if !(PCL || SL5)

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using ServiceStack.Text;

namespace ServiceStack
{
    public class GetPublicKey : IReturn<string> { }

    public class EncryptedMessage : IReturn<EncryptedMessageResponse>
    {
        public string EncryptedSymmetricKey { get; set; }
        public string EncryptedBody { get; set; }
    }

    public class EncryptedMessageResponse
    {
        public string EncryptedBody { get; set; }
    }

    public class EncryptedServiceClient : IEncryptedClient
    {
        public string ServerPublicKeyXml { get; private set; }
        public int Version { get; set; }
        public string SessionId { get; set; }
        public RSAParameters PublicKey { get; set; }
        public IJsonServiceClient Client { get; set; }

        public EncryptedServiceClient(IJsonServiceClient client, string publicKeyXml)
            : this(client, publicKeyXml.ToPublicRSAParameters()) {}

        public EncryptedServiceClient(IJsonServiceClient client, RSAParameters publicKey)
        {
            Client = client;
            Client.ClearCookies();
            PublicKey = publicKey;
            ServerPublicKeyXml = publicKey.ToPublicKeyXml();
        }

        public TResponse Send<TResponse>(object request)
        {
            return Send<TResponse>(HttpMethods.Post, request);
        }

        public TResponse Send<TResponse>(string httpMethod, object request)
        {
            byte[] cryptKey, authKey, iv;
            AesUtils.CreateCryptAuthKeysAndIv(out cryptKey, out authKey, out iv);

            try
            {
                var encryptedMessage = CreateEncryptedMessage(request, request.GetType().Name, cryptKey, authKey, iv, httpMethod);
                var encResponse = Client.Send(encryptedMessage);

                var authEncryptedBytes = Convert.FromBase64String(encResponse.EncryptedBody);

                if (!HmacUtils.Verify(authEncryptedBytes, authKey))
                    throw new Exception("Invalid EncryptedBody");

                var decryptedBytes = HmacUtils.DecryptAuthenticated(authEncryptedBytes, cryptKey);

                var responseJson = decryptedBytes.FromUtf8Bytes();
                var response = JsonServiceClient.FromJson<TResponse>(responseJson);

                return response;
            }
            catch (WebServiceException ex)
            {
                throw DecryptedException(ex, cryptKey, authKey);
            }
        }

        public TResponse Send<TResponse>(string httpMethod, IReturn<TResponse> request)
        {
            return Send<TResponse>(httpMethod, (object) request);
        }

        public List<TResponse> SendAll<TResponse>(IEnumerable<IReturn<TResponse>> requests)
        {
            byte[] cryptKey, authKey, iv;
            AesUtils.CreateCryptAuthKeysAndIv(out cryptKey, out authKey, out iv);

            try
            {
                var elType = requests.GetType().GetCollectionType();
                var encryptedMessage = CreateEncryptedMessage(requests, elType.Name + "[]", cryptKey, authKey, iv);
                var encResponse = Client.Send(encryptedMessage);

                var authEncryptedBytes = Convert.FromBase64String(encResponse.EncryptedBody);

                if (!HmacUtils.Verify(authEncryptedBytes, authKey))
                    throw new Exception("Invalid EncryptedBody");

                var decryptedBytes = HmacUtils.DecryptAuthenticated(authEncryptedBytes, cryptKey);

                var responseJson = decryptedBytes.FromUtf8Bytes();
                var response = JsonServiceClient.FromJson<List<TResponse>>(responseJson);

                return response;
            }
            catch (WebServiceException ex)
            {
                throw DecryptedException(ex, cryptKey, authKey);
            }
        }

        public EncryptedMessage CreateEncryptedMessage(object request, string operationName, byte[] cryptKey, byte[] authKey, byte[] iv, string verb = null)
        {
            this.PopulateRequestMetadata(request);

            if (verb == null)
                verb = HttpMethods.Post;

            var cryptAuthKeys = cryptKey.Combine(authKey);

            var rsaEncCryptAuthKeys = RsaUtils.Encrypt(cryptAuthKeys, PublicKey);
            var authRsaEncCryptAuthKeys = HmacUtils.Authenticate(rsaEncCryptAuthKeys, authKey, iv);

            var timestamp = DateTime.UtcNow.ToUnixTime();
            var requestBody = timestamp + " " + verb + " " + operationName + " " + JsonServiceClient.ToJson(request);

            var encryptedBytes = AesUtils.Encrypt(requestBody.ToUtf8Bytes(), cryptKey, iv);
            var authEncryptedBytes = HmacUtils.Authenticate(encryptedBytes, authKey, iv);

            var encryptedMessage = new EncryptedMessage
            {
                EncryptedSymmetricKey = Convert.ToBase64String(authRsaEncCryptAuthKeys),
                EncryptedBody = Convert.ToBase64String(authEncryptedBytes),
            };
            
            return encryptedMessage;
        }

        public TResponse Send<TResponse>(IReturn<TResponse> request)
        {
            return Send<TResponse>((object)request);
        }

        public void Send(IReturnVoid request)
        {
            byte[] cryptKey, authKey, iv;
            AesUtils.CreateCryptAuthKeysAndIv(out cryptKey, out authKey, out iv);

            try
            {
                var encryptedMessage = CreateEncryptedMessage(request, request.GetType().Name, cryptKey, authKey, iv);
                Client.SendOneWay(encryptedMessage);
            }
            catch (WebServiceException ex)
            {
                throw DecryptedException(ex, cryptKey, authKey);
            }
        }

        public WebServiceException DecryptedException(WebServiceException ex, byte[] cryptKey, byte[] authKey)
        {
            var encResponse = ex.ResponseDto as EncryptedMessageResponse;

            if (encResponse != null)
            {
                var authEncryptedBytes = Convert.FromBase64String(encResponse.EncryptedBody);
                if (!HmacUtils.Verify(authEncryptedBytes, authKey))
                    throw new Exception("EncryptedBody is Invalid");

                var responseBytes = HmacUtils.DecryptAuthenticated(authEncryptedBytes, cryptKey);
                var responseJson = responseBytes.FromUtf8Bytes();
                var errorResponse = JsonServiceClient.FromJson<ErrorResponse>(responseJson);

                ex.ResponseDto = errorResponse;
            }

            return ex;
        }
    }

    public static partial class ServiceClientExtensions
    {
        public static IEncryptedClient GetEncryptedClient(this IJsonServiceClient client, string serverPublicKeyXml)
        {
            if (string.IsNullOrEmpty(serverPublicKeyXml))
                throw new ArgumentNullException("serverPublicKeyXml");

            return new EncryptedServiceClient(client, serverPublicKeyXml);
        }

        public static IEncryptedClient GetEncryptedClient(this IJsonServiceClient client, RSAParameters publicKey)
        {
            return new EncryptedServiceClient(client, publicKey);
        }

        public static TResponse Get<TResponse>(this IEncryptedClient client, IReturn<TResponse> request)
        {
            return client.Send(HttpMethods.Get, request);
        }

        public static TResponse Delete<TResponse>(this IEncryptedClient client, IReturn<TResponse> request)
        {
            return client.Send(HttpMethods.Delete, request);
        }

        public static TResponse Post<TResponse>(this IEncryptedClient client, IReturn<TResponse> request)
        {
            return client.Send(HttpMethods.Post, request);
        }

        public static TResponse Put<TResponse>(this IEncryptedClient client, IReturn<TResponse> request)
        {
            return client.Send(HttpMethods.Put, request);
        }
    }
}

#endif