using System;
using Funq;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.ServiceHost.Tests.Support;
using ServiceStack.ServiceHost.Tests.TypeFactory;
using ServiceStack.Testing;

namespace ServiceStack.ServiceHost.Tests
{
    [TestFixture]
    public class ServiceHostTests
    {
        private ServiceController serviceController;
        private ServiceStackHost appHost;

        [SetUp]
        public void SetUp()
        {
            appHost = new BasicAppHost().Init();
            serviceController = appHost.ServiceController;
        }

        [TearDown]
        public void TearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_execute_BasicService()
        {
            serviceController.RegisterService(typeof(BasicService));
            var result = serviceController.Execute(new EmptyRequest()) as EmptyRequestResponse;

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Can_execute_BasicService_from_dynamic_Type()
        {
            var requestType = typeof(EmptyRequest);

            serviceController.RegisterService(typeof(BasicService));

            object request = Activator.CreateInstance(requestType);

            var result = serviceController.Execute(request) as EmptyRequestResponse;

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

        private MockHttpRequest CreateContext(string httpMethod)
        {
            var ctx = new MockHttpRequest { HttpMethod = httpMethod };
            return ctx;
        }

        [Test]
        public void Can_execute_RestTestService()
        {
            serviceController.RegisterService(typeof(RestTestService));

            var result = serviceController.Execute(new RestTest(),
                CreateContext(HttpMethods.Options)) as RestTestResponse;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.MethodName, Is.EqualTo("Any"));
        }

        [Test]
        public void Can_RestTestService_GET()
        {
            serviceController.RegisterService(typeof(RestTestService));

            var result = serviceController.Execute(new RestTest(),
                CreateContext(HttpMethods.Get)) as RestTestResponse;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.MethodName, Is.EqualTo("Get"));
        }

        [Test]
        public void Can_RestTestService_PUT()
        {
            serviceController.RegisterService(typeof(RestTestService));

            var result = serviceController.Execute(new RestTest(),
                CreateContext(HttpMethods.Put)) as RestTestResponse;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.MethodName, Is.EqualTo("Put"));
        }

        [Test]
        public void Can_RestTestService_POST()
        {
            serviceController.RegisterService(typeof(RestTestService));

            var result = serviceController.Execute(new RestTest(),
                CreateContext(HttpMethods.Post)) as RestTestResponse;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.MethodName, Is.EqualTo("Post"));
        }

        [Test]
        public void Can_RestTestService_DELETE()
        {
            serviceController.RegisterService(typeof(RestTestService));

            var result = serviceController.Execute(new RestTest(),
                CreateContext(HttpMethods.Delete)) as RestTestResponse;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.MethodName, Is.EqualTo("Delete"));
        }
    }
}
