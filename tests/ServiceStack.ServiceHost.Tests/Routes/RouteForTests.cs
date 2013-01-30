using System;
using System.ComponentModel;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface;

namespace ServiceStack.ServiceHost.Tests.Routes
{
    [TestFixture]
    class RouteForTests
    {
        [Test]
        public void Can_register_route_with_path()
        {
            RouteFor<NewApiRequestDto>.WithPath("path");
            var routeAttribute = TypeDescriptor.GetAttributes(typeof(NewApiRequestDto)).OfType<RouteAttribute>().Single();
            Assert.That(routeAttribute.Path == "path");
        }

        [Test]
        public void Can_register_route_with_path_and_verb()
        {
            RouteFor<NewApiRequestDto>.WithPath("path").AndVerbs(HttpMethods.Get);
            var routeAttribute = TypeDescriptor.GetAttributes(typeof(NewApiRequestDto)).OfType<RouteAttribute>().Single();
            Assert.That(routeAttribute.Path == "path");
            Assert.That(routeAttribute.Verbs == HttpMethods.Get);
        }

        [Test]
        public void Can_register_route_with_path_and_multiple_verbs()
        {
            RouteFor<NewApiRequestDto>.WithPath("path").AndVerbs(HttpMethods.Get, HttpMethods.Post);
            var routeAttribute = TypeDescriptor.GetAttributes(typeof(NewApiRequestDto)).OfType<RouteAttribute>().Single();
            Assert.That(routeAttribute.Path == "path");
            Assert.That(routeAttribute.Verbs == HttpMethods.Get + "," + HttpMethods.Post);
        }

        [Test]
        public void Can_register_route_with_path_expression()
        {
            RouteFor<NewApiRequestDto>.WithPath("path/{0}", x => x.Name);
            var routeAttribute = TypeDescriptor.GetAttributes(typeof(NewApiRequestDto)).OfType<RouteAttribute>().Single();
            Assert.That(routeAttribute.Path == "path/{Name}");
        }

        [Test]
        public void Can_retrieve_registered_route_path()
        {
            RouteFor<NewApiRequestDto>.WithPath("path");
            Assert.AreEqual(RouteFor<NewApiRequestDto>.Path, "path");
        }

        [Test]
        public void Can_retrieve_registered_route_verbs()
        {
            RouteFor<NewApiRequestDto>.WithPath("path").AndVerbs(HttpMethods.Get);
            Assert.AreEqual(RouteFor<NewApiRequestDto>.Verbs, HttpMethods.Get);
        }

        [Test, ExpectedException(typeof(NullReferenceException))]
        public void Cannot_retrieve_unregistered_route_path()
        {
            var path = RouteFor<Customer>.Path;
        }

        [Test, ExpectedException(typeof(NullReferenceException))]
        public void Cannot_retrieve_unregistered_route_verbs()
        {
            var verbs = RouteFor<Customer>.Verbs;
        }

        [Test]
        public void Can_reuse_routes()
        {
            RouteFor<NewApiRequestDtoWithId>.WithPath("path/{0}", x => x.Id);
            RouteFor<NewApiRequestDto>.WithPath(RouteFor<NewApiRequestDtoWithId>.Path + "/{0}", x => x.Name);
            var routeAttribute = TypeDescriptor.GetAttributes(typeof(NewApiRequestDto)).OfType<RouteAttribute>().Single();
            Assert.That(routeAttribute.Path == "path/{Id}/{Name}");
        }
    }
}