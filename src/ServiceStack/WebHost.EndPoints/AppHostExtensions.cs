using System;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints
{
	public static class AppHostExtensions
	{
		public static void RegisterService<TService>(this IAppHost appHost, params string[] atRestPaths)
		{
			appHost.RegisterService(typeof(TService), atRestPaths);
		}

		public static void RegisterRequestBinder<TRequest>(this IAppHost appHost, Func<IHttpRequest, object> binder)
		{
			appHost.RequestBinders[typeof(TRequest)] = binder;
		}
	}
}