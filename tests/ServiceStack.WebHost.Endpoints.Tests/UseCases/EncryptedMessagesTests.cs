using System;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests.UseCases
{
    public class SecureConfig
    {
        public static string PublicKey = "<RSAKeyValue><Modulus>s1/rrg2UxchL5O4yFKCHTaDQgr8Bfkr1kmPf8TCXUFt4WNgAxRFGJ4ap1Kc22rt/k0BRJmgC3xPIh7Z6HpYVzQroXuYI6+q66zyk0DRHG7ytsoMiGWoj46raPBXRH9Gj5hgv+E3W/NRKtMYXqq60hl1DvtGLUs2wLGv15K9NABc=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        public static string PrivateKey = "<RSAKeyValue><Modulus>s1/rrg2UxchL5O4yFKCHTaDQgr8Bfkr1kmPf8TCXUFt4WNgAxRFGJ4ap1Kc22rt/k0BRJmgC3xPIh7Z6HpYVzQroXuYI6+q66zyk0DRHG7ytsoMiGWoj46raPBXRH9Gj5hgv+E3W/NRKtMYXqq60hl1DvtGLUs2wLGv15K9NABc=</Modulus><Exponent>AQAB</Exponent><P>6CiNjgn8Ov6nodG56rCOXBoSGksYUf/2C8W23sEBfwfLtKyqTbTk3WolBj8sY8QptjwFBF4eaQiFdVLt3jg08w==</P><Q>xcuu4OGTcSOs5oYqyzsQrOAys3stMauM2RYLIWqw7JGEF1IV9LBwbaW/7foq2dG8saEI48jxcskySlDgq5dhTQ==</Q><DP>KqzhsH13ZyTOjblusox37shAEaNCOjiR8wIKJpJWAxLcyD6BI72f4G+VlLtiHoi9nikURwRCFM6jMbjnztSILw==</DP><DQ>H4CvW7XRy+VItnaL/k5r+3zB1oA51H1kM3clUq8xepw6k5RJVu17GpuZlAeSJ5sWGJxzVAQ/IG8XCWsUPYAgyQ==</DQ><InverseQ>vTLuAT3rSsoEdNwZeH2/JDEWmQ1NGa5PUq1ak1UbDD0snhsfJdLo6at3isRqEtPVsSUK6I07Nrfkd6okGhzGDg==</InverseQ><D>M8abO9lVuSVQqtsKf6O6inDB3wuNPcwbSE8l4/O3qY1Nlq96wWd0DZK0UNqXXdnDQFjPU7uwIH4QYwQMCeoejl3dZlllkyvKVa3jihImDD++qgswX2DmHGDqTIkVABf1NF730gqTmt1kqXoVp5Y+VcO7CZPEygIQyTK4WwYlRjk=</D></RSAKeyValue>";
    }

    public class EncryptedMessagesAppHost : AppSelfHostBase
    {
        public EncryptedMessagesAppHost()
            : base(typeof(EncryptedMessagesAppHost).Name, typeof(EncryptedMessagesService).Assembly) { }

        public override void Configure(Container container)
        {
            RequestConverters.Add((req, requestDto) => {
                var encRequest = requestDto as EncryptedMessage;
                if (encRequest == null)
                    return null;

                var requestType = Metadata.GetOperationType(encRequest.OperationName);
                var decryptedJson = CryptUtils.Decrypt(SecureConfig.PrivateKey, encRequest.EncryptedBody);
                var request = JsonSerializer.DeserializeFromString(decryptedJson, requestType);

                req.Items["_encrypt"] = encRequest;

                return request;
            });

            ResponseConverters.Add((req, response) => {
                if (!req.Items.ContainsKey("_encrypt"))
                    return null;

                var encResponse = CryptUtils.Encrypt(SecureConfig.PublicKey, response.ToJson());
                return new EncryptedMessageResponse
                {
                    OperationName = response.GetType().Name,
                    EncryptedBody = encResponse
                };
            });
        }
    }

    public class EncryptedMessage : IReturn<EncryptedMessageResponse>
    {
        public string OperationName { get; set; }
        public string EncryptedBody { get; set; }
    }

    public class EncryptedMessageResponse
    {
        public string OperationName { get; set; }
        public string EncryptedBody { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }

    public class HelloSecure : IReturn<HelloSecureResponse>
    {
        public string Name { get; set; }
    }

    public class HelloSecureResponse
    {
        public string Result { get; set; }
    }

    public class EncryptedMessagesService : Service
    {
        public object Any(EncryptedMessage request)
        {
            throw new NotImplementedException("Dummy method so EncryptedMessage is treated as a Service");
        }

        public object Any(HelloSecure request)
        {
            return new HelloSecureResponse { Result = "Hello, {0}!".Fmt(request.Name) };
        }
    }


    [TestFixture]
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
        public void Generate_Key_Pair()
        {
            var keyPair = CryptUtils.CreatePublicAndPrivateKeyPair();

            "Public Key: {0}\n".Print(keyPair.PublicKey);
            "Private Key: {0}\n".Print(keyPair.PrivateKey);
        }

        [Test]
        public void Can_Encryt_and_Decrypt_String()
        {
            var request = new HelloSecure { Name = "World" };
            var requestJson = request.ToJson();
            var encRequest = CryptUtils.Encrypt(SecureConfig.PublicKey, requestJson);

            var decJson = CryptUtils.Decrypt(SecureConfig.PrivateKey, encRequest);

            Assert.That(decJson, Is.EqualTo(requestJson));
        }

        [Test]
        public void Can_Send_Encrypted_Message()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            var request = new HelloSecure { Name = "World" };
            var encRequest = CryptUtils.Encrypt(SecureConfig.PublicKey, request.ToJson());

            var encResponse = client.Post(new EncryptedMessage
            {
                OperationName = typeof(HelloSecure).Name,
                EncryptedBody = encRequest
            });

            var responseJson = CryptUtils.Decrypt(SecureConfig.PrivateKey, encResponse.EncryptedBody);
            var response = responseJson.FromJson<HelloSecureResponse>();

            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }
    }

}