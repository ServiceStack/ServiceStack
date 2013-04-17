using System;
using System.Collections.Generic;
using System.Linq;
using Funq;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.MiniProfiler;
using ServiceStack.ServiceHost;
using ServiceStack.VirtualPath;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Formats;
using ServiceStack.WebHost.Endpoints.Support;
using ServiceStack.WebHost.Endpoints.Utils;

namespace ServiceStack.WebHost.Endpoints
{
    public class EndpointHost
    {
        public static IAppHost AppHost { get; internal set; }

        public static IContentTypeFilter ContentTypeFilter { get; set; }

        public static List<Action<IHttpRequest, IHttpResponse>> RawRequestFilters { get; private set; }

        public static List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get; private set; }

        public static List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get; private set; }

        public static List<IViewEngine> ViewEngines { get; set; }

        public static HandleUncaughtExceptionDelegate ExceptionHandler { get; set; }

        public static HandleServiceExceptionDelegate ServiceExceptionHandler { get; set; }

        public static List<HttpHandlerResolverDelegate> CatchAllHandlers { get; set; }

        private static bool pluginsLoaded = false;

        public static List<IPlugin> Plugins { get; set; }

        public static IVirtualPathProvider VirtualPathProvider { get; set; }

        public static DateTime StartedAt { get; set; }

        public static DateTime ReadyAt { get; set; }

        private static void Reset()
        {
            ContentTypeFilter = HttpResponseFilter.Instance;
            RawRequestFilters = new List<Action<IHttpRequest, IHttpResponse>>();
            RequestFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
            ResponseFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
            ViewEngines = new List<IViewEngine>();
            CatchAllHandlers = new List<HttpHandlerResolverDelegate>();
            Plugins = new List<IPlugin> {
                new HtmlFormat(),
                new CsvFormat(),
                new MarkdownFormat(),
                new PredefinedRoutesFeature(),
                new MetadataFeature(),
            };

            //Default Config for projects that want to use components but not WebFramework (e.g. MVC)
            Config = new EndpointHostConfig(
                "Empty Config",
                new ServiceManager(new Container(), new ServiceController(null)));
        }

        // Pre user config
        public static void ConfigureHost(IAppHost appHost, string serviceName, ServiceManager serviceManager)
        {
            Reset();
            AppHost = appHost;

            EndpointHostConfig.Instance.ServiceName = serviceName;
            EndpointHostConfig.Instance.ServiceManager = serviceManager;

            var config = EndpointHostConfig.Instance;
            Config = config; // avoid cross-dependency on Config setter
            VirtualPathProvider = new FileSystemVirtualPathProvider(AppHost, Config.WebHostPhysicalPath);

            Config.DebugMode = appHost.GetType().Assembly.IsDebugBuild();
            if (Config.DebugMode)
            {
                Plugins.Add(new RequestInfoFeature());
            }
        }

        // Config has changed
        private static void ApplyConfigChanges()
        {
            config.ServiceEndpointsMetadataConfig = ServiceEndpointsMetadataConfig.Create(config.ServiceStackHandlerFactoryPath);

            JsonDataContractSerializer.Instance.UseBcl = config.UseBclJsonSerializers;
            JsonDataContractDeserializer.Instance.UseBcl = config.UseBclJsonSerializers;
        }

        //After configure called
        public static void AfterInit()
        {
            StartedAt = DateTime.UtcNow;

            if (config.EnableFeatures != Feature.All)
            {
                if ((Feature.Xml & config.EnableFeatures) != Feature.Xml)
                    config.IgnoreFormatsInMetadata.Add("xml");
                if ((Feature.Json & config.EnableFeatures) != Feature.Json)
                    config.IgnoreFormatsInMetadata.Add("json");
                if ((Feature.Jsv & config.EnableFeatures) != Feature.Jsv)
                    config.IgnoreFormatsInMetadata.Add("jsv");
                if ((Feature.Csv & config.EnableFeatures) != Feature.Csv)
                    config.IgnoreFormatsInMetadata.Add("csv");
                if ((Feature.Html & config.EnableFeatures) != Feature.Html)
                    config.IgnoreFormatsInMetadata.Add("html");
                if ((Feature.Soap11 & config.EnableFeatures) != Feature.Soap11)
                    config.IgnoreFormatsInMetadata.Add("soap11");
                if ((Feature.Soap12 & config.EnableFeatures) != Feature.Soap12)
                    config.IgnoreFormatsInMetadata.Add("soap12");
            }

            if ((Feature.Html & config.EnableFeatures) != Feature.Html)
                Plugins.RemoveAll(x => x is HtmlFormat);

            if ((Feature.Csv & config.EnableFeatures) != Feature.Csv)
                Plugins.RemoveAll(x => x is CsvFormat);

            if ((Feature.Markdown & config.EnableFeatures) != Feature.Markdown)
                Plugins.RemoveAll(x => x is MarkdownFormat);

            if ((Feature.PredefinedRoutes & config.EnableFeatures) != Feature.PredefinedRoutes)
                Plugins.RemoveAll(x => x is PredefinedRoutesFeature);

            if ((Feature.Metadata & config.EnableFeatures) != Feature.Metadata)
                Plugins.RemoveAll(x => x is MetadataFeature);

            if ((Feature.RequestInfo & config.EnableFeatures) != Feature.RequestInfo)
                Plugins.RemoveAll(x => x is RequestInfoFeature);

            if ((Feature.Razor2 & config.EnableFeatures) != Feature.Razor2)
                Plugins.RemoveAll(x => x is IRazor2Plugin);    //external

            if ((Feature.ProtoBuf & config.EnableFeatures) != Feature.ProtoBuf)
                Plugins.RemoveAll(x => x is IProtoBufPlugin); //external

            if ((Feature.MsgPack & config.EnableFeatures) != Feature.MsgPack)
                Plugins.RemoveAll(x => x is IMsgPackPlugin);  //external

            if (ExceptionHandler == null)
            {
                ExceptionHandler = (httpReq, httpRes, operationName, ex) => {
                    var errorMessage = String.Format("Error occured while Processing Request: {0}", ex.Message);
                    var statusCode = ex.ToStatusCode();
                    //httpRes.WriteToResponse always calls .Close in it's finally statement so 
                    //if there is a problem writing to response, by now it will be closed
                    if (!httpRes.IsClosed)
                    {
                        httpRes.WriteErrorToResponse(httpReq, httpReq.ResponseContentType, operationName, errorMessage, ex, statusCode);
                    }
                };
            }

            if (config.ServiceStackHandlerFactoryPath != null)
                config.ServiceStackHandlerFactoryPath = config.ServiceStackHandlerFactoryPath.TrimStart('/');

            var specifiedContentType = config.DefaultContentType; //Before plugins loaded

            ConfigurePlugins();

            AppHost.LoadPlugin(Plugins.ToArray());
            pluginsLoaded = true;

            AfterPluginsLoaded(specifiedContentType);

            var registeredCacheClient = AppHost.TryResolve<ICacheClient>();
            using (registeredCacheClient)
            {
                if (registeredCacheClient == null)
                {
                    Container.Register<ICacheClient>(new MemoryCacheClient());
                }
            }

            ReadyAt = DateTime.UtcNow;
        }

        public static T TryResolve<T>()
        {
            return AppHost != null ? AppHost.TryResolve<T>() : default(T);
        }

        /// <summary>
        /// The AppHost.Container. Note: it is not thread safe to register dependencies after AppStart.
        /// </summary>
        public static Container Container
        {
            get
            {
                var aspHost = AppHost as AppHostBase;
                if (aspHost != null)
                    return aspHost.Container;
                var listenerHost = AppHost as HttpListenerBase;
                return listenerHost != null ? listenerHost.Container : new Container(); //testing may use alt AppHost
            }
        }

        private static void ConfigurePlugins()
        {
            //Some plugins need to initialize before other plugins are registered.

            foreach (var plugin in Plugins)
            {
                var preInitPlugin = plugin as IPreInitPlugin;
                if (preInitPlugin != null)
                {
                    preInitPlugin.Configure(AppHost);
                }
            }
        }

        private static void AfterPluginsLoaded(string specifiedContentType)
        {
            if (!String.IsNullOrEmpty(specifiedContentType))
                config.DefaultContentType = specifiedContentType;
            else if (String.IsNullOrEmpty(config.DefaultContentType))
                config.DefaultContentType = ContentType.Json;

            config.ServiceManager.AfterInit();
            ServiceManager = config.ServiceManager; //reset operations
        }

        public static T GetPlugin<T>() where T : class, IPlugin 
        {
            return Plugins.FirstOrDefault(x => x is T) as T;
        }

        public static void AddPlugin(params IPlugin[] plugins)
        {
            if (pluginsLoaded)
            {
                AppHost.LoadPlugin(plugins);
            }
            else
            {
                foreach (var plugin in plugins)
                {
                    Plugins.Add(plugin);
                }
            }
        }

        public static ServiceManager ServiceManager
        {
            get { return config.ServiceManager; }
            set { config.ServiceManager = value; }
        }

        private static EndpointHostConfig config;

        public static EndpointHostConfig Config
        {
            get
            {
                return config;
            }
            set
            {
                if (value.ServiceName == null)
                    throw new ArgumentNullException("ServiceName");

                if (value.ServiceController == null)
                    throw new ArgumentNullException("ServiceController");

                config = value;
                ApplyConfigChanges();
            }
        }

        public static bool DebugMode
        {
            get { return Config != null && Config.DebugMode; }
        }

        public static ServiceMetadata Metadata { get { return Config.Metadata; } }

        /// <summary>
        /// Applies the raw request filters. Returns whether or not the request has been handled 
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public static bool ApplyPreRequestFilters(IHttpRequest httpReq, IHttpResponse httpRes)
        {
            foreach (var requestFilter in RawRequestFilters)
            {
                requestFilter(httpReq, httpRes);
                if (httpRes.IsClosed) break;
            }

            return httpRes.IsClosed;
        }

        /// <summary>
        /// Applies the request filters. Returns whether or not the request has been handled 
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public static bool ApplyRequestFilters(IHttpRequest httpReq, IHttpResponse httpRes, object requestDto)
        {
            httpReq.ThrowIfNull("httpReq");
            httpRes.ThrowIfNull("httpRes");

            using (Profiler.Current.Step("Executing Request Filters"))
            {
                //Exec all RequestFilter attributes with Priority < 0
                var attributes = FilterAttributeCache.GetRequestFilterAttributes(requestDto.GetType());
                var i = 0;
                for (; i < attributes.Length && attributes[i].Priority < 0; i++)
                {
                    var attribute = attributes[i];
                    ServiceManager.Container.AutoWire(attribute);
                    attribute.RequestFilter(httpReq, httpRes, requestDto);
                    if (AppHost != null) //tests
                        AppHost.Release(attribute);
                    if (httpRes.IsClosed) return httpRes.IsClosed;
                }

                //Exec global filters
                foreach (var requestFilter in RequestFilters)
                {
                    requestFilter(httpReq, httpRes, requestDto);
                    if (httpRes.IsClosed) return httpRes.IsClosed;
                }

                //Exec remaining RequestFilter attributes with Priority >= 0
                for (; i < attributes.Length; i++)
                {
                    var attribute = attributes[i];
                    ServiceManager.Container.AutoWire(attribute);
                    attribute.RequestFilter(httpReq, httpRes, requestDto);
                    if (AppHost != null) //tests
                        AppHost.Release(attribute);
                    if (httpRes.IsClosed) return httpRes.IsClosed;
                }

                return httpRes.IsClosed;
            }
        }

        /// <summary>
        /// Applies the response filters. Returns whether or not the request has been handled 
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public static bool ApplyResponseFilters(IHttpRequest httpReq, IHttpResponse httpRes, object response)
        {
            httpReq.ThrowIfNull("httpReq");
            httpRes.ThrowIfNull("httpRes");

            using (Profiler.Current.Step("Executing Response Filters"))
            {
                var responseDto = response.ToResponseDto();
                var attributes = responseDto != null
                    ? FilterAttributeCache.GetResponseFilterAttributes(responseDto.GetType())
                    : null;

                //Exec all ResponseFilter attributes with Priority < 0
                var i = 0;
                if (attributes != null)
                {
                    for (; i < attributes.Length && attributes[i].Priority < 0; i++)
                    {
                        var attribute = attributes[i];
                        ServiceManager.Container.AutoWire(attribute);
                        attribute.ResponseFilter(httpReq, httpRes, response);
                        if (AppHost != null) //tests
                            AppHost.Release(attribute);
                        if (httpRes.IsClosed) return httpRes.IsClosed;
                    }
                }

                //Exec global filters
                foreach (var responseFilter in ResponseFilters)
                {
                    responseFilter(httpReq, httpRes, response);
                    if (httpRes.IsClosed) return httpRes.IsClosed;
                }

                //Exec remaining RequestFilter attributes with Priority >= 0
                if (attributes != null)
                {
                    for (; i < attributes.Length; i++)
                    {
                        var attribute = attributes[i];
                        ServiceManager.Container.AutoWire(attribute);
                        attribute.ResponseFilter(httpReq, httpRes, response);
                        if (AppHost != null) //tests
                            AppHost.Release(attribute);
                        if (httpRes.IsClosed) return httpRes.IsClosed;
                    }
                }

                return httpRes.IsClosed;
            }
        }

        internal static object ExecuteService(object request, EndpointAttributes endpointAttributes, IHttpRequest httpReq, IHttpResponse httpRes)
        {
            using (Profiler.Current.Step("Execute Service"))
            {
                return config.ServiceController.Execute(request,
                    new HttpRequestContext(httpReq, httpRes, request, endpointAttributes));
            }
        }

        public static IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
        {
            return AppHost != null
                ? AppHost.CreateServiceRunner<TRequest>(actionContext)
                : new ServiceRunner<TRequest>(null, actionContext);
        }

        /// <summary>
        /// Call to signal the completion of a ServiceStack-handled Request
        /// </summary>
        internal static void CompleteRequest()
        {
            try
            {
                if (AppHost != null)
                {
                    AppHost.OnEndRequest();
                }
            }
            catch (Exception ex) { }
        }
    }
}