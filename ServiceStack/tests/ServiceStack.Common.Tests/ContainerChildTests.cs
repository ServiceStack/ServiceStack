using Funq;
using NUnit.Framework;

namespace ServiceStack.Common.Tests
{
    public interface ISomeService
    { }

    public class SomeService : ISomeService
    {
        public readonly Dependency dependancy;

        public SomeService(Dependency dependancy)
        {
            this.dependancy = dependancy;
        }
    }

    public class Dependency
    {
        public readonly string label;

        public Dependency(string label)
        {
            this.label = label;
        }
    }

    public class ContainerChildTests
    {
        [Test]
        public void Can_use_child_containers()
        {
            Container parent = new Container();

            // Create two childs, each with a specific instance Dependancy
            Container child1 = parent.CreateChildContainer();
            Container child2 = parent.CreateChildContainer();

            child1.Register(new Dependency("First"));
            child2.Register(new Dependency("Second"));

            // Now register two factories for ISomeService.
            child1.Register<ISomeService>(x => Factory(x));
            child2.Register<ISomeService>(x => Factory(x));

            ISomeService resolved1 = child1.Resolve<ISomeService>();
            ISomeService resolved2 = child2.Resolve<ISomeService>();

            Assert.That(((SomeService)resolved1).dependancy.label, Is.EqualTo("First"));
            Assert.That(((SomeService)resolved2).dependancy.label, Is.EqualTo("Second"));
        }

        public static ISomeService Factory(Container c)
        {
            // Register the service by type...
            c.RegisterAutoWiredType(typeof(SomeService), ReuseScope.Hierarchy);
            // ... and force auto-wiring to happen.
            ISomeService result = (ISomeService)c.TryResolve(typeof(SomeService));
            return result;
        }
    }
}