using System;
using Funq;
using NUnit.Framework;
using ServiceStack.ServiceHost.Tests.Support;
using ServiceStack.ServiceHost.Tests.TypeFactory;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Web;

namespace ServiceStack.ServiceHost.Tests
{
	[TestFixture]
	public class ServiceHostTests
	{
	    private ServiceManager serviceManager;

		[SetUp]
		public void OnBeforeEachTest()
		{
            serviceManager = new ServiceManager(null, new ServiceController(null));
        }

		[Test]
		public void Can_execute_BasicService()
		{
            serviceManager.RegisterService(typeof(BasicService));
            var result = serviceManager.ServiceController.Execute(new BasicRequest()) as BasicRequestResponse;

			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void Can_execute_BasicService_from_dynamic_Type()
		{
			var requestType = typeof(BasicRequest);

            serviceManager.RegisterService(typeof(BasicService));

			object request = Activator.CreateInstance(requestType);

            var result = serviceManager.ServiceController.Execute(request) as BasicRequestResponse;

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

        private HttpRequestContext CreateContext(string httpMethod)
        {
            var ctx = new MockHttpRequest { HttpMethod = httpMethod };
            return new HttpRequestContext(ctx, new MockHttpResponse(), null, HttpMethods.GetEndpointAttribute(httpMethod));
        }

		[Test]
		public void Can_execute_RestTestService()
		{
            serviceManager.RegisterService(typeof(RestTestService));

            var result = serviceManager.ServiceController.Execute(new RestTest(),
                CreateContext(HttpMethods.Options)) as RestTestResponse;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.MethodName, Is.EqualTo("Any"));
		}

		[Test] 
		public void Can_RestTestService_GET()
		{            
            serviceManager.RegisterService(typeof(RestTestService));

            var result = serviceManager.ServiceController.Execute(new RestTest(),
                CreateContext(HttpMethods.Get)) as RestTestResponse;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.MethodName, Is.EqualTo("Get"));
		}

		[Test]
		public void Can_RestTestService_PUT()
		{
            serviceManager.RegisterService(typeof(RestTestService));

            var result = serviceManager.ServiceController.Execute(new RestTest(),
                CreateContext(HttpMethods.Put)) as RestTestResponse;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.MethodName, Is.EqualTo("Put"));
		}

		[Test]
		public void Can_RestTestService_POST()
		{
            serviceManager.RegisterService(typeof(RestTestService));

            var result = serviceManager.ServiceController.Execute(new RestTest(),
                CreateContext(HttpMethods.Post)) as RestTestResponse;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.MethodName, Is.EqualTo("Post"));
		}

		[Test]
		public void Can_RestTestService_DELETE()
		{
            serviceManager.RegisterService(typeof(RestTestService));

            var result = serviceManager.ServiceController.Execute(new RestTest(),
                CreateContext(HttpMethods.Delete)) as RestTestResponse;

			Assert.That(result, Is.Not.Null);
			Assert.That(result.MethodName, Is.EqualTo("Delete"));
		}
	}
}
