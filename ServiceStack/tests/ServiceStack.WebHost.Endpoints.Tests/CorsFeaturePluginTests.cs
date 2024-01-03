using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests;

[Route("/corsplugin", "GET")]
public class CorsFeaturePlugin { }

public class CorsFeaturePluginResponse
{
    public bool IsSuccess { get; set; }
}

public class CorsFeaturePluginService : IService
{
    public object Any(CorsFeaturePlugin request)
    {
        return new CorsFeaturePluginResponse { IsSuccess = true };
    }
}

[TestFixture]
public class CorsFeaturePluginTests
{
    public class CorsFeaturePluginAppHostHttpListener
        : AppHostHttpListenerBase
    {
        public CorsFeaturePluginAppHostHttpListener()
            : base("Cors Feature Tests", typeof(CorsFeatureService).Assembly) { }

        public override void Configure(Funq.Container container)
        {
            Plugins.Add(new CorsFeature { AutoHandleOptionsRequests = true });
        }
    }

    ServiceStackHost appHost;

    [OneTimeSetUp]
    public void OnTestFixtureSetUp()
    {
        appHost = new CorsFeaturePluginAppHostHttpListener()
            .Init()
            .Start(Config.AbsoluteBaseUri);
    }

    [OneTimeTearDown]
    public void OnTestFixtureTearDown()
    {
        appHost.Dispose();
    }

    [Test]
    public void Can_Get_CORS_Headers_with_non_matching_OPTIONS_Request()
    {
        "{0}/corsplugin".Fmt(Config.ServiceStackBaseUri).OptionsFromUrl(responseFilter: r =>
        {
            Assert.That(r.GetHeader(HttpHeaders.AllowOrigin), Is.EqualTo(CorsFeature.DefaultOrigin));
            Assert.That(r.GetHeader(HttpHeaders.AllowMethods), Is.EqualTo(CorsFeature.DefaultMethods));
            Assert.That(r.GetHeader(HttpHeaders.AllowHeaders), Is.EqualTo(CorsFeature.DefaultHeaders));
        });
    }

    [Test]
    public void Can_Get_CORS_Headers_with_not_found_OPTIONS_Request()
    {
        "{0}/notfound".Fmt(Config.ServiceStackBaseUri).OptionsFromUrl(responseFilter: r =>
        {
            Assert.That(r.GetHeader(HttpHeaders.AllowOrigin), Is.EqualTo(CorsFeature.DefaultOrigin));
            Assert.That(r.GetHeader(HttpHeaders.AllowMethods), Is.EqualTo(CorsFeature.DefaultMethods));
            Assert.That(r.GetHeader(HttpHeaders.AllowHeaders), Is.EqualTo(CorsFeature.DefaultHeaders));
        });
    }
}