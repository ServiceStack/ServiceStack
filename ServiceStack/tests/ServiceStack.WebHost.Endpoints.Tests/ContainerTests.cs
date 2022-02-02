using System;
using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    class SimpleContainerTests : ContainerTests
    {
        protected override IContainer CreateContainer()
        {
            return new SimpleContainer();
        }
    }

    class FunqContainerTests : ContainerTests
    {
        protected override IContainer CreateContainer()
        {
            return new Funq.Container();
        }

        [Test]
        public void Does_use_native_Exists_method()
        {
            var container = new Funq.Container();

            container.AddTransient<IFoo>(() => new Foo());

            Assert.That(container.Exists(typeof(IFoo)));
            Assert.That(container.Exists<IFoo>());

            Assert.That(!container.Exists(typeof(Foo)));
            Assert.That(!container.Exists<Foo>());
        }
    }

    public abstract class ContainerTests
    {
        protected abstract IContainer CreateContainer();

        public class Foo : IFoo
        {
        }

        public class Foo2 : IFoo
        {
        }

        public interface IFoo
        {
        }

        public class Bar : IBar
        {
        }

        public class Bar2 : IBar
        {
        }

        public interface IBar
        {
        }
        
        public class Test
        {
            public IFoo Foo { get; set; }
            public IBar Bar { get; set; }
            public Foo2 Foo2 { get; set; }
            public IEnumerable<string> Names { get; set; }
            public int Age { get; set; }
            public string Name { get; set; }

            public Test()
            {
                this.Age = 27;
                this.Name = "foo";
                this.Names = new List<string> { "bar" };
            }
        }
        
        public class TestCtor
        {
            public IFoo Foo;
            public Foo2 Foo2;
            public IBar Bar { get; set; }

            public TestCtor(IFoo foo, Foo2 foo2)
            {
                Foo = foo;
                Foo2 = foo2;
            }
        }

        [Test]
        public void Does_not_throw_when_registering_null_dependency()
        {
            var container = new Funq.Container();
            container.Register((IFoo) new Foo());
            Assert.That(container.TryResolve<IFoo>() != null);
            container.Register((IFoo) null);
            Assert.That(container.TryResolve<IFoo>() == null);
        }
        
        [Test]
        public void Can_register_transient()
        {
            var container = CreateContainer();
            
            container.AddTransient(() => new Test());

            var instance = container.Resolve(typeof(Test));
            Assert.That(instance, Is.Not.Null);
            Assert.That(container.Resolve(typeof(Test)), Is.Not.EqualTo(instance));
            
            container.AddTransient(() => new Foo());
            var foo = container.Resolve<Foo>();
            Assert.That(foo, Is.Not.Null);
            Assert.That(container.Resolve<Foo>(), Is.Not.EqualTo(foo));
            
            container.AddTransient<IFoo>(() => new Foo());
            var ifoo = container.Resolve<IFoo>();
            Assert.That(ifoo, Is.Not.Null);
            Assert.That(container.Resolve<IFoo>(), Is.Not.EqualTo(ifoo));
        }

        [Test]
        public void Can_register_singleton()
        {
            var container = CreateContainer();
            
            container.AddSingleton(() => new Test());

            var instance = container.Resolve(typeof(Test));
            Assert.That(instance, Is.Not.Null);
            Assert.That(container.Resolve(typeof(Test)), Is.EqualTo(instance));
            
            container.AddSingleton(() => new Foo());
            var foo = container.Resolve<Foo>();
            Assert.That(foo, Is.Not.Null);
            Assert.That(container.Resolve<Foo>(), Is.EqualTo(foo));
            
            container.AddSingleton<IFoo>(() => new Foo());
            var ifoo = container.Resolve<IFoo>();
            Assert.That(ifoo, Is.Not.Null);
            Assert.That(container.Resolve<IFoo>(), Is.EqualTo(ifoo));
        }

        [Test]
        public void Can_register_Autowired_Transient()
        {
            var container = CreateContainer();
            
            container.AddTransient<IFoo>(() => new Foo());
            container.AddTransient<IBar>(() => new Bar());
            container.AddTransient(() => new Foo2());
            
            //Should not be autowired
            container.AddTransient(() => "Replaced String");
            container.AddTransient(() => 99);
            
            container.AddTransient<Test>();

            var instance1 = container.Resolve<Test>();
            var instance2 = container.Resolve<Test>();
         
            Assert.That(instance1, Is.Not.Null);
            Assert.That(instance1.Foo, Is.Not.Null);
            Assert.That(instance1.Bar, Is.Not.Null);
            Assert.That(instance1.Foo2, Is.Not.Null);
            Assert.That(instance1.Age, Is.EqualTo(27));
            Assert.That(instance1.Name, Is.EqualTo("foo"));
            Assert.That(instance1.Names, Is.Null); //overridden

            Assert.That(instance1, Is.Not.EqualTo(instance2));
            Assert.That(instance1.Foo, Is.Not.EqualTo(instance2.Foo));
            Assert.That(instance1.Bar, Is.Not.EqualTo(instance2.Bar));
            Assert.That(instance1.Foo2, Is.Not.EqualTo(instance2.Foo2));
        }

        [Test]
        public void Can_register_Autowired_Singleton()
        {
            var container = CreateContainer();
            
            container.AddSingleton<IFoo>(() => new Foo());
            container.AddSingleton<IBar>(() => new Bar());
            container.AddSingleton(() => new Foo2());
            
            //Should not be autowired
            container.AddSingleton(() => "Replaced String");
            container.AddSingleton(() => 99);
            
            container.AddSingleton<Test>();

            var instance1 = container.Resolve<Test>();
            var instance2 = container.Resolve<Test>();
         
            Assert.That(instance1, Is.Not.Null);
            Assert.That(instance1.Foo, Is.Not.Null);
            Assert.That(instance1.Bar, Is.Not.Null);
            Assert.That(instance1.Foo2, Is.Not.Null);
            Assert.That(instance1.Age, Is.EqualTo(27));
            Assert.That(instance1.Name, Is.EqualTo("foo"));
            Assert.That(instance1.Names, Is.Null); //overridden

            Assert.That(instance1, Is.EqualTo(instance2));
            Assert.That(instance1.Foo, Is.EqualTo(instance2.Foo));
            Assert.That(instance1.Bar, Is.EqualTo(instance2.Bar));
            Assert.That(instance1.Foo2, Is.EqualTo(instance2.Foo2));
        }

        [Test]
        public void Resolve_does_use_ctor_and_property_injection()
        {
            var container = CreateContainer();
            
            container.AddTransient<IFoo>(() => new Foo());
            container.AddTransient<IBar>(() => new Bar());
            container.AddTransient(() => new Foo2());
            
            container.AddTransient<TestCtor>();

            var instance = container.Resolve<TestCtor>();
            
            Assert.That(instance.Foo, Is.Not.Null);
            Assert.That(instance.Foo2, Is.Not.Null);
            Assert.That(instance.Bar, Is.Not.Null);
        }

        [Test]
        public void Missing_ctor_dependency_should_throw()
        {
            var container = CreateContainer();
            
            container.AddTransient<IFoo>(() => new Foo());
            container.AddTransient<IBar>(() => new Bar());
            
            container.AddTransient<TestCtor>();

            try
            {
                var instance = container.Resolve<TestCtor>();
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                e.ToString().Print();
            }

            try
            {
                var instance = container.Resolve(typeof(TestCtor));
                Assert.Fail("Should throw");
            }
            catch (Exception e)
            {
                e.ToString().Print();
            }
        }

        [Test]
        public void Can_use_exists()
        {
            var container = CreateContainer();

            container.AddTransient<IFoo>(() => new Foo());

            Assert.That(container.Exists(typeof(IFoo)));
            Assert.That(container.Exists<IFoo>());

            Assert.That(!container.Exists(typeof(Foo)));
            Assert.That(!container.Exists<Foo>());
        }

    }
}