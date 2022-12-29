using System;
using System.ComponentModel;

namespace Funq
{
    /// <summary>
    /// Fluent API for customizing the registration of a service.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRegistration : IFluentInterface, IReusedOwned
    {
    }

    /// <summary>
    /// Fluent API for customizing the registration of a service.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRegistration<TService> : IFluentInterface, IRegistration, IInitializable<TService>
    {
    }

    /// <summary>
    /// Fluent API that allows registering an initializer for the 
    /// service.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IInitializable<TService> : IFluentInterface
    {
        /// <summary>
        /// Specifies an initializer that should be invoked after 
        /// the service instance has been created by the factory.
        /// </summary>
        IReusedOwned InitializedBy(Action<Container, TService> initializer);
    }

    /// <summary>
    /// Fluent API that exposes both <see cref="IReused.ReusedWithin"/>
    /// and owner (<see cref="IOwned.OwnedBy"/>).
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IReusedOwned : IFluentInterface, IReused, IOwned { }

    /// <summary>
    /// Fluent API that allows specifying the reuse instances.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IReused : IFluentInterface
    {
        /// <summary>
        /// Specifies how instances are reused within a container or hierarchy. Default 
        /// scope is <see cref="ReuseScope.Hierarchy"/>.
        /// </summary>
        IOwned ReusedWithin(ReuseScope scope);
    }

    /// <summary>
    /// Fluent API that allows specifying the owner of instances 
    /// created from a registration.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IOwned : IFluentInterface
    {
        /// <summary>
        /// Specifies the owner of instances created from this registration. Default 
        /// owner is <see cref="Owner.Container"/>.
        /// </summary>
        void OwnedBy(Owner owner);
    }
}
