using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/customresult")]
    public class CustomResult { }

    public class CustomXmlResult : IStreamWriter, IHasOptions
    {
        public IDictionary<string, string> Options { get; set; }

        public CustomXmlResult()
        {
            Options = new Dictionary<string, string>
            {
                { "Content-Type", "application/xml" },
                { "Content-Disposition", "attachement; filename=\"file.xml\"" },
            };
        }

        public void WriteTo(Stream stream)
        {
            stream.Write("<Foo bar=\"baz\">quz</Foo>");
        }
    }



    public class CustomService : Service
    {
        public object Any(CustomResult request)
        {
            return new CustomXmlResult();
        }
    }


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
                Thread.Sleep(1);
                Assert.That(EndRequestCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void Can_set_Headers_with_Custom_Result()
        {
            var xml = Config.ListeningOn.CombineWith("customresult")
                .GetStringFromUrl(responseFilter: res => {
                    Assert.That(res.ContentType, Is.EqualTo("application/xml"));
                    Assert.That(res.Headers["Content-Disposition"], Is.EqualTo("attachement; filename=\"file.xml\""));
                });

            Assert.That(xml, Is.EqualTo("<Foo bar=\"baz\">quz</Foo>"));
        }

    }
}