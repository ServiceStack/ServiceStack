using System.Linq;
using NUnit.Framework;
using ServiceStack.ServiceInterface;

namespace ServiceStack.ServiceHost.Tests.Routes
{
    [TestFixture]
    public class ServiceRoutesTests
    {
        [Test]
        public void Can_Register_Routes_From_Assembly()
        {
            var routes = new ServiceRoutes();
            routes.AddFromAssembly(typeof(RestServiceWithAllVerbsImplemented).Assembly);

            RestPath restWithAllMethodsRoute = (from r in routes.RestPaths
                                                where r.Path == "RestServiceWithAllVerbsImplemented"
                                                select r).FirstOrDefault();

            Assert.That(restWithAllMethodsRoute, Is.Not.Null);

            Assert.That(restWithAllMethodsRoute.AllowedVerbs.Contains("GET"));
            Assert.That(restWithAllMethodsRoute.AllowedVerbs.Contains("POST"));
            Assert.That(restWithAllMethodsRoute.AllowedVerbs.Contains("PUT"));
            Assert.That(restWithAllMethodsRoute.AllowedVerbs.Contains("DELETE"));
            Assert.That(restWithAllMethodsRoute.AllowedVerbs.Contains("PATCH"));
        }

        [Test]
        public void Can_Register_Routes_With_Partially_Implemented_REST_Verbs()
        {
            var routes = new ServiceRoutes();
            routes.AddFromAssembly(typeof(RestServiceWithSomeVerbsImplemented).Assembly);

            RestPath restWithAFewMethodsRoute = (from r in routes.RestPaths
                                                 where r.Path == "RestServiceWithSomeVerbsImplemented"
                                                select r).FirstOrDefault();

            Assert.That(restWithAFewMethodsRoute, Is.Not.Null);

            Assert.That(restWithAFewMethodsRoute.AllowedVerbs.Contains("GET"), Is.True);
            Assert.That(restWithAFewMethodsRoute.AllowedVerbs.Contains("POST"), Is.False);
            Assert.That(restWithAFewMethodsRoute.AllowedVerbs.Contains("PUT"), Is.True);
            Assert.That(restWithAFewMethodsRoute.AllowedVerbs.Contains("DELETE"), Is.False);
            Assert.That(restWithAFewMethodsRoute.AllowedVerbs.Contains("PATCH"), Is.False);
        }
    }
}