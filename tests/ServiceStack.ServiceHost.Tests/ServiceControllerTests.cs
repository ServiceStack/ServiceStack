using System;
using Funq;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.ServiceHost.Tests.Support;
using ServiceStack.Testing;

namespace ServiceStack.ServiceHost.Tests
{
    [TestFixture]
    public class ServiceControllerTests
    {
        [Test]
        public void Can_register_all_services_in_an_assembly()
        {
            using (var appHost = new BasicAppHost(typeof(BasicService).Assembly).Init())
            {
                var container = appHost.Container;
                var serviceController = appHost.ServiceController;

                container.Register<IFoo>(c => new Foo());
                container.Register<IBar>(c => new Bar());

                var request = new AutoWire();
                var response = serviceController.Execute(request) as AutoWireResponse;

                Assert.That(response, Is.Not.Null);
            }
        }

        [Test]
        public void Can_override_service_creation_with_custom_implementation()
        {
            using (var appHost = new BasicAppHost(typeof(BasicService).Assembly).Init())
            {
                var container = appHost.Container;
                var serviceController = appHost.ServiceController;

                container.Register<IFoo>(c => new Foo());
                container.Register<IBar>(c => new Bar());

                var request = new AutoWire();

                var response = serviceController.Execute(request) as AutoWireResponse;

                Assert.That(response, Is.Not.Null);
                Assert.That(response.Foo as Foo, Is.Not.Null);
                Assert.That(response.Bar as Bar, Is.Not.Null);

                container.Register(c => new AutoWireService(new Foo2())
                {
                    Bar = new Bar2()
                });

                response = serviceController.Execute(request) as AutoWireResponse;

                Assert.That(response, Is.Not.Null);
                Assert.That(response.Foo as Foo2, Is.Not.Null);
                Assert.That(response.Bar as Bar2, Is.Not.Null);
            }
        }

        [Test]
        public void Can_inject_RequestContext_for_IRequiresRequestContext_services()
        {
            using (var appHost = new BasicAppHost(typeof(RequiresService).Assembly).Init())
            {
                var serviceController = appHost.ServiceController;

                var request = new RequiresContext();
                var response = serviceController.Execute(request, new BasicRequest(request))
                    as RequiresContextResponse;

                Assert.That(response, Is.Not.Null);
            }
        }

        [Test]
        public void Generic_Service_should_not_get_registered_with_generic_parameter()
        {
            using (var appHost = new BasicAppHost(typeof(GenericService<>).Assembly).Init())
            {
                // We should definately *not* be able to call the generic service with a "T" request object :)
                var requestType = typeof(GenericService<>).GetGenericArguments()[0];
                var exception = Assert.Throws<NotImplementedException>(() => appHost.ServiceController.GetService(requestType));

                Assert.That(exception.Message, Is.StringContaining("Unable to resolve service"));
            }
        }

        [Test]
        public void Generic_service_with_recursive_ceneric_type_should_not_get_registered()
        {
            using (var appHost = new BasicAppHost
            {
                UseServiceController = x =>
                    new ServiceController(x, () => new[] {
                        typeof(GenericService<>).MakeGenericType(new[] { typeof(Generic3<>) })
                    })
            }.Init())
            {
                // Tell manager to register GenericService<Generic3<>>, which should not be possible since Generic3<> is an open type
                var exception = Assert.Throws<System.NotImplementedException>(() =>
                    appHost.ServiceController.GetService(typeof(Generic3<>)));

                Assert.That(exception.Message, Is.StringContaining("Unable to resolve service"));
            }
        }

        [Test]
        public void Generic_service_can_be_registered_with_closed_types()
        {
            using (var appHost = new BasicAppHost
            {
                UseServiceController = x => new ServiceController(x, () => new[]
                {
                    typeof (GenericService<Generic1>),
                    typeof (GenericService<>).MakeGenericType(new[] {typeof (Generic2)}),
                    // GenericService<Generic2> created through reflection
                    typeof (GenericService<Generic3<string>>),
                    typeof (GenericService<Generic3<int>>),
                    typeof (GenericService<>).MakeGenericType(new[]
                        {typeof (Generic3<>).MakeGenericType(new[] {typeof (double)})}),
                    // GenericService<Generic3<double>> created through reflection
                })
            }.Init())
            {
                var serviceController = appHost.ServiceController;

                Assert.AreEqual(typeof(Generic1).FullName, ((Generic1Response)serviceController.Execute(new Generic1())).Data);
                Assert.AreEqual(typeof(Generic2).FullName, ((Generic1Response)serviceController.Execute(new Generic2())).Data);
                Assert.AreEqual(typeof(Generic3<string>).FullName, ((Generic1Response)serviceController.Execute(new Generic3<string>())).Data);
                Assert.AreEqual(typeof(Generic3<int>).FullName, ((Generic1Response)serviceController.Execute(new Generic3<int>())).Data);
                Assert.AreEqual(typeof(Generic3<double>).FullName, ((Generic1Response)serviceController.Execute(new Generic3<double>())).Data);
            }
        }

        [Test]
        public void Service_with_generic_IGet_marker_interface_can_be_registered_without_DefaultRequestAttribute()
        {
            var appHost = new AppHost();

            Assert.That(appHost.RestPaths.Count, Is.EqualTo(0));

            appHost.RegisterService<GetMarkerService>("/route");

            Assert.That(appHost.RestPaths.Count, Is.EqualTo(1));
        }
    }

    public class GetRequest { }

    public class GetRequestResponse { }

    [DefaultRequest(typeof(GetRequest))]
    public class GetMarkerService : Service
    {
        public object Get(GetRequest request)
        {
            return new GetRequestResponse();
        }
    }

    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost() : base("Test", typeof(AppHost).Assembly) { }

        public override void Configure(Container container)
        {
        }
    }
}
