using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
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
			var response = serviceController.Execute(request, new RequestContext(request))
				as RequiresContextResponse;

			Assert.That(response, Is.Not.Null);
		}

	}
}
