using System;
using Check.ServiceModel;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;

namespace CheckHttpListener
{
    [TestFixture]
    public class ErrorTests
    {
        private ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AppHost()
                .Init()
                .Start("http://*:2020/");
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        IServiceClient CreateClient()
        {
            return new JsonServiceClient("http://localhost:2020/");
        }

        [Test]
        public void Can_call_GET_ThrowError()
        {
            var client = CreateClient();

            try
            {
                client.Get(new ThrowHttpError());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.ResponseDto.GetType().Name,
                    Is.EqualTo(typeof(ThrowHttpErrorResponse).Name));
            }
        }

        [Test]
        public void Can_call_POST_ThrowError()
        {
            var client = CreateClient();

            try
            {
                client.Post(new ThrowHttpError());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.ResponseDto.GetType().Name, 
                    Is.EqualTo(typeof(ThrowHttpErrorResponse).Name));
            }
        }
    }
}