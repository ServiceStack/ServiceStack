using System;
using System.Collections.Generic;
using Funq;
using ServiceStack.Common;
using ServiceStack.Configuration;
using ServiceStack.ServiceHost;
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

        private IocAdapter iocAdapter;

		public override void Configure(Container container)
		{
			container.Adapter = iocAdapter = new IocAdapter();
			container.Register(c => new FunqDepCtor());
			container.Register(c => new FunqDepProperty());
			container.Register(c => new FunqDepDisposableProperty());

            container.Register(c => new FunqSingletonScope()).ReusedWithin(ReuseScope.Default);
            container.Register(c => new FunqRequestScope()).ReusedWithin(ReuseScope.Request);
            container.Register(c => new FunqNoneScope()).ReusedWithin(ReuseScope.None);
            container.Register(c => new FunqRequestScopeDepDisposableProperty()).ReusedWithin(ReuseScope.Request);

            container.Register(c => new FunqSingletonScopeDisposable()).ReusedWithin(ReuseScope.Default);
            container.Register(c => new FunqRequestScopeDisposable()).ReusedWithin(ReuseScope.Request);
            container.Register(c => new FunqNoneScopeDisposable()).ReusedWithin(ReuseScope.None);

            Routes.Add<Ioc>("/ioc");
            Routes.Add<IocScope>("/iocscope");
		}

        public override void Release(object instance)
        {
            iocAdapter.Release(instance);
        }
	}

    public class IocAdapter : IContainerAdapter, IRelease
	{
		public T TryResolve<T>()
		{
			if (typeof(T) == typeof(IRequestContext))
				throw new ArgumentException("should not ask for IRequestContext");

			if (typeof(T) == typeof(AltDepProperty))
				return (T)(object)new AltDepProperty();
            if (typeof(T) == typeof(AltDepDisposableProperty))
                return (T)(object)new AltDepDisposableProperty();
            if (typeof(T) == typeof(AltRequestScopeDepDisposableProperty))
                return (T)(object)HostContext.Instance.GetOrCreate(() => new AltRequestScopeDepDisposableProperty());
            
			return default(T);
		}

		public T Resolve<T>()
		{
			if (typeof(T) == typeof(AltDepCtor))
				return (T)(object)new AltDepCtor();

			return default(T);
		}

        public void Release(object instance)
        {
            var disposable = instance as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }
    }


    public class IocRequestFilterAttribute : Attribute, IHasRequestFilter
    {
        public FunqSingletonScope FunqSingletonScope { get; set; }
        public FunqRequestScope FunqRequestScope { get; set; }
        public FunqNoneScope FunqNoneScope { get; set; }
        public FunqRequestScopeDepDisposableProperty FunqRequestScopeDepDisposableProperty { get; set; }
        public AltRequestScopeDepDisposableProperty AltRequestScopeDepDisposableProperty { get; set; }

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