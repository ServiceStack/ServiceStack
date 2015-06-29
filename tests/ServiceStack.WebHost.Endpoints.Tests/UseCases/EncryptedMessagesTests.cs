// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Net;
using System.Security.Cryptography;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;

namespace ServiceStack.WebHost.Endpoints.Tests.UseCases
{
    public class EncryptedMessagesAppHost : AppSelfHostBase
    {
        public EncryptedMessagesAppHost()
            : base(typeof(EncryptedMessagesAppHost).Name, typeof(SecureServices).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add(new EncryptedMessagesFeature
            {
                PrivateKeyXml = SecureConfig.PrivateKeyXml
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

    public class EncryptedMessagesTests
    {
        private readonly ServiceStackHost appHost;

        public EncryptedMessagesTests()
        {
            appHost = new EncryptedMessagesAppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_Send_Encrypted_Message()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var request = new HelloSecure { Name = "World" };

            var aes = new AesManaged { KeySize = AesUtils.KeySize };

            var aesKeyBytes = aes.Key.Combine(aes.IV);
            var rsaEncAesKeyBytes = RsaUtils.Encrypt(aesKeyBytes, SecureConfig.PublicKeyXml);

            var requestBody = typeof(HelloSecure).Name + " " + request.ToJson();

            var encryptedMessage = new EncryptedMessage
            {
                SymmetricKeyEncrypted = Convert.ToBase64String(rsaEncAesKeyBytes),
                EncryptedBody = AesUtils.Encrypt(requestBody, aes.Key, aes.IV)
            };
            var encResponse = client.Post(encryptedMessage);

            var responseJson = AesUtils.Decrypt(encResponse.EncryptedBody, aes.Key, aes.IV);
            var response = responseJson.FromJson<HelloSecureResponse>();

            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }

        static readonly IServiceClient[] JsonClients = 
        {
            new JsonServiceClient(Config.AbsoluteBaseUri),
            new JsonHttpClient(Config.AbsoluteBaseUri), 
        };

        [Test, TestCaseSource("JsonClients")]
        public void Can_Send_Encrypted_Message_with_ServiceClients(IServiceClient client)
        {
            IEncryptedClient encryptedClient = client.GetEncryptedClient();

            var response = encryptedClient.Send(new HelloSecure { Name = "World" });

            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }

        [Test, TestCaseSource("JsonClients")]
        public void Does_handle_Exceptions(IServiceClient client)
        {
            IEncryptedClient encryptedClient = client.GetEncryptedClient();

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

        [Test, TestCaseSource("JsonClients")]
        public void Can_authenticate_and_call_authenticated_Service(IServiceClient client)
        {
            IEncryptedClient encryptedClient = client.GetEncryptedClient();

            var authResponse = encryptedClient.Send(new Authenticate {
                provider = CredentialsAuthProvider.Name,
                UserName = "test@gmail.com",
                Password = "p@55word",
            });

            var response = encryptedClient.Send(new HelloAuthenticated {
                SessionId = authResponse.SessionId,
            });

            Assert.That(response.IsAuthenticated);
            Assert.That(response.Email, Is.EqualTo("test@gmail.com"));
            Assert.That(response.SessionId, Is.EqualTo(authResponse.SessionId));
        }
    }
}