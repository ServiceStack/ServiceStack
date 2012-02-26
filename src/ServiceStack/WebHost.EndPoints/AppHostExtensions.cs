using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceStack.Common.Utils;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints
{
	public static class AppHostExtensions
	{
		private static ILog log = LogManager.GetLogger(typeof(AppHostExtensions));

		public static void RegisterService<TService>(this IAppHost appHost, params string[] atRestPaths)
		{
			appHost.RegisterService(typeof(TService), atRestPaths);
		}

		public static void RegisterRequestBinder<TRequest>(this IAppHost appHost, Func<IHttpRequest, object> binder)
		{
			appHost.RequestBinders[typeof(TRequest)] = binder;
		}

		public static void AddPluginsFromAssembly(this IAppHost appHost, params Assembly[] assembliesWithPlugins)
		{
			foreach (Assembly assembly in assembliesWithPlugins)
			{
				var pluginTypes =
					from t in assembly.GetExportedTypes()
					where t.GetInterfaces().Any(x => x == typeof(IPlugin))
					select t;

				foreach (var pluginType in pluginTypes)
				{
					try
					{
						var plugin = ReflectionUtils.CreateInstance(pluginType) as IPlugin;
						if (plugin != null)
						{
							EndpointHost.AddPlugin(plugin);
						}
					}
					catch (Exception ex)
					{
						log.Error("Error adding new Plugin " + pluginType.Name, ex);
					}
				}
			}
		}
	}

}