using Funq;
using ServiceStack.Configuration;
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

			Routes.Add<Ioc>("/ioc");
		}
	}

	public class IocAdapter : IContainerAdapter
	{
		public T TryResolve<T>()
		{
			if (typeof(T) == typeof(AltDepProperty))
				return (T)(object)new AltDepProperty();

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