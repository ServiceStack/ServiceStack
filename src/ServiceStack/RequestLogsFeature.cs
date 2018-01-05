using System;
using ServiceStack.Admin;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack
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
        /// Turn On/Off Logging of Raw Request Body, default is Off
        /// </summary>
        public bool EnableRequestBodyTracking { get; set; }

        /// <summary>
        /// Turn On/Off Tracking of Responses
        /// </summary>
        public bool EnableResponseTracking { get; set; }

        /// <summary>
        /// Turn On/Off Tracking of Exceptions
        /// </summary>
        public bool EnableErrorTracking { get; set; }

        /// <summary>
        /// Don't log matching requests
        /// </summary>
        public Func<IRequest, bool> SkipLogging { get; set; }

        /// <summary>
        /// Size of InMemoryRollingRequestLogger circular buffer
        /// </summary>
        public int? Capacity { get; set; }

        /// <summary>
        /// Limit access to /requestlogs service to these roles
        /// </summary>
        public string[] RequiredRoles { get; set; }

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
        
        /// <summary>
        /// Limit logging to only Service Requests
        /// </summary>
        public bool LimitToServiceRequests { get; set; }

        public RequestLogsFeature(int capacity) : this()
        {
            this.Capacity = capacity;
        }

        public RequestLogsFeature()
        {
            this.AtRestPath = "/requestlogs";
            this.RequiredRoles = new[] { RoleNames.Admin };
            this.EnableErrorTracking = true;
            this.EnableRequestBodyTracking = false;
            this.LimitToServiceRequests = true;
            this.ExcludeRequestDtoTypes = new[] { typeof(RequestLogs) };
            this.HideRequestBodyForRequestDtoTypes = new[] {
                typeof(Authenticate), typeof(Register)
            };
        }

        public void Register(IAppHost appHost)
        {
            appHost.RegisterService<RequestLogsService>(AtRestPath);

            var requestLogger = RequestLogger ?? new InMemoryRollingRequestLogger(Capacity);
            requestLogger.EnableSessionTracking = EnableSessionTracking;
            requestLogger.EnableResponseTracking = EnableResponseTracking;
            requestLogger.EnableRequestBodyTracking = EnableRequestBodyTracking;
            requestLogger.LimitToServiceRequests = LimitToServiceRequests;
            requestLogger.SkipLogging = SkipLogging;
            requestLogger.RequiredRoles = RequiredRoles;
            requestLogger.EnableErrorTracking = EnableErrorTracking;
            requestLogger.ExcludeRequestDtoTypes = ExcludeRequestDtoTypes;
            requestLogger.HideRequestBodyForRequestDtoTypes = HideRequestBodyForRequestDtoTypes;

            appHost.Register(requestLogger);

            if (EnableRequestBodyTracking)
            {
                appHost.PreRequestFilters.Insert(0, (httpReq, httpRes) =>
                {
                    httpReq.UseBufferedStream = EnableRequestBodyTracking;
                });
            }

            appHost.GetPlugin<MetadataFeature>()
                .AddDebugLink(AtRestPath, "Request Logs");
        }
    }
}