using System.Threading;
using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests.Issues
{
    public class RequestScopeAppHost : AppSelfHostBase
    {
        public RequestScopeAppHost() 
            : base(typeof(RequestScopeAppHost).Name, typeof(RequestScopeService).Assembly) {}

        private static int counter = 0;

        public override void Configure(Container container)
        {
            container.Register(c => new MasterConfig {
                Id = Interlocked.Increment(ref counter)
            }).ReusedWithin(ReuseScope.Request);
        }
    }

    public class MasterConfig
    {
        public int Id { get; set; }
    }

    public class GetMasterConfig : IReturn<MasterConfig> { }

    public class RequestScopeService : Service
    {
        private readonly MasterConfig config;

        public RequestScopeService(MasterConfig config)
        {
            this.config = config;
        }

        public object Any(GetMasterConfig request)
        {
            return config;
        }
    }

    [TestFixture]
    public class RequestScopeIssue
    {
        private readonly ServiceStackHost appHost;

        public RequestScopeIssue()
        {
            appHost = new RequestScopeAppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_get_RequestScope_dependency()
        {
            var client = new JsonServiceClient(Config.AbsoluteBaseUri);

            Assert.That(client.Get(new GetMasterConfig()).Id, Is.EqualTo(1));
            Assert.That(client.Get(new GetMasterConfig()).Id, Is.EqualTo(2));
            Assert.That(client.Get(new GetMasterConfig()).Id, Is.EqualTo(3));
        }
    }
}