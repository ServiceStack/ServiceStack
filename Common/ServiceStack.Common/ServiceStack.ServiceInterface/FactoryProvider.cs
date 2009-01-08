using System;
using System.Collections.Generic;
using ServiceStack.Common.Utils;
using ServiceStack.Configuration;
using ServiceStack.Logging;

namespace ServiceStack.ServiceInterface
{
	public class FactoryProvider : IFactoryProvider
	{
		private readonly ILog log = LogManager.GetLogger(typeof(FactoryProvider));

		private IObjectFactory Factory { get; set; }
		private static readonly object semaphore = new object();
		List<IDisposable> Disposables { get; set; }
		private Dictionary<string, object> ConfigInstanceCache { get; set; }
		private Dictionary<Type, object> RuntimeInstanceCache { get; set; }
		private Dictionary<Type, Type> TypeMapLookup { get; set; }

		public FactoryProvider(IObjectFactory factory, params object[] providers)
			: this(factory)
		{
			foreach (var provider in providers)
			{
				Register(provider);
			}
		}

		public FactoryProvider(IObjectFactory factory)
		{
			this.Factory = factory;
			this.ConfigInstanceCache = new Dictionary<string, object>();
			this.RuntimeInstanceCache = new Dictionary<Type, object>();
			this.TypeMapLookup = new Dictionary<Type, Type>();
			this.Disposables = new List<IDisposable>();
		}

		/// <summary>
		/// Registers the specified provider.
		/// </summary>
		/// <param name="provider">The provider.</param>
		public void Register<T>(T provider)
		{
			var key = typeof(T);
			this.RuntimeInstanceCache[key] = provider;
			RegisterDisposable(provider);
		}

		private void RegisterDisposable<T>(T provider)
		{
			var disposable = provider as IDisposable;
			if (disposable != null)
			{
				this.Disposables.Add(disposable);
			}
		}

		private Type FindTypeLookup(Type resolveType)
		{
			if (!this.TypeMapLookup.ContainsKey(resolveType))
			{
				foreach (var cacheEntry in this.RuntimeInstanceCache)
				{
					if (!resolveType.IsAssignableFrom(cacheEntry.Value.GetType())) continue;
					this.TypeMapLookup[resolveType] = cacheEntry.Key;
					return cacheEntry.Key;
				}
				return null;
			}
			return this.TypeMapLookup[resolveType];
		}

		/// <summary>
		/// Resolves this instance based on the typeof(T)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Resolve<T>()
		{
			var key = FindTypeLookup(typeof(T));
			if (key != null && this.RuntimeInstanceCache.ContainsKey(key))
			{
				return (T)this.RuntimeInstanceCache[key];
			}
			//This will throw an exception if there is more than 1 match for typeof(T)
			return this.Factory != null ? this.Factory.Create<T>() : default(T);
		}

		/// <summary>
		/// Gets a cached instance of a factory object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		public T Resolve<T>(string name)
		{
			AssertFactory();
			if (!ConfigInstanceCache.ContainsKey(name))
			{
				ConfigInstanceCache[name] = Create<T>(name);
			}
			return (T)ConfigInstanceCache[name];
		}

		/// <summary>
		/// Resolves the optional.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name">The name.</param>
		/// <param name="defaultValue">The default value.</param>
		/// <returns></returns>
		public T ResolveOptional<T>(string name, T defaultValue)
		{
			AssertFactory();
			return Factory.Contains(name) ? Resolve<T>(name) : defaultValue;
		}

		/// <summary>
		/// Creates a new instance of a factory object
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		public T Create<T>(string name)
		{
			AssertFactory();
			lock (semaphore)
			{
				var instance = Factory.Create<T>(name);
				RegisterDisposable(instance);
				return instance;
			}
		}

		private void AssertFactory()
		{
			if (this.Factory == null)
			{
				throw new NotSupportedException("this requires 'Factory' needs to be initialized");
			}
		}

		~FactoryProvider()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			foreach (var disposable in this.Disposables)
			{
				try
				{
					disposable.Dispose();

				}
				catch (Exception ex)
				{
					log.Error(string.Format("Error disposing of type '{0}'", disposable.GetType().Name), ex);
					throw;
				}
			}
			this.Disposables.Clear();
		}
	}
}