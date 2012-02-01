using System;
using Funq;
using NUnit.Framework;
using ServiceStack.ServiceHost.Tests.Support;
using ServiceStack.ServiceHost.Tests.TypeFactory;
using ServiceStack.Text;
using ServiceStack.Text.Common;

namespace ServiceStack.ServiceHost.Tests
{
	[TestFixture]
	public class ServiceHostTests
	{
		private ServiceController serviceController;

		[SetUp]
		public void OnBeforeEachTest()
		{
			serviceController = new ServiceController(null);
		}

		[Test]
		public void Can_execute_BasicService()
		{
			serviceController.Register(() => new BasicService());
			var result = serviceController.Execute(new BasicRequest()) as BasicRequestResponse;

			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void Can_execute_BasicService_from_dynamic_Type()
		{
			var requestType = typeof(BasicRequest);

			serviceController.Register(requestType, typeof(BasicService));

			object request = Activator.CreateInstance(requestType);

			var result = serviceController.Execute(request) as BasicRequestResponse;

			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void Can_AutoWire_types_dynamically_with_reflection()
		{
			var serviceType = typeof(AutoWireService);

			var container = new Container();
			container.Register<IFoo>(c => new Foo());
			container.Register<IBar>(c => new Bar());

			var typeContainer = new ReflectionTypeFunqContainer(container);
			typeContainer.Register(serviceType);

			var service = container.Resolve<AutoWireService>();

			Assert.That(service.Foo, Is.Not.Null);
			Assert.That(service.Bar, Is.Not.Null);
		}

		[Test]
		public void Can_AutoWire_types_dynamically_with_expressions()
		{
			var serviceType = typeof(AutoWireService);

			var container = new Container();
			container.Register<IFoo>(c => new Foo());
			container.Register<IBar>(c => new Bar());

			container.RegisterAutoWiredType(serviceType);

			var service = container.Resolve<AutoWireService>();

			Assert.That(service.Foo, Is.Not.Null);
			Assert.That(service.Bar, Is.Not.Null);
		}

		[Test]
		public void Can_execute_RestTestService()
		{
			serviceController.Register(() => new RestTestService());
			var result = serviceController.Execute(new RestTest()) as RestTestResponse;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.MethodName, Is.EqualTo("Execute"));
		}

		[Test]
		public void Can_RestTestService_GET()
		{
			serviceController.Register(() => new RestTestService());
			var result = serviceController.Execute(new RestTest(),
				new HttpRequestContext((object)null, EndpointAttributes.HttpGet)) as RestTestResponse;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.MethodName, Is.EqualTo("Get"));
		}

		[Test]
		public void Can_RestTestService_PUT()
		{
			serviceController.Register(() => new RestTestService());
			var result = serviceController.Execute(new RestTest(),
				new HttpRequestContext((object)null, EndpointAttributes.HttpPut)) as RestTestResponse;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.MethodName, Is.EqualTo("Put"));
		}

		[Test]
		public void Can_RestTestService_POST()
		{
			serviceController.Register(() => new RestTestService());
			var result = serviceController.Execute(new RestTest(),
				new HttpRequestContext((object)null, EndpointAttributes.HttpPost)) as RestTestResponse;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.MethodName, Is.EqualTo("Post"));
		}

		[Test]
		public void Can_RestTestService_DELETE()
		{
			serviceController.Register(() => new RestTestService());
			var result = serviceController.Execute(new RestTest(),
				new HttpRequestContext((object)null, EndpointAttributes.HttpDelete)) as RestTestResponse;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.MethodName, Is.EqualTo("Delete"));
		}
	}
}
