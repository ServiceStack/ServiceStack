using Funq;
using NUnit.Framework;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class FunqTests
    {
        interface IBar { }
        class Bar : IBar { }
        class TestFoo { public IBar Bar { get; set; } }

        [Test]
        public void Test1()
        {
            var container = new Container();
            var m = new TestFoo();
            container.Register<IBar>(new Bar());
            Assert.NotNull(container.Resolve<IBar>(), "Resolve");
            container.AutoWire(m);
            Assert.NotNull(m.Bar, "Autowire");
        }

        [Test]
        public void Test2()
        {
            var container = new Container();
            var m = new TestFoo();
            container.AutoWire(m);
            Assert.Throws<ResolutionException>(() => container.Resolve<IBar>());
            Assert.IsNull(m.Bar); // FAILS HERE
        }         
    }
}