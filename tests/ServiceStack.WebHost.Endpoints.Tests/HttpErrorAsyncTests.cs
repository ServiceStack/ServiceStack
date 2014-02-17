using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class HttpErrorAsyncTests
    {
        private const string ListeningOn = "http://localhost:82/";

        ExampleAppHostHttpListener appHost;

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

        private static void FailOnAsyncError<T>(T response, Exception ex)
        {
            Assert.Fail(ex.Message);
        }

        public IRestClientAsync CreateRestClient(string baseUri = null)
        {
            return baseUri != null
                ? new JsonServiceClient(baseUri)
                : new JsonServiceClient();
        }

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

    }

}