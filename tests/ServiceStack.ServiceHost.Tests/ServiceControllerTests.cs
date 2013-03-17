using System;
using NUnit.Framework;
using ServiceStack.ServiceHost.Tests.Support;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceHost.Tests
{
    [TestFixture]
    public class ServiceControllerTests
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            EndpointHostConfig.SkipRouteValidation = true;
        }

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
                new AutoWireService(new Foo2())
                {
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

        [Test]
        public void Generic_service_with_recursive_ceneric_type_should_not_get_registered()
        {
            // Tell manager to register GenericService<Generic3<>>, which should not be possible since Generic3<> is an open type
            var serviceManager = new ServiceManager(null, new ServiceController(() => new[] { typeof(GenericService<>).MakeGenericType(new[] { typeof(Generic3<>) }) }));

            serviceManager.Init();

            var serviceController = serviceManager.ServiceController;
            var exception = Assert.Throws<System.NotImplementedException>(() => serviceController.GetService(typeof(Generic3<>)));

            Assert.That(exception.Message, Is.StringContaining("Unable to resolve service"));
        }

        [Test]
        public void Generic_service_can_be_registered_with_closed_types()
        {
            var serviceManager = new ServiceManager(null, new ServiceController(() => new[]
            {
                typeof(GenericService<Generic1>),
                typeof(GenericService<>).MakeGenericType(new[] { typeof (Generic2) }), // GenericService<Generic2> created through reflection
                typeof(GenericService<Generic3<string>>),
                typeof(GenericService<Generic3<int>>),
                typeof(GenericService<>).MakeGenericType(new[] { typeof (Generic3<>).MakeGenericType(new[] { typeof(double) }) }), // GenericService<Generic3<double>> created through reflection
            }));

            serviceManager.Init();
            var serviceController = serviceManager.ServiceController;

            Assert.AreEqual(typeof(Generic1).FullName, ((Generic1Response)serviceController.Execute(new Generic1())).Data);
            Assert.AreEqual(typeof(Generic2).FullName, ((Generic1Response)serviceController.Execute(new Generic2())).Data);
            Assert.AreEqual(typeof(Generic3<string>).FullName, ((Generic1Response)serviceController.Execute(new Generic3<string>())).Data);
            Assert.AreEqual(typeof(Generic3<int>).FullName, ((Generic1Response)serviceController.Execute(new Generic3<int>())).Data);
            Assert.AreEqual(typeof(Generic3<double>).FullName, ((Generic1Response)serviceController.Execute(new Generic3<double>())).Data);
        }


        [Route("route/{Id}")]
        public class NoSlashPrefix : IReturn
        {
            public long Id { get; set; }
        }

        [Route("/route?id={Id}")]
        public class UsesQueryString : IReturn
        {
            public long Id { get; set; }
        }

        public class MyService : IService
        {
            public object Any(NoSlashPrefix request)
            {
                return null;
            }

            public object Any(UsesQueryString request)
            {
                return null;
            }
        }

        [Test]
        public void Does_throw_on_invalid_Route_Definitions()
        {
            EndpointHostConfig.SkipRouteValidation = false;

            var controller = new ServiceController(() => new[] { typeof(MyService) });

            Assert.Throws<ArgumentException>(
                () => controller.RegisterRestPaths(typeof(NoSlashPrefix)));

            Assert.Throws<ArgumentException>(
                () => controller.RegisterRestPaths(typeof(UsesQueryString)));

            EndpointHostConfig.SkipRouteValidation = true;
        }
    }
}
