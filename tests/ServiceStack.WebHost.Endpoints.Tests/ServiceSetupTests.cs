using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class ServiceSetup : IReturn<ServiceSetup>
    {
        public int Id { get; set; }
    }

    public class BaseService<T> : Service
    {
        public virtual object Get(T dto)
        {
            return null;
        }
    }

    public class Actual : BaseService<ServiceSetup>
    {
        public override object Get(ServiceSetup dto)
        {
            dto.Id++;
            return dto;
        }
    }

    public class ServiceSetupAppHost : AppHostHttpListenerBase
    {
        public ServiceSetupAppHost() : base("Service Setup Tests", typeof(Actual).GetAssembly()) { }
        public override void Configure(Container container) { }
    }

    [TestFixture]
    public class ServiceSetupTests
    {
        private const string BaseUri = "http://localhost:8001/";
        JsonServiceClient client = new JsonServiceClient(BaseUri);
        private ServiceSetupAppHost appHost;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new ServiceSetupAppHost();
            appHost.Init();
            appHost.Start(BaseUri);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_still_load_with_Abstract_Generic_BaseTypes()
        {
            var response = client.Get(new ServiceSetup { Id = 1 });
            Assert.That(response.Id, Is.EqualTo(2));
        }
    }
}