using System.Net;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class RequestFilterTests
    {
        private const string ServiceClientBaseUri = Config.ServiceStackBaseUri;

        [Test]
        public void Does_return_bare_401_StatusCode()
        {
            try
            {
                var webRequest = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri
                    + "/json/reply/RequestFilter?StatusCode=401");

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                webResponse.Method.Print();
                Assert.Fail("Should throw 401 WebException");
            }
            catch (WebException ex)
            {
                var httpResponse = (HttpWebResponse)ex.Response;
                Assert.That(httpResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public void Does_return_bare_401_with_AuthRequired_header()
        {
            try
            {
                var webRequest = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri
                    + "/json/reply/RequestFilter?StatusCode=401"
                    + "&HeaderName=" + HttpHeaders.WwwAuthenticate
                    + "&HeaderValue=" + "Basic realm=\"Auth Required\"".UrlEncode());

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                webResponse.Method.Print();
                Assert.Fail("Should throw 401 WebException");
            }
            catch (WebException ex)
            {
                var httpResponse = (HttpWebResponse)ex.Response;
                Assert.That(httpResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

                Assert.That(ex.Response.Headers[HttpHeaders.WwwAuthenticate],
                    Is.EqualTo("Basic realm=\"Auth Required\""));
            }
        }


        [Test]
        public void Does_return_send_401_for_access_to_ISecure_requests()
        {
            try
            {
                var webRequest = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri
                    + "/json/reply/Secure?SessionId=175BEA29-DC79-4555-BD42-C4DD5D57A004");

                var webResponse = (HttpWebResponse)webRequest.GetResponse();
                webResponse.Method.Print();
                Assert.Fail("Should throw 401 WebException");
            }
            catch (WebException ex)
            {
                var httpResponse = (HttpWebResponse)ex.Response;
                Assert.That(httpResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            }
        }
    }
}