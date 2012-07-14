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

            container.Register(c => new FunqSingletonScope()).ReusedWithin(ReuseScope.Default);
            container.Register(c => new FunqRequestScope()).ReusedWithin(ReuseScope.Request);
            container.Register(c => new FunqNoneScope()).ReusedWithin(ReuseScope.None);

            Routes.Add<Ioc>("/ioc");
            Routes.Add<IocScope>("/iocscope");
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


    public class IocRequestFilterAttribute : Attribute, IHasRequestFilter
    {
        public FunqSingletonScope FunqSingletonScope { get; set; }
        public FunqRequestScope FunqRequestScope { get; set; }
        public FunqNoneScope FunqNoneScope { get; set; }

        public int Priority { get; set; }

        public void RequestFilter(IHttpRequest req, IHttpResponse res, object requestDto)
        {            
        }

        public IHasRequestFilter Copy()
        {
            return (IHasRequestFilter) this.MemberwiseClone();
        }
    }
}