using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    class AppHostHttpListenerLongRunningBaseTests
    {
        private const string ListeningOn = "http://localhost:82/";
        ExampleAppHostHttpListenerLongRunning appHost;

        static AppHostHttpListenerLongRunningBaseTests()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
        }

        [TestFixtureSetUp]
        public void OnTestFixtureStartUp()
        {
            appHost = new ExampleAppHostHttpListenerLongRunning();
            appHost.Init();
            appHost.Start(ListeningOn);

            System.Console.WriteLine("ExampleAppHost Created at {0}, listening on {1}",
                                     DateTime.Now, ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
            appHost = null;
        }

        [Test]
        public void Root_path_redirects_to_metadata_page()
        {
            var html = ListeningOn.GetStringFromUrl();
            Assert.That(html.Contains("The following operations are supported."));
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
            var html = (ListeningOn + "_requestinfo").GetStringFromUrl();
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
            const string url = ListeningOn + "jsv/syncreply/GetFactorial?ForNumber=3&debug=true";
            var contents = url.GetStringFromUrl();


            Console.WriteLine("JSV DEBUG: " + contents);

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
                Console.WriteLine("Error [{0}]: {1}", ex.GetType().Name, ex.Message);
            }
            try
            {
                missingUrl.GetStringFromUrl();
            }
            catch (Exception ex)
            {
                errorCount++;
                Console.WriteLine("Error [{0}]: {1}", ex.GetType().Name, ex.Message);
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
                var response = client.Put<MoviesZipResponse>("movies.zip", new MoviesZip());
                Assert.Fail("Should throw 405 excetpion");
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

            var webReq = (HttpWebRequest)WebRequest.Create(url);
            webReq.Accept = "*/*";
            using (var webRes = webReq.GetResponse())
            {
                Assert.That(webRes.ContentType, Is.StringStarting(ContentType.Json));
                response = webRes.DownloadText();
            }

            Assert.That(response, Is.Not.Null, "No response received");
            Console.WriteLine(response);
            Assert.That(response, Is.StringStarting("cb("));
            Assert.That(response, Is.StringEnding(")"));
        }

        [Test, Ignore]
        public void DebugHost()
        {
            Thread.Sleep(180 * 1000);
        }

        [Test, Ignore]
        public void PerformanceTest()
        {
            const int clientCount = 500;
            var threads = new List<Thread>(clientCount);
            //ThreadPool.SetMinThreads(500, 50);
            //ThreadPool.SetMaxThreads(1000, 50);

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
    }
}