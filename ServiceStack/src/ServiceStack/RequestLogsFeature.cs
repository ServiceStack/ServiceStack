using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Admin;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.NativeTypes;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack;

public class RequestLogsFeature : IPlugin, Model.IHasStringId, IPreInitPlugin, IConfigureServices
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
    /// Turn On/Off Raw Request Body Tracking per-request
    /// </summary>
    public Func<IRequest, bool> RequestBodyTrackingFilter { get; set; }

    /// <summary>
    /// Turn On/Off Tracking of Responses
    /// </summary>
    public bool EnableResponseTracking { get; set; }

    /// <summary>
    /// Turn On/Off Tracking of Responses per-request
    /// </summary>
    public Func<IRequest, bool> ResponseTrackingFilter { get; set; }
        
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
    /// Don't log Response DTO Types
    /// </summary>
    public Type[] ExcludeResponseTypes { get; set; }

    /// <summary>
    /// Limit logging to only Service Requests
    /// </summary>
    public bool LimitToServiceRequests { get; set; }
        
    /// <summary>
    /// Customize Request Log Entry
    /// </summary>
    public Action<IRequest, RequestLogEntry> RequestLogFilter { get; set; }

    /// <summary>
    /// Ignore logging and serializing these Request DTOs
    /// </summary>
    public List<Type> IgnoreTypes { get; set; } = [];
        
    /// <summary>
    /// Use custom Ignore Request DTO predicate
    /// </summary>
    public Func<object,bool> IgnoreFilter { get; set; } 

    /// <summary>
    /// Change what DateTime to use for the current Date (defaults to UtcNow)
    /// </summary>
    public Func<DateTime> CurrentDateFn { get; set; } = () => DateTime.UtcNow;

    /// <summary>
    /// Default take, if none is specified
    /// </summary>
    public int DefaultLimit { get; set; } = 100;

    public string RegisterAllowRuntimeTypeInTypes { get; set; } = typeof(RequestLogEntry).FullName;
        
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
            
        // Sync with ProfilingFeature
        this.ExcludeRequestDtoTypes =
        [
            typeof(RequestLogs),
            typeof(HotReloadFiles),
            typeof(TypesCommonJs),
            typeof(MetadataApp),
            typeof(AdminDashboard),
            typeof(AdminProfiling),
            typeof(AdminRedis),
            typeof(NativeTypesBase)
        ];
        this.HideRequestBodyForRequestDtoTypes =
        [
            typeof(Authenticate), 
            typeof(Register)
        ];
        this.ExcludeResponseTypes =
        [
            typeof(AppMetadata),
            typeof(MetadataTypes),
            typeof(byte[]),
            typeof(string)
        ];
    }

    private Type requestLoggerType = null;

    public void Configure(IServiceCollection services)
    {
        if (!string.IsNullOrEmpty(AtRestPath))
            services.RegisterService<RequestLogsService>(AtRestPath);

        var requestLogger = RequestLogger ?? new InMemoryRollingRequestLogger(Capacity);
        requestLogger.EnableSessionTracking = EnableSessionTracking;
        requestLogger.EnableResponseTracking = EnableResponseTracking;
        requestLogger.ResponseTrackingFilter = ResponseTrackingFilter;
        requestLogger.EnableRequestBodyTracking = EnableRequestBodyTracking;
        requestLogger.RequestBodyTrackingFilter = RequestBodyTrackingFilter;
        requestLogger.LimitToServiceRequests = LimitToServiceRequests;
        requestLogger.SkipLogging = SkipLogging;
        requestLogger.EnableErrorTracking = EnableErrorTracking;
        requestLogger.ExcludeRequestDtoTypes = ExcludeRequestDtoTypes;
        requestLogger.HideRequestBodyForRequestDtoTypes = HideRequestBodyForRequestDtoTypes;
        requestLogger.ExcludeResponseTypes = ExcludeResponseTypes;
        requestLogger.RequestLogFilter = RequestLogFilter;
        requestLogger.IgnoreFilter = IgnoreFilter;
        requestLogger.CurrentDateFn = CurrentDateFn;

        RequestLogger ??= requestLogger;
        services.AddSingleton(requestLogger);
        requestLoggerType = requestLogger.GetType();
    }

    public void Register(IAppHost appHost)
    {
        if (appHost is ServiceStackHost host)
            host.AddTimings = true;
        
        if (RegisterAllowRuntimeTypeInTypes != null)
            JsConfig.AllowRuntimeTypeInTypes.Add(RegisterAllowRuntimeTypeInTypes);

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
                ServiceRoutes = new() {
                    { nameof(RequestLogsService), [AtRestPath] },
                },
                RequestLogger = requestLoggerType?.Name,
                DefaultLimit = DefaultLimit,
            };
        });

        if (RequestLogger is IRequireRegistration requireRegistration)
        {
            requireRegistration.Register(appHost);
        }
    }

    public void BeforePluginsLoaded(IAppHost appHost)
    {
        appHost.ConfigurePlugin<UiFeature>(feature =>
        {
            var role = AccessRole; 
            feature.AddAdminLink(AdminUiFeature.Logging, new LinkInfo {
                Id = "logging",
                Label = "Logging",
                Icon = Svg.ImageSvg(Svg.Create(Svg.Body.Logs)),
                Show = $"role:{role}",
            });
        });
    }
}