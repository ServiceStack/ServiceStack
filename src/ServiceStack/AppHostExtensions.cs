using System;
using System.Linq;
using System.Reflection;
using Funq;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
	public static class AppHostExtensions
	{
		private static ILog log = LogManager.GetLogger(typeof(AppHostExtensions));

		public static void RegisterService<TService>(this IAppHost appHost, params string[] atRestPaths)
		{
			appHost.RegisterService(typeof(TService), atRestPaths);
		}

		public static void RegisterRequestBinder<TRequest>(this IAppHost appHost, Func<IRequest, object> binder)
		{
			appHost.RequestBinders[typeof(TRequest)] = binder;
		}

		public static void AddPluginsFromAssembly(this IAppHost appHost, params Assembly[] assembliesWithPlugins)
		{
		    var ssHost = (ServiceStackHost)appHost;
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
                        var plugin = pluginType.CreateInstance() as IPlugin;
						if (plugin != null)
						{
                            ssHost.LoadPlugin(plugin);
						}
					}
					catch (Exception ex)
					{
						log.Error("Error adding new Plugin " + pluginType.GetOperationName(), ex);
					}
				}
			}
		}

        public static T GetPlugin<T>(this IAppHost appHost) where T : class, IPlugin
        {
            return appHost.Plugins.FirstOrDefault(x => x is T) as T;
        }

        public static bool HasPlugin<T>(this IAppHost appHost) where T : class, IPlugin
        {
            return appHost.Plugins.FirstOrDefault(x => x is T) != null;
        }

        public static bool HasMultiplePlugins<T>(this IAppHost appHost) where T : class, IPlugin
        {
            return appHost.Plugins.Count(x => x is T) > 1;
        }

        /// <summary>
        /// Get an IAppHost container. 
        /// Note: Registering dependencies should only be done during setup/configuration 
        /// stage and remain immutable there after for thread-safety.
        /// </summary>
        /// <param name="appHost"></param>
        /// <returns></returns>
        public static Container GetContainer(this IAppHost appHost)
        {
            if (appHost == null) return null;

            var hasContainer = appHost as IHasContainer;
            return hasContainer != null ? hasContainer.Container : null;
        }

        public static bool NotifyStartupException(this IAppHost appHost, Exception ex)
        {
            var ssHost = HostContext.AppHost;
            if (ssHost == null) return false;

            if (!ssHost.HasStarted)
            {
                ssHost.OnStartupException(ex);
            }
            return !ssHost.HasStarted;
        }

        public static string Localize(this string text, IRequest request)
        {
            return HostContext.AppHost.ResolveLocalizedString(text, request);
        }
	}

}