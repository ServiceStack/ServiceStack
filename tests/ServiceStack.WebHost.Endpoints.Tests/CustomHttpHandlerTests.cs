using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Funq;
using NUnit.Framework;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class CustomHttpHandlerAppHost : AppHostHttpListenerBase
    {
        public CustomHttpHandlerAppHost() : base("Custom Handlers", typeof (CustomHttpHandlerAppHost).Assembly)
        {
        }

        public override void Configure(Container container)
        {
            SetConfig(new EndpointHostConfig
            {
                CustomHttpHandlers = new Dictionary<HttpStatusCode, IServiceStackHttpHandler>
                {
                    {HttpStatusCode.NotFound, new SomeStandardHandler("/404")}
                }
            });
        }
    }

    // The idea here is that this handler is not specific to 404s, it is a general purpose handler
    // that can be used in many contexts, just like the RazorHandler. Therefor this handler
    // can never write specific HTTP status codes to the response stream.
    public class SomeStandardHandler : IServiceStackHttpHandler
    {
        private readonly string responseFilePath;

        public SomeStandardHandler(string responseFilePath)
        {
            this.responseFilePath = responseFilePath;
        }

        public void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName)
        {
            httpRes.Write(responseFilePath);
            httpRes.EndHttpHandlerRequest();
        }
    }

    public class CustomHttpHandlerTests
    {
        private const string ListeningOn = "http://localhost:82/";
        private CustomHttpHandlerAppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new CustomHttpHandlerAppHost();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
            appHost = null;
        }

        [Test]
        public void Custom_404_handler_returns_404_status_code()
        {
            var statusCode = (ListeningOn + "/non-existent-path").GetResponseStatus();
            Assert.That(statusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
    }
}
