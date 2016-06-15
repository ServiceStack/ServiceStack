// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Net;
using System.Security.Cryptography;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.UseCases
{
    public class EncryptedMessagesAppHost : AppSelfHostBase
    {
        public EncryptedMessagesAppHost()
            : base(typeof(EncryptedMessagesAppHost).Name, typeof(SecureServices).Assembly)
        { }

        public override void Configure(Container container)
        {
            Plugins.Add(new EncryptedMessagesFeature
            {
                PrivateKey = SecureConfig.PrivateKeyXml.ToPrivateRSAParameters(),
                FallbackPrivateKeys = {
                    SecureConfig.FallbackPrivateKeyXml.ToPrivateRSAParameters()
                },
            });

            Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                new IAuthProvider[] {
                    new CredentialsAuthProvider(AppSettings),
                }));

            container.Register<IUserAuthRepository>(c => new InMemoryAuthRepository());
            container.Resolve<IUserAuthRepository>().CreateUserAuth(
                new UserAuth { Email = "test@gmail.com" }, "p@55word");
        }
    }

    public class JsonServiceClientEncryptedMessagesTests : EncryptedMessagesTests
    {
        protected override IJsonServiceClient CreateClient()
        {
            return new JsonServiceClient(Config.AbsoluteBaseUri);
        }
    }

    public class JsonHttpClientEncryptedMessagesTests : EncryptedMessagesTests
    {
        protected override IJsonServiceClient CreateClient()
        {
            return new JsonHttpClient(Config.AbsoluteBaseUri);
        }
    }

    public abstract class EncryptedMessagesTests
    {
        private readonly ServiceStackHost appHost;

        protected EncryptedMessagesTests()
        {
            appHost = new EncryptedMessagesAppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            ((IClearable)appHost.TryResolve<IAuthRepository>()).Clear(); //Flush InMemoryAuthProvider
            appHost.Dispose();
        }

        protected abstract IJsonServiceClient CreateClient();

        [Test]
        public void Can_Send_Encrypted_Message_with_ServiceClients()
        {
            var client = CreateClient();
            IEncryptedClient encryptedClient = client.GetEncryptedClient(client.Get(new GetPublicKey()));

            var response = encryptedClient.Send(new HelloSecure { Name = "World" });

            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }

        [Test]
        public void Can_Send_Encrypted_OneWay_Message_with_ServiceClients()
        {
            var client = CreateClient();
            IEncryptedClient encryptedClient = client.GetEncryptedClient(client.Get(new GetPublicKey()));

            encryptedClient.Send(new HelloOneWay { Name = "World" });

            Assert.That(HelloOneWay.LastName, Is.EqualTo("World"));
        }

        [Test]
        public void Can_authenticate_and_call_authenticated_Service()
        {
            try
            {
                var client = CreateClient();
                IEncryptedClient encryptedClient = client.GetEncryptedClient(client.Get<string>("/publickey"));

                var authResponse = encryptedClient.Send(new Authenticate
                {
                    provider = CredentialsAuthProvider.Name,
                    UserName = "test@gmail.com",
                    Password = "p@55word",
                });

                var encryptedClientCookies = client.GetCookieValues();
                Assert.That(encryptedClientCookies.Count, Is.EqualTo(0));

                var response = encryptedClient.Send(new HelloAuthenticated
                {
                    SessionId = authResponse.SessionId,
                });

                Assert.That(response.IsAuthenticated);
                Assert.That(response.Email, Is.EqualTo("test@gmail.com"));
                Assert.That(response.SessionId, Is.EqualTo(authResponse.SessionId));

                encryptedClientCookies = client.GetCookieValues();
                Assert.That(encryptedClientCookies.Count, Is.EqualTo(0));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [Test]
        public void Does_populate_Request_metadata()
        {
            var client = CreateClient();
            IEncryptedClient encryptedClient = client.GetEncryptedClient(client.Get<string>("/publickey"));

            var authResponse = encryptedClient.Send(new Authenticate
            {
                provider = CredentialsAuthProvider.Name,
                UserName = "test@gmail.com",
                Password = "p@55word",
            });

            var encryptedClientCookies = client.GetCookieValues();
            Assert.That(encryptedClientCookies.Count, Is.EqualTo(0));

            encryptedClient.Version = 1;
            encryptedClient.SessionId = authResponse.SessionId;

            var response = encryptedClient.Send(new HelloAuthenticated());
            Assert.That(response.SessionId, Is.EqualTo(encryptedClient.SessionId));
            Assert.That(response.Version, Is.EqualTo(encryptedClient.Version));

            encryptedClientCookies = client.GetCookieValues();
            Assert.That(encryptedClientCookies.Count, Is.EqualTo(0));

            client.SessionId = authResponse.SessionId;
            client.Version = 2;

            response = client.Send(new HelloAuthenticated());
            Assert.That(response.SessionId, Is.EqualTo(client.SessionId));
            Assert.That(response.Version, Is.EqualTo(client.Version));
        }

        [Test]
        public void Can_Authenticate_then_call_AuthOnly_Services_with_ServiceClients_Temp()
        {
            var client = CreateClient();
            IEncryptedClient encryptedClient = client.GetEncryptedClient(client.Get<string>("/publickey"));

            var authResponse = encryptedClient.Send(new Authenticate
            {
                provider = CredentialsAuthProvider.Name,
                UserName = "test@gmail.com",
                Password = "p@55word",
            });

            client.SetCookie("ss-id", authResponse.SessionId);
            var response = client.Get(new HelloAuthSecure { Name = "World" });
        }

        [Test]
        public void Can_Authenticate_then_call_AuthOnly_Services_with_ServiceClients_Perm()
        {
            var client = CreateClient();
            IEncryptedClient encryptedClient = client.GetEncryptedClient(client.Get<string>("/publickey"));

            var authResponse = encryptedClient.Send(new Authenticate
            {
                provider = CredentialsAuthProvider.Name,
                UserName = "test@gmail.com",
                Password = "p@55word",
                RememberMe = true,
            });

            client.SetCookie("ss-pid", authResponse.SessionId);
            client.SetCookie("ss-opt", "perm");
            var response = client.Get(new HelloAuthSecure { Name = "World" });
        }

        [Test]
        public void Does_handle_Exceptions()
        {
            var client = CreateClient();
            IEncryptedClient encryptedClient = client.GetEncryptedClient(client.Get<string>("/publickey"));

            try
            {
                var response = encryptedClient.Send(new HelloSecure());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ResponseStatus.ErrorCode, Is.EqualTo(typeof(ArgumentNullException).Name));
                Assert.That(ex.ResponseStatus.Message, Is.EqualTo("Value cannot be null.\r\nParameter name: Name"));
            }

            try
            {
                var response = encryptedClient.Send(new HelloAuthenticated());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Assert.That(ex.StatusDescription, Is.EqualTo("Unauthorized"));
            }
        }

        [Test]
        public void Can_call_GET_only_Services()
        {
            var client = CreateClient();
            IEncryptedClient encryptedClient = client.GetEncryptedClient(client.Get<string>("/publickey"));

            var response = encryptedClient.Get(new GetSecure { Name = "World" });

            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }

        [Test]
        public void Can_send_large_messages()
        {
            var client = CreateClient();
            IEncryptedClient encryptedClient = client.GetEncryptedClient(client.Get<string>("/publickey"));

            var request = new LargeMessage
            {
                Messages = 100.Times(i => new HelloSecure { Name = "Name" + i })
            };

            var response = encryptedClient.Send(request);

            Assert.That(response.Messages.Count, Is.EqualTo(request.Messages.Count));
        }

        [Test]
        public void Can_send_auto_batched_requests()
        {
            var client = CreateClient();
            IEncryptedClient encryptedClient = client.GetEncryptedClient(client.Get<string>("/publickey"));

            var names = new[] { "Foo", "Bar", "Baz" };
            var requests = names.Map(x => new HelloSecure { Name = x });

            var responses = encryptedClient.SendAll(requests);
            var responseNames = responses.Map(x => x.Result);

            Assert.That(responseNames, Is.EqualTo(names.Map(x => "Hello, {0}!".Fmt(x))));
        }

        [Test]
        public void Can_send_PublishAll_requests()
        {
            var client = CreateClient();
            IEncryptedClient encryptedClient = client.GetEncryptedClient(client.Get<string>("/publickey"));

            var names = new[] { "Foo", "Bar", "Baz" };
            var requests = names.Map(x => new HelloSecure { Name = x });

            encryptedClient.PublishAll(requests);
        }

        [Test]
        public void Can_Send_Encrypted_Message()
        {
            var client = CreateClient();

            var request = new HelloSecure { Name = "World" };

            byte[] cryptKey, authKey, iv;
            AesUtils.CreateCryptAuthKeysAndIv(out cryptKey, out authKey, out iv);

            var cryptAuthKeys = cryptKey.Combine(authKey);

            var rsaEncCryptAuthKeys = RsaUtils.Encrypt(cryptAuthKeys, SecureConfig.PublicKeyXml);
            var authRsaEncCryptAuthKeys = HmacUtils.Authenticate(rsaEncCryptAuthKeys, authKey, iv);

            var timestamp = DateTime.UtcNow.ToUnixTime();
            var requestBody = timestamp + " POST " + typeof(HelloSecure).Name + " " + request.ToJson();

            var encryptedBytes = AesUtils.Encrypt(requestBody.ToUtf8Bytes(), cryptKey, iv);
            var authEncryptedBytes = HmacUtils.Authenticate(encryptedBytes, authKey, iv);

            var encryptedMessage = new EncryptedMessage
            {
                EncryptedSymmetricKey = Convert.ToBase64String(authRsaEncCryptAuthKeys),
                EncryptedBody = Convert.ToBase64String(authEncryptedBytes),
            };

            var encResponse = client.Post(encryptedMessage);

            authEncryptedBytes = Convert.FromBase64String(encResponse.EncryptedBody);

            if (!HmacUtils.Verify(authEncryptedBytes, authKey))
                throw new Exception("Invalid EncryptedBody");

            var decryptedBytes = HmacUtils.DecryptAuthenticated(authEncryptedBytes, cryptKey);

            var responseJson = decryptedBytes.FromUtf8Bytes();
            var response = responseJson.FromJson<HelloSecureResponse>();

            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }

        [Test]
        public void Does_throw_on_old_messages()
        {
            var client = CreateClient();

            var request = new HelloSecure { Name = "World" };

            byte[] cryptKey, authKey, iv;
            AesUtils.CreateCryptAuthKeysAndIv(out cryptKey, out authKey, out iv);

            var cryptAuthKeys = cryptKey.Combine(authKey);

            var rsaEncCryptAuthKeys = RsaUtils.Encrypt(cryptAuthKeys, SecureConfig.PublicKeyXml);
            var authRsaEncCryptAuthKeys = HmacUtils.Authenticate(rsaEncCryptAuthKeys, authKey, iv);

            var timestamp = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(21)).ToUnixTime();

            var requestBody = timestamp + " POST " + typeof(HelloSecure).Name + " " + request.ToJson();

            var encryptedBytes = AesUtils.Encrypt(requestBody.ToUtf8Bytes(), cryptKey, iv);
            var authEncryptedBytes = HmacUtils.Authenticate(encryptedBytes, authKey, iv);

            try
            {
                var encryptedMessage = new EncryptedMessage
                {
                    EncryptedSymmetricKey = Convert.ToBase64String(authRsaEncCryptAuthKeys),
                    EncryptedBody = Convert.ToBase64String(authEncryptedBytes),
                };
                var encResponse = client.Post(encryptedMessage);

                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                ex.StatusDescription.Print();

                var errorResponse = (EncryptedMessageResponse)ex.ResponseDto;

                authEncryptedBytes = Convert.FromBase64String(errorResponse.EncryptedBody);
                if (!HmacUtils.Verify(authEncryptedBytes, authKey))
                    throw new Exception("EncryptedBody is Invalid");

                var responseBytes = HmacUtils.DecryptAuthenticated(authEncryptedBytes, cryptKey);
                var responseJson = responseBytes.FromUtf8Bytes();
                var response = responseJson.FromJson<ErrorResponse>();
                Assert.That(response.ResponseStatus.Message, Is.EqualTo("Request too old"));
            }
        }

        [Test]
        public void Does_throw_on_replayed_messages()
        {
            var client = CreateClient();

            var request = new HelloSecure { Name = "World" };

            byte[] cryptKey, iv;
            AesUtils.CreateKeyAndIv(out cryptKey, out iv);

            byte[] authKey = AesUtils.CreateKey();

            var cryptAuthKeys = cryptKey.Combine(authKey);

            var rsaEncCryptAuthKeys = RsaUtils.Encrypt(cryptAuthKeys, SecureConfig.PublicKeyXml);
            var authRsaEncCryptAuthKeys = HmacUtils.Authenticate(rsaEncCryptAuthKeys, authKey, iv);

            var timestamp = DateTime.UtcNow.ToUnixTime();
            var requestBody = timestamp + " POST " + typeof(HelloSecure).Name + " " + request.ToJson();

            var encryptedBytes = AesUtils.Encrypt(requestBody.ToUtf8Bytes(), cryptKey, iv);
            var authEncryptedBytes = HmacUtils.Authenticate(encryptedBytes, authKey, iv);

            var encryptedMessage = new EncryptedMessage
            {
                EncryptedSymmetricKey = Convert.ToBase64String(authRsaEncCryptAuthKeys),
                EncryptedBody = Convert.ToBase64String(authEncryptedBytes),
            };

            var encResponse = client.Post(encryptedMessage);

            try
            {
                client.Post(encryptedMessage);

                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                ex.StatusDescription.Print();

                var errorResponse = (EncryptedMessageResponse)ex.ResponseDto;

                authEncryptedBytes = Convert.FromBase64String(errorResponse.EncryptedBody);
                if (!HmacUtils.Verify(authEncryptedBytes, authKey))
                    throw new Exception("EncryptedBody is Invalid");

                var responseBytes = HmacUtils.DecryptAuthenticated(authEncryptedBytes, cryptKey);
                var responseJson = responseBytes.FromUtf8Bytes();
                var response = responseJson.FromJson<ErrorResponse>();
                Assert.That(response.ResponseStatus.Message, Is.EqualTo("Nonce already seen"));
            }
        }

        [Test]
        public void Can_send_encrypted_messages_with_old_registered_PublicKey()
        {
            var client = CreateClient();
            var encryptedClient = client.GetEncryptedClient(SecureConfig.FallbackPublicKeyXml);

            var response = encryptedClient.Send(new HelloSecure { Name = "Fallback Key" });

            Assert.That(response.Result, Is.EqualTo("Hello, Fallback Key!"));
        }

        [Test]
        public void Fails_when_sending_invalid_KeyId()
        {
            var client = CreateClient();
            var encryptedClient = (EncryptedServiceClient)client.GetEncryptedClient(SecureConfig.FallbackPublicKeyXml);
            encryptedClient.KeyId = "AAAAAA";

            try
            {
                var response = encryptedClient.Send(new HelloSecure { Name = "Fallback Key" });
                Assert.Fail("Should Throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.NotFound));
                Assert.That(ex.StatusDescription, Is.EqualTo("KeyNotFoundException"));
                Assert.That(ex.ResponseStatus.Message, Is.StringStarting(EncryptedMessagesFeature.ErrorKeyNotFound.Substring(0,10)));
            }
        }
    }

    public class CryptUtilsTests
    {
        [Test]
        public void Can_Encrypt_and_Decrypt_with_AES()
        {
            var msg = new HelloSecure { Name = "World" };

            byte[] cryptKey, iv;
            AesUtils.CreateKeyAndIv(out cryptKey, out iv);

            var encryptedText = AesUtils.Encrypt(msg.ToJson(), cryptKey, iv);

            var decryptedJson = AesUtils.Decrypt(encryptedText, cryptKey, iv);

            var decryptedMsg = decryptedJson.FromJson<HelloSecure>();

            Assert.That(decryptedMsg.Name, Is.EqualTo(msg.Name));
        }

        [Test]
        public void Can_Encrypt_and_Decrypt_with_AES_bytes()
        {
            var msg = new HelloSecure { Name = "World" };

            byte[] cryptKey, iv;
            AesUtils.CreateKeyAndIv(out cryptKey, out iv);

            var encryptedBytes = AesUtils.Encrypt(msg.ToJson().ToUtf8Bytes(), cryptKey, iv);

            var msgBytes = AesUtils.Decrypt(encryptedBytes, cryptKey, iv);

            var decryptedMsg = msgBytes.FromUtf8Bytes().FromJson<HelloSecure>();

            Assert.That(decryptedMsg.Name, Is.EqualTo(msg.Name));
        }

        [Test]
        public void Does_Hybrid_RSA_Crypt_and_Auth_AES_with_HMAC_SHA256()
        {
            var request = new HelloSecure { Name = "World" };
            var timestamp = DateTime.UtcNow.ToUnixTime();
            var msg = timestamp + " POST " + request.GetType().Name + " " + request.ToJson();
            var msgBytes = msg.ToUtf8Bytes();

            byte[] cryptKey, authKey, iv;
            AesUtils.CreateCryptAuthKeysAndIv(out cryptKey, out authKey, out iv);

            var encryptedBytes = AesUtils.Encrypt(msgBytes, cryptKey, iv);

            var decryptedBytes = AesUtils.Decrypt(encryptedBytes, cryptKey, iv);
            Assert.That(decryptedBytes, Is.EquivalentTo(msgBytes));

            var authEncryptedBytes = HmacUtils.Authenticate(encryptedBytes, authKey, iv);

            var cryptAuthKeys = cryptKey.Combine(authKey);

            var rsaEncCryptAuthKeys = RsaUtils.Encrypt(cryptAuthKeys, SecureConfig.PublicKeyXml);
            var authRsaEncCryptAuthKeys = HmacUtils.Authenticate(rsaEncCryptAuthKeys, authKey, iv);

            var decryptedMsg = ValidateAndDecrypt(authRsaEncCryptAuthKeys, authEncryptedBytes);

            var parts = decryptedMsg.SplitOnFirst(' ');
            Assert.That(long.Parse(parts[0]), Is.EqualTo(timestamp));

            parts = parts[1].SplitOnFirst(' ');
            Assert.That(parts[0], Is.EqualTo("POST"));

            parts = parts[1].SplitOnFirst(' ');
            Assert.That(parts[0], Is.EqualTo(request.GetType().Name));

            var decryptedJson = parts[1];
            var decryptedRequest = decryptedJson.FromJson<HelloSecure>();

            Assert.That(decryptedRequest.Name, Is.EqualTo(request.Name));
        }

        private static string ValidateAndDecrypt(byte[] authRsaEncCryptKey, byte[] authEncryptedBytes)
        {
            byte[] iv = new byte[AesUtils.BlockSizeBytes];
            const int tagLength = HmacUtils.KeySizeBytes;
            byte[] rsaEncCryptAuthKeys = new byte[authRsaEncCryptKey.Length - iv.Length - tagLength];

            Buffer.BlockCopy(authRsaEncCryptKey, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(authRsaEncCryptKey, iv.Length, rsaEncCryptAuthKeys, 0, rsaEncCryptAuthKeys.Length);

            var cryptAuthKeys = RsaUtils.Decrypt(rsaEncCryptAuthKeys, SecureConfig.PrivateKeyXml);

            byte[] cryptKey = new byte[AesUtils.KeySizeBytes];
            byte[] authKey = new byte[AesUtils.KeySizeBytes];

            Buffer.BlockCopy(cryptAuthKeys, 0, cryptKey, 0, cryptKey.Length);
            Buffer.BlockCopy(cryptAuthKeys, cryptKey.Length, authKey, 0, authKey.Length);

            if (!HmacUtils.Verify(authRsaEncCryptKey, authKey))
                throw new Exception("authRsaEncCryptKey is Invalid");

            if (!HmacUtils.Verify(authEncryptedBytes, authKey))
                throw new Exception("authEncryptedBytes is Invalid");

            var msgBytes = HmacUtils.DecryptAuthenticated(authEncryptedBytes, cryptKey);

            return msgBytes.FromUtf8Bytes();
        }

        //Alternate approach use Master Key with SHA-512 to create Crypt + Auth Keys

        [Test]
        public void Does_Hybrid_RSA_SHA512_AES_MasterKey_and_HmacSha256()
        {
            var request = new HelloSecure { Name = "World" };
            var msgBytes = request.ToJson().ToUtf8Bytes();

            byte[] masterKey, iv;
            AesUtils.CreateKeyAndIv(out masterKey, out iv);

            var sha512KeyBytes = masterKey.ToSha512HashBytes();

            var cryptKey = new byte[sha512KeyBytes.Length / 2];
            var authKey = new byte[sha512KeyBytes.Length / 2];

            Buffer.BlockCopy(sha512KeyBytes, 0, cryptKey, 0, cryptKey.Length);
            Buffer.BlockCopy(sha512KeyBytes, cryptKey.Length, authKey, 0, authKey.Length);

            var encryptedBytes = AesUtils.Encrypt(msgBytes, cryptKey, iv);
            var authEncryptedBytes = HmacUtils.Authenticate(encryptedBytes, authKey, iv);

            var aesKeyNonceBytes = iv.Combine(masterKey);
            var rsaEncAesKeyNonceBytes = RsaUtils.Encrypt(aesKeyNonceBytes, SecureConfig.PublicKeyXml);

            var json = ValidateAndDecryptWithMasterKey(rsaEncAesKeyNonceBytes, authEncryptedBytes);

            var fromJson = json.FromJson<HelloSecure>();

            Assert.That(fromJson.Name, Is.EqualTo(request.Name));
        }

        private static string ValidateAndDecryptWithMasterKey(byte[] rsaEncAesKeyNonceBytes, byte[] authEncryptedBytes)
        {
            var aesKeyNonceBytes = RsaUtils.Decrypt(rsaEncAesKeyNonceBytes, SecureConfig.PrivateKeyXml);

            var aesKey = new byte[AesUtils.KeySizeBytes];
            var iv = new byte[AesUtils.BlockSizeBytes];

            Buffer.BlockCopy(aesKeyNonceBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(aesKeyNonceBytes, iv.Length, aesKey, 0, aesKey.Length);

            var sha512HashBytes = aesKey.ToSha512HashBytes();
            var cryptKey = new byte[sha512HashBytes.Length / 2];
            var authKey = new byte[sha512HashBytes.Length / 2];

            Buffer.BlockCopy(sha512HashBytes, 0, cryptKey, 0, cryptKey.Length);
            Buffer.BlockCopy(sha512HashBytes, cryptKey.Length, authKey, 0, authKey.Length);

            if (!HmacUtils.Verify(authEncryptedBytes, authKey))
                throw new Exception("Verification Failed");

            var msgBytes = HmacUtils.DecryptAuthenticated(authEncryptedBytes, cryptKey);

            var json = msgBytes.FromUtf8Bytes();
            return json;
        }
    }
}