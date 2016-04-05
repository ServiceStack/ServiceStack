using System;
using System.IO;
using System.Net;
using System.Text;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class RestWebServiceTests
        : RestsTestBase
    {

        [Test]
        public void Can_call_EchoRequest_with_AcceptAll()
        {
            var response = GetWebResponse(ServiceClientBaseUri + "/echo/1/One", "*/*");
            var contents = GetContents(response);

            Assert.That(contents, Is.Not.Null);
            Assert.That(contents.Contains("\"id\":1"));
            Assert.That(contents.Contains("\"string\":\"One\""));
        }

        [Test]
        public void Can_call_EchoRequest_with_AcceptJson()
        {
            var response = GetWebResponse(ServiceClientBaseUri + "/echo/1/One", MimeTypes.Json);
            AssertResponse<EchoRequest>(response, MimeTypes.Json, x =>
            {
                Assert.That(x.Id, Is.EqualTo(1));
                Assert.That(x.String, Is.EqualTo("One"));
            });
        }

        [Test]
        public void Can_call_EchoRequest_with_AcceptXml()
        {
            var response = GetWebResponse(ServiceClientBaseUri + "/echo/1/One", MimeTypes.Xml);
            AssertResponse<EchoRequest>(response, MimeTypes.Xml, x =>
            {
                Assert.That(x.Id, Is.EqualTo(1));
                Assert.That(x.String, Is.EqualTo("One"));
            });
        }

        [Test]
        public void Can_call_EchoRequest_with_AcceptJsv()
        {
            var response = GetWebResponse(ServiceClientBaseUri + "/echo/1/One", MimeTypes.Jsv);
            AssertResponse<EchoRequest>(response, MimeTypes.Jsv, x =>
            {
                Assert.That(x.Id, Is.EqualTo(1));
                Assert.That(x.String, Is.EqualTo("One"));
            });
        }

        [Test]
        public void Can_call_EchoRequest_with_QueryString()
        {
            var response = GetWebResponse(ServiceClientBaseUri + "/echo/1/One?Long=2&Bool=True", MimeTypes.Json);
            AssertResponse<EchoRequest>(response, MimeTypes.Json, x =>
            {
                Assert.That(x.Id, Is.EqualTo(1));
                Assert.That(x.String, Is.EqualTo("One"));
                Assert.That(x.Long, Is.EqualTo(2));
                Assert.That(x.Bool, Is.EqualTo(true));
            });
        }

        private HttpWebResponse EmulateHttpMethod(string emulateMethod, string useMethod)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri + "/echomethod");
            webRequest.Accept = MimeTypes.Json;
            webRequest.Method = useMethod;
            webRequest.Headers[HttpHeaders.XHttpMethodOverride] = emulateMethod;
            if (useMethod == HttpMethods.Post)
                webRequest.ContentLength = 0;
            var response = (HttpWebResponse)webRequest.GetResponse();
            return response;
        }

        [Test]
        public void Can_emulate_Put_HttpMethod_with_POST()
        {
            var response = EmulateHttpMethod(HttpMethods.Put, HttpMethods.Post);

            AssertResponse<EchoMethodResponse>(response, MimeTypes.Json, x =>
                Assert.That(x.Result, Is.EqualTo(HttpMethods.Put)));
        }

        [Test]
        public void Can_emulate_Put_HttpMethod_with_GET()
        {
            var response = EmulateHttpMethod(HttpMethods.Put, HttpMethods.Get);

            AssertResponse<EchoMethodResponse>(response, MimeTypes.Json, x =>
                Assert.That(x.Result, Is.EqualTo(HttpMethods.Put)));
        }

        [Test]
        public void Can_emulate_Delete_HttpMethod_with_GET()
        {
            var response = EmulateHttpMethod(HttpMethods.Delete, HttpMethods.Get);

            AssertResponse<EchoMethodResponse>(response, MimeTypes.Json, x =>
                Assert.That(x.Result, Is.EqualTo(HttpMethods.Delete)));
        }

        [Test]
        public void Can_call_WildCardRequest_with_alternate_WildCard_defined()
        {
            var response = GetWebResponse(ServiceClientBaseUri + "/wildcard/1/aPath/edit", MimeTypes.Json);
            AssertResponse<WildCardRequest>(response, MimeTypes.Json, x =>
            {
                Assert.That(x.Id, Is.EqualTo(1));
                Assert.That(x.Path, Is.EqualTo("aPath"));
                Assert.That(x.Action, Is.EqualTo("edit"));
                Assert.That(x.RemainingPath, Is.Null);
            });
        }

        [Test]
        public void Can_call_WildCardRequest_WildCard_mapping()
        {
            var response = GetWebResponse(ServiceClientBaseUri + "/wildcard/1/remaining/path/to/here", MimeTypes.Json);
            AssertResponse<WildCardRequest>(response, MimeTypes.Json, x =>
            {
                Assert.That(x.Id, Is.EqualTo(1));
                Assert.That(x.Path, Is.Null);
                Assert.That(x.Action, Is.Null);
                Assert.That(x.RemainingPath, Is.EqualTo("remaining/path/to/here"));
            });
        }

        [Test]
        public void Can_call_WildCardRequest_WildCard_mapping_with_QueryString()
        {
            var response = GetWebResponse(ServiceClientBaseUri + "/wildcard/1/remaining/path/to/here?Action=edit", MimeTypes.Json);
            AssertResponse<WildCardRequest>(response, MimeTypes.Json, x =>
            {
                Assert.That(x.Id, Is.EqualTo(1));
                Assert.That(x.Path, Is.Null);
                Assert.That(x.Action, Is.EqualTo("edit"));
                Assert.That(x.RemainingPath, Is.EqualTo("remaining/path/to/here"));
            });
        }

        [Test(Description = "Test for error processing empty XML request")]
        public void Can_call_ResetMovies_mapping_with_empty_Xml_post()
        {
            var response = GetWebResponse(HttpMethods.Post, ServiceClientBaseUri + "/reset-movies", MimeTypes.Xml, 0);
            AssertResponse<ResetMoviesResponse>(response, MimeTypes.Xml, x =>
            {
            });
        }

        [Test]
        public void Can_POST_new_movie_with_FormData()
        {
            const string formData = "Id=0&ImdbId=tt0110912&Title=Pulp+Fiction&Rating=8.9&Director=Quentin+Tarantino&ReleaseDate=1994-11-24&TagLine=Girls+like+me+don't+make+invitations+like+this+to+just+anyone!&Genres=Crime%2CDrama%2CThriller";
            var formDataBytes = Encoding.UTF8.GetBytes(formData);
            var webRequest = (HttpWebRequest)WebRequest.Create(ServiceClientBaseUri + "/movies");
            webRequest.Accept = MimeTypes.Json;
            webRequest.ContentType = MimeTypes.FormUrlEncoded;
            webRequest.Method = HttpMethods.Post;
            webRequest.ContentLength = formDataBytes.Length;
            webRequest.GetRequestStream().Write(formDataBytes, 0, formDataBytes.Length);

            try
            {
                var response = (HttpWebResponse)webRequest.GetResponse();

                AssertResponse<MovieResponse>(response, MimeTypes.Json, x =>
                {
                    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
                    Assert.That(response.Headers["Location"], Is.EqualTo(ServiceClientBaseUri + "/movies/" + x.Movie.Id));
                });
            }
            catch (WebException webEx)
            {
                if (webEx.Status == WebExceptionStatus.ProtocolError)
                {
                    var errorResponse = ((HttpWebResponse)webEx.Response);
                    Console.WriteLine("Error: " + webEx);
                    Console.WriteLine("Status Code : {0}", errorResponse.StatusCode);
                    Console.WriteLine("Status Description : {0}", errorResponse.StatusDescription);

                    Console.WriteLine("Body:" + new StreamReader(errorResponse.GetResponseStream()).ReadToEnd());
                }

                throw;
            }
        }

    }

}