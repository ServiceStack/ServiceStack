using NUnit.Framework;
using ServiceStack.ServiceHost.Tests.Support;

namespace ServiceStack.ServiceHost.Tests
{
	[TestFixture]
	public class ServiceControllerTests
	{
		[Test]
		public void Can_register_all_services_in_an_assembly()
		{
			var serviceManager = new ServiceManager(typeof(BasicService).Assembly);
			serviceManager.Init();

			var container = serviceManager.Container;
			container.Register<IFoo>(c => new Foo());
			container.Register<IBar>(c => new Bar());

			var serviceController = serviceManager.ServiceController;

			var request = new AutoWire();

			var response = serviceController.Execute(request) as AutoWireResponse;

			Assert.That(response, Is.Not.Null);
		}

		[Test]
		public void Can_override_service_creation_with_custom_implementation()
		{
			var serviceManager = new ServiceManager(typeof(BasicService).Assembly);
			serviceManager.Init();

			var container = serviceManager.Container;
			container.Register<IFoo>(c => new Foo());
			container.Register<IBar>(c => new Bar());

			var serviceController = serviceManager.ServiceController;

			var request = new AutoWire();

			var response = serviceController.Execute(request) as AutoWireResponse;

			Assert.That(response, Is.Not.Null);
			Assert.That(response.Foo as Foo, Is.Not.Null);
			Assert.That(response.Bar as Bar, Is.Not.Null);

			container.Register(c =>
				new AutoWireService(new Foo2()) {
					Bar = new Bar2()
				});

			response = serviceController.Execute(request) as AutoWireResponse;

			Assert.That(response, Is.Not.Null);
			Assert.That(response.Foo as Foo2, Is.Not.Null);
			Assert.That(response.Bar as Bar2, Is.Not.Null);
		}

		[Test]
		public void Can_inject_RequestContext_for_IRequiresRequestContext_services()
		{
			var serviceManager = new ServiceManager(typeof(RequiresContextService).Assembly);
			serviceManager.Init();

			var serviceController = serviceManager.ServiceController;

			var request = new RequiresContext();
			var response = serviceController.Execute(request, new HttpRequestContext(request))
				as RequiresContextResponse;

			Assert.That(response, Is.Not.Null);
		}

        [Test]
        public void Generic_Service_should_not_get_registered_with_generic_parameter()
        {
            var serviceManager = new ServiceManager(typeof(GenericService<>).Assembly);
            serviceManager.Init();

            // We should definately *not* be able to call the generic service with a "T" request object :)
            var serviceController = serviceManager.ServiceController;
            var requestType = typeof(GenericService<>).GetGenericArguments()[0];
            var exception = Assert.Throws<System.NotImplementedException>(() => serviceController.GetService(requestType));

            Assert.That(exception.Message, Is.StringContaining("Unable to resolve service"));
        }
	}
}
