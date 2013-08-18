using System;
using System.IO;
using System.Net;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.ServiceClient.Web;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using System.Collections.Specialized;
using System.Linq;

namespace ServiceStack.WebHost.Endpoints.Tests
{

    [TestFixture]
	public class HttpResultContentTypeTests {
        #region setup for example plaintext service
        public class SimpleAppHostHttpListener : AppHostHttpListenerBase {
            //Tell Service Stack the name of your application and where to find your web services
            public SimpleAppHostHttpListener()
                : base("Test Services", typeof(SimpleAppHostHttpListener).Assembly) {
                LogManager.LogFactory = new TestLogFactory();
            }

            /// <summary>
            /// AppHostHttpListenerBase method.
            /// </summary>
            /// <param name="container">SS's funq container</param>
            public override void Configure(Funq.Container container) {
                EndpointHostConfig.Instance.GlobalResponseHeaders.Clear();

			//Signal advanced web browsers what HTTP Methods you accept
			//base.SetConfig(new EndpointHostConfig());
			Routes.Add<PlainText>("/test/plaintext", "GET");
            }
        }

        /// <summary>
        /// *Request* DTO
        /// </summary>
        public class PlainText {
            /// <summary>
            /// Controls if the service calls response.ContentType or just new HttpResult
            /// </summary>
            public bool SetContentTypeBrutally { get; set; }
            /// <summary>
            /// Text to respond with
            /// </summary>
            public string Text { get; set; }
        }

        public class TimedService : ServiceStack.ServiceInterface.Service
        {
            public object Any(PlainText request) 
            {
                string contentType = "text/plain";
                var response = new HttpResult(request.Text, contentType);
                if(request.SetContentTypeBrutally) {
                    response.ContentType = contentType;
                }
                return response;
            }
        }
#endregion


		private const string ListeningOn = "http://localhost:82/";
		SimpleAppHostHttpListener appHost;

        public HttpResultContentTypeTests()
		{
            
		}

		[TestFixtureSetUp]
		public void OnTestFixtureStartUp() 
		{
			appHost = new SimpleAppHostHttpListener();
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

            //Clear the logs so other tests dont inherit log entries
            TestLogger.GetLogs().Clear();
		}


        /// <summary>
        /// This test calls a simple web service which uses HttpResult(string responseText, string contentType) constructor 
        /// to set the content type. 
        /// 
        /// 
        /// </summary>
        /// <param name="setContentTypeBrutally">If true the service additionally 'brutally' sets the content type after using HttpResult constructor which should do it anyway</param>
        //This test case fails on mono 2.6.7 (the content type is 'text/html')
        [TestCase(false)]
        //This test case passes on mono 2.6.7
        [TestCase(true)]
        public void TestHttpRestulSettingContentType(bool setContentTypeBrutally) {
            string text = "Some text";
            string url = string.Format("{0}/test/plaintext?SetContentTypeBrutally={1}&Text={2}", ListeningOn, setContentTypeBrutally,text);
            HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;

            HttpWebResponse res = null;
            try {
             res = (HttpWebResponse)req.GetResponse();
            
            string downloaded;
                using(StreamReader s = new StreamReader(res.GetResponseStream())) {
                    downloaded = s.ReadToEnd();
                }

                Assert.AreEqual(text, downloaded, "Checking the downloaded string");

                Assert.AreEqual("text/plain", res.ContentType, "Checking for expected contentType" );
            }
            finally {
                if(res != null) {
                    res.Close();
                }
            }
        }
	}
}