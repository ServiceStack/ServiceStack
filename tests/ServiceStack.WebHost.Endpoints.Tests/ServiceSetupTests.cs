using Funq;
using NUnit.Framework;
using ServiceStack.Clients;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class ServiceSetup : IReturn<ServiceSetup>
    {
        public int Id { get; set; }
    }

    public class BaseService<T> : ServiceInterface.Service
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
        public ServiceSetupAppHost() : base("Service Setup Tests", typeof(Actual).Assembly) { }
        public override void Configure(Container container) { }
    }

    [TestFixture]
    public class ServiceSetupTests
    {
        private const string BaseUri = "http://localhost:8000/";
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
            appHost = null;
        }

        [Test]
        public void Can_still_load_with_Abstract_Generic_BaseTypes()
        {
            var response = client.Get(new ServiceSetup { Id = 1 });
            Assert.That(response.Id, Is.EqualTo(2));
        }
    }
}