// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.UseCases;

public class BasicEncryptedMessagesAppHost : AppSelfHostBase
{
    public BasicEncryptedMessagesAppHost()
        : base(nameof(BasicEncryptedMessagesAppHost), typeof(BasicEncryptedMessagesService).Assembly) { }

    public override void Configure(Container container)
    {
        RequestConverters.Add((req, requestDto) => {
            if (!(requestDto is BasicEncryptedMessage encRequest))
                return null;

            var requestType = Metadata.GetOperationType(encRequest.OperationName);
            var decryptedJson = RsaUtils.Decrypt(encRequest.EncryptedBody, SecureConfig.PrivateKeyXml);
            var request = JsonSerializer.DeserializeFromString(decryptedJson, requestType);

            req.Items["_encrypt"] = encRequest;

            return Task.FromResult(request);
        });

        ResponseConverters.Add((req, response) => {
            if (!req.Items.ContainsKey("_encrypt"))
                return TypeConstants.EmptyTask;

            var encResponse = RsaUtils.Encrypt(response.ToJson(), SecureConfig.PublicKeyXml);
            return Task.FromResult((object)new BasicEncryptedMessageResponse
            {
                OperationName = response.GetType().Name,
                EncryptedBody = encResponse
            });
        });
    }
}

public class BasicEncryptedMessage : IReturn<BasicEncryptedMessageResponse>
{
    public string OperationName { get; set; }
    public string EncryptedBody { get; set; }
}

public class BasicEncryptedMessageResponse
{
    public string OperationName { get; set; }
    public string EncryptedBody { get; set; }

    public ResponseStatus ResponseStatus { get; set; }
}

public class BasicEncryptedMessagesService : Service
{
    public object Any(BasicEncryptedMessage request)
    {
        throw new NotImplementedException("Dummy method so EncryptedMessage is treated as a Service");
    }
}

[TestFixture]
public class BasicEncryptedMessagesTests
{
    private readonly ServiceStackHost appHost;

    public BasicEncryptedMessagesTests()
    {
        appHost = new BasicEncryptedMessagesAppHost()
            .Init()
            .Start(Config.AbsoluteBaseUri);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        appHost.Dispose();
    }

    [Test]
    public void Generate_Key_Pair()
    {
        var keyPair = RsaUtils.CreatePublicAndPrivateKeyPair();

        "Public Key: {0}\n".Print(keyPair.PublicKey);
        "Private Key: {0}\n".Print(keyPair.PrivateKey);
    }

    [Test]
    public void Can_Encryt_and_Decrypt_String()
    {
        var request = new HelloSecure { Name = "World" };
        var requestJson = request.ToJson();
        var encRequest = RsaUtils.Encrypt(requestJson, SecureConfig.PublicKeyXml);

        var decJson = RsaUtils.Decrypt(encRequest, SecureConfig.PrivateKeyXml);

        Assert.That(decJson, Is.EqualTo(requestJson));
    }

    [Test]
    public void Can_Send_Encrypted_Message()
    {
        var client = new JsonServiceClient(Config.AbsoluteBaseUri);

        var request = new HelloSecure { Name = "World" };
        var encRequest = RsaUtils.Encrypt(request.ToJson(), SecureConfig.PublicKeyXml);

        var encResponse = client.Post(new BasicEncryptedMessage
        {
            OperationName = typeof(HelloSecure).Name,
            EncryptedBody = encRequest
        });

        var responseJson = RsaUtils.Decrypt(encResponse.EncryptedBody, SecureConfig.PrivateKeyXml);
        var response = responseJson.FromJson<HelloSecureResponse>();

        Assert.That(response.Result, Is.EqualTo("Hello, World!"));
    }
}