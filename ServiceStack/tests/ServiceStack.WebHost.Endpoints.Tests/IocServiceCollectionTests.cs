using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Funq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Commands;
using ServiceStack.FluentValidation;
using ServiceStack.Testing;
using ServiceStack.Validation;

namespace ServiceStack.WebHost.Endpoints.Tests;

[DataContract]
public class AutoWire { }

[DataContract]
public class AutoWireResponse
{
    public IFoo Foo { get; set; }
    public IBar Bar { get; set; }
}

public class AutoWireService(IFoo foo, IBar bar) : IService
{
    public IFoo Foo => foo;

    public IBar Bar => bar;

    public int Count { get; set; }

    public object Any(AutoWire request)
    {
        return new AutoWireResponse { Foo = foo, Bar = bar };
    }
}

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

public class IocFuncServiceCollectionTests : IocServiceCollectionTests
{
    protected override IServiceCollection CreateServiceCollection() => new Container();
    protected override IServiceProvider GetServiceProvider(IServiceCollection services) => (IServiceProvider)services;
}
    
public class IocServiceCollectionTests
{
    protected virtual IServiceCollection CreateServiceCollection() => new ServiceCollection();
    protected virtual IServiceProvider GetServiceProvider(IServiceCollection services) => services.BuildServiceProvider();

    [Test]
    public void Can_Register_and_resolve_Singleton()
    {
        var serviceType = typeof(AutoWireService);

        var services = CreateServiceCollection();
        services.AddSingleton<IFoo>(c => new Foo());
        services.AddSingleton<IBar>(c => new Bar());
        services.AddSingleton(serviceType);
        
        var resolver = GetServiceProvider(services);
        
        var service = resolver.GetService<AutoWireService>();

        Assert.That(service.Foo, Is.Not.Null);
        Assert.That(service.Bar, Is.Not.Null);
        Assert.That(service.Count, Is.EqualTo(0));
    }

    public class Test
    {
        public IFoo Foo { get; set; }
        public IBar Bar { get; set; }
        public IFoo Foo2 { get; set; } = new Foo2();
        public IEnumerable<string> Names { get; set; } = ["Steffen", "Demis"];
        public int Age { get; set; }
    }
    
    public class FunqTest(Func<IFoo> ctorFoo, Func<IFoo> funqFoo)
    {
        public Func<IFoo> CtorFoo => ctorFoo;
        public Func<IFoo> FunqFoo => funqFoo;
    }

    [Test]
    public void Does_Resolve_lazy_Func_types()
    {
        var services = CreateServiceCollection();

        services.AddSingleton<Func<IFoo>>(c => () => new Foo());

        services.AddSingleton<FunqTest>();

        var resolver = GetServiceProvider(services);
        var test = resolver.GetService<FunqTest>();

        Assert.That(test.CtorFoo, Is.Not.Null);
        Assert.That(test.CtorFoo(), Is.Not.Null);
        Assert.That(test.FunqFoo, Is.Not.Null);
        Assert.That(test.FunqFoo(), Is.Not.Null);
    }
    
    public class FooCommand(Foo foo) : ICommand<Foo>
    {
        public Foo Foo { get; set; } = foo;

        public Foo Execute()
        {
            return Foo;
        }
    }
    public class BarCommand(Bar bar) : ICommand<Bar>
    {
        public Bar Bar { get; set; } = bar;

        public Bar Execute()
        {
            return Bar;
        }
    }

    [Test]
    public void Can_autowire_generic_type_definitions()
    {
        var services = CreateServiceCollection();
        services.AddSingleton(c => new Foo());
        services.AddSingleton(c => new Bar());

        GetType().Assembly.GetTypes()
            .Where(x => x.IsOrHasGenericInterfaceTypeOf(typeof(ICommand<>)))
            .Each(x => services.AddSingleton(x));

        var resolver = GetServiceProvider(services);

        var fooCmd = resolver.GetService<FooCommand>();
        Assert.That(fooCmd.Execute(), Is.EqualTo(resolver.GetService<Foo>()));
        var barCmd = resolver.GetService<BarCommand>();
        Assert.That(barCmd.Execute(), Is.EqualTo(resolver.GetService<Bar>()));
    }
    
    [Test]
    public void Can_resolve_using_untyped_Container_Api()
    {
        var services = CreateServiceCollection();
        services.AddSingleton(c => new Foo());

        var resolver = GetServiceProvider(services);

        var instance = resolver.GetService(typeof(Foo));
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void Can_register_validator_and_check_it_exists()
    {
        var services = CreateServiceCollection();

        Assert.That(services.Exists<IValidator<Register>>(), Is.False);

        services.AddSingleton<IValidator<Register>, RegistrationValidator>();
        
        var resolver = GetServiceProvider(services);

        var validator = resolver.GetService<IValidator<Register>>();
        
        Assert.That(validator, Is.Not.Null);
        Assert.That(services.Exists<IValidator<Register>>(), Is.True);
    }

    [Test]
    public void Can_register_and_resolve_singleton_instance()
    {
        var services = CreateServiceCollection();

        var foo = new Foo();
        services.AddSingleton<IFoo>(foo);

        var resolver = GetServiceProvider(services);
        
        Assert.That(resolver.GetService<IFoo>(), Is.EqualTo(foo));
        Assert.That(resolver.GetService<IFoo>(), Is.EqualTo(foo));
    }

    [Test] public void Can_register_and_resolve_factory_singleton()
    {
        var services = CreateServiceCollection();

        services.AddSingleton<IFoo>(c => new Foo());

        var resolver = GetServiceProvider(services);

        var service1 = resolver.GetService<IFoo>();
        var service2 = resolver.GetService<IFoo>();
        Assert.That(service1, Is.Not.Null);
        Assert.That(service1, Is.EqualTo(service2));
    }

    [Test]
    public void Can_register_and_resolve_factory_transient()
    {
        var services = CreateServiceCollection();

        services.AddTransient<IFoo>(c => new Foo());

        var resolver = GetServiceProvider(services);

        var service1 = resolver.GetService<IFoo>();
        var service2 = resolver.GetService<IFoo>();
        Assert.That(service1, Is.Not.Null);
        Assert.That(service1, Is.Not.EqualTo(service2));
    }

    [Test]
    public void Can_register_runtime_interface_and_implementation_Types()
    {
        var services = CreateServiceCollection();

        var serviceType = typeof(IValidator<Register>);
        var implType = typeof(RegistrationValidator);

        services.AddTransient(serviceType, implType);
        
        var resolver = GetServiceProvider(services);

        var validator = resolver.GetService(serviceType);
        
        Assert.That(validator, Is.InstanceOf<RegistrationValidator>());
    }

    [Test]
    public void Can_register_validators()
    {
        var services = CreateServiceCollection();
        
        var implType = typeof(RegistrationValidator);

        using var appHost = new BasicAppHost().Init();
        services.RegisterValidator(implType);

        var resolver = GetServiceProvider(services);
        
        var registerValidator = resolver.GetService<IValidator<Register>>();
        Assert.That(registerValidator, Is.InstanceOf<RegistrationValidator>());
    }

    [Test]
    public void Can_check_for_registered_type_interfaces()
    {
        var services = CreateServiceCollection();

        services.AddSingleton<IValidationSource,MemoryValidationSource>();
        
        var hasValidationSourceAdmin = services.Any(x =>
            x.ServiceType == typeof(IValidationSource) &&
            x.ImplementationType?.HasInterface(typeof(IValidationSourceAdmin)) == true);

        Assert.That(hasValidationSourceAdmin);
        
        var resolver = GetServiceProvider(services);
        var validationSource = resolver.GetService<IValidationSource>();
        Assert.That(validationSource, Is.InstanceOf<IValidationSourceAdmin>());
    }
}

