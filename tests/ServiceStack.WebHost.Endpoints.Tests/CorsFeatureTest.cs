﻿using System;
using System.Threading;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/corsmethod")]
    [EnableCors("http://localhost http://localhost2", "POST, GET", "Type1, Type2", true)]
    public class CorsFeatureRequest { }

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
                .Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        static IRestClient[] RestClients = 
        {
            new JsonServiceClient(Config.AbsoluteBaseUri),
            new XmlServiceClient(Config.AbsoluteBaseUri),
            new JsvServiceClient(Config.AbsoluteBaseUri)
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

            var response = RequestContextTests.GetResponseHeaders(Config.ServiceStackBaseUri + "/corsmethod");
            Assert.That(response[HttpHeaders.AllowOrigin], Is.EqualTo("http://localhost http://localhost2"));
            Assert.That(response[HttpHeaders.AllowMethods], Is.EqualTo("POST, GET"));
            Assert.That(response[HttpHeaders.AllowHeaders], Is.EqualTo("Type1, Type2"));
            Assert.That(response[HttpHeaders.AllowCredentials], Is.EqualTo("true"));
        }

        [Test, TestCaseSource("RestClients")]
        public void GlobalCorsHasAccessControlHeaders(IRestClient client)
        {
            appHost.LoadPlugin(new CorsFeature { AutoHandleOptionsRequests = false });

            var response = RequestContextTests.GetResponseHeaders(Config.ServiceStackBaseUri + "/globalcorsfeature");
            Assert.That(response[HttpHeaders.AllowOrigin], Is.EqualTo("*"));
            Assert.That(response[HttpHeaders.AllowMethods], Is.EqualTo("GET, POST, PUT, DELETE, OPTIONS"));
            Assert.False(response.ContainsKey(HttpHeaders.AllowCredentials));
            Assert.That(response[HttpHeaders.AllowHeaders], Is.EqualTo("Content-Type"));
        }
    }
}
