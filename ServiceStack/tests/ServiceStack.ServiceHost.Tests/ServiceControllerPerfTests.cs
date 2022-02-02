using System;
using System.Diagnostics;
using Funq;
using NUnit.Framework;
using ServiceStack.ServiceHost.Tests.Support;
using ServiceStack.ServiceHost.Tests.TypeFactory;

namespace ServiceStack.ServiceHost.Tests
{
    [Ignore("Perf Test Only")]
    [TestFixture]
    public class ServiceControllerPerfTests
    {
        private const int Times = 100000;

        [Test]
        public void RunAll()
        {
            With_Funq_and_Expressions();
            With_Native_Funq();
            With_Funq_and_Reflection(); //Very slow
        }

        [Test]
        public void With_Native_Funq()
        {
            var container = new Container();
            container.Register<IFoo>(c => new Foo());
            container.Register<IBar>(c => new Bar());

            container.Register(
                c => new AutoWireService(c.Resolve<IFoo>())
                {
                    Bar = c.Resolve<IBar>()
                }).ReusedWithin(ReuseScope.None);

            Console.WriteLine("With_Native_Funq(): {0}", Measure(() => container.Resolve<AutoWireService>(), Times));
        }

        [Test]
        [Ignore("Slow to run")]
        public void With_Funq_and_Reflection()
        {
            var container = new Container();
            container.Register<IFoo>(c => new Foo());
            container.Register<IBar>(c => new Bar());

            var funqlet = new ReflectionTypeFunqContainer(container);
            funqlet.Register(typeof(AutoWireService));

            Console.WriteLine("With_Funq_and_Reflection(): {0}", Measure(() => container.Resolve<AutoWireService>(), Times));
        }

        [Test]
        public void With_Funq_and_Expressions()
        {
            var container = new Container();
            container.Register<IFoo>(c => new Foo());
            container.Register<IBar>(c => new Bar());

            container.RegisterAutoWiredType(typeof(AutoWireService));

            Console.WriteLine("With_Funq_and_Expressions(): {0}", Measure(() => container.Resolve<AutoWireService>(), Times));
        }

        private static long Measure(Action action, int iterations)
        {
            GC.Collect();
            var watch = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                action();
            }

            return watch.ElapsedTicks;
        }
    }
}