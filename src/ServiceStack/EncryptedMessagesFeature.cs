using System;
using System.Net;
using ServiceStack.Auth;
using ServiceStack.Text;
using System.Security.Cryptography;
using ServiceStack.Web;

namespace ServiceStack
{
    public class EncryptedMessage : IReturn<EncryptedMessageResponse>
    {
        public string SymmetricKeyEncrypted { get; set; }
        public string EncryptedBody { get; set; }
    }

    public class EncryptedMessageResponse
    {
        public string EncryptedBody { get; set; }
    }

    [Route("/encryptedmessages/publickey")]
    public class GetPublicKey : IReturn<string> {}

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
            var rsaParameters = HostContext.GetPlugin<EncryptedMessagesFeature>().PrivateKey;
            return rsaParameters.ToPublicKeyXml();
        }
    }

    public class EncryptedMessagesFeature : IPlugin
    {
        public static readonly string RequestItemsAesKey = "_encryptKey";
        public static readonly string RequestItemsIv = "_encryptIv";

        public RSAParameters PrivateKey { get; set; }

        public string PublicKeyPath { get; set; }

        public string PrivateKeyXml
        {
            get { return PrivateKey.FromRSAParameters(); }
            set { PrivateKey = value.ToRSAParameters(); }
        }

        public EncryptedMessagesFeature()
        {
            PublicKeyPath = "/publickey";
        }

        public void Register(IAppHost appHost)
        {
            if (Equals(PrivateKey, default(RSAParameters)))
                PrivateKey = Rsa.CreatePrivateKeyParams();

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
                    var rsaEncAesKeyBytes = Rsa.Decrypt(Convert.FromBase64String(encRequest.SymmetricKeyEncrypted), PrivateKey);

                    aesKey = new byte[Aes.KeySize / 8];
                    iv = new byte[Aes.IvSize / 8];

                    Buffer.BlockCopy(rsaEncAesKeyBytes, 0, aesKey, 0, aesKey.Length);
                    Buffer.BlockCopy(rsaEncAesKeyBytes, aesKey.Length, iv, 0, iv.Length);

                    var requestBodyBytes = Aes.Decrypt(Convert.FromBase64String(encRequest.EncryptedBody), aesKey, iv);
                    var requestBody = requestBodyBytes.FromUtf8Bytes();

                    if (string.IsNullOrEmpty(requestBody))
                        throw new ArgumentNullException("EncryptedBody");

                    var parts = requestBody.SplitOnFirst(' ');
                    var operationName = parts[0];
                    var requestJson = parts[1];

                    var requestType = appHost.Metadata.GetOperationType(operationName);
                    var request = JsonSerializer.DeserializeFromString(requestJson, requestType);

                    req.Items[RequestItemsAesKey] = aesKey;
                    req.Items[RequestItemsIv] = iv;

                    var hasSessionId = request as IHasSessionId;
                    if (hasSessionId != null)
                        req.Items[SessionFeature.RequestItemsSessionKey] = hasSessionId.SessionId;

                    return request;
                }
                catch (Exception ex)
                {
                    WriteEncryptedError(req, aesKey, iv, ex);
                    return null;
                }
            });

            appHost.ResponseConverters.Add((req, dto) =>
            {
                object oAesKey;
                object oIv;
                if (!req.Items.TryGetValue(RequestItemsAesKey, out oAesKey) ||
                    !req.Items.TryGetValue(RequestItemsIv, out oIv))
                    return null;

                var responseBody = dto.ToJson();
                var encryptedBody = Aes.Encrypt(responseBody, (byte[])oAesKey, (byte[])oIv);
                return new EncryptedMessageResponse
                {
                    EncryptedBody = encryptedBody
                };
            });
        }

        public static void WriteEncryptedError(IRequest req, byte[] aesKey, byte[] iv, Exception ex)
        {
            var error = new ErrorResponse {
                ResponseStatus = ex.ToResponseStatus()
            };

            var responseBody = error.ToJson();
            var encryptedBody = Aes.Encrypt(responseBody, aesKey, iv);

            req.Response.StatusCode = (int) HttpStatusCode.BadRequest;
            req.Response.StatusDescription = "Invalid EncryptedMessage";
            req.Response.Write(encryptedBody.ToJson());
            req.Response.EndRequest();
        }
    }
}