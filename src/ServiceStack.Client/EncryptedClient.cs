// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if !(PCL || SL5)

using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace ServiceStack
{
    public class GetPublicKey : IReturn<string> { }

    public class EncryptedMessage : IReturn<EncryptedMessageResponse>
    {
        public string SymmetricKeyEncrypted { get; set; }
        public string EncryptedBody { get; set; }
    }

    public class EncryptedMessageResponse
    {
        public string EncryptedBody { get; set; }
    }

    public class EncryptedServiceClient : IEncryptedClient
    {
        public string PublicKeyPath { get; set; }
        public string PublicKeyXml { get; set; }
        public RSAParameters? PublicKey { get; set; }
        public IServiceClient Client { get; set; }

        public EncryptedServiceClient(IServiceClient client)
        {
            PublicKeyPath = "/publickey";
            Client = client;
        }

        private RSAParameters GetPublicKey()
        {
            if (PublicKey == null)
            {
                if (PublicKeyXml == null)
                {
                    PublicKeyXml = Client.Get<string>(PublicKeyPath);
                }

                PublicKey = PublicKeyXml.ToPublicRSAParameters();
            }

            return PublicKey.Value;
        }

        public TResponse Send<TResponse>(object request)
        {
            using (var aes = new AesManaged { KeySize = AesUtils.KeySize })
            {
                try
                {
                    var encryptedMessage = CreateEncryptedMessage(request, aes);
                    var encResponse = Client.Send(encryptedMessage);

                    var responseJson = AesUtils.Decrypt(encResponse.EncryptedBody, aes.Key, aes.IV);
                    var response = responseJson.FromJson<TResponse>();

                    return response;
                }
                catch (WebServiceException ex)
                {
                    throw DecryptedException(ex, aes);
                }
            }
        }

        public EncryptedMessage CreateEncryptedMessage(object request, SymmetricAlgorithm aes)
        {
            var aesKeyBytes = aes.Key.Combine(aes.IV);

            var publicKey = GetPublicKey();

            var rsaEncAesKeyBytes = RsaUtils.Encrypt(aesKeyBytes, publicKey);

            var requestType = request.GetType();
            var requestBody = requestType.Name + " " + request.ToJson();

            var encryptedMessage = new EncryptedMessage
            {
                SymmetricKeyEncrypted = Convert.ToBase64String(rsaEncAesKeyBytes),
                EncryptedBody = AesUtils.Encrypt(requestBody, aes.Key, aes.IV)
            };

            return encryptedMessage;
        }

        public TResponse Send<TResponse>(IReturn<TResponse> request)
        {
            return Send<TResponse>((object)request);
        }

        public void Send(IReturnVoid request)
        {
            using (var aes = new AesManaged { KeySize = AesUtils.KeySize })
            {
                try
                {
                    var encryptedMessage = CreateEncryptedMessage(request, aes);
                    Client.SendOneWay(encryptedMessage);
                }
                catch (WebServiceException ex)
                {
                    throw DecryptedException(ex, aes);
                }
            }
        }

        public WebServiceException DecryptedException(WebServiceException ex, SymmetricAlgorithm aes)
        {
            var encResponse = ex.ResponseDto as EncryptedMessageResponse;

            if (encResponse != null)
            {
                var responseJson = AesUtils.Decrypt(encResponse.EncryptedBody, aes.Key, aes.IV);
                var errorResponse = responseJson.FromJson<ErrorResponse>();
                ex.ResponseDto = errorResponse;
            }

            return ex;
        }

        public List<TResponse> SendAll<TResponse>(IEnumerable<IReturn<TResponse>> requests)
        {
            throw new NotImplementedException();
        }
    }

    public static partial class ServiceClientExtensions
    {
        public static IEncryptedClient GetEncryptedClient(this IServiceClient client)
        {
            return new EncryptedServiceClient(client);
        }
    }
}

#endif