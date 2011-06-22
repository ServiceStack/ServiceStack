using System;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Host
{

	public class IocAppHost : AppHostHttpListenerBase
	{
		public IocAppHost()
			: base("IocApp Service", typeof(IocService).Assembly)
		{
			Instance = null;
		}

		public override void Configure(Container container)
		{
			container.Adapter = new IocAdapter();
			container.Register(c => new FunqDepCtor());
			container.Register(c => new FunqDepProperty());
			container.Register(c => new FunqDepDisposableProperty());

			Routes.Add<Ioc>("/ioc");
		}
	}

	public class IocAdapter : IContainerAdapter
	{
		public T TryResolve<T>()
		{
			if (typeof(T) == typeof(IRequestContext))
				throw new ArgumentException("should not ask for IRequestContext");

			if (typeof(T) == typeof(AltDepProperty))
				return (T)(object)new AltDepProperty();
			if (typeof(T) == typeof(AltDepDisposableProperty))
				return (T)(object)new AltDepDisposableProperty();

			return default(T);
		}

		public T Resolve<T>()
		{
			if (typeof(T) == typeof(AltDepCtor))
				return (T)(object)new AltDepCtor();

			return default(T);
		}
	}

}