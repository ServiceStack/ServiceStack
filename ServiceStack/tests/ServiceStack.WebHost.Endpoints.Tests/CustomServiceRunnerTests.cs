using Funq;
using NUnit.Framework;
using ServiceStack.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class CustomServiceRunnerTests
    {
        string ListeningOn = Config.AbsoluteBaseUri;
        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new CustomServiceRunnerAppHost()
                .Init()
                .Start(ListeningOn);
        }

        [OneTimeTearDown]
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

            public override object OnAfterExecute(Web.IRequest req, object response, object service)
            {
                if (response is CustomRunnerResponse dto)
                {
                    dto.ServiceName = base.ActionContext.ServiceType.Name;
                    dto.RequestName = base.ActionContext.RequestType.Name;
                }
                return base.OnAfterExecute(req, response, service);
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
            Assert.That(response.ServiceName, Is.EqualTo(nameof(CustomRunnerService)));
            Assert.That(response.RequestName, Is.EqualTo(nameof(CustomRunner)));
        }
    }
}
