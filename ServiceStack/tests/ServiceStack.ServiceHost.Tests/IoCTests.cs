﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ServiceStack.Commands;
using ServiceStack.ServiceHost.Tests.Support;
using Funq;
using ServiceStack.Configuration;

namespace ServiceStack.ServiceHost.Tests;

[TestFixture]
public class IoCTests
{
    [Test]
    public void Can_AutoWire_types_dynamically_with_expressions()
    {
        var serviceType = typeof(AutoWireService);

        var container = new Container();
        container.Register<IFoo>(c => new Foo());
        container.Register<IBar>(c => new Bar());
        container.Register<int>(c => 100);

        container.RegisterAutoWiredType(serviceType);

        var service = container.Resolve<AutoWireService>();

        Assert.That(service.Foo, Is.Not.Null);
        Assert.That(service.Bar, Is.Not.Null);
        Assert.That(service.Count, Is.EqualTo(0));
    }

    public class Test
    {
        public IFoo Foo { get; set; }
        public IBar Bar { get; set; }
        public IFoo Foo2 { get; set; }
        public IEnumerable<string> Names { get; set; }
        public int Age { get; set; }

        public Test()
        {
            this.Foo2 = new Foo2();
            this.Names = ["Steffen", "Demis"];
        }
    }

    [Test]
    public void Can_AutoWire_Existing_Instance()
    {
        var test = new Test();

        var container = new Container();
        container.Register<IFoo>(c => new Foo());
        container.Register<IBar>(c => new Bar());
        container.Register<int>(c => 10);

        container.AutoWire(test);

        Assert.That(test.Foo, Is.Not.Null);
        Assert.That(test.Bar, Is.Not.Null);
        Assert.That(test.Foo2 as Foo, Is.Null);
        Assert.That(test.Names, Is.Not.Null);
        Assert.That(test.Age, Is.EqualTo(0));
    }

    public class DependencyWithBuiltInTypes
    {
        public DependencyWithBuiltInTypes()
        {
            this.String = "A String";
            this.Age = 27;
        }

        public IFoo Foo { get; set; }
        public IBar Bar { get; set; }
        public string String { get; set; }
        public int Age { get; set; }
    }

    [Test]
    public void Does_not_AutoWire_BuiltIn_BCL_and_ValueTypes()
    {
        var container = new Container();
        container.Register<IFoo>(c => new Foo());
        container.Register<IBar>(c => new Bar());

        //Should not be autowired
        container.Register<string>(c => "Replaced String");
        container.Register<int>(c => 99);

        container.RegisterAutoWired<DependencyWithBuiltInTypes>();

        var test = container.Resolve<DependencyWithBuiltInTypes>();
        Assert.That(test.Foo, Is.Not.Null);
        Assert.That(test.Bar, Is.Not.Null);
        Assert.That(test.String, Is.EqualTo("A String"));
        Assert.That(test.Age, Is.EqualTo(27));
    }

    public class FunqTest(Func<IFoo> ctorFoo)
    {
        public Func<IFoo> CtorFoo => ctorFoo;
        public Func<IFoo> FunqFoo { get; set; }
    }

    [Test]
    public void Does_Resolve_lazy_Func_types()
    {
        var container = new Container();

        container.Register<Func<IFoo>>(c => () => new Foo());

        container.RegisterAutoWired<FunqTest>();

        var test = container.Resolve<FunqTest>();
        Assert.That(test.CtorFoo, Is.Not.Null);
        Assert.That(test.CtorFoo(), Is.Not.Null);
        Assert.That(test.FunqFoo, Is.Not.Null);
        Assert.That(test.FunqFoo(), Is.Not.Null);
    }

    [Test]
    public void Does_AutoWire_Funq_types()
    {
        var container = new Container();

        container.RegisterAutoWiredAs<Foo, IFoo>();

        container.RegisterAutoWired<FunqTest>();

        var test = container.Resolve<FunqTest>();
        Assert.That(test.CtorFoo, Is.Not.Null);
        Assert.That(test.CtorFoo(), Is.Not.Null);
        Assert.That(test.FunqFoo, Is.Not.Null);
        Assert.That(test.FunqFoo(), Is.Not.Null);
    }

    public class MultiFunqTest(Func<IFoo, IBar> ctorFooBar)
    {
        public Func<IFoo, IBar> CtorFooBar => ctorFooBar;

        public Func<IFoo, IBar> FunqFooBar { get; set; }

        public Func<Test, IFoo, IBar> FunqTestFooBar { get; set; }
    }

    [Test]
    public void Does_AutoWire_MultiFunq_types()
    {
        var container = new Container();

        container.RegisterAutoWiredAs<Foo, IFoo>();
        container.RegisterAutoWiredAs<Bar, IBar>();
        container.RegisterAutoWired<Test>();

        var foo = container.Resolve<IFoo>();
        Assert.That(foo, Is.Not.Null);
        var bar = container.Resolve<IBar>();
        Assert.That(bar, Is.Not.Null);

        container.RegisterAutoWired<MultiFunqTest>();

        var test = container.Resolve<MultiFunqTest>();
        Assert.That(test.CtorFooBar, Is.Not.Null);
        Assert.That(test.CtorFooBar(new Foo()), Is.Not.Null);
        Assert.That(test.FunqFooBar, Is.Not.Null);
        Assert.That(test.FunqFooBar(new Foo()), Is.Not.Null);
        Assert.That(test.FunqTestFooBar, Is.Not.Null);
        Assert.That(test.FunqTestFooBar(new Test(), new Foo()), Is.Not.Null);
    }

    public class FooCommand : ICommand<Foo>
    {
        public Foo Foo { get; set; }
        public Foo Execute()
        {
            return Foo;
        }
    }
    public class BarCommand : ICommand<Bar>
    {
        public Bar Bar { get; set; }
        public Bar Execute()
        {
            return Bar;
        }
    }

    [Test]
    public void Can_autowire_generic_type_definitions()
    {
        var container = new Container();
        container.Register(c => new Foo());
        container.Register(c => new Bar());

        GetType().Assembly.GetTypes()
            .Where(x => x.IsOrHasGenericInterfaceTypeOf(typeof(ICommand<>)))
            .Each(x => container.RegisterAutoWiredType(x));

        var fooCmd = container.Resolve<FooCommand>();
        Assert.That(fooCmd.Execute(), Is.EqualTo(container.Resolve<Foo>()));
        var barCmd = container.Resolve<BarCommand>();
        Assert.That(barCmd.Execute(), Is.EqualTo(container.Resolve<Bar>()));
    }

    [Test]
    public void Can_resolve_using_untyped_Container_Api()
    {
        var container = new Container();
        container.Register(c => new Foo());

        var instance = container.TryResolve(typeof(Foo));
        Assert.That(instance, Is.Not.Null);
    }

    class CustomAdapter : IContainerAdapter
    {
        public T TryResolve<T>()
        {
            if (typeof(T) == typeof(IFoo))
                return (T)(object)new Foo();
            return default(T);
        }

        public T Resolve<T>()
        {
            throw new NotImplementedException();
        }
    }

    [Test]
    public void Does_fallback_to_Funq_when_missing_in_Adapter()
    {
        var container = new Container { Adapter = new CustomAdapter() };
        container.Register<IBar>(c => new Bar());

        Assert.That(container.TryResolve<IFoo>(), Is.Not.Null);
        Assert.That(container.TryResolve<IBar>(), Is.Not.Null);
    }

    [Test]
    public void Can_configure_and_resolve_Named_instances()
    {
        var container = new Container();
        container.Register<IFoo>("foo1", new Foo());
        container.Register<IFoo>("foo2", new Foo2());

        Assert.That(container.ResolveNamed<IFoo>("foo1") is Foo);
        Assert.That(container.ResolveNamed<IFoo>("foo2") is Foo2);
    }

    [Test]
    public void Can_autowire_named_instances()
    {
        var container = new Container();
        container.Register<IFoo>(c => new Foo());
        container.Register<IBar>(c => new Bar());
        container.Register<int>(c => 100);

        container.RegisterAutoWired<AutoWireService>("one");
        container.RegisterAutoWired<AutoWireService>("two");

        var one = container.ResolveNamed<AutoWireService>("one");
        var two = container.ResolveNamed<AutoWireService>("two");
        Assert.That(one, Is.Not.Null);
        Assert.AreNotSame(one, two);
    }
        
    [Test]
    public void Can_autowireAs_named_instances()
    {
        var container = new Container();
        container.Register<IFoo>(c => new Foo());
        container.Register<IBar>(c => new Bar());
        container.Register<int>(c => 100);

        container.RegisterAutoWiredAs<AutoWireService, IService>("one");
        container.RegisterAutoWiredAs<AutoWireService, IService>("two");

        var one = container.ResolveNamed<IService>("one");
        var two = container.ResolveNamed<IService>("two");
        Assert.That(one, Is.Not.Null);
        Assert.AreNotSame(one, two);
    }

    [Test]
    public void Can_registerAs_named_instances()
    {
        var container = new Container();
        container.Register<IFoo>(c => new Foo());
        container.Register<IBar>(c => new Bar());
        container.Register<int>(c => 100);

        container.RegisterAs<AutoWireService, IService>("one");
        container.RegisterAs<AutoWireService, IService>("two");

        var one = container.ResolveNamed<IService>("one");
        var two = container.ResolveNamed<IService>("two");
        Assert.That(one, Is.Not.Null);
        Assert.AreNotSame(one, two);
    }

    [Test]
    public void Can_registerAutoWiredType_as_named_instance()
    {
        var container = new Container();
        container.Register<IFoo>(c => new Foo());
        container.Register<IBar>(c => new Bar());
        container.Register<int>(c => 100);

        container.RegisterAutoWiredType("one", typeof(AutoWireService), typeof(IService));
        container.RegisterAutoWiredType("two", typeof(AutoWireService), typeof(IService));

        var one = container.ResolveNamed<IService>("one");
        var two = container.ResolveNamed<IService>("two");
        Assert.That(one, Is.Not.Null);
        Assert.AreNotSame(one, two);
    }

    [Test]
    public void Can_registerAutoWiredType_named_instance()
    {
        var container = new Container();
        container.Register<IFoo>(c => new Foo());
        container.Register<IBar>(c => new Bar());
        container.Register<int>(c => 100);

        container.RegisterAutoWiredType("one", typeof(AutoWireService));
        container.RegisterAutoWiredType("two", typeof(AutoWireService));

        var one = container.ResolveNamed<AutoWireService>("one");
        var two = container.ResolveNamed<AutoWireService>("two");
        Assert.That(one, Is.Not.Null);
        Assert.AreNotSame(one, two);
    }


    class CustomAdapterWithFunc : IContainerAdapter
    {
        public T Resolve<T>()
        {
            if (typeof(T) == typeof(Func<int>))
            {                    
                return (T)(object)(() => 5);
            }
            throw new InvalidOperationException();
        }

        public T TryResolve<T>()
        {
            try
            {
                return Resolve<T>();
            }
            catch (Exception)
            {
                return default;
            }
        }
    }

        
    [Test]
    public void Does_resolve_from_adapter_before_fallback_to_Lazy_Resolver()
    {
        var container = new Container()
        {
            Adapter = new CustomAdapterWithFunc()
        };

        var func = container.Resolve<Func<int>>();

        Assert.AreEqual(5, func());
    }

    [Test]
    public void Lazy_Resolver_works_with_adapter_services()
    {
        var container = new Container()
        {
            Adapter = new CustomAdapterWithFunc()
        };

        var func = container.Resolve<Func<Func<int>>>();

        Assert.AreEqual(5, func()());
    }


    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void Throws_when_Resolve_nonexisting(bool withAdapter)
    {
        var container = new Container();
        if (withAdapter)
        {
            container.Adapter = new CustomAdapterWithFunc();
        }

        Assert.Catch(() => container.Resolve<Func<string>>()());
    }
        
}