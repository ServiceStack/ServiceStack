using System;
using NUnit.Framework;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class HttpErrorSyncJsonServiceClientTests : HttpErrorSyncTests
    {
        public override IRestClient CreateRestClient(string baseUri = null)
        {
            return baseUri != null
                ? new JsonServiceClient(baseUri)
                : new JsonServiceClient();
        }
    }

    public class HttpErrorSyncJsonHttpClientTests : HttpErrorSyncTests
    {
        public override IRestClient CreateRestClient(string baseUri = null)
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

        public abstract IRestClient CreateRestClient(string baseUri = null);

        [Test]
        public void PUT_returning_custom_403_Exception()
        {
            var restClient = CreateRestClient(ListeningOn);

            try
            {
                var response = restClient.Put(new ThrowHttpError
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
            var restClient = CreateRestClient(ListeningOn);

            try
            {
                restClient.Put(new ThrowHttpErrorNoReturn
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
    }
}
