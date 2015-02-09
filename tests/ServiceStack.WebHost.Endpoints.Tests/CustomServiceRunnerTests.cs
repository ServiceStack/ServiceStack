using Funq;
using NUnit.Framework;
using ServiceStack.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class CustomServiceRunnerTests
    {
        private const string ListeningOn = Config.AbsoluteBaseUri;
        private ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new CustomServiceRunnerAppHost()
                .Init()
                .Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public class CustomServiceRunnerAppHost : AppHostHttpListenerBase
        {
            public CustomServiceRunnerAppHost()
                : base("CustomServiceRunner", typeof(CustomServiceRunnerAppHost).Assembly) { }

            public override void Configure(Container container) {}

            public override Web.IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
            {
                return new CustomServiceRunner<TRequest>(this, actionContext);
            }
        }

        public class CustomServiceRunner<T> : ServiceRunner<T>
        {
            public CustomServiceRunner(IAppHost appHost, ActionContext actionContext)
                : base(appHost, actionContext) {
            }

            public override object OnAfterExecute(Web.IRequest requestContext, object response)
            {
                var dto = response as CustomRunnerResponse;
                if (dto != null)
                {
                    dto.ServiceName = base.ActionContext.ServiceType.Name;
                    dto.RequestName = base.ActionContext.RequestType.Name;
                }
                return base.OnAfterExecute(requestContext, response);
            }
        }

        public class CustomRunner : IReturn<CustomRunnerResponse>
        {
            public int Id { get; set; }
        }

        public class CustomRunnerResponse
        {
            public int Id { get; set; }
            public string RequestName { get; set; }
            public string ServiceName { get; set; }
        }

        public class CustomRunnerService : Service
        {
            public object Get(CustomRunner request)
            {
                return new CustomRunnerResponse { Id = 1 };
            }
        }

        [Test]
        public void ServiceRunner_has_Request_and_ServiceType()
        {
            var client = new JsonServiceClient(ListeningOn);

            var response = client.Get(new CustomRunner { Id = 1 });

            Assert.That(response.Id, Is.EqualTo(1));
            Assert.That(response.ServiceName, Is.EqualTo(typeof(CustomRunnerService).Name));
            Assert.That(response.RequestName, Is.EqualTo(typeof(CustomRunner).Name));
        }
    }
}
