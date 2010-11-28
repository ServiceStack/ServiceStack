using System;

namespace Funq
{
	internal sealed class ServiceEntry<TService, TFunc> : ServiceEntry, IRegistration<TService>
	{
		public ServiceEntry(TFunc factory)
		{
			this.Factory = factory;
		}

		/// <summary>
		/// The Func delegate that creates instances of the service.
		/// </summary>
		public TFunc Factory;

		/// <summary>
		/// The cached service instance if the scope is <see cref="ReuseScope.Hierarchy"/> or 
		/// <see cref="ReuseScope.Container"/>.
		/// </summary>
		internal TService Instance;

		/// <summary>
		/// The Func delegate that initializes the object after creation.
		/// </summary>
		internal Action<Container, TService> Initializer;

		internal void InitializeInstance(TService instance)
		{
			// Save instance if Hierarchy or Container Reuse 
			if (Reuse != ReuseScope.None)
				Instance = instance;

			// Track for disposal if necessary
			if (Owner == Owner.Container && instance is IDisposable)
				Container.TrackDisposable(instance);

			// Call initializer if necessary
			if (Initializer != null)
				Initializer(Container, instance);
		}

		public IReusedOwned InitializedBy(Action<Container, TService> initializer)
		{
			this.Initializer = initializer;
			return this;
		}

		/// <summary>
		/// Clones the service entry assigning the <see cref="Container"/> to the 
		/// <paramref name="newContainer"/>. Does not copy the <see cref="Instance"/>.
		/// </summary>
		public ServiceEntry<TService, TFunc> CloneFor(Container newContainer)
		{
			return new ServiceEntry<TService, TFunc>(Factory)
			{
				Owner = Owner,
				Reuse = Reuse,
				Container = newContainer,
				Initializer = Initializer,
			};
		}
	}
}
