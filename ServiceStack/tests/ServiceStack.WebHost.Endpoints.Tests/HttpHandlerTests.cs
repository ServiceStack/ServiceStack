using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

[Route("/customresult")]
public class CustomResult { }

public class CustomXmlResult : IStreamWriterAsync, IHasOptions
{
    public IDictionary<string, string> Options { get; set; } = new Dictionary<string, string>
    {
        { "Content-Type", "application/xml" },
        { "Content-Disposition", "attachement; filename=\"file.xml\"" },
    };

    public async Task WriteToAsync(Stream responseStream, CancellationToken token = new())
    {
        await responseStream.WriteAsync("<Foo bar=\"baz\">quz</Foo>", token);
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

    [OneTimeTearDown]
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

#if !NETCORE
        protected override void OnBeginRequest(HttpListenerContext context)
        {
            Interlocked.Increment(ref BeginRequestCount);
            base.OnBeginRequest(context);
        }
#endif

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
#if NETCORE
        [Ignore("NotFoundHttpHandler is not used in .NET Core and is skipped in AppSelfHostBase.ProcessRequest")]
#endif
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
            Assert.That(ex.Message, Does.Contain("(404) Not Found"));

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
                Assert.That(res.MatchesContentType(MimeTypes.Xml));
                Assert.That(res.GetHeader(HttpHeaders.ContentDisposition), Is.EqualTo("attachement; filename=\"file.xml\""));
            });

        Assert.That(xml, Is.EqualTo("<Foo bar=\"baz\">quz</Foo>"));
    }

}