using System;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class CancellableRequestAppHost : AppSelfHostBase
    {
        public CancellableRequestAppHost()
            : base("CancellableRequests", typeof(CancellableRequestTestService).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add(new CancellableRequestsFeature());
        }
    }

    public class TestCancelRequest : IReturn<TestCancelRequestResponse>
    {
        public string Tag { get; set; }
    }

    public class TestCancelRequestResponse
    {
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class CancellableRequestTestService : Service
    {
        public object Any(TestCancelRequest req)
        {
            using (var cancellableRequest = base.Request.CreateCancellableRequest())
            {
                while (true)
                {
                    cancellableRequest.Token.ThrowIfCancellationRequested();
                    Thread.Sleep(100);
                }
            }
        }
    }

    public class CancellableRequestTests
    {
        ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new CancellableRequestAppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public async Task Can_Cancel_long_running_request()
        {
            var tag = Guid.NewGuid().ToString();
            var client = new JsonServiceClient(Config.AbsoluteBaseUri) {
                RequestFilter = req => req.Headers[HttpHeaders.XTag] = tag
            };

            var responseTask = client.PostAsync(new TestCancelRequest
            {
                Tag = tag
            });

            Thread.Sleep(1000);

            var cancelResponse = client.Post(new CancelRequest { Tag = tag });
            Assert.That(cancelResponse.Tag, Is.EqualTo(tag));

            try
            {
                var response = await responseTask;
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                ex.ResponseStatus.PrintDump();
                Assert.That(ex.ErrorCode, Is.EqualTo(typeof(OperationCanceledException).Name));
            }
        }
    }
}