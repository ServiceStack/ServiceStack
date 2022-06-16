using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Admin;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack
{
    public class RequestLogsFeature : IPlugin, Model.IHasStringId, IPreInitPlugin
    {
        public string Id { get; set; } = Plugins.RequestLogs;
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
        [Obsolete("Use AccessRole")]
        public string[] RequiredRoles { get; set; }
        
        /// <summary>
        /// Limit API access to users in role
        /// </summary>
        public string AccessRole { get; set; } = RoleNames.Admin;

        /// <summary>
        /// Change the RequestLogger provider. Default is InMemoryRollingRequestLogger
        /// </summary>
        public IRequestLogger RequestLogger { get; set; }

        /// <summary>
        /// Don't log requests of these types. By default RequestLog's are excluded
        /// </summary>
        public Type[] ExcludeRequestDtoTypes { get; set; }

        /// <summary>
        /// Don't log request body's for services with sensitive information.
        /// By default Auth and Registration requests are hidden.
        /// </summary>
        public Type[] HideRequestBodyForRequestDtoTypes { get; set; }
        
        /// <summary>
        /// Limit logging to only Service Requests
        /// </summary>
        public bool LimitToServiceRequests { get; set; }
        
        /// <summary>
        /// Customize Request Log Entry
        /// </summary>
        public Action<IRequest, RequestLogEntry> RequestLogFilter { get; set; }

        /// <summary>
        /// Never attempt to serialize these types
        /// </summary>
        public List<Type> IgnoreTypes { get; set; } = new();
        
        /// <summary>
        /// Allow ignoring 
        /// </summary>
        public Func<object,bool> IgnoreFilter { get; set; } 

        /// <summary>
        /// Change what DateTime to use for the current Date (defaults to UtcNow)
        /// </summary>
        public Func<DateTime> CurrentDateFn { get; set; } = () => DateTime.UtcNow;

        
        public bool DefaultIgnoreFilter(object o)
        {
            var type = o.GetType();
            return IgnoreTypes?.Contains(type) == true || o is IDisposable;
        }
        

        public RequestLogsFeature(int capacity) : this()
        {
            this.Capacity = capacity;
        }

        public RequestLogsFeature()
        {
            this.AtRestPath = "/requestlogs";
            this.IgnoreFilter = DefaultIgnoreFilter;
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
            if (!string.IsNullOrEmpty(AtRestPath))
                appHost.RegisterService<RequestLogsService>(AtRestPath);

            var requestLogger = RequestLogger ?? new InMemoryRollingRequestLogger(Capacity);
            requestLogger.EnableSessionTracking = EnableSessionTracking;
            requestLogger.EnableResponseTracking = EnableResponseTracking;
            requestLogger.EnableRequestBodyTracking = EnableRequestBodyTracking;
            requestLogger.LimitToServiceRequests = LimitToServiceRequests;
            requestLogger.SkipLogging = SkipLogging;
            requestLogger.RequiredRoles = RequiredRoles ?? new []{ AccessRole };
            requestLogger.EnableErrorTracking = EnableErrorTracking;
            requestLogger.ExcludeRequestDtoTypes = ExcludeRequestDtoTypes;
            requestLogger.HideRequestBodyForRequestDtoTypes = HideRequestBodyForRequestDtoTypes;
            requestLogger.RequestLogFilter = RequestLogFilter;
            requestLogger.IgnoreFilter = IgnoreFilter;
            requestLogger.CurrentDateFn = CurrentDateFn;

            appHost.Register(requestLogger);

            if (EnableRequestBodyTracking)
            {
                appHost.PreRequestFilters.Insert(0, (httpReq, httpRes) =>
                {
#if NETCORE
                    // https://forums.servicestack.net/t/unexpected-end-of-stream-when-uploading-to-aspnet-core/6478/6
                    if (httpReq.ContentType.MatchesContentType(MimeTypes.MultiPartFormData))
                        return;                    
#endif
                    httpReq.UseBufferedStream = EnableRequestBodyTracking;
                });
            }

            appHost.ConfigurePlugin<MetadataFeature>(feature =>
            {
                feature.ExportTypes.Add(typeof(RequestLogEntry));
                feature.AddDebugLink(AtRestPath, "Request Logs");
            });
            
            appHost.AddToAppMetadata(meta => {
                meta.Plugins.RequestLogs = new RequestLogsInfo {
                    AccessRole = AccessRole,
                    RequiredRoles = RequiredRoles,
                    ServiceRoutes = new Dictionary<string, string[]> {
                        { nameof(RequestLogsService), new[] {AtRestPath} },
                    },
                    RequestLogger = requestLogger.GetType().Name,
                };
            });
        }

        public void BeforePluginsLoaded(IAppHost appHost)
        {
            appHost.ConfigurePlugin<UiFeature>(feature =>
            {
                var role = RequiredRoles?.Length > 0 ? RequiredRoles[0] : AccessRole; 
                feature.AddAdminLink(AdminUi.Logging, new LinkInfo {
                    Id = "logging",
                    Label = "Logging",
                    Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Logs)),
                    Show = $"role:{role}",
                });
            });
        }
    }
}
