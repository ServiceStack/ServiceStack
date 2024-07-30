using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
#if NETFRAMEWORK
using ServiceStack.Host.HttpListener;
#endif
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class AppHostListenerBaseTests
{
    private const string ListeningOn = Config.BaseUriHost;
    ServiceStackHost appHost;

    static AppHostListenerBaseTests()
    {
        LogManager.LogFactory = new ConsoleLogFactory();
    }

    [OneTimeSetUp]
    public void OnTestFixtureStartUp()
    {
        appHost = new ExampleAppHostHttpListener()
            .Init()
            .Start(ListeningOn);

        "ExampleAppHost Created at {0}, listening on {1}".Print(DateTime.Now, ListeningOn);
    }

    [OneTimeTearDown]
    public void OnTestFixtureTearDown()
    {
        appHost.Dispose();
    }

    [Test]
    public void Root_path_redirects_to_metadata_page()
    {
        var html = ListeningOn.GetStringFromUrl();
        Assert.That(html.Contains("The following operations are available."));
    }

    [Test]
    public void Can_download_webpage_html_page()
    {
        var html = (ListeningOn + "webpage.html").GetStringFromUrl();
        Assert.That(html.Contains("Default index ServiceStack.WebHost.Endpoints.Tests page"));
    }

    [Test]
    public void Can_download_requestinfo_json()
    {
        var html = (ListeningOn + "?debug=requestinfo").GetStringFromUrl();
        Assert.That(html.Contains("\"Host\":"));
    }

    [Test]
    public void Gets_404_on_non_existant_page()
    {
        var webRes = (ListeningOn + "nonexistant.html").GetErrorResponse();
        Assert.That(webRes.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public void Gets_403_on_page_with_non_whitelisted_extension()
    {
        var webRes = (ListeningOn + "webpage.forbidden").GetErrorResponse();
        Assert.That(webRes.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public void Can_call_GetFactorial_WebService()
    {
        var client = new XmlServiceClient(ListeningOn);
        var request = new GetFactorial { ForNumber = 3 };
        var response = client.Send<GetFactorialResponse>(request);

        Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(request.ForNumber)));
    }

    [Test]
    public void Can_call_jsv_debug_on_GetFactorial_WebService()
    {
        var url = ListeningOn + "jsv/reply/GetFactorial?ForNumber=3&debug=true";
        var contents = url.GetStringFromUrl();

        Console.WriteLine(@"JSV DEBUG: " + contents);

        Assert.That(contents, Is.Not.Null);
    }

    [Test]
    public void Calling_missing_web_service_does_not_break_HttpListener()
    {
        var missingUrl = ListeningOn + "missing.html";
        int errorCount = 0;
        try
        {
            missingUrl.GetStringFromUrl();
        }
        catch (Exception ex)
        {
            errorCount++;
            Console.WriteLine(@"Error [{0}]: {1}", ex.GetType().Name, ex.Message);
        }
        try
        {
            missingUrl.GetStringFromUrl();
        }
        catch (Exception ex)
        {
            errorCount++;
            Console.WriteLine(@"Error [{0}]: {1}", ex.GetType().Name, ex.Message);
        }

        Assert.That(errorCount, Is.EqualTo(2));
    }

    [Test]
    public void Can_call_MoviesZip_WebService()
    {
        var client = new JsonServiceClient(ListeningOn);
        var request = new MoviesZip();
        var response = client.Send<MoviesZipResponse>(request);

        Assert.That(response.Movies.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Calling_not_implemented_method_returns_405()
    {
        var client = new JsonServiceClient(ListeningOn);
        try
        {
            var response = client.Put<MoviesZipResponse>("all-movies.zip", new MoviesZip());
            Assert.Fail("Should throw 405 exception");
        }
        catch (WebServiceException ex)
        {
            Assert.That(ex.StatusCode, Is.EqualTo(405));
        }
    }

    [Test]
    public void Can_GET_single_gethttpresult_using_RestClient_with_JSONP_from_service_returning_HttpResult()
    {
        var url = ListeningOn + "gethttpresult?callback=cb";
        string response;

#pragma warning disable CS0618, SYSLIB0014
        var webReq = WebRequest.CreateHttp(url);
#pragma warning restore CS0618, SYSLIB0014
        webReq.Accept = "*/*";
        using (var webRes = webReq.GetResponse())
        {
            Assert.That(webRes.ContentType, Does.StartWith(MimeTypes.JavaScript));
            response = webRes.ReadToEnd();
        }

        Assert.That(response, Is.Not.Null, "No response received");
        Console.WriteLine(response);
        Assert.That(response, Does.StartWith("cb("));
        Assert.That(response, Does.EndWith(")"));
    }

    [Test, Ignore("Dubug test")]
    public void DebugHost()
    {
        Thread.Sleep(180 * 1000);
    }

    [Test, Ignore("Performance test")]
    public void PerformanceTest()
    {
        const int clientCount = 500;
        var threads = new List<Thread>(clientCount);
#if NETFRAMEWORK
            ThreadPool.SetMinThreads(500, 50);
            ThreadPool.SetMaxThreads(1000, 50);
#endif           

        for (int i = 0; i < clientCount; i++)
        {
            threads.Add(new Thread(() => {
                var html = (ListeningOn + "long_running").GetStringFromUrl();
            }));
        }

        var sw = new Stopwatch();
        sw.Start();
        for (int i = 0; i < clientCount; i++)
        {
            threads[i].Start();
        }


        for (int i = 0; i < clientCount; i++)
        {
            threads[i].Join();
        }

        sw.Stop();

        Trace.TraceInformation("Elapsed time for " + clientCount + " requests : " + sw.Elapsed);
    }

#if NETFRAMEWORK
        [Test]
        public void Can_infer_handler_path_from_listener_uris()
        {
            var map = new Dictionary<string, string> {
                {"http://*:1337/",null},
                {"http://localhost:1337/",null},
                {"http://127.0.0.1:1337/",null},
                {"http://*/",null},
                {"http://localhost/",null},
                {"http://localhost:1337/subdir/","subdir"},
                {"http://localhost:1337/subdir/subdir2/","subdir/subdir2"},
            };

            foreach (var entry in map)
            {
                var handlerPath = ListenerRequest.GetHandlerPathIfAny(entry.Key);
                Assert.That(handlerPath, Is.EqualTo(entry.Value));
            }
        }
#endif

    [Test, Ignore("You have to manually check the test output if there where NullReferenceExceptions!")]
    public void Rapid_Start_Stop_should_not_cause_exceptions()
    {
        var localAppHost = new ExampleAppHostHttpListener();

        for (int i = 0; i < 100; i++)
        {
            localAppHost.Start(GetBaseAddressWithFreePort());
#if NETFRAMEWORK                
                localAppHost.Stop();
#endif
        }
    }

    private static string GetBaseAddressWithFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        if (listener.LocalEndpoint is IPEndPoint endPoint)
        {
            string address = endPoint.Address.ToString();
            int port = endPoint.Port;
            Uri uri = new UriBuilder("http://", address, port).Uri;

            listener.Stop();

            return uri.ToString();
        }

        throw new InvalidOperationException("Can not find a port to start the WpcsServer!");
    }
}