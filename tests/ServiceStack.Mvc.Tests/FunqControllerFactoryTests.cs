using NUnit.Framework;
using Funq;
using ServiceStack.Mvc.Stubs.Tests;
using ServiceStack.Mvc.Tests.Stubs;

namespace ServiceStack.Mvc.Tests
{
    [TestFixture]
    public class FunqControllerFactoryTests
    {
        [Test]
        public void ConstructFactoryPopulatesLocalControllerByDefault()
        {
            var container = new Container();
            var factory = new FunqControllerFactory(container);
            var testController = container.Resolve<LocalController>();
            Assert.That(testController, Is.Not.Null);
        }

        [Test]
        public void ConstructFactoryPopulatesLocalControllerAndExternalControllerByDefault()
        {
            var container = new Container();
            var factory = new FunqControllerFactory(container, typeof(ExternalController).Assembly);
            
            // test we can still resolve the local one (by default)
            var testController = container.Resolve<LocalController>();
            Assert.That(testController, Is.Not.Null);

            // test we can resolve the external controller (via params assembly)
            var externalController = container.Resolve<ExternalController>();
            Assert.That(externalController, Is.Not.Null);
        }
    }
}
