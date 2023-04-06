using System.Net;
using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests.Issues;

[TestFixture]
public class ApiIndexCustomPathBase
{
    class AppHost : AppSelfHostBase
    {
        public AppHost()
            : base(nameof(ApiIndexCustomPathBase), typeof(ApiIndexCustomPathBase).Assembly)
        {

        }

        public override void Configure(Container container)
        {
            
        }
    }
    
    private ServiceStackHost appHost;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        appHost = new AppHost()
                .Init();
        appHost.PathBase = "/backend";
        appHost.Start(Config.ListeningOn);
    }
    
    [OneTimeTearDown]
    public void TearDown()
    {
        appHost.Dispose();
    }

    [Test]
    public void TestApiIndexWithPathBase()
    {
        var client = new JsonServiceClient(Config.ListeningOn);
        var response = client.Get<HttpWebResponse>("/backend/api");
        var responseText = response.GetResponseStream().ReadLines()
            .Join("\n");
        
        Assert.That(responseText, Is.Not.Empty);
    }
}