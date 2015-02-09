using System;
using System.Linq;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class ServiceRoutes : IServiceRoutes
    {
        private static ILog log = LogManager.GetLogger(typeof(ServiceRoutes));

        private readonly ServiceStackHost appHost;
        public ServiceRoutes(ServiceStackHost appHost)
        {
            this.appHost = appHost;
        }

        public IServiceRoutes Add<TRequest>(string restPath)
        {
            if (HasExistingRoute(typeof(TRequest), restPath)) return this;

            appHost.RestPaths.Add(new RestPath(typeof(TRequest), restPath));
            return this;
        }

        public IServiceRoutes Add<TRequest>(string restPath, string verbs)
        {
            if (HasExistingRoute(typeof(TRequest), restPath)) return this;

            appHost.RestPaths.Add(new RestPath(typeof(TRequest), restPath, verbs));
            return this;
        }

        public IServiceRoutes Add(Type requestType, string restPath, string verbs)
        {
            if (HasExistingRoute(requestType, restPath)) return this;

            appHost.RestPaths.Add(new RestPath(requestType, restPath, verbs));
            return this;
        }

        public IServiceRoutes Add(Type requestType, string restPath, string verbs, int priority)
        {
            if (HasExistingRoute(requestType, restPath)) return this;

            appHost.RestPaths.Add(new RestPath(requestType, restPath, verbs)
            {
                Priority = priority
            });
            return this;
        }

        public IServiceRoutes Add(Type requestType, string restPath, string verbs, string summary, string notes)
        {
            if (HasExistingRoute(requestType, restPath)) return this;

            appHost.RestPaths.Add(new RestPath(requestType, restPath, verbs, summary, notes));
            return this;
        }

        private bool HasExistingRoute(Type requestType, string restPath)
        {
            var existingRoute = appHost.RestPaths.FirstOrDefault(
                x => x.RequestType == requestType && x.Path == restPath);

            if (existingRoute != null)
            {
                var existingRouteMsg = "Existing Route for '{0}' at '{1}' already exists".Fmt(requestType.GetOperationName(), restPath);

                //if (!EndpointHostConfig.SkipRouteValidation) //wait till next deployment
                //    throw new Exception(existingRouteMsg);

                log.Warn(existingRouteMsg);
                return true;
            }

            return false;
        }
    }
}