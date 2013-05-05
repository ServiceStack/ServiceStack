using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceHost
{
    public class ServiceRoutes : IServiceRoutes
    {
        private static ILog log = LogManager.GetLogger(typeof(ServiceRoutes));

        public readonly List<RestPath> RestPaths = new List<RestPath>();

        public IServiceRoutes Add<TRequest>(string restPath)
        {
            RestPaths.Add(new RestPath(typeof(TRequest), restPath));
            return this;
        }

        public IServiceRoutes Add<TRequest>(string restPath, string verbs)
        {
            AssertNoExistingRoute(typeof(TRequest), restPath);

            RestPaths.Add(new RestPath(typeof(TRequest), restPath, verbs));
            return this;
        }

        public IServiceRoutes Add(Type requestType, string restPath, string verbs)
        {
            AssertNoExistingRoute(requestType, restPath);

            RestPaths.Add(new RestPath(requestType, restPath, verbs));
            return this;
        }

        public IServiceRoutes Add(Type requestType, string restPath, string verbs, string summary, string notes)
        {
            AssertNoExistingRoute(requestType, restPath);

            RestPaths.Add(new RestPath(requestType, restPath, verbs, summary, notes));
            return this;
        }

        private void AssertNoExistingRoute(Type requestType, string restPath)
	    {
	        var existingRoute = RestPaths.FirstOrDefault(
	            x => x.RequestType == requestType && x.Path == restPath);
	        
            if (existingRoute != null)
	        {
                var existingRouteMsg = "Existing Route for '{0}' at '{1}' already exists".Fmt(requestType.Name, restPath);
                if (!EndpointHostConfig.SkipRouteValidation)
                    throw new Exception(existingRouteMsg);
	
                log.Warn(existingRouteMsg);
	        }       
	    }
    }
}