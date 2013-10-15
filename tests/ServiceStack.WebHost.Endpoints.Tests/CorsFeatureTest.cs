using System;
using System.Threading;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/corsmethod")]
    public class CorsFeatureRequest
    {
    }

    [EnableCors("http://localhost http://localhost2", "POST, GET", "Type1, Type2", true)]
    public class CorsFeatureResponse
    {
        public bool IsSuccess { get; set; }
    }

    public class CorsFeatureService : IService
    {
        public object Any(CorsFeatureRequest request)
        {
            return new CorsFeatureResponse { IsSuccess = true };
        }
    }

    [Route("/globalcorsfeature")]
    public class GlobalCorsFeatureRequest
    {
    }

    public class GlobalCorsFeatureResponse
    {
        public bool IsSuccess { get; set; }
    }

    public class GlobalCorsFeatureService : IService
    {
        public object Any(GlobalCorsFeatureRequest request)
        {
            return new GlobalCorsFeatureResponse { IsSuccess = true };
        }
    }

    [TestFixture]
    public class CorsFeatureServiceTest
    {
        private const string ListeningOn = "http://localhost:8022/";
        private const string ServiceClientBaseUri = "http://localhost:8022/";

        public class CorsFeatureAppHostHttpListener
            : AppHostHttpListenerBase
        {
            public CorsFeatureAppHostHttpListener()
                : base("Cors Feature Tests", typeof(CorsFeatureService).Assembly) { }

            public override void Configure(Funq.Container container) {}
        }

        ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new CorsFeatureAppHostHttpListener()
                .Init()
                .Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        static IRestClient[] RestClients = 
        {
            new JsonServiceClient(ServiceClientBaseUri),
            new XmlServiceClient(ServiceClientBaseUri),
            new JsvServiceClient(ServiceClientBaseUri)
        };

        [Test, Explicit]
        public void RunFor5Mins()
        {
            Thread.Sleep(TimeSpan.FromMinutes(5));
        }

        [Test, TestCaseSource("RestClients")]
        public void CorsMethodHasAccessControlHeaders(IRestClient client)
        {
            appHost.Config.GlobalResponseHeaders.Clear();

            var response = RequestContextTests.GetResponseHeaders(ListeningOn + "/corsmethod");
            Assert.That(response[HttpHeaders.AllowOrigin], Is.EqualTo("http://localhost http://localhost2"));
            Assert.That(response[HttpHeaders.AllowMethods], Is.EqualTo("POST, GET"));
            Assert.That(response[HttpHeaders.AllowHeaders], Is.EqualTo("Type1, Type2"));
            Assert.That(response[HttpHeaders.AllowCredentials], Is.EqualTo("true"));
        }

        [Test, TestCaseSource("RestClients")]
        public void GlobalCorsHasAccessControlHeaders(IRestClient client)
        {
            appHost.LoadPlugin(new CorsFeature());

            var response = RequestContextTests.GetResponseHeaders(ListeningOn + "/globalcorsfeature");
            Assert.That(response[HttpHeaders.AllowOrigin], Is.EqualTo("*"));
            Assert.That(response[HttpHeaders.AllowMethods], Is.EqualTo("GET, POST, PUT, DELETE, OPTIONS"));
            Assert.False(response.ContainsKey(HttpHeaders.AllowCredentials));
            Assert.That(response[HttpHeaders.AllowHeaders], Is.EqualTo("Content-Type"));
        }
    }

    [Route("/corsplugin", "GET")]
    public class CorsFeaturePlugin {}

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

    public class CorsFeaturePluginTests
    {
        private const string ListeningOn = "http://localhost:8022/";

        public class CorsFeaturePluginAppHostHttpListener
            : AppHostHttpListenerBase
        {

            public CorsFeaturePluginAppHostHttpListener()
                : base("Cors Feature Tests", typeof(CorsFeatureService).Assembly) { }

            public override void Configure(Funq.Container container)
            {
                Plugins.Add(new CorsFeature { AutoHandleOptionRequests = true });
            }
        }

        ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new CorsFeaturePluginAppHostHttpListener()
                .Init()
                .Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_Get_CORS_Headers_with_OPTIONS_Request()
        {
            "{0}corsplugin".Fmt(ListeningOn).OptionsFromUrl(responseFilter: r =>
            {
                Assert.That(r.Headers[HttpHeaders.AllowOrigin], Is.EqualTo("*"));
                Assert.That(r.Headers[HttpHeaders.AllowMethods], Is.EqualTo("GET, POST, PUT, DELETE, OPTIONS"));
                Assert.That(r.Headers[HttpHeaders.AllowHeaders], Is.EqualTo("Content-Type"));
            });

            "{0}notfound".Fmt(ListeningOn).OptionsFromUrl(responseFilter: r =>
            {
                Assert.That(r.Headers[HttpHeaders.AllowOrigin], Is.EqualTo("*"));
                Assert.That(r.Headers[HttpHeaders.AllowMethods], Is.EqualTo("GET, POST, PUT, DELETE, OPTIONS"));
                Assert.That(r.Headers[HttpHeaders.AllowHeaders], Is.EqualTo("Content-Type"));
            });
        }
    }
}
