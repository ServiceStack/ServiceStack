using System;
using System.IO;
using System.Net;
using NUnit.Framework;
using ServiceStack.Common.Tests;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    public class RestsTestBase
        : TestBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RestsTestBase));

        readonly HostConfig defaultConfig = new HostConfig();

        public RestsTestBase()
            : base(Config.ServiceStackBaseUri, typeof(HelloService).Assembly)
        //: base("http://localhost:4000", typeof(HelloService).Assembly) //Uncomment to test on dev web server
        {
        }

        protected override void Configure(Funq.Container container) { }

        public HttpWebResponse GetWebResponse(string uri, string acceptContentTypes)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Accept = acceptContentTypes;
            return (HttpWebResponse)webRequest.GetResponse();
        }

        public static HttpWebResponse GetWebResponse(string httpMethod, string uri, string contentType, int contentLength)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Accept = contentType;
            webRequest.ContentType = contentType;
            webRequest.Method = HttpMethods.Post;
            webRequest.ContentLength = contentLength;
            return (HttpWebResponse)webRequest.GetResponse();
        }

        public static string GetContents(WebResponse webResponse)
        {
            using (var stream = webResponse.GetResponseStream())
            {
                var contents = new StreamReader(stream).ReadToEnd();
                return contents;
            }
        }

        public T DeserializeContents<T>(WebResponse webResponse)
        {
            var contentType = webResponse.ContentType ?? defaultConfig.DefaultContentType;
            return DeserializeContents<T>(webResponse, contentType);
        }

        private static T DeserializeContents<T>(WebResponse webResponse, string contentType)
        {
            try
            {
                var contents = GetContents(webResponse);
                var result = DeserializeResult<T>(webResponse, contents, contentType);
                return result;
            }
            catch (WebException webEx)
            {
                if (webEx.Status == WebExceptionStatus.ProtocolError)
                {
                    var errorResponse = ((HttpWebResponse)webEx.Response);
                    Log.Error(webEx);
                    Log.DebugFormat("Status Code : {0}", errorResponse.StatusCode);
                    Log.DebugFormat("Status Description : {0}", errorResponse.StatusDescription);

                    try
                    {
                        using (var stream = errorResponse.GetResponseStream())
                        {
                            var response = ContentTypes.Instance.DeserializeFromStream(contentType, typeof(T), stream);
                            return (T)response;
                        }
                    }
                    catch (WebException)
                    {
                        // Oh, well, we tried
                        throw;
                    }
                }

                throw;
            }
        }

        public void AssertResponse(HttpWebResponse response, string contentType)
        {
            var statusCode = (int)response.StatusCode;
            Assert.That(statusCode, Is.LessThan(400));
            Assert.That(response.ContentType.StartsWith(contentType));
        }

        public void AssertErrorResponse<T>(HttpWebResponse webResponse, HttpStatusCode statusCode, Func<T, ResponseStatus> responseStatusFn)
        {
            Assert.That(webResponse.StatusCode, Is.EqualTo(statusCode));
            var response = DeserializeContents<T>(webResponse);
            Assert.That(responseStatusFn(response).ErrorCode, Is.Not.Null);
        }

        public void AssertErrorResponse<T>(HttpWebResponse webResponse, HttpStatusCode statusCode, Func<T, ResponseStatus> responseStatusFn, string errorCode)
        {
            Assert.That(webResponse.StatusCode, Is.EqualTo(statusCode));
            var response = DeserializeContents<T>(webResponse);
            Assert.That(responseStatusFn(response).ErrorCode, Is.EqualTo(errorCode));
        }

        public void AssertErrorResponse<T>(HttpWebResponse webResponse, HttpStatusCode statusCode)
            where T : IHasResponseStatus
        {
            Assert.That(webResponse.StatusCode, Is.EqualTo(statusCode));
            var response = DeserializeContents<T>(webResponse);
            Assert.That(response.ResponseStatus.ErrorCode, Is.Not.Null);
        }

        public void AssertErrorResponse<T>(HttpWebResponse webResponse, HttpStatusCode statusCode, string errorCode)
            where T : IHasResponseStatus
        {
            Assert.That(webResponse.StatusCode, Is.EqualTo(statusCode));
            var response = DeserializeContents<T>(webResponse);
            Assert.That(response.ResponseStatus.ErrorCode, Is.EqualTo(errorCode));
        }

        public void AssertResponse<T>(HttpWebResponse response, Action<T> customAssert)
        {
            var contentType = response.ContentType ?? defaultConfig.DefaultContentType;

            AssertResponse(response, contentType);

            var result = DeserializeContents<T>(response, contentType);

            customAssert(result);
        }

        public void AssertResponse<T>(HttpWebResponse response, string contentType, Action<T> customAssert)
        {
            contentType = contentType ?? defaultConfig.DefaultContentType;

            AssertResponse(response, contentType);

            var result = DeserializeContents<T>(response, contentType);

            customAssert(result);
        }

        private static T DeserializeResult<T>(WebResponse response, string contents, string contentType)
        {
            T result;
            switch (contentType)
            {
                case MimeTypes.Xml:
                    result = XmlSerializer.DeserializeFromString<T>(contents);
                    break;

                case MimeTypes.Json:
                case MimeTypes.Json + ContentFormat.Utf8Suffix:
                    result = JsonSerializer.DeserializeFromString<T>(contents);
                    break;

                case MimeTypes.Jsv:
                    result = TypeSerializer.DeserializeFromString<T>(contents);
                    break;

                default:
                    throw new NotSupportedException(response.ContentType);
            }
            return result;
        }

    }
}