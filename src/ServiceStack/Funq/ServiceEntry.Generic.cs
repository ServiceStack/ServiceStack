using System;
using System.Threading;
using ServiceStack.Common;

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
	    TService instance;
        internal TService Instance
	    {
	        get
	        {
                if (Reuse == ReuseScope.Request)
                    return HostContext.Instance.Items[this] is TService 
                        ? (TService) HostContext.Instance.Items[this] 
                        : default(TService);
	            
                return instance;
	        }
            set
            {
                if (Reuse == ReuseScope.Request)
                    HostContext.Instance.Items[this] = value;
                else 
                    instance = value;
            }

	    }

		/// <summary>
		/// The Func delegate that initializes the object after creation.
		/// </summary>
		internal Action<Container, TService> Initializer;

		internal void InitializeInstance(TService instance)
		{
			// Save instance if Hierarchy or Container Reuse 
            if (Reuse != ReuseScope.None)
            {
                Instance = instance;
            }
            else
            {
                //Keep track of ReuseScope.None IDisposable instances to dispose of end of the request
                HostContext.Instance.TrackDisposable(instance as IDisposable);
            }

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

	    public IDisposable AquireLockIfNeeded()
	    {
            if (Reuse == ReuseScope.None || Reuse == ReuseScope.Request || Instance != null)
                return null;

	        return new AquiredLock(this);
	    }

	    internal class AquiredLock : IDisposable
	    {
	        private readonly object syncRoot;

	        public AquiredLock(object syncRoot)
	        {
                Monitor.Enter(this.syncRoot = syncRoot);
	        }

	        public void Dispose()
	        {
	            Monitor.Exit(syncRoot);
	        }
	    }
	}
}
