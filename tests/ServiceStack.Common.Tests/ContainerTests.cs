using Funq;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.Testing;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class ContainerTests
    {
        class Foo : IFoo
        {
            private static int Count;

            public Foo()
            {
                Id = Count++;
            }

            public int Id { get; set; }
            public IBar Bar { get; set; }
        }

        interface IFoo
        {
            int Id { get; set; }
        }

        class Bar : IBar
        {
            private static int Count;

            public Bar()
            {
                Count++;
            }

            public string Name { get; set; }
        }

        interface IBar
        {
            string Name { get; set; }
        }

        [Test]
        public void Does_TryResolve_from_delegate_cache()
        {
            var container = new Container();
            container.Register(c => new Foo { Id = 1 });

            var instance = (Foo)container.TryResolve(typeof(Foo));
            Assert.That(instance.Id, Is.EqualTo(1));

            instance = (Foo)container.TryResolve(typeof(Foo));
            Assert.That(instance.Id, Is.EqualTo(1));
        }

        [Test]
        public void Can_use_NetCore_APIs_to_register_dependencies()
        {
            using (var appHost = new BasicAppHost().Init())
            {
                var services = appHost.Container;

                services.AddTransient<IFoo, Foo>();
                services.AddSingleton<IBar>(c => new Bar { Name = "bar" });

                var bar = (Bar)services.GetService(typeof(IBar));
                Assert.That(bar.Name, Is.EqualTo("bar"));

                var foo = (Foo)services.GetService(typeof(IFoo));
                Assert.That(foo.Id, Is.EqualTo(0));
                Assert.That(ReferenceEquals(foo.Bar, bar));

                foo = (Foo)services.GetService(typeof(IFoo));
                Assert.That(foo.Id, Is.EqualTo(1));
                Assert.That(ReferenceEquals(foo.Bar, bar));
            }
        }

        [Test]
        public void CreateInstance_throws_on_missing_dependency()
        {
            using (var appHost = new BasicAppHost().Init())
            {
                var services = appHost.Container;
                services.AddTransient<IFoo, Foo>();

                var typeFactory = new ContainerResolveCache(appHost.Container);

                var foo = typeFactory.CreateInstance(services, typeof(IFoo), tryResolve: true);
                Assert.That(foo, Is.Not.Null);

                var bar = typeFactory.CreateInstance(services, typeof(IBar), tryResolve: true);
                Assert.That(bar, Is.Null);

                Assert.Throws<ResolutionException>(() =>
                    typeFactory.CreateInstance(services, typeof(IBar), tryResolve: false));
            }
        }
    }
}