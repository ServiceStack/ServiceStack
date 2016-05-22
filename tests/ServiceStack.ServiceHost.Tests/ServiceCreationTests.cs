using NUnit.Framework;
using ServiceStack.Caching;
using Funq;

namespace ServiceStack.ServiceHost.Tests
{
    [Route("/notsingleton")]
    public class ServiceCreation
    {
        public bool RequestFilterExecuted { get; set; }
        public bool ContextualRequestFilterExecuted { get; set; }
        public bool RequestFilterDependenyIsResolved { get; set; }
    }
    public class ServiceCreationResponse
    {
        public int RequestCount { get; set; }
    }

    public class ServiceCreationService : Service
    {
        public int RequestCounter = 0;

        public object Any(ServiceCreation request)
        {
            this.RequestCounter++;
            return new ServiceCreationResponse()
            {
                RequestCount = this.RequestCounter
            };
        }
    }

    [TestFixture]
    public class ServiceCreationTest
    {
        private const string ListeningOn = "http://localhost:82/";
        private const string ServiceClientBaseUri = "http://localhost:82/";

        public class AttributeFiltersAppHostHttpListener
            : AppHostHttpListenerBase
        {

            public AttributeFiltersAppHostHttpListener()
                : base("Service Creation Tests", typeof(ServiceCreationService).Assembly) { }

            public override void Configure(Funq.Container container)
            {
                container.Register<ICacheClient>(new MemoryCacheClient());
            }
        }

        AttributeFiltersAppHostHttpListener appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new AttributeFiltersAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        protected static IRestClient[] RestClients =
        {
            new JsonServiceClient(ServiceClientBaseUri),
            new XmlServiceClient(ServiceClientBaseUri),
            new JsvServiceClient(ServiceClientBaseUri)
        };

        [Test, TestCaseSource("RestClients")]
        public void Service_is_not_singleton(IRestClient client)
        {
            for (int i = 0; i < 5; i++)
            {
                var response = client.Post<ServiceCreationResponse>("notsingleton", new ServiceCreation() { });
                Assert.That(response.RequestCount, Is.EqualTo(1));
            }
        }

        public class Foo
        {
            public static int GlobalId = 0;
            public int Id { get; set; }

            public Foo()
            {
                this.Id = GlobalId++;
            }
        }

        [Test]
        public void Funq_is_singleton_by_Default()
        {
            Foo.GlobalId = 0;
            var container = new Container();
            container.Register(c => new Foo());

            var foo = container.Resolve<Foo>();
            Assert.That(foo.Id, Is.EqualTo(0));
            foo = container.Resolve<Foo>();
            Assert.That(foo.Id, Is.EqualTo(0));
            foo = container.Resolve<Foo>();
            Assert.That(foo.Id, Is.EqualTo(0));
        }

        [Test]
        public void Funq_does_transient_scope()
        {
            Foo.GlobalId = 0;
            var container = new Container();
            container.Register(c => new Foo()).ReusedWithin(ReuseScope.None);

            var foo = container.Resolve<Foo>();
            Assert.That(foo.Id, Is.EqualTo(0));
            foo = container.Resolve<Foo>();
            Assert.That(foo.Id, Is.EqualTo(1));
            foo = container.Resolve<Foo>();
            Assert.That(foo.Id, Is.EqualTo(2));
        }
    }
}
