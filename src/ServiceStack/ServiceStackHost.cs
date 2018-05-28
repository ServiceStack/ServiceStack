// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Funq;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Formats;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Metadata;
using ServiceStack.NativeTypes;
using ServiceStack.Serialization;
using ServiceStack.Text;
using ServiceStack.VirtualPath;
using ServiceStack.Web;
using ServiceStack.Redis;
using static System.String;

namespace ServiceStack
{
    public abstract partial class ServiceStackHost
        : IAppHost, IFunqlet, IHasContainer, IDisposable
    {
        private readonly ILog Log = LogManager.GetLogger(typeof(ServiceStackHost));

        public static ServiceStackHost Instance { get; protected set; }

        /// <summary>
        /// When the AppHost was instantiated.
        /// </summary>
        public DateTime StartedAt { get; set; }
        /// <summary>
        /// When the Init function was done.
        /// Called at begin of <see cref="OnAfterInit"/>
        /// </summary>
        public DateTime? AfterInitAt { get; set; }
        /// <summary>
        /// When all configuration was completed.
        /// Called at the end of <see cref="OnAfterInit"/>
        /// </summary>
        public DateTime? ReadyAt { get; set; }
        /// <summary>
        /// If app currently runs for unit tests.
        /// Used for overwritting AuthSession.
        /// </summary>
        public bool TestMode { get; set; }

        /// <summary>
        /// The assemblies reflected to find api services.
        /// These can be provided in the constructor call.
        /// </summary>
        public List<Assembly> ServiceAssemblies { get; private set; }

        /// <summary>
        /// Wether AppHost configuration is done.
        /// Note: It doesn't mean the start function was called.
        /// </summary>
        public bool HasStarted => ReadyAt != null;

        /// <summary>
        /// Wether AppHost is ready configured and either ready to run or already running.
        /// Equals <see cref="HasStarted"/>
        /// </summary>
        public static bool IsReady() => Instance?.ReadyAt != null;

        protected ServiceStackHost(string serviceName, params Assembly[] assembliesWithServices)
        {
            this.StartedAt = DateTime.UtcNow;

            ServiceName = serviceName;
            AppSettings = new AppSettings();
            Container = new Container { DefaultOwner = Owner.External };
            ServiceAssemblies = assembliesWithServices.ToList();

            ContentTypes = new ContentTypes();
            RestPaths = new List<RestPath>();
            Routes = new ServiceRoutes(this);
            Metadata = new ServiceMetadata(RestPaths);
            PreRequestFilters = new List<Action<IRequest, IResponse>>();
            RequestConverters = new List<Func<IRequest, object, Task<object>>>();
            ResponseConverters = new List<Func<IRequest, object, Task<object>>>();
            GlobalRequestFilters = new List<Action<IRequest, IResponse, object>>();
            GlobalRequestFiltersAsync = new List<Func<IRequest, IResponse, object, Task>>();
            GlobalTypedRequestFilters = new Dictionary<Type, ITypedFilter>();
            GlobalResponseFilters = new List<Action<IRequest, IResponse, object>>();
            GlobalResponseFiltersAsync = new List<Func<IRequest, IResponse, object, Task>>();
            GlobalTypedResponseFilters = new Dictionary<Type, ITypedFilter>();
            GlobalMessageRequestFilters = new List<Action<IRequest, IResponse, object>>();
            GlobalMessageRequestFiltersAsync = new List<Func<IRequest, IResponse, object, Task>>();
            GlobalTypedMessageRequestFilters = new Dictionary<Type, ITypedFilter>();
            GlobalMessageResponseFilters = new List<Action<IRequest, IResponse, object>>();
            GlobalMessageResponseFiltersAsync = new List<Func<IRequest, IResponse, object, Task>>();
            GlobalTypedMessageResponseFilters = new Dictionary<Type, ITypedFilter>();
            GatewayRequestFilters = new List<Action<IRequest, object>>();
            GatewayRequestFiltersAsync = new List<Func<IRequest, object, Task>>();
            GatewayResponseFilters = new List<Action<IRequest, object>>();
            GatewayResponseFiltersAsync = new List<Func<IRequest, object, Task>>();
            ViewEngines = new List<IViewEngine>();
            ServiceExceptionHandlers = new List<HandleServiceExceptionDelegate>();
            ServiceExceptionHandlersAsync = new List<HandleServiceExceptionAsyncDelegate>();
            UncaughtExceptionHandlers = new List<HandleUncaughtExceptionDelegate>();
            UncaughtExceptionHandlersAsync = new List<HandleUncaughtExceptionAsyncDelegate>();
            BeforeConfigure = new List<Action<ServiceStackHost>>();
            AfterConfigure = new List<Action<ServiceStackHost>>();
            AfterInitCallbacks = new List<Action<IAppHost>>();
            OnDisposeCallbacks = new List<Action<IAppHost>>();
            OnEndRequestCallbacks = new List<Action<IRequest>>();
            AddVirtualFileSources = new List<IVirtualPathProvider>();
            RawHttpHandlers = new List<Func<IHttpRequest, IHttpHandler>> {
                ReturnRedirectHandler,
                ReturnRequestInfoHandler,
            };
            CatchAllHandlers = new List<HttpHandlerResolverDelegate>();
            CustomErrorHttpHandlers = new Dictionary<HttpStatusCode, IServiceStackHandler> {
                { HttpStatusCode.Forbidden, new ForbiddenHttpHandler() },
                { HttpStatusCode.NotFound, new NotFoundHttpHandler() },
            };
            StartUpErrors = new List<ResponseStatus>();
            AsyncErrors = new List<ResponseStatus>();
            PluginsLoaded = new List<string>();
            Plugins = new List<IPlugin> {
                new HtmlFormat(),
                new CsvFormat(),
                new PredefinedRoutesFeature(),
                new MetadataFeature(),
                new NativeTypesFeature(),
                new HttpCacheFeature(),
                new RequestInfoFeature(),
            };
            ExcludeAutoRegisteringServiceTypes = new HashSet<Type> {
                typeof(AuthenticateService),
                typeof(RegisterService),
                typeof(AssignRolesService),
                typeof(UnAssignRolesService),
                typeof(NativeTypesService),
                typeof(PostmanService),
                typeof(TemplateHotReloadService),
                typeof(HotReloadFilesService),
                typeof(TemplateApiPagesService),
                typeof(TemplateMetadataDebugService),
                typeof(ServerEventsSubscribersService),
                typeof(ServerEventsUnRegisterService),
            };

            JsConfig.InitStatics();
        }

        public abstract void Configure(Container container);

        protected virtual ServiceController CreateServiceController(params Assembly[] assembliesWithServices)
        {
            return new ServiceController(this, assembliesWithServices);
            //Alternative way to inject Service Resolver strategy
            //return new ServiceController(this, () => assembliesWithServices.ToList().SelectMany(x => x.GetTypes()));
        }

        /// <summary>
        /// Set the host config of the AppHost.
        /// </summary>
        public virtual void SetConfig(HostConfig config)
        {
            Config = config;
        }

        /// <summary>
        /// Initializes the AppHost.
        /// Calls the <see cref="Configure"/> method.
        /// Should be called before start.
        /// </summary>
        public virtual ServiceStackHost Init()
        {
            if (Instance != null)
                throw new InvalidDataException($"ServiceStackHost.Instance has already been set ({Instance.GetType().Name})");

            Service.GlobalResolver = Instance = this;

            RegisterLicenseKey(AppSettings.GetNullableString("servicestack:license"));

            if (ServiceController == null)
                ServiceController = CreateServiceController(ServiceAssemblies.ToArray());

            Config = HostConfig.ResetInstance();
            OnConfigLoad();

            AbstractVirtualFileBase.ScanSkipPaths = Config.ScanSkipPaths;
            ResourceVirtualDirectory.EmbeddedResourceTreatAsFiles = Config.EmbeddedResourceTreatAsFiles;

            OnBeforeInit();
            ServiceController.Init();

            BeforeConfigure.Each(fn => fn(this));
            Configure(Container);
            AfterConfigure.Each(fn => fn(this));

            if (Config.StrictMode == null && Config.DebugMode)
                Config.StrictMode = true;

            if (!Config.DebugMode)
                Plugins.RemoveAll(x => x is RequestInfoFeature);

            ConfigurePlugins();

            List<IVirtualPathProvider> pathProviders = null;
            if (VirtualFileSources == null)
            {
                pathProviders = GetVirtualFileSources().Where(x => x != null).ToList();

                VirtualFileSources = pathProviders.Count > 1
                    ? new MultiVirtualFiles(pathProviders.ToArray())
                    : pathProviders.First();
            }

            if (VirtualFiles == null)
                VirtualFiles = pathProviders?.FirstOrDefault(x => x is FileSystemVirtualFiles) as IVirtualFiles
                    ?? GetVirtualFileSources().FirstOrDefault(x => x is FileSystemVirtualFiles) as IVirtualFiles;

            OnAfterInit();

            PopulateArrayFilters();

            LogInitComplete();

            HttpHandlerFactory.Init();

            return this;
        }

        protected virtual void RegisterLicenseKey(string licenseKeyText)
        {
            if (!string.IsNullOrEmpty(licenseKeyText))
            {
                Licensing.RegisterLicense(licenseKeyText);
            }
        }

        protected void PopulateArrayFilters()
        {
            PreRequestFiltersArray = PreRequestFilters.ToArray();
            RequestConvertersArray = RequestConverters.ToArray();
            ResponseConvertersArray = ResponseConverters.ToArray();
            GlobalRequestFiltersArray = GlobalRequestFilters.ToArray();
            GlobalRequestFiltersAsyncArray = GlobalRequestFiltersAsync.ToArray();
            GlobalResponseFiltersArray = GlobalResponseFilters.ToArray();
            GlobalResponseFiltersAsyncArray = GlobalResponseFiltersAsync.ToArray();
            GlobalMessageRequestFiltersArray = GlobalMessageRequestFilters.ToArray();
            GlobalMessageRequestFiltersAsyncArray = GlobalMessageRequestFiltersAsync.ToArray();
            GlobalMessageResponseFiltersArray = GlobalMessageResponseFilters.ToArray();
            GlobalMessageResponseFiltersAsyncArray = GlobalMessageResponseFiltersAsync.ToArray();
            RawHttpHandlersArray = RawHttpHandlers.ToArray();
            CatchAllHandlersArray = CatchAllHandlers.ToArray();
            GatewayRequestFiltersArray = GatewayRequestFilters.ToArray();
            GatewayRequestFiltersAsyncArray = GatewayRequestFiltersAsync.ToArray();
            GatewayResponseFiltersArray = GatewayResponseFilters.ToArray();
            GatewayResponseFiltersAsyncArray = GatewayResponseFiltersAsync.ToArray();
        }

        private void LogInitComplete()
        {
            var elapsed = DateTime.UtcNow - StartedAt;
            var hasErrors = StartUpErrors.Any();

            if (hasErrors)
            {
                Log.ErrorFormat(
                    "Initializing Application {0} took {1}ms. {2} error(s) detected: {3}",
                    ServiceName,
                    elapsed.TotalMilliseconds,
                    StartUpErrors.Count,
                    StartUpErrors.ToJson());

                Config.GlobalResponseHeaders["X-Startup-Errors"] = StartUpErrors.Count.ToString();
            }
            else
            {
                Log.InfoFormat(
                    "Initializing Application {0} took {1}ms. No errors detected.",
                    ServiceName,
                    elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Gets Full Directory Path of where the app is running
        /// </summary>
        public virtual string GetWebRootPath() => Config.WebHostPhysicalPath;

        public virtual List<IVirtualPathProvider> GetVirtualFileSources()
        {
            var pathProviders = new List<IVirtualPathProvider> {
                new FileSystemVirtualFiles(GetWebRootPath())
            };

            pathProviders.AddRange(Config.EmbeddedResourceBaseTypes.Distinct()
                .Map(x => new ResourceVirtualFiles(x) { LastModified = GetAssemblyLastModified(x.Assembly) } ));

            pathProviders.AddRange(Config.EmbeddedResourceSources.Distinct()
                .Map(x => new ResourceVirtualFiles(x) { LastModified = GetAssemblyLastModified(x) } ));

            if (AddVirtualFileSources.Count > 0)
                pathProviders.AddRange(AddVirtualFileSources);

            return pathProviders;
        }

        private static DateTime GetAssemblyLastModified(Assembly asm)
        {
            try
            {
                if (asm.Location != null)
                    return new FileInfo(asm.Location).LastWriteTime;
            }
            catch (Exception) { /* ignored */ }
            return default(DateTime);
        }

        /// <summary>
        /// Starts the AppHost.
        /// this methods needs to be overwritten in subclass to provider a listener to start handling requests.
        /// </summary>
        /// <param name="urlBase">Url to listen to</param>
        public virtual ServiceStackHost Start(string urlBase)
        {
            throw new NotImplementedException("Start(listeningAtUrlBase) is not supported by this AppHost");
        }

        public string ServiceName { get; set; }

        public IAppSettings AppSettings { get; set; }

        public ServiceMetadata Metadata { get; set; }

        public ServiceController ServiceController { get; set; }

        // Rare for a user to auto register all avaialable services in ServiceStack.dll
        // But happens when ILMerged, so exclude autoregistering SS services by default 
        // and let them register them manually
        public HashSet<Type> ExcludeAutoRegisteringServiceTypes { get; set; }

        /// <summary>
        /// The AppHost.Container. Note: it is not thread safe to register dependencies after AppStart.
        /// </summary>
        public virtual Container Container { get; private set; }

        public IServiceRoutes Routes { get; set; }

        public List<RestPath> RestPaths;

        public Dictionary<Type, Func<IRequest, object>> RequestBinders => ServiceController.RequestTypeFactoryMap;

        public IContentTypes ContentTypes { get; set; }

        /// <summary>
        /// Collection of PreRequest filters.
        /// They are called before each request is handled by a service, but after an HttpHandler is by the <see cref="HttpHandlerFactory"/> chosen.
        /// called in <see cref="ApplyPreRequestFilters"/>.
        /// </summary>
        public List<Action<IRequest, IResponse>> PreRequestFilters { get; set; }
        internal Action<IRequest, IResponse>[] PreRequestFiltersArray;

        /// <summary>
        /// Collection of RequestConverters.
        /// Can be used to convert/change Input Dto
        /// Called after routing and model binding, but before request filters.
        /// All request converters are called unless <see cref="IResponse.IsClosed"></see>
        /// Converter can return null, orginal model will be used.
        /// 
        /// Note one converter could influence the input for the next converter!
        /// </summary>
        public List<Func<IRequest, object, Task<object>>> RequestConverters { get; set; }
        internal Func<IRequest, object, Task<object>>[] RequestConvertersArray;

        /// <summary>
        /// Collection of ResponseConverters.
        /// Can be used to convert/change Output Dto
        /// 
        /// Called directly after response is handled, even before <see cref="ApplyResponseFiltersAsync"></see>!
        /// </summary>
        public List<Func<IRequest, object, Task<object>>> ResponseConverters { get; set; }
        internal Func<IRequest, object, Task<object>>[] ResponseConvertersArray;

        public List<Action<IRequest, IResponse, object>> GlobalRequestFilters { get; set; }
        internal Action<IRequest, IResponse, object>[] GlobalRequestFiltersArray;

        public List<Func<IRequest, IResponse, object, Task>> GlobalRequestFiltersAsync { get; set; }
        internal Func<IRequest, IResponse, object, Task>[] GlobalRequestFiltersAsyncArray;

        public Dictionary<Type, ITypedFilter> GlobalTypedRequestFilters { get; set; }

        public List<Action<IRequest, IResponse, object>> GlobalResponseFilters { get; set; }
        internal Action<IRequest, IResponse, object>[] GlobalResponseFiltersArray;

        public List<Func<IRequest, IResponse, object, Task>> GlobalResponseFiltersAsync { get; set; }
        internal Func<IRequest, IResponse, object, Task>[] GlobalResponseFiltersAsyncArray;

        public Dictionary<Type, ITypedFilter> GlobalTypedResponseFilters { get; set; }

        public List<Action<IRequest, IResponse, object>> GlobalMessageRequestFilters { get; }
        internal Action<IRequest, IResponse, object>[] GlobalMessageRequestFiltersArray;

        public List<Func<IRequest, IResponse, object, Task>> GlobalMessageRequestFiltersAsync { get; }
        internal Func<IRequest, IResponse, object, Task>[] GlobalMessageRequestFiltersAsyncArray;

        public Dictionary<Type, ITypedFilter> GlobalTypedMessageRequestFilters { get; set; }

        public List<Action<IRequest, IResponse, object>> GlobalMessageResponseFilters { get; }
        internal Action<IRequest, IResponse, object>[] GlobalMessageResponseFiltersArray;

        public List<Func<IRequest, IResponse, object, Task>> GlobalMessageResponseFiltersAsync { get; }
        internal Func<IRequest, IResponse, object, Task>[] GlobalMessageResponseFiltersAsyncArray;

        public Dictionary<Type, ITypedFilter> GlobalTypedMessageResponseFilters { get; set; }

        /// <summary>
        /// Lists of view engines for this app.
        /// If view is needed list is looped until view is found.
        /// </summary>
        public List<IViewEngine> ViewEngines { get; set; }

        public List<HandleServiceExceptionDelegate> ServiceExceptionHandlers { get; set; }

        public List<HandleServiceExceptionAsyncDelegate> ServiceExceptionHandlersAsync { get; set; }

        public List<HandleUncaughtExceptionDelegate> UncaughtExceptionHandlers { get; set; }

        public List<HandleUncaughtExceptionAsyncDelegate> UncaughtExceptionHandlersAsync { get; set; }

        public List<Action<ServiceStackHost>> BeforeConfigure { get; set; }

        public List<Action<ServiceStackHost>> AfterConfigure { get; set; }

        public List<Action<IAppHost>> AfterInitCallbacks { get; set; }

        public List<Action<IAppHost>> OnDisposeCallbacks { get; set; }

        public List<Action<IRequest>> OnEndRequestCallbacks { get; set; }

        public List<Func<IHttpRequest, IHttpHandler>> RawHttpHandlers { get; set; }
        internal Func<IHttpRequest, IHttpHandler>[] RawHttpHandlersArray;

        public List<HttpHandlerResolverDelegate> CatchAllHandlers { get; set; }
        internal HttpHandlerResolverDelegate[] CatchAllHandlersArray;

        public IServiceStackHandler GlobalHtmlErrorHttpHandler { get; set; }

        public Dictionary<HttpStatusCode, IServiceStackHandler> CustomErrorHttpHandlers { get; set; }

        public List<ResponseStatus> StartUpErrors { get; set; }

        public List<ResponseStatus> AsyncErrors { get; set; }

        public List<string> PluginsLoaded { get; set; }

        /// <summary>
        /// Collection of added plugins.
        /// </summary>
        public List<IPlugin> Plugins { get; set; }

        public IVirtualFiles VirtualFiles { get; set; }

        public IVirtualPathProvider VirtualFileSources { get; set; }

        public List<IVirtualPathProvider> AddVirtualFileSources { get; set; }

        public List<Action<IRequest, object>> GatewayRequestFilters { get; set; }
        internal Action<IRequest, object>[] GatewayRequestFiltersArray;

        public List<Func<IRequest, object, Task>> GatewayRequestFiltersAsync { get; set; }
        internal Func<IRequest, object, Task>[] GatewayRequestFiltersAsyncArray;

        public List<Action<IRequest, object>> GatewayResponseFilters { get; set; }
        internal Action<IRequest, object>[] GatewayResponseFiltersArray;

        public List<Func<IRequest, object, Task>> GatewayResponseFiltersAsync { get; set; }
        internal Func<IRequest, object, Task>[] GatewayResponseFiltersAsyncArray;

        /// <summary>
        /// Executed immediately before a Service is executed. Use return to change the request DTO used, must be of the same type.
        /// </summary>
        public virtual object OnPreExecuteServiceFilter(IService service, object request, IRequest httpReq, IResponse httpRes)
        {
            return request;
        }

        /// <summary>
        /// Executed immediately after a service is executed. Use return to change response used.
        /// </summary>
        public virtual object OnPostExecuteServiceFilter(IService service, object response, IRequest httpReq, IResponse httpRes)
        {
            return response;
        }

        /// <summary>
        /// Occurs when the Service throws an Exception.
        /// </summary>
        public virtual async Task<object> OnServiceException(IRequest httpReq, object request, Exception ex)
        {
            object lastError = null;
            foreach (var errorHandler in ServiceExceptionHandlers)
            {
                lastError = errorHandler(httpReq, request, ex) ?? lastError;
            }
            foreach (var errorHandler in ServiceExceptionHandlersAsync)
            {
                lastError = await errorHandler(httpReq, request, ex) ?? lastError;
            }
            return lastError;
        }

        /// <summary>
        /// Occurs when an exception is thrown whilst processing a request.
        /// </summary>
        public virtual async Task OnUncaughtException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
        {
            foreach (var errorHandler in UncaughtExceptionHandlers)
            {
                errorHandler(httpReq, httpRes, operationName, ex);
            }
            foreach (var errorHandler in UncaughtExceptionHandlersAsync)
            {
                await errorHandler(httpReq, httpRes, operationName, ex);
            }
        }

        public virtual Task HandleUncaughtException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
        {
            //Only add custom error messages to StatusDescription
            var httpError = ex as IHttpError;
            var errorMessage = httpError?.Message;
            var statusCode = ex.ToStatusCode();

            //httpRes.WriteToResponse always calls .Close in it's finally statement so 
            //if there is a problem writing to response, by now it will be closed
            return httpRes.WriteErrorToResponse(httpReq, httpReq.ResponseContentType, operationName, errorMessage, ex, statusCode);
        }

        public virtual async Task HandleShortCircuitedErrors(IRequest req, IResponse res, object requestDto)
        {
            var httpError = new HttpError(res.StatusCode, res.StatusDescription);
            var response = await OnServiceException(req, requestDto, httpError);
            if (response != null)
            {
                await res.EndHttpHandlerRequestAsync(afterHeaders: async httpRes =>
                {
                    await ContentTypes.SerializeToStreamAsync(req, response, httpRes.OutputStream);
                });
            }
            else
            {
                res.EndRequest();
            }
        }

        public virtual void OnStartupException(Exception ex)
        {
            if (Config.StrictMode == true)
                throw ex;

            this.StartUpErrors.Add(DtoUtils.CreateErrorResponse(null, ex).GetResponseStatus());
        }

        private HostConfig config;
        public HostConfig Config
        {
            get => config;
            set
            {
                config = value;
                OnAfterConfigChanged();
            }
        }

        public virtual void OnConfigLoad()
        {
            Config.DebugMode = GetType().Assembly.IsDebugBuild();
        }

        // Config has changed
        public virtual void OnAfterConfigChanged()
        {
            config.ServiceEndpointsMetadataConfig = ServiceEndpointsMetadataConfig.Create(config.HandlerFactoryPath);

            JsonDataContractSerializer.Instance.UseBcl = config.UseBclJsonSerializers;
            JsonDataContractSerializer.Instance.UseBcl = config.UseBclJsonSerializers;
        }

        public virtual void OnBeforeInit()
        {
            Container.Register<IHashProvider>(c => new SaltedHash()).ReusedWithin(ReuseScope.None);
            Container.Register<IPasswordHasher>(c => new PasswordHasher());
        }

        //After configure called
        public virtual void OnAfterInit()
        {
            AfterInitAt = DateTime.UtcNow;

            if (config.EnableFeatures != Feature.All)
            {
                if ((Feature.Xml & config.EnableFeatures) != Feature.Xml)
                {
                    config.IgnoreFormatsInMetadata.Add("xml");
                    Config.PreferredContentTypes.Remove(MimeTypes.Xml);
                    ContentTypes.Remove(MimeTypes.Xml);
                }
                if ((Feature.Json & config.EnableFeatures) != Feature.Json)
                {
                    config.IgnoreFormatsInMetadata.Add("json");
                    Config.PreferredContentTypes.Remove(MimeTypes.Json);
                    ContentTypes.Remove(MimeTypes.Json);
                }
                if ((Feature.Jsv & config.EnableFeatures) != Feature.Jsv)
                {
                    config.IgnoreFormatsInMetadata.Add("jsv");
                    Config.PreferredContentTypes.Remove(MimeTypes.Jsv);
                    ContentTypes.Remove(MimeTypes.Jsv);
                }
                if ((Feature.Csv & config.EnableFeatures) != Feature.Csv)
                {
                    config.IgnoreFormatsInMetadata.Add("csv");
                    Config.PreferredContentTypes.Remove(MimeTypes.Csv);
                    ContentTypes.Remove(MimeTypes.Csv);
                }
                if ((Feature.Html & config.EnableFeatures) != Feature.Html)
                {
                    config.IgnoreFormatsInMetadata.Add("html");
                    Config.PreferredContentTypes.Remove(MimeTypes.Html);
                    ContentTypes.Remove(MimeTypes.Html);
                }
            }

            if ((Feature.Html & config.EnableFeatures) != Feature.Html)
                Plugins.RemoveAll(x => x is HtmlFormat);

            if ((Feature.Csv & config.EnableFeatures) != Feature.Csv)
                Plugins.RemoveAll(x => x is CsvFormat);

            if ((Feature.PredefinedRoutes & config.EnableFeatures) != Feature.PredefinedRoutes)
                Plugins.RemoveAll(x => x is PredefinedRoutesFeature);

            if ((Feature.Metadata & config.EnableFeatures) != Feature.Metadata)
            {
                Plugins.RemoveAll(x => x is MetadataFeature);
                Plugins.RemoveAll(x => x is NativeTypesFeature);
            }

            if ((Feature.RequestInfo & config.EnableFeatures) != Feature.RequestInfo)
                Plugins.RemoveAll(x => x is RequestInfoFeature);

            if ((Feature.Razor & config.EnableFeatures) != Feature.Razor)
                Plugins.RemoveAll(x => x is IRazorPlugin);    //external

            if ((Feature.ProtoBuf & config.EnableFeatures) != Feature.ProtoBuf)
                Plugins.RemoveAll(x => x is IProtoBufPlugin); //external

            if ((Feature.MsgPack & config.EnableFeatures) != Feature.MsgPack)
                Plugins.RemoveAll(x => x is IMsgPackPlugin);  //external

            if (config.HandlerFactoryPath != null)
                config.HandlerFactoryPath = config.HandlerFactoryPath.TrimStart('/');

            if (config.UseCamelCase)
                JsConfig.EmitCamelCaseNames = true;

            if (config.EnableOptimizations)
            {
                MemoryStreamFactory.UseRecyclableMemoryStream = true;
            }

            var specifiedContentType = config.DefaultContentType; //Before plugins loaded

            var plugins = Plugins.ToArray();
            delayedLoadPlugin = true;
            LoadPluginsInternal(plugins);

            AfterPluginsLoaded(specifiedContentType);

            if (!TestMode && Container.Exists<IAuthSession>())
                throw new Exception(ErrorMessages.ShouldNotRegisterAuthSession);

            if (!Container.Exists<IAppSettings>())
                Container.Register(AppSettings);

            if (!Container.Exists<ICacheClient>())
            {
                if (Container.Exists<IRedisClientsManager>())
                    Container.Register(c => c.Resolve<IRedisClientsManager>().GetCacheClient());
                else
                    Container.Register<ICacheClient>(DefaultCache);
            }

            if (!Container.Exists<MemoryCacheClient>())
                Container.Register(DefaultCache);

            if (Container.Exists<IMessageService>()
                && !Container.Exists<IMessageFactory>())
            {
                Container.Register(c => c.Resolve<IMessageService>().MessageFactory);
            }

            if (Container.Exists<IUserAuthRepository>()
                && !Container.Exists<IAuthRepository>())
            {
                Container.Register<IAuthRepository>(c => c.Resolve<IUserAuthRepository>());
            }

            if (Config.UseJsObject)
                JS.Configure();

            if (config.LogUnobservedTaskExceptions)
            {
                TaskScheduler.UnobservedTaskException += (sender, args) =>
                {
                    args.SetObserved();
                    args.Exception.Handle(ex =>
                    {
                        lock (AsyncErrors)
                        {
                            AsyncErrors.Add(DtoUtils.CreateErrorResponse(null, ex).GetResponseStatus());
                            return true;
                        }
                    });
                };
            }

            foreach (var callback in AfterInitCallbacks)
            {
                callback(this);
            }

            ReadyAt = DateTime.UtcNow;
        }

        private void ConfigurePlugins()
        {
            //Some plugins need to initialize before other plugins are registered.
            foreach (var plugin in Plugins)
            {
                if (plugin is IPreInitPlugin preInitPlugin)
                {
                    try
                    {
                        preInitPlugin.Configure(this);
                    }
                    catch (Exception ex)
                    {
                        OnStartupException(ex);
                    }
                }
            }
        }

        private void AfterPluginsLoaded(string specifiedContentType)
        {
            if (!IsNullOrEmpty(specifiedContentType))
                config.DefaultContentType = specifiedContentType;
            else if (IsNullOrEmpty(config.DefaultContentType))
                config.DefaultContentType = MimeTypes.Json;

            Config.PreferredContentTypes.Remove(Config.DefaultContentType);
            Config.PreferredContentTypes.Insert(0, Config.DefaultContentType);

            Config.PreferredContentTypesArray = Config.PreferredContentTypes.ToArray();

            foreach (var plugin in Plugins)
            {
                if (plugin is IPostInitPlugin preInitPlugin)
                {
                    try
                    {
                        preInitPlugin.AfterPluginsLoaded(this);
                    }
                    catch (Exception ex)
                    {
                        OnStartupException(ex);
                    }
                }
            }

            ServiceController.AfterInit();
        }

        public virtual void Release(object instance)
        {
            try
            {
                if (Container.Adapter is IRelease iocAdapterReleases)
                {
                    iocAdapterReleases.Release(instance);
                }
                else
                {
                    var disposable = instance as IDisposable;
                    disposable?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Error("ServiceStackHost.Release", ex);
            }
        }

        public virtual void OnEndRequest(IRequest request = null)
        {
            try
            {
                var disposables = RequestContext.Instance.Items.Values;
                foreach (var item in disposables)
                {
                    Release(item);
                }

                RequestContext.Instance.EndRequest();

                foreach (var fn in OnEndRequestCallbacks)
                {
                    fn(request);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error when Disposing Request Context", ex);
            }
        }

        /// <summary>
        /// Register singleton in the Ioc Container of the AppHost.
        /// </summary>
        public virtual void Register<T>(T instance)
        {
            this.Container.Register(instance);
        }

        /// <summary>
        /// Registers type to be automatically wired by the Ioc container of the AppHost.
        /// </summary>
        /// <typeparam name="T">Concrete type</typeparam>
        /// <typeparam name="TAs">Abstract type</typeparam>
        public virtual void RegisterAs<T, TAs>() where T : TAs
        {
            this.Container.RegisterAutoWiredAs<T, TAs>();
        }

        /// <summary>
        /// Tries to resolve type through the ioc container of the AppHost. 
        /// Can return null.
        /// </summary>
        public virtual T TryResolve<T>()
        {
            return this.Container.TryResolve<T>();
        }

        /// <summary>
        /// Resolves Type through the Ioc container of the AppHost.
        /// </summary>
        /// <exception cref="ResolutionException">If type is not registered</exception>
        public virtual T Resolve<T>()
        {
            return this.Container.Resolve<T>();
        }

        /// <summary>
        /// Looks for first plugin of this type in Plugins.
        /// Reflection performance penalty.
        /// </summary>
        public T GetPlugin<T>() where T : class, IPlugin
        {
            return Plugins.FirstOrDefault(x => x is T) as T;
        }

        public bool HasPlugin<T>() where T : class, IPlugin
        {
            return Plugins.FirstOrDefault(x => x is T) != null;
        }

        public virtual IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
        {
            //cached per service action
            return new ServiceRunner<TRequest>(this, actionContext);
        }

        public virtual string ResolveLocalizedString(string text, IRequest request)
        {
            return text;
        }

        public virtual string ResolveAbsoluteUrl(string virtualPath, IRequest httpReq)
        {
            if (httpReq == null)
                return (Config.WebHostUrl ?? "/").CombineWith(virtualPath.TrimStart('~'));

            return httpReq.GetAbsoluteUrl(virtualPath); //Http Listener, TODO: ASP.NET overrides
        }

        public virtual bool UseHttps(IRequest httpReq)
        {
            return Config.UseHttpsLinks || httpReq.GetHeader(HttpHeaders.XForwardedProtocol) == "https";
        }

        public virtual string GetBaseUrl(IRequest httpReq)
        {
            var useHttps = UseHttps(httpReq);
            var baseUrl = HostContext.Config.WebHostUrl;
            if (baseUrl != null)
                return baseUrl.NormalizeScheme(useHttps);

            baseUrl = httpReq.AbsoluteUri.InferBaseUrl(fromPathInfo: httpReq.PathInfo);
            if (baseUrl != null)
                return baseUrl.NormalizeScheme(useHttps);

            var handlerPath = Config.HandlerFactoryPath;

            return new Uri(httpReq.AbsoluteUri).GetLeftAuthority()
                .NormalizeScheme(useHttps)
                .CombineWith(handlerPath)
                .TrimEnd('/');
        }

        public virtual string ResolvePhysicalPath(string virtualPath, IRequest httpReq)
        {
            return VirtualFileSources.CombineVirtualPath(VirtualFileSources.RootDirectory.RealPath, virtualPath);
        }

        private bool delayedLoadPlugin;
        public virtual void LoadPlugin(params IPlugin[] plugins)
        {
            if (delayedLoadPlugin)
            {
                LoadPluginsInternal(plugins);
                Plugins.AddRange(plugins);
                PopulateArrayFilters();
            }
            else
            {
                foreach (var plugin in plugins)
                {
                    Plugins.Add(plugin);
                }
            }
        }

        internal virtual void LoadPluginsInternal(params IPlugin[] plugins)
        {
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.Register(this);
                    PluginsLoaded.Add(plugin.GetType().Name);
                }
                catch (Exception ex)
                {
                    OnStartupException(ex);
                }
            }
        }

        public virtual object ExecuteService(object requestDto) => ExecuteService(requestDto, RequestAttributes.None);

        public virtual object ExecuteService(object requestDto, IRequest req) => ServiceController.Execute(requestDto, req);

        public virtual Task<object> ExecuteServiceAsync(object requestDto, IRequest req) => ServiceController.ExecuteAsync(requestDto, req);

        public virtual object ExecuteService(object requestDto, RequestAttributes requestAttributes) => ServiceController.Execute(requestDto, new BasicRequest(requestDto, requestAttributes));

        public virtual object ExecuteMessage(IMessage mqMessage) => ServiceController.ExecuteMessage(mqMessage, new BasicRequest(mqMessage));

        public virtual object ExecuteMessage(IMessage dto, IRequest req) => ServiceController.ExecuteMessage(dto, req);

        public virtual void RegisterService(Type serviceType, params string[] atRestPaths)
        {
            ServiceController.RegisterService(serviceType);
            var reqAttr = serviceType.FirstAttribute<DefaultRequestAttribute>();
            if (reqAttr != null)
            {
                foreach (var atRestPath in atRestPaths)
                {
                    if (atRestPath == null) continue;

                    this.Routes.Add(reqAttr.RequestType, atRestPath, null);
                }
            }
        }

        public void RegisterServicesInAssembly(Assembly assembly)
        {
            ServiceController.RegisterServicesInAssembly(assembly);
        }

        public virtual RouteAttribute[] GetRouteAttributes(Type requestType)
        {
            return requestType.AllAttributes<RouteAttribute>();
        }

        public virtual string GenerateWsdl(WsdlTemplateBase wsdlTemplate)
        {
            var wsdl = wsdlTemplate.ToString();

            wsdl = wsdl.Replace("http://schemas.datacontract.org/2004/07/ServiceStack", Config.WsdlServiceNamespace);

            if (Config.WsdlServiceNamespace != HostConfig.DefaultWsdlNamespace)
            {
                wsdl = wsdl.Replace(HostConfig.DefaultWsdlNamespace, Config.WsdlServiceNamespace);
            }

            return wsdl;
        }

        public void RegisterTypedRequestFilter<T>(Action<IRequest, IResponse, T> filterFn)
        {
            GlobalTypedRequestFilters[typeof(T)] = new TypedFilter<T>(filterFn);
        }

        public void RegisterTypedRequestFilter<T>(Func<Container, ITypedFilter<T>> filter)
        {
            RegisterTypedFilter(RegisterTypedRequestFilter, filter);
        }

        public void RegisterTypedResponseFilter<T>(Action<IRequest, IResponse, T> filterFn)
        {
            GlobalTypedResponseFilters[typeof(T)] = new TypedFilter<T>(filterFn);
        }

        public void RegisterTypedResponseFilter<T>(Func<Container, ITypedFilter<T>> filter)
        {
            RegisterTypedFilter(RegisterTypedResponseFilter, filter);
        }

        private void RegisterTypedFilter<T>(Action<Action<IRequest, IResponse, T>> registerTypedFilter, Func<Container, ITypedFilter<T>> filter)
        {
            registerTypedFilter.Invoke((request, response, dto) =>
            {
                // The filter MUST be resolved inside the RegisterTypedFilter call.
                // Otherwise, the container will not be able to resolve some auto-wired dependencies.
                filter
                    .Invoke(Container)
                    .Invoke(request, response, dto);
            });
        }

        public void RegisterTypedMessageRequestFilter<T>(Action<IRequest, IResponse, T> filterFn)
        {
            GlobalTypedMessageRequestFilters[typeof(T)] = new TypedFilter<T>(filterFn);
        }

        public void RegisterTypedMessageResponseFilter<T>(Action<IRequest, IResponse, T> filterFn)
        {
            GlobalTypedMessageResponseFilters[typeof(T)] = new TypedFilter<T>(filterFn);
        }

        public virtual string MapProjectPath(string relativePath)
        {
            return relativePath.MapProjectPath();
        }

        public virtual string ResolvePathInfo(IRequest request, string originalPathInfo)
        {
            var pathInfo = NormalizePathInfo(originalPathInfo, Config.HandlerFactoryPath);            
            return pathInfo;
        }

        public static string NormalizePathInfo(string pathInfo, string mode)
        {
            if (mode?.Length > 0 && mode[0] == '/')
                mode = mode.Substring(1);

            if (string.IsNullOrEmpty(mode))
                return pathInfo;

            var pathNoPrefix = pathInfo[0] == '/'
                ? pathInfo.Substring(1)
                : pathInfo;

            var normalizedPathInfo = pathNoPrefix.StartsWith(mode)
                ? pathNoPrefix.Substring(mode.Length)
                : pathInfo;

            return normalizedPathInfo.Length > 0 && normalizedPathInfo[0] != '/'
                ? '/' + normalizedPathInfo
                : normalizedPathInfo;
        }

        public virtual IHttpHandler ReturnRedirectHandler(IHttpRequest httpReq)
        {
            var pathInfo = NormalizePathInfo(httpReq.OriginalPathInfo, Config.HandlerFactoryPath);
            return Config.RedirectPaths.TryGetValue(pathInfo, out string redirectPath)
                ? new RedirectHttpHandler { RelativeUrl = redirectPath }
                : null;
        }
        
        public virtual IHttpHandler ReturnRequestInfoHandler(IHttpRequest httpReq)
        {
            if ((Config.DebugMode
                 || Config.AdminAuthSecret != null)
                && httpReq.QueryString[Keywords.Debug] == Keywords.RequestInfo)
            {
                if (Config.DebugMode || HasValidAuthSecret(httpReq))
                {
                    var reqInfo = RequestInfoHandler.GetRequestInfo(httpReq);

                    reqInfo.Host = Config.DebugHttpListenerHostEnvironment + "_v" + Env.ServiceStackVersion + "_" + ServiceName;
                    reqInfo.PathInfo = httpReq.PathInfo;
                    reqInfo.GetPathUrl = httpReq.GetPathUrl();

                    return new RequestInfoHandler { RequestInfo = reqInfo };
                }
            }

            return null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //clear managed resources here
                foreach (var callback in OnDisposeCallbacks)
                {
                    callback(this);
                }

                if (Container != null)
                {
                    Container.Dispose();
                    Container = null;
                }

                JS.UnConfigure();
                JsConfig.Reset(); //Clears Runtime Attributes

                Instance = null;
            }
            //clear unmanaged resources here
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ServiceStackHost()
        {
            Dispose(false);
        }
    }
}
