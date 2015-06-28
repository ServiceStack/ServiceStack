using System;
using System.Security.Cryptography;
using Funq;
using NUnit.Framework;

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

            var aes = new AesManaged { KeySize = Aes.KeySize };

            var aesKeyBytes = aes.Key.Combine(aes.IV);
            var rsaEncAesKeyBytes = Rsa.Encrypt(SecureConfig.PublicKeyXml, aesKeyBytes);

            var requestBody = typeof(HelloSecure).Name + " " + request.ToJson();

            var encryptedMessage = new EncryptedMessage
            {
                SymmetricKeyEncrypted = Convert.ToBase64String(rsaEncAesKeyBytes),
                EncryptedBody = Aes.Encrypt(requestBody, aes.Key, aes.IV)
            };
            var encResponse = client.Post(encryptedMessage);

            var responseJson = Aes.Decrypt(encResponse.EncryptedBody, aes.Key, aes.IV);
            var response = responseJson.FromJson<HelloSecureResponse>();

            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }
    }
}