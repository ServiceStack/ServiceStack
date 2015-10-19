using System;
using System.Net;
using NUnit.Framework;
using ServiceStack.Model;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class HttpErrorSyncJsonServiceClientTests : HttpErrorSyncTests
    {
        public override IRestClient CreateClient(string baseUri = null)
        {
            return baseUri != null
                ? new JsonServiceClient(baseUri)
                : new JsonServiceClient();
        }
    }

    public class HttpErrorSyncJsonHttpClientTests : HttpErrorSyncTests
    {
        public override IRestClient CreateClient(string baseUri = null)
        {
            return baseUri != null
                ? new JsonHttpClient(baseUri)
                : new JsonHttpClient();
        }
    }

    [TestFixture]
    public abstract class HttpErrorSyncTests
    {
        private const string ListeningOn = "http://localhost:1337/";

        private ExampleAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new ExampleAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public abstract IRestClient CreateClient(string baseUri = null);

        [Test]
        public void PUT_returning_custom_403_Exception()
        {
            var client = CreateClient(ListeningOn);

            try
            {
                var response = client.Put(new ThrowHttpError
                {
                    StatusCode = 403,
                    Type = typeof(Exception).Name,
                    Message = "ForbiddenErrorMessage",
                });

                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(403));
                Assert.That(webEx.ResponseStatus.ErrorCode, Is.EqualTo(typeof(Exception).Name));
                Assert.That(webEx.ResponseStatus.Message, Is.EqualTo("ForbiddenErrorMessage"));
            }
        }

        [Test]
        public void PUT_throwing_custom_403_Exception()
        {
            var client = CreateClient(ListeningOn);

            try
            {
                client.Put(new ThrowHttpErrorNoReturn
                {
                    StatusCode = 403,
                    Type = typeof(Exception).Name,
                    Message = "ForbiddenErrorMessage",
                });

                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(403));
                Assert.That(webEx.ResponseStatus.ErrorCode, Is.EqualTo(typeof(Exception).Name));
                Assert.That(webEx.ResponseStatus.Message, Is.EqualTo("ForbiddenErrorMessage"));
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [Test]
        public void Throw404_does_return_404()
        {
            var client = CreateClient(ListeningOn);

            try
            {
                var response = client.Get<string>(new Throw404());
            }
            catch (WebServiceException webEx)
            {
                Assert404(webEx);
            }
        }

        [Test]
        public void Return404_does_return_404()
        {
            var client = CreateClient(ListeningOn);

            try
            {
                var response = client.Get<string>(new Return404());
            }
            catch (WebServiceException webEx)
            {
                Assert404(webEx);
            }
        }

        [Test]
        public void Return404Result_does_return_404_with_Empty_Response_Body()
        {
            var client = CreateClient(ListeningOn);

            try
            {
                var response = client.Get<string>(new Return404Result());
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(404));
                Assert.That(webEx.ResponseStatus, Is.Null);
                Assert.That(webEx.ResponseBody, Is.Null.Or.Empty);
            }
        }

        private static void Assert404(WebServiceException webEx)
        {
            Assert.That(webEx.StatusCode, Is.EqualTo(404));
            Assert.That(webEx.ResponseStatus.ErrorCode, Is.EqualTo(HttpStatusCode.NotFound.ToString()));
            Assert.That(webEx.ResponseStatus.Message, Is.EqualTo("Custom Status Description"));
        }

        [Test]
        public void ThrowCustom404_does_return_404()
        {
            var client = CreateClient(ListeningOn);

            try
            {
                var response = client.Get<string>(new ThrowCustom404());
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(404));
                Assert.That(webEx.ResponseStatus.ErrorCode, Is.EqualTo(typeof(Custom404Exception).Name));
                Assert.That(webEx.ResponseStatus.Message, Is.EqualTo("Custom Status Description"));
                Assert.That(webEx.ResponseStatus.Errors[0].ErrorCode, Is.EqualTo("FieldErrorCode"));
                Assert.That(webEx.ResponseStatus.Errors[0].Message, Is.EqualTo("FieldMessage"));
                Assert.That(webEx.ResponseStatus.Errors[0].FieldName, Is.EqualTo("FieldName"));
            }
        }
    }

    public class Custom400Exception : Exception { }

    public class Custom400SubException : Custom400Exception { }

    public class Custom401Exception : Exception, IHasStatusCode
    {
        public int StatusCode { get { return 401; } }
    }

    [TestFixture]
    public class ErrorStatusTests
    {
        [Test]
        public void Does_map_Exception_to_StatusCode()
        {
            using (new BasicAppHost
            {
                ConfigFilter = c =>
                {
                    c.MapExceptionToStatusCode[typeof(Custom400Exception)] = 400;
                }
            }.Init())
            {
                Assert.That(new Custom400Exception().ToStatusCode(), Is.EqualTo(400));
                Assert.That(new Custom400SubException().ToStatusCode(), Is.EqualTo(400));
                Assert.That(new Custom401Exception().ToStatusCode(), Is.EqualTo(401));
            }
        }
    }
}
