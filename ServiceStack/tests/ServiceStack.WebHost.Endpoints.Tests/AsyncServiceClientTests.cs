using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests;

public abstract class AsyncServiceClientTests
{
    ExampleAppHostHttpListener appHost;

    [OneTimeSetUp]
    public void OnTestFixtureSetUp()
    {
        appHost = new ExampleAppHostHttpListener();
        appHost.Init();
        appHost.Start(Config.ListeningOn);
    }

    [OneTimeTearDown]
    public void OnTestFixtureTearDown() => appHost.Dispose();

    protected abstract IServiceClient CreateServiceClient(string compressionType=null);

    [Test]
    public async Task Can_call_SendAsync_on_ServiceClient()
    {
        var client = CreateServiceClient();

        var request = new GetFactorial { ForNumber = 3 };
        var response = await client.SendAsync<GetFactorialResponse>(request);

        Assert.That(response, Is.Not.Null, "No response received");
        Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(request.ForNumber)));
    }


#if NET6_0_OR_GREATER
    [TestCase(CompressionTypes.Brotli)]
#endif
    [TestCase(CompressionTypes.Deflate)]
    [TestCase(CompressionTypes.GZip)]
    public async Task Can_call_SendAsync_with_compression_on_ServiceClient(string compressionType)
    {
        var jsonClient = CreateServiceClient(compressionType);

        var request = new GetFactorial { ForNumber = 3 };
        var response = await jsonClient.SendAsync<GetFactorialResponse>(request);

        Assert.That(response, Is.Not.Null, "No response received");
        Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(request.ForNumber)));
    }

#if NET6_0_OR_GREATER
    [TestFixture]
    public class JsonAsyncApiClientTests : AsyncServiceClientTests
    {
        protected override IServiceClient CreateServiceClient(string compressionType=null) => 
            new JsonApiClient(Config.ListeningOn) { RequestCompressionType = compressionType };
    }
#endif

    [TestFixture]
    public class JsonAsyncServiceClientTests : AsyncServiceClientTests
    {
        protected override IServiceClient CreateServiceClient(string compressionType=null) => 
            new JsonServiceClient(Config.ListeningOn) { RequestCompressionType = compressionType };
    }

    [TestFixture]
    public class JsonAsyncHttpClientTests : AsyncServiceClientTests
    {
        protected override IServiceClient CreateServiceClient(string compressionType=null) => 
            new JsonHttpClient(Config.ListeningOn) { RequestCompressionType = compressionType };
    }

    [TestFixture]
    public class JsvAsyncServiceClientTests : AsyncServiceClientTests
    {
        protected override IServiceClient CreateServiceClient(string compressionType=null) => 
            new JsvServiceClient(Config.ListeningOn) { RequestCompressionType = compressionType };
    }

    [TestFixture]
    public class XmlAsyncServiceClientTests : AsyncServiceClientTests
    {
        protected override IServiceClient CreateServiceClient(string compressionType=null) => 
            new XmlServiceClient(Config.ListeningOn) { RequestCompressionType = compressionType };
    }
}