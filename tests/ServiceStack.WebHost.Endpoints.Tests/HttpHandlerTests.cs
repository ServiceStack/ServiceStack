using System;
using System.Net;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class HttpHandlerTests
    {
        private readonly ServiceStackHost appHost;

        public HttpHandlerTests()
        {
            appHost = new HttpHandlerAppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public static int BeginRequestCount = 0;
        public static int EndRequestCount = 0;

        public class HttpHandlerAppHost : AppSelfHostBase
        {
            public HttpHandlerAppHost() : base("HttpHandlerAppHost", typeof(PerfServices).Assembly) { }

            public override void Configure(Container container)
            {
            }

            protected override void OnBeginRequest(HttpListenerContext context)
            {
                Interlocked.Increment(ref BeginRequestCount);
                base.OnBeginRequest(context);
            }

            public override void OnEndRequest(IRequest request = null)
            {
                Interlocked.Increment(ref EndRequestCount);
                base.OnEndRequest(request);
            }
        }

        [SetUp]
        public void SetUp()
        {
            BeginRequestCount = EndRequestCount = 0;
        }

        [Test]
        public void Does_call_begin_and_end_on_Raw_HttpHandler_requests()
        {
            try
            {
                var response = Config.ListeningOn.CombineWith("/non-existing-request")
                    .GetJsonFromUrl();
                Assert.Fail("Should throw");
            }
            catch (WebException ex)
            {
                Assert.That(ex.Message, Is.StringContaining("(404) Not Found"));

                Assert.That(BeginRequestCount, Is.EqualTo(1));
                Assert.That(EndRequestCount, Is.EqualTo(1));
            }
        }

    }
}