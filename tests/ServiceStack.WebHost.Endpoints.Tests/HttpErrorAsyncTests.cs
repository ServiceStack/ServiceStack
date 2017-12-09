using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class HttpErrorAsyncJsonServiceClientTests : HttpErrorAsyncTests
    {
        public override IRestClientAsync CreateRestClient(string baseUri = null)
        {
            return baseUri != null
                ? new JsonServiceClient(baseUri)
                : new JsonServiceClient();
        }
    }

    public class HttpErrorAsyncJsonHttpClientTests : HttpErrorAsyncTests
    {
        public override IRestClientAsync CreateRestClient(string baseUri = null)
        {
            return baseUri != null
                ? new JsonHttpClient(baseUri)
                : new JsonHttpClient();
        }
    }


    public abstract class HttpErrorAsyncTests
    {
        private const string ListeningOn = "http://localhost:1337/";

        ExampleAppHostHttpListener appHost;

        [OneTimeSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new ExampleAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [OneTimeTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public abstract IRestClientAsync CreateRestClient(string baseUri = null);

        [Test]
        public async Task GET_returns_ArgumentNullException()
        {
            var restClient = CreateRestClient();
            try
            {
                var response = await restClient.GetAsync<ThrowHttpErrorResponse>(ListeningOn + "errors");

                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(400));
                Assert.That(webEx.ResponseStatus.ErrorCode, Is.EqualTo(typeof(ArgumentNullException).Name));
            }
        }

        [Test]
        public async Task GET_returns_custom_Exception_and_StatusCode()
        {
            var restClient = CreateRestClient();
            try
            {
                var response = await restClient.GetAsync<ThrowHttpErrorResponse>(
                    ListeningOn + "errors/FileNotFoundException/404");

                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(404));
                Assert.That(webEx.ResponseStatus.ErrorCode, Is.EqualTo(typeof(FileNotFoundException).Name));
            }
        }

        [Test]
        public async Task GET_returns_custom_Exception_Message_and_StatusCode()
        {
            var restClient = CreateRestClient();

            try
            {
                var response = await restClient.GetAsync<ThrowHttpErrorResponse>(
                    ListeningOn + "errors/FileNotFoundException/404/ClientErrorMessage");

                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(404));
                Assert.That(webEx.ResponseStatus.ErrorCode, Is.EqualTo(typeof(FileNotFoundException).Name));
                Assert.That(webEx.ResponseStatus.Message, Is.EqualTo("ClientErrorMessage"));
            }
        }

        [Test]
        public async Task PUT_returning_custom_403_Exception()
        {
            var restClient = CreateRestClient(ListeningOn);

            try
            {
                var response = await restClient.PutAsync(new ThrowHttpError
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
        public async Task Can_catch_async_error_to_non_existing_host()
        {
            var client = CreateRestClient("https://blahblahblah/");
            try
            {
                var response = await client.GetAsync<ThrowHttpError>("/not-here");
                Assert.Fail("Should throw");
            }
            catch (AggregateException ex) //JsonHttpClient
            {
                var innerEx = ex.UnwrapIfSingleException().InnerException;
#if !NETCORE
                Assert.That(((WebException)innerEx).Status, Is.EqualTo(WebExceptionStatus.NameResolutionFailure));
#else
                Assert.That(innerEx.Message, Is.EqualTo("Couldn't resolve host name")
                                            .Or.EqualTo("The server name or address could not be resolved")); // .NET Core
#endif        
            }
            catch (WebException ex) //JsonServiceClient
            {
                Assert.That(ex.Status, Is.EqualTo(WebExceptionStatus.NameResolutionFailure));
            }
        }
    }
}
