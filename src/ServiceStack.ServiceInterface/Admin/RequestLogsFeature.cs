using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceInterface.Providers;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.Admin
{
    public class RequestLogsFeature : IPlugin
    {
        /// <summary>
        /// RequestLogs service Route, default is /requestlogs
        /// </summary>
        public string AtRestPath { get; set; }

        /// <summary>
        /// Turn On/Off Session Tracking
        /// </summary>
        public bool EnableSessionTracking { get; set; }

        /// <summary>
        /// Turn On/Off Tracking of Responses
        /// </summary>
        public bool EnableResponseTracking { get; set; }

        /// <summary>
        /// Turn On/Off Tracking of Exceptions
        /// </summary>
        public bool EnableErrorTracking { get; set; }

        /// <summary>
        /// Size of InMemoryRollingRequestLogger circular buffer
        /// </summary>
        public int? Capacity { get; set; }

        /// <summary>
        /// Access to /requestlogs requires this password
        /// </summary>
        public string AdminPassword { get; set; }

        /// <summary>
        /// Change the RequestLogger provider. Default is InMemoryRollingRequestLogger
        /// </summary>
        public IRequestLogger RequestLogger { get; set; }

        /// <summary>
        /// Don't log requests of these types. By default RequestLog's are excluded
        /// </summary>
        public Type[] ExcludeRequestDtoTypes { get; set; }

        /// <summary>
        /// Don't log request bodys for services with sensitive information.
        /// By default Auth and Registration requests are hidden.
        /// </summary>
        public Type[] HideRequestBodyForRequestDtoTypes { get; set; }

        public RequestLogsFeature(string adminPassword, int? capacity = null)
        {
            this.AtRestPath = "/requestlogs";
            this.AdminPassword = adminPassword;
            this.Capacity = capacity;
            this.EnableErrorTracking = true;
            this.ExcludeRequestDtoTypes = new[] { typeof(RequestLogs) };
            this.HideRequestBodyForRequestDtoTypes = new[] {
                typeof(Auth.Auth), typeof(Registration)
            };
        }

        public void Register(IAppHost appHost)
        {
            appHost.RegisterService<RequestLogsService>(AtRestPath);

            var requestLogger = RequestLogger ?? new InMemoryRollingRequestLogger(AdminPassword, Capacity);
            requestLogger.EnableSessionTracking = EnableSessionTracking;
            requestLogger.EnableResponseTracking = EnableResponseTracking;
            requestLogger.EnableErrorTracking = EnableErrorTracking;
            requestLogger.ExcludeRequestDtoTypes = ExcludeRequestDtoTypes;
            requestLogger.HideRequestBodyForRequestDtoTypes = HideRequestBodyForRequestDtoTypes;

            appHost.Register(requestLogger);
        }
    }
}
