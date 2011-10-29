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
	public class RemoteEndDropsConnectionTests 
	{
		private const string ListeningOn = "http://localhost:82/";
		ExampleAppHostHttpListener appHost;

        public RemoteEndDropsConnectionTests()
		{
            LogManager.LogFactory = new TestLogFactory();
		}

		[TestFixtureSetUp]
		public void OnTestFixtureStartUp() 
		{
            

			appHost = new ExampleAppHostHttpListener();
            LogManager.LogFactory = new TestLogFactory();
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

        [SetUp]
        public void SetUp() {
            //Clear the logs to get rid of setup messages (registering services)
            TestLogger.GetLogs().Clear();
        }


        [TearDown]
        public void TearDown() {
            //Clear the logs so other tests dont inherit log entries
            TestLogger.GetLogs().Clear();
        }


        /// <summary>
        /// *Request* DTO
        /// </summary>
        [ServiceStack.ServiceHost.RestService("/test/timed", "GET")]
        public class Timed {
            /// <summary>
            /// Time for the request handler to take before returning.
            /// </summary>
            public int Milliseconds { get; set; }
        }

        public class TimedService : ServiceStack.ServiceInterface.ServiceBase<Timed> {
            protected override object Run(Timed request) {
                Thread.Sleep(request.Milliseconds);
                return true;
            }
        }

		/// <summary>
        /// This test calls a test service and then aborts the HTTP GET and verifies the host behave in the expected way.
        /// a) when the setting is to write errors to response
        /// b) when the setting is NOT to write errors to response
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void TestClientDropsConnection( bool writeErrorsToResponse ) {
            NameValueCollection settings = System.Configuration.ConfigurationManager.AppSettings;
            settings["ServiceStack.WriteErrorsToResponse"] = writeErrorsToResponse.ToString();

            int SleepMs = 1000;
            string url = string.Format("{0}/test/timed?Milliseconds={1}", ListeningOn,SleepMs);
            HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
            //Set a short timeout so we'll give up before the request is processed
            req.Timeout = 100;
            try {
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            }
            catch(Exception ex) {
                if( ex.Message != "The operation has timed out" ) {
                    throw;
                }

                //Do nothing - we are expecting a time out
            }
            
            //Sleep to give the appHost the chance to log the problems so we can investigate
            System.Threading.Thread.Sleep(SleepMs*2);

            foreach(var pair in TestLogger.GetLogs()) {
                Console.WriteLine("TEST: {0}: {1}", pair.Key, pair.Value);
            }

            if(!writeErrorsToResponse) {
                //Arguably there should be only one Error reported, but we get two. Lets check them both

                //We should get only one log entry: An ERROR from ProcessRequest
                Assert.AreEqual(2, TestLogger.GetLogs().Count(), "Checking if there is only one log entry");
                Assert.AreEqual(2, TestLogger.GetLogs().Where(o => o.Key == TestLogger.Levels.ERROR).Count(), "Checking if there is only one ERROR entry");

                StringAssert.Contains("Error in HttpListenerResponseWrapper", TestLogger.GetLogs()[0].Value, "Checking if the error is from HttpListenerResponseWrapper");
                StringAssert.Contains("ProcessRequest", TestLogger.GetLogs()[1].Value, "Checking if the error is from ProcessRequest");
            } else {
                //There is quite a lot of logging going on here and arguably there should be only one Error reported.
            }

        }
	}
}