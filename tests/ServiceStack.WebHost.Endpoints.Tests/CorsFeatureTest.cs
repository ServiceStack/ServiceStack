using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using NUnit.Framework;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.ServiceInterface.Cors;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Utils;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/corsmethod")]
    public class CorsFeatureRequest
    {
    }

    [CorsSupport(new[] { "http://localhost", "http://localhost2" }, new[] {"POST", "GET"}, new[]{"Type1", "Type2"}, true)]
    public class CorsFeatureResponse
    {
        public bool IsSuccess { get; set; }
    }

    public class CorsFeatureService : IService<CorsFeatureRequest>
    {
        public object Execute(CorsFeatureRequest request)
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

    public class GlobalCorsFeatureService : IService<GlobalCorsFeatureRequest>
    {
        public object Execute(GlobalCorsFeatureRequest request)
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

            public override void Configure(Funq.Container container)
            {
            }
        }

        CorsFeatureAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new CorsFeatureAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
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
            var response = RequestContextTests.GetResponseHeaders(ListeningOn + "/corsmethod");
            Assert.That(response[CorsFeature.AllowOriginHeader], Is.EqualTo("http://localhost http://localhost2"));
            Assert.That(response[CorsFeature.AllowMethodsHeader], Is.EqualTo("POST, GET"));
            Assert.That(response[CorsFeature.AllowHeadersHeader], Is.EqualTo("Type1, Type2"));
            Assert.That(response[CorsFeature.AllowCredentialsHeader], Is.EqualTo("true"));
        }

        [Test, TestCaseSource("RestClients")]
        public void GlobalCorsHasAccessControlHeaders(IRestClient client)
        {
            appHost.LoadPlugin(new CorsFeature());

            var response = RequestContextTests.GetResponseHeaders(ListeningOn + "/globalcorsfeature");
            Assert.That(response[CorsFeature.AllowOriginHeader], Is.EqualTo("*"));
            Assert.That(response[CorsFeature.AllowMethodsHeader], Is.EqualTo("GET, POST, PUT, DELETE, OPTIONS"));
            Assert.False(response.ContainsKey(CorsFeature.AllowCredentialsHeader));
            Assert.That(response[CorsFeature.AllowHeadersHeader], Is.EqualTo("Content-Type"));
        }

    }
}
