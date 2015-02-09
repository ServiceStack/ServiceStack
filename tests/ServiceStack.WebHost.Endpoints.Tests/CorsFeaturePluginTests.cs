﻿using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
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

    //[Explicit]
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

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new CorsFeaturePluginAppHostHttpListener()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_Get_CORS_Headers_with_non_matching_OPTIONS_Request()
        {
            "{0}/corsplugin".Fmt(Config.ServiceStackBaseUri).OptionsFromUrl(responseFilter: r =>
                {
                    Assert.That(r.Headers[HttpHeaders.AllowOrigin], Is.EqualTo("*"));
                    Assert.That(r.Headers[HttpHeaders.AllowMethods], Is.EqualTo("GET, POST, PUT, DELETE, OPTIONS"));
                    Assert.That(r.Headers[HttpHeaders.AllowHeaders], Is.EqualTo("Content-Type"));
                });
        }

        [Test]
        public void Can_Get_CORS_Headers_with_not_found_OPTIONS_Request()
        {
            "{0}/notfound".Fmt(Config.ServiceStackBaseUri).OptionsFromUrl(responseFilter: r =>
            {
                Assert.That(r.Headers[HttpHeaders.AllowOrigin], Is.EqualTo("*"));
                Assert.That(r.Headers[HttpHeaders.AllowMethods], Is.EqualTo("GET, POST, PUT, DELETE, OPTIONS"));
                Assert.That(r.Headers[HttpHeaders.AllowHeaders], Is.EqualTo("Content-Type"));
            });
        }
    }
}