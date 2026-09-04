using System;
using System.Threading.Tasks;
using Funq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class FunqModernizationTests
{
    public interface ITestService
    {
        string GetMessage();
    }

    public class TestService : ITestService
    {
        public string GetMessage() => "Success";
    }

    [Test]
    public void TryResolve_WithNullType_ReturnsNull()
    {
        using var container = new Container();
        var result = container.TryResolve((Type)null);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Exists_WithNullType_ReturnsFalse()
    {
        using var container = new Container();
        var result = container.Exists((Type)null);
        Assert.That(result, Is.False);
    }

    [Test]
    public void AutoWire_WithNullInstance_DoesNotThrow()
    {
        using var container = new Container();
        Assert.DoesNotThrow(() => container.AutoWire(null));
        Assert.DoesNotThrow(() => container.AutoWire(container, null));
    }

    [Test]
    public void GetLazyResolver_WithNullOrEmpty_ReturnsNull()
    {
        using var container = new Container();
        Assert.That(container.GetLazyResolver(null), Is.Null);
        Assert.That(container.GetLazyResolver(TypeConstants.EmptyTypeArray), Is.Null);
    }

    [Test]
    public void ResolutionException_WithNullType_ConstructsSafely()
    {
        ResolutionException ex1 = null;
        Assert.DoesNotThrow(() => ex1 = new ResolutionException((Type)null));
        Assert.That(ex1, Is.Not.Null);
        Assert.That(ex1.Message, Does.Contain("null"));

        ResolutionException ex2 = null;
        Assert.DoesNotThrow(() => ex2 = new ResolutionException((Type)null, null));
        Assert.That(ex2, Is.Not.Null);
        Assert.That(ex2.Message, Does.Contain("null"));
    }

    [Test]
    public void ServiceCollection_Add_NullDescriptor_ThrowsArgumentNullException()
    {
        using var container = new Container();
        var serviceCollection = (IServiceCollection)container;

        Assert.Throws<ArgumentNullException>(() => serviceCollection.Add(null));
        Assert.Throws<ArgumentNullException>(() => container.CreateFactory((ServiceDescriptor)null));
    }

    private class DisposableItem : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    [Test]
    public void Container_Dispose_HandlesDisposablesSafely()
    {
        var container = new Container();
        var item = new DisposableItem();
        container.Register<DisposableItem>(c => item)
            .ReusedWithin(ReuseScope.Container)
            .OwnedBy(Owner.Container);

        var resolved = container.Resolve<DisposableItem>();
        Assert.That(resolved, Is.SameAs(item));

        var child = container.CreateChildContainer();
        var childItem = new DisposableItem();
        child.Register<DisposableItem>(c => childItem)
            .ReusedWithin(ReuseScope.Container)
            .OwnedBy(Owner.Container);
        var childResolved = child.Resolve<DisposableItem>();
        Assert.That(childResolved, Is.SameAs(childItem));

        container.Dispose();
        Assert.That(item.IsDisposed, Is.True);
        Assert.That(childItem.IsDisposed, Is.True);

        // Multiple dispose calls are safe
        Assert.DoesNotThrow(() => container.Dispose());
    }

    [Test]
    public void Container_Dispose_ConcurrentServiceRegistrations_DoesNotThrow()
    {
        var container = new Container();
        container.Register<ITestService>(new TestService());

        var registerTask = Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                try
                {
                    container.Register($"test_{i}", new TestService());
                }
                catch
                {
                    // If disposed during registration, ignore
                }
            }
        });

        var disposeTask = Task.Run(() =>
        {
            container.Dispose();
        });

        Assert.DoesNotThrowAsync(async () => await Task.WhenAll(registerTask, disposeTask));
    }
}
