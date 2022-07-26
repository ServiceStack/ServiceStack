// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

#if !NETCORE
using System.Web;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using ServiceStack.Admin;
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
using ServiceStack.Script;

namespace ServiceStack
{
    public abstract partial class ServiceStackHost
        : IAppHost, IFunqlet, IHasContainer, IDisposable
    {
        protected ILog Log = LogManager.GetLogger(typeof(ServiceStackHost));
        public bool IsDebugLogEnabled => Log.IsDebugEnabled;

        /// <summary>
        /// Singleton access to AppHost
        /// </summary>
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
        /// Used for overwriting AuthSession.
        /// </summary>
        public bool TestMode { get; set; }

        /// <summary>
        /// The base path ServiceStack is hosted on
        /// </summary>
        public virtual string PathBase
        {
            get => Config?.HandlerFactoryPath;
            set => Config.HandlerFactoryPath = value;
        }

        /// <summary>
        /// The assemblies reflected to find api services.
        /// These can be provided in the constructor call.
        /// </summary>
        public List<Assembly> ServiceAssemblies { get; private set; }

        /// <summary>
        /// Whether AppHost has been already initialized
        /// </summary>
        public static bool HasInit => Instance != null;

        /// <summary>
        /// Whether AppHost configuration is done.
        /// Note: It doesn't mean the start function was called.
        /// </summary>
        public bool HasStarted => ReadyAt != null;

        /// <summary>
        /// Whether AppHost is ready configured and either ready to run or already running.
        /// Equals <see cref="HasStarted"/>
        /// </summary>
        public static bool IsReady() => Instance?.ReadyAt != null;

        protected ServiceStackHost(string serviceName, params Assembly[] assembliesWithServices)
        {
            this.StartedAt = DateTime.UtcNow;

            ServiceName = serviceName;
            ServiceAssemblies = assembliesWithServices.ToList();
            AppSettings = new AppSettings();
            Container = new Container { DefaultOwner = Owner.External };

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
            GlobalTypedRequestFiltersAsync = new Dictionary<Type, ITypedFilterAsync>();
            GlobalResponseFilters = new List<Action<IRequest, IResponse, object>>();
            GlobalResponseFiltersAsync = new List<Func<IRequest, IResponse, object, Task>>();
            GlobalTypedResponseFilters = new Dictionary<Type, ITypedFilter>();
            GlobalTypedResponseFiltersAsync = new Dictionary<Type, ITypedFilterAsync>();
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
            GatewayExceptionHandlers = new List<HandleGatewayExceptionDelegate>();
            GatewayExceptionHandlersAsync = new List<HandleGatewayExceptionAsyncDelegate>();
            OnPreRegisterPlugins = new Dictionary<Type, List<Action<IPlugin>>>();
            OnPostRegisterPlugins = new Dictionary<Type, List<Action<IPlugin>>>();
            OnAfterPluginsLoaded = new Dictionary<Type, List<Action<IPlugin>>>();
            BeforeConfigure = new List<Action<ServiceStackHost>>();
            AfterConfigure = new List<Action<ServiceStackHost>>();
            AfterPluginsLoaded = new List<Action<ServiceStackHost>>();
            AfterInitCallbacks = new List<Action<IAppHost>>();
            OnDisposeCallbacks = new List<Action<IAppHost>>();
            OnEndRequestCallbacks = new List<Action<IRequest>>();
            InsertVirtualFileSources = new List<IVirtualPathProvider> {
                new MemoryVirtualFiles(), //allow injecting files
            };
            AddVirtualFileSources = new List<IVirtualPathProvider>();
            RawHttpHandlers = new List<Func<IHttpRequest, IHttpHandler>> {
                ReturnRedirectHandler,
                ReturnRequestInfoHandler,
            };
            CatchAllHandlers = new List<HttpHandlerResolverDelegate>();
            FallbackHandlers = new List<HttpHandlerResolverDelegate>();
            CustomErrorHttpHandlers = new Dictionary<HttpStatusCode, IServiceStackHandler> {
                { HttpStatusCode.Forbidden, new ForbiddenHttpHandler() },
                { HttpStatusCode.NotFound, new NotFoundHttpHandler() },
            };
            StartUpErrors = new List<ResponseStatus>();
            AsyncErrors = new List<ResponseStatus>();
            DefaultScriptContext = new ScriptContext {
                ScriptLanguages = { ScriptLisp.Language },
            }.InitForSharpPages(this);
            PluginsLoaded = new List<string>();
            Plugins = new List<IPlugin> {
                new PreProcessRequest(),
                new HtmlFormat(),
                new CsvFormat(),
                new PredefinedRoutesFeature(),
                new MetadataFeature(),
                new NativeTypesFeature(),
                new HttpCacheFeature(),
                new RequestInfoFeature(),
                new SvgFeature(),
                new UiFeature(),
                new Validation.ValidationFeature(),
            };
            ExcludeAutoRegisteringServiceTypes = new HashSet<Type> {
                typeof(AuthenticateService),
                typeof(RegisterService),
                typeof(AssignRolesService),
                typeof(UnAssignRolesService),
                typeof(NativeTypesService),
                typeof(PostmanService),
                typeof(HotReloadPageService),
                typeof(HotReloadFilesService),
                typeof(SpaFallbackService),
                typeof(SharpApiService),
                typeof(MetadataDebugService),
                typeof(ServerEventsSubscribersService),
                typeof(ServerEventsUnRegisterService),
                typeof(MetadataAppService),
                typeof(MetadataNavService),
                typeof(ScriptAdminService),
                typeof(AdminDashboardServices),
                typeof(RequestLogsService),
                typeof(AutoQueryMetadataService),
                typeof(AdminUsersService),
                typeof(GetApiKeysService),
                typeof(RegenerateApiKeysService),
                typeof(ConvertSessionToTokenService),
                typeof(GetAccessTokenService),
                typeof(StoreFileUploadService),
                typeof(ReplaceFileUploadService),
                typeof(GetFileUploadService),
                typeof(DeleteFileUploadService),
                typeof(Validation.GetValidationRulesService),
                typeof(Validation.ModifyValidationRulesService),
#if NET472 || NET6_0_OR_GREATER
                typeof(AdminProfilingService),
#endif
            };

            JsConfig.InitStatics();
        }

        /// <summary>
        /// Configure your AppHost and its dependencies
        /// </summary>
        public abstract void Configure(Container container);

        protected virtual ServiceController CreateServiceController(params Assembly[] assembliesWithServices)
        {
            return new(this, assembliesWithServices);
        }

        protected virtual ServiceController CreateServiceController(params Type[] serviceTypes)
        {
            return new(this, () => serviceTypes);
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
            if (HasInit)
                throw new InvalidDataException($"ServiceStackHost.Instance has already been set ({Instance.GetType().Name})");

            Service.GlobalResolver = Instance = this;

            RegisterLicenseKey(AppSettings.GetNullableString("servicestack:license"));

            var scanAssemblies = new List<Assembly>(ServiceAssemblies);
            scanAssemblies.AddIfNotExists(GetType().Assembly);
            var scanTypes = scanAssemblies.SelectMany(x => x.GetTypes())
                .Where(x => (x.HasInterface(typeof(IPreConfigureAppHost))
                             || x.HasInterface(typeof(IConfigureAppHost))
                             || x.HasInterface(typeof(IAfterInitAppHost))))
                .ToArray();
            
            var startupConfigs = scanTypes.Where(x => !x.HasInterface(typeof(IPlugin)))
                .Select(x => x.CreateInstance()).WithPriority();
            var configInstances = startupConfigs.PriorityOrdered();
            var preStartupConfigs = startupConfigs.PriorityBelowZero();
            var postStartupConfigs = startupConfigs.PriorityZeroOrAbove();

            void RunPreConfigure(object instance)
            {
                try
                {
                    if (instance is IPreConfigureAppHost preConfigureAppHost)
                        preConfigureAppHost.PreConfigure(this);
                }
                catch (Exception ex)
                {
                    OnStartupException(ex, instance.GetType().Name, nameof(RunPreConfigure));
                }
            }

            var priorityPlugins = Plugins.WithPriority().PriorityOrdered().Map(x => (IPlugin)x);
            priorityPlugins.ForEach(RunPreConfigure);
            configInstances.ForEach(RunPreConfigure);
            
            if (ServiceController == null)
                ServiceController = CreateServiceController(ServiceAssemblies.ToArray());
            
            RpcGateway = new RpcGateway(this);

            Config = HostConfig.ResetInstance();
            OnConfigLoad();

            AbstractVirtualFileBase.ScanSkipPaths = Config.ScanSkipPaths;
            ResourceVirtualDirectory.EmbeddedResourceTreatAsFiles = Config.EmbeddedResourceTreatAsFiles;

            OnBeforeInit();
            ServiceController.Init();

            void RunConfigure(object instance)
            {
                try
                {
                    if (instance is IConfigureAppHost configureAppHost)
                        configureAppHost.Configure(this);
                }
                catch (Exception ex)
                {
                    OnStartupException(ex, instance.GetType().Name, nameof(RunConfigure));
                }
            }

            GlobalBeforeConfigure.Each(RunManagedAction);
            preStartupConfigs.ForEach(RunConfigure);
            BeforeConfigure.Each(RunManagedAction);

            Configure(Container);

            ConfigureLogging();
            AfterConfigure.Each(RunManagedAction);
            postStartupConfigs.ForEach(RunConfigure);
            GlobalAfterConfigure.Each(RunManagedAction);

            if (Config.StrictMode == null && Config.DebugMode)
                Config.StrictMode = true;

            if (!Config.DebugMode)
                Plugins.RemoveAll(x => x is RequestInfoFeature);

            var validationPluginsCount = Plugins.Count(x => x is Validation.ValidationFeature);
            if (validationPluginsCount > 1)
            {
                Log.Warn($"Multiple ValidationFeature Plugins detected. Removing default ValidationFeature plugin...");
                for (var i = 0; i < Plugins.Count; i++)
                {
                    var plugin = Plugins[i];
                    if (plugin is Validation.ValidationFeature)
                    {
                        Plugins.RemoveAt(i);
                        break;
                    }
                }
            }

            //Some plugins need to initialize before other plugins are registered.
            Plugins.ToList().ForEach(RunPreInitPlugin);
            configInstances.ForEach(RunPreInitPlugin);

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
            
            configInstances.ForEach(RunPostInitPlugin);
            GlobalAfterPluginsLoaded.Each(RunManagedAction);

            PopulateArrayFilters();

            LogInitComplete();

            HttpHandlerFactory.Init();

            foreach (var callback in AfterInitCallbacks)
            {
                callback(this);
            }
            
            Plugins.ForEach(RunAfterInitAppHost);
            configInstances.ForEach(RunAfterInitAppHost);
            GlobalAfterAppHostInit.Each(RunManagedAction);

            ReadyAt = DateTime.UtcNow;

            return this;
        }

        public virtual void ConfigureLogging()
        {
            Log = LogManager.GetLogger(typeof(ServiceStackHost));
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
            FallbackHandlersArray = FallbackHandlers.ToArray();
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
        
        /// <summary>
        /// Override to intercept VFS Providers registered for this AppHost
        /// </summary>
        public virtual List<IVirtualPathProvider> GetVirtualFileSources()
        {
            var pathProviders = new List<IVirtualPathProvider>(InsertVirtualFileSources ?? TypeConstants<IVirtualPathProvider>.EmptyList) {                 
                new FileSystemVirtualFiles(GetWebRootPath())
            };

            pathProviders.AddRange(Config.EmbeddedResourceBaseTypes.Distinct()
                .Map(x => new ResourceVirtualFiles(x)));

            pathProviders.AddRange(Config.EmbeddedResourceSources.Distinct()
                .Map(x => new ResourceVirtualFiles(x)));

            if (AddVirtualFileSources.Count > 0)
                pathProviders.AddRange(AddVirtualFileSources);

            return pathProviders;
        }

        public virtual T GetVirtualFileSource<T>() where T : class => AfterInitAt != null
            ? VirtualFileSources.GetVirtualFileSource<T>()
            : GetVirtualFileSources().FirstOrDefault(x => x is T) as T;

        /// <summary>
        /// Starts the AppHost.
        /// this methods needs to be overwritten in subclass to provider a listener to start handling requests.
        /// </summary>
        /// <param name="urlBase">Url to listen to</param>
        public virtual ServiceStackHost Start(string urlBase)
        {
            throw new NotImplementedException("Start(listeningAtUrlBase) is not supported by this AppHost");
        }

        /// <summary>
        /// The public name of this App
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// ServiceStack's Configuration API, see: https://docs.servicestack.net/appsettings  
        /// </summary>
        public IAppSettings AppSettings { get; set; }

        /// <summary>
        /// The populated Metadata for this AppHost's Services
        /// </summary>
        public ServiceMetadata Metadata { get; set; }

        /// <summary>
        /// The ServiceController that executes Services
        /// </summary>
        public ServiceController ServiceController { get; set; }
        
        /// <summary>
        /// Provides a pure object model for executing the full HTTP Request pipeline which returns the Response DTO
        /// back to ASP .NET Core gRPC which handles sending the response back to the HTTP/2 connected client.
        /// </summary>
        public RpcGateway RpcGateway { get; set; }

        // Rare for a user to auto register all available services in ServiceStack.dll
        // But happens when ILMerged, so exclude auto-registering SS services by default 
        // and let them register them manually
        public HashSet<Type> ExcludeAutoRegisteringServiceTypes { get; set; }

        /// <summary>
        /// The AppHost.Container. Note: it is not thread safe to register dependencies after AppStart.
        /// </summary>
        public virtual Container Container { get; private set; }

        /// <summary>
        /// Dynamically register Service Routes
        /// </summary>
        public IServiceRoutes Routes { get; set; }

        /// <summary>
        /// Registered Routes
        /// </summary>
        public List<RestPath> RestPaths { get; set; }

        /// <summary>
        /// Register custom Request Binder
        /// </summary>
        public Dictionary<Type, Func<IRequest, object>> RequestBinders => ServiceController.RequestTypeFactoryMap;

        /// <summary>
        /// Manage registered Content Types &amp; their sync/async serializers supported by this AppHost
        /// </summary>
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
        /// Converter can return null, original model will be used.
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
        public Dictionary<Type, ITypedFilterAsync> GlobalTypedRequestFiltersAsync { get; set; }

        public List<Action<IRequest, IResponse, object>> GlobalResponseFilters { get; set; }
        internal Action<IRequest, IResponse, object>[] GlobalResponseFiltersArray;

        public List<Func<IRequest, IResponse, object, Task>> GlobalResponseFiltersAsync { get; set; }
        internal Func<IRequest, IResponse, object, Task>[] GlobalResponseFiltersAsyncArray;

        public Dictionary<Type, ITypedFilter> GlobalTypedResponseFilters { get; set; }
        public Dictionary<Type, ITypedFilterAsync> GlobalTypedResponseFiltersAsync { get; set; }

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

        public List<HandleGatewayExceptionDelegate> GatewayExceptionHandlers { get; set; }
        public List<HandleGatewayExceptionAsyncDelegate> GatewayExceptionHandlersAsync { get; set; }

        /// <summary>
        /// Register static callbacks fired just before AppHost.Configure() 
        /// </summary>
        public static List<Action<ServiceStackHost>> GlobalBeforeConfigure { get; } = new();
        
        /// <summary>
        /// Register callbacks fired just before AppHost.Configure() 
        /// </summary>
        public List<Action<ServiceStackHost>> BeforeConfigure { get; set; }

        /// <summary>
        /// Register static callbacks fired just after AppHost.Configure() 
        /// </summary>
        public static List<Action<ServiceStackHost>> GlobalAfterConfigure { get; } = new();

        /// <summary>
        /// Register callbacks fired just after AppHost.Configure() 
        /// </summary>
        public List<Action<ServiceStackHost>> AfterConfigure { get; set; }

        /// <summary>
        /// Register static callbacks fired just after plugins are loaded 
        /// </summary>
        public static List<Action<ServiceStackHost>> GlobalAfterPluginsLoaded { get; } = new();

        /// <summary>
        /// Register callbacks fired just after plugins are loaded 
        /// </summary>
        public List<Action<ServiceStackHost>> AfterPluginsLoaded { get; set; }

        /// <summary>
        /// Register callbacks that's fired after the AppHost is initialized
        /// </summary>
        public List<Action<IAppHost>> AfterInitCallbacks { get; set; }

        /// <summary>
        /// Register static callbacks fired after the AppHost is initialized 
        /// </summary>
        public static List<Action<ServiceStackHost>> GlobalAfterAppHostInit { get; } = new();

        /// <summary>
        /// Register callbacks that's fired when AppHost is disposed
        /// </summary>
        public List<Action<IAppHost>> OnDisposeCallbacks { get; set; }

        /// <summary>
        /// Register callbacks to execute at the end of a Request
        /// </summary>
        public List<Action<IRequest>> OnEndRequestCallbacks { get; set; }

        /// <summary>
        /// Register highest priority IHttpHandler callbacks
        /// </summary>
        public List<Func<IHttpRequest, IHttpHandler>> RawHttpHandlers { get; set; }
        internal Func<IHttpRequest, IHttpHandler>[] RawHttpHandlersArray;

        /// <summary>
        /// Get "Catch All" IHttpHandler predicate IHttpHandler's, e.g. Used by HTML View Engines
        /// </summary>
        public List<HttpHandlerResolverDelegate> CatchAllHandlers { get; set; }
        internal HttpHandlerResolverDelegate[] CatchAllHandlersArray;

        /// <summary>
        /// Register fallback Request Handlers e.g. Used by #Script &amp; Razor Page Based Routing
        /// </summary>
        public List<HttpHandlerResolverDelegate> FallbackHandlers { get; set; }
        internal HttpHandlerResolverDelegate[] FallbackHandlersArray;

        /// <summary>
        /// Fallback IServiceStackHandler to handle Error Responses
        /// </summary>
        public IServiceStackHandler GlobalHtmlErrorHttpHandler { get; set; }

        /// <summary>
        /// Register Custom IServiceStackHandler to handle specific HttpStatusCode's 
        /// </summary>
        public Dictionary<HttpStatusCode, IServiceStackHandler> CustomErrorHttpHandlers { get; set; }

        /// <summary>
        /// Captured StartUp Exceptions
        /// </summary>
        public List<ResponseStatus> StartUpErrors { get; set; }

        /// <summary>
        /// Captured Unobserved Async Errors
        /// </summary>
        public List<ResponseStatus> AsyncErrors { get; set; }

        /// <summary>
        /// Which plugins were loaded in this AppHost
        /// </summary>
        public List<string> PluginsLoaded { get; set; }

        /// <summary>
        /// Collection of added plugins.
        /// </summary>
        public List<IPlugin> Plugins { get; set; }

        /// <summary>
        /// Writable Virtual File Source, uses FileSystemVirtualFiles at content root by default 
        /// </summary>
        public IVirtualFiles VirtualFiles { get; set; }

        /// <summary>
        /// Virtual File Sources from WebRoot, typically a MultiVirtualFiles containing a cascading list of Sources
        /// </summary>
        public IVirtualPathProvider VirtualFileSources { get; set; }

        /// <summary>
        /// FileSystem VFS for WebRoot
        /// </summary>
        public IVirtualDirectory RootDirectory =>
            (VirtualFileSources.GetFileSystemVirtualFiles() 
             ?? VirtualFileSources
             ?? new FileSystemVirtualFiles(GetWebRootPath())).RootDirectory;

        /// <summary>
        /// The Content Root Directory for this AppHost
        /// </summary>
        public IVirtualDirectory ContentRootDirectory => 
            VirtualFiles?.RootDirectory
            ?? new FileSystemVirtualFiles(MapProjectPath("~/")).RootDirectory;
        
        /// <summary>
        /// Insert higher priority VFS providers at the start of the VFS providers list
        /// </summary>
        public List<IVirtualPathProvider> InsertVirtualFileSources { get; set; }
        
        /// <summary>
        /// Append lower priority VFS providers at the end of the VFS providers list
        /// </summary>
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
        /// The fallback ScriptContext to use if no SharpPagesFeature plugin was registered
        /// </summary>
        public ScriptContext DefaultScriptContext { get; set; }

        /// <summary>
        /// Global #Script ScriptContext for AppHost. Returns SharpPagesFeature plugin or fallsback to DefaultScriptContext.
        /// </summary>
        public ScriptContext ScriptContext => scriptContext ??= (GetPlugin<SharpPagesFeature>() ?? DefaultScriptContext);
        private ScriptContext scriptContext;

        /// <summary>
        /// Evaluate Expressions in ServiceStack's ScriptContext.
        /// Can be overridden if you want to customize how different expressions are evaluated.
        /// </summary>
        public virtual object EvalExpressionCached(string expr) => JS.evalCached(ScriptContext, expr);
        public virtual object EvalExpression(string expr) => JS.eval(ScriptContext, expr);

        /// <summary>
        /// Evaluate a script value, `IScriptValue.Expression` results are cached globally.
        /// If `IRequest` is provided, results from the same `IScriptValue.Eval` are cached per request. 
        /// </summary>
        public virtual object EvalScriptValue(IScriptValue scriptValue, IRequest req = null, Dictionary<string, object> args=null)
        {
            if (ResolveScriptValue(scriptValue, out var exprValue)) 
                return exprValue;

            var evalCode = ScriptCodeUtils.EnsureReturn(scriptValue.Eval);

            object value = null;
            var evalCacheKey = JS.EvalCacheKeyPrefix + evalCode;

            if (!scriptValue.NoCache && req?.Items.TryGetValue(evalCacheKey, out value) == true)
                return value;

            // Cache AST Globally
            var cachedCodePage = JS.scriptCached(ScriptContext, evalCode);
            
            var evalCodeValue = EvalScript(new PageResult(cachedCodePage), req, args);
            if (!scriptValue.NoCache && req != null)
                req.Items[evalCacheKey] = evalCodeValue;

            return evalCodeValue;
        }

        /// <summary>
        /// Evaluate a script value, `IScriptValue.Expression` results are cached globally.
        /// If `IRequest` is provided, results from the same `IScriptValue.Eval` are cached per request. 
        /// </summary>
        public virtual async Task<object> EvalScriptValueAsync(IScriptValue scriptValue, IRequest req = null, Dictionary<string, object> args=null)
        {
            if (ResolveScriptValue(scriptValue, out var exprValue)) 
                return exprValue;

            var evalCode = ScriptCodeUtils.EnsureReturn(scriptValue.Eval);

            object value = null;
            var evalCacheKey = JS.EvalCacheKeyPrefix + evalCode;

            if (!scriptValue.NoCache && req?.Items.TryGetValue(evalCacheKey, out value) == true)
                return value;

            // Cache AST Globally
            var cachedCodePage = JS.scriptCached(ScriptContext, evalCode);
            
            var evalCodeValue = await EvalScriptAsync(new PageResult(cachedCodePage), req, args).ConfigAwait();
            if (!scriptValue.NoCache && req != null)
                req.Items[evalCacheKey] = evalCodeValue;

            return evalCodeValue;
        }

        private bool ResolveScriptValue(IScriptValue scriptValue, out object exprValue)
        {
            if (scriptValue == null)
                throw new ArgumentNullException(nameof(scriptValue));

            if (scriptValue.Value != null)
            {
                exprValue = scriptValue.Value;
                return true;
            }

            if (scriptValue.Expression != null)
            {
                {
                    exprValue = !scriptValue.NoCache
                        ? EvalExpressionCached(scriptValue.Expression)
                        : JS.eval(scriptValue.Expression);
                    return true;
                }
            }

            exprValue = null;
            return scriptValue.Eval == null;
        }

        private static void InitPageResult(PageResult pageResult, IRequest req, Dictionary<string, object> args)
        {
            if (args != null)
            {
                foreach (var entry in args)
                {
                    pageResult.Args[entry.Key] = entry.Value;
                }
            }

            if (req != null)
            {
                pageResult.Args[ScriptConstants.Request] = req;
                pageResult.Args[ScriptConstants.Dto] = req.Dto;
            }
        }

        /// <summary>
        /// Override to intercept sync #Script execution
        /// </summary>
        public virtual object EvalScript(PageResult pageResult, IRequest req = null, Dictionary<string, object> args=null)
        {
            InitPageResult(pageResult, req, args);

            if (!pageResult.EvaluateResult(out var returnValue))
                ScriptContextUtils.ThrowNoReturn();

            return ScriptLanguage.UnwrapValue(returnValue);
        }

        /// <summary>
        /// Override to intercept async #Script execution
        /// </summary>
        public virtual async Task<object> EvalScriptAsync(PageResult pageResult, IRequest req = null, Dictionary<string, object> args=null)
        {
            InitPageResult(pageResult, req, args);

            var ret = await pageResult.EvaluateResultAsync().ConfigAwait();
            if (!ret.Item1)
                ScriptContextUtils.ThrowNoReturn();

            return ScriptLanguage.UnwrapValue(ret.Item2);
        }

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
        /// Occurs when the Service throws an Service Gateway Exception
        /// </summary>
        public virtual async Task OnGatewayException(IRequest httpReq, object request, Exception ex)
        {
            httpReq.Items[nameof(OnGatewayException)] = bool.TrueString;

            foreach (var errorHandler in GatewayExceptionHandlers)
            {
                errorHandler(httpReq, request, ex);
            }

            foreach (var errorHandler in GatewayExceptionHandlersAsync)
            {
                await errorHandler(httpReq, request, ex).ConfigAwait();
            }
        }

        /// <summary>
        /// Occurs when the Service throws an Exception.
        /// </summary>
        public virtual async Task<object> OnServiceException(IRequest httpReq, object request, Exception ex)
        {
            httpReq.Items[nameof(OnServiceException)] = bool.TrueString;
            
            object lastError = null;
            foreach (var errorHandler in ServiceExceptionHandlers)
            {
                lastError = errorHandler(httpReq, request, ex) ?? lastError;
            }
            foreach (var errorHandler in ServiceExceptionHandlersAsync)
            {
                lastError = await errorHandler(httpReq, request, ex).ConfigAwait() ?? lastError;
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
                await errorHandler(httpReq, httpRes, operationName, ex).ConfigAwait();
            }
        }

        [Obsolete("Use HandleResponseException")]
        protected virtual Task HandleUncaughtException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex) =>
            HandleResponseException(httpReq, httpRes, operationName, ex);
        
        /// <summary>
        /// Override to intercept Response Exceptions
        /// </summary>
        public virtual Task HandleResponseException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
        {
            //Only add custom error messages to StatusDescription
            var httpError = ex as IHttpError;
            var errorMessage = httpError?.Message;
            var statusCode = ex.ToStatusCode();

            //httpRes.WriteToResponse always calls .Close in it's finally statement so 
            //if there is a problem writing to response, by now it will be closed
            return httpRes.WriteErrorToResponse(httpReq, httpReq.ResponseContentType, operationName, errorMessage, ex, statusCode);
        }

        public virtual Task HandleShortCircuitedErrors(IRequest req, IResponse res, object requestDto, 
            HttpStatusCode statusCode, string statusDescription=null)
        {
            res.StatusCode = (int)statusCode;
            res.StatusDescription = statusDescription;
            return HandleShortCircuitedErrors(req, res, requestDto);
        }
        
        /// <summary>
        /// Override to intercept Short Circuited Authentication Errors
        /// </summary>
        public virtual async Task HandleShortCircuitedErrors(IRequest req, IResponse res, object requestDto)
        {
            object response = null;
            try
            {
                var httpError = new HttpError(res.StatusCode, res.StatusDescription);
                response = await OnServiceException(req, requestDto, httpError).ConfigAwait();
                if (response != null)
                {
                    await res.EndHttpHandlerRequestAsync(afterHeaders: async httpRes => {
                        await ContentTypes.SerializeToStreamAsync(req, response, httpRes.OutputStream).ConfigAwait();
                    }).ConfigAwait();
                }
                else
                {
                    var handler = HostContext.AppHost.GetCustomErrorHandler(res.StatusCode);
                    if (handler != null)
                    {
                        await handler.ProcessRequestAsync(req, res, req.OperationName);                        
                    }
                }
            }
            finally
            {
                if (!res.IsClosed)
                {
                    await res.EndRequestAsync().ConfigAwait();
                }
            }
        }

        /// <summary>
        /// Override to intercept Exceptions thrown at Startup.
        /// Use StrictMode to rethrow Startup Exceptions
        /// </summary>
        public virtual void OnStartupException(Exception ex)
        {
            if (Config.StrictMode == true || Config.DebugMode)
            {
                if (Config.DebugMode)
                {
                    if (Log is NullDebugLogger)
                    {
                        Console.WriteLine(nameof(OnStartupException));
                        Console.WriteLine(ex);
                    }
                    else
                    {
                        Log.Error(nameof(OnStartupException), ex);
                    }
                }
                throw ex;
            }

            this.StartUpErrors.Add(DtoUtils.CreateErrorResponse(null, ex).GetResponseStatus());
        }

        public virtual void OnStartupException(Exception ex,  string target, string method)
        {
            Log.Error($"{method} {target}:\n{ex}");
            OnStartupException(ex);
        }

        private HostConfig config;
        /// <summary>
        /// The Configuration for this AppHost 
        /// </summary>
        public HostConfig Config
        {
            get => config;
            set
            {
                config = value;
                OnAfterConfigChanged();
            }
        }

        /// <summary>
        /// Override to intercept after the Config was loaded
        /// </summary>
        public virtual void OnConfigLoad()
        {
            Config.DebugMode = GetType().Assembly.IsDebugBuild();
        }

        /// <summary>
        /// Override to intercept when the Config has changed
        /// </summary>
        public virtual void OnAfterConfigChanged()
        {
            config.ServiceEndpointsMetadataConfig = ServiceEndpointsMetadataConfig.Create(config.HandlerFactoryPath);

            JsonDataContractSerializer.Instance.UseBcl = config.UseBclJsonSerializers;
            JsonDataContractSerializer.Instance.UseBcl = config.UseBclJsonSerializers;
        }

        /// <summary>
        /// Override to intercept before the AppHost is initialized
        /// </summary>
        public virtual void OnBeforeInit()
        {
            Container.Register<IHashProvider>(c => new SaltedHash()).ReusedWithin(ReuseScope.None);
            Container.Register<IPasswordHasher>(c => new PasswordHasher());
        }

        /// <summary>
        /// Override to intercept after the AppHost has been initialized
        /// </summary>
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

            if ((Feature.Validation & config.EnableFeatures) != Feature.Validation)
                Plugins.RemoveAll(x => x is Validation.ValidationFeature);

            if ((Feature.Razor & config.EnableFeatures) != Feature.Razor)
                Plugins.RemoveAll(x => x is IRazorPlugin);    //external

            if ((Feature.ProtoBuf & config.EnableFeatures) != Feature.ProtoBuf)
                Plugins.RemoveAll(x => x is IProtoBufPlugin); //external

            if ((Feature.MsgPack & config.EnableFeatures) != Feature.MsgPack)
                Plugins.RemoveAll(x => x is IMsgPackPlugin);  //external

            if (!string.IsNullOrEmpty(config.HandlerFactoryPath))
            {
                var handlerPath = config.HandlerFactoryPath.TrimStart('/');
                config.HandlerFactoryPath = handlerPath; 
                config.PathBase = handlerPath[0] != '/' ? '/' + handlerPath : null;
            }
            else
            {
                config.HandlerFactoryPath = null;
            }

            if (config.UseCamelCase && JsConfig.TextCase == TextCase.Default)
                ServiceStack.Text.Config.UnsafeInit(new Text.Config { TextCase = TextCase.CamelCase });

            if (config.EnableOptimizations)
            {
                MemoryStreamFactory.UseRecyclableMemoryStream = true;
            }

            var specifiedContentType = config.DefaultContentType; //Before plugins loaded

            var plugins = Plugins.ToArray();
            delayedLoadPlugin = true;
            LoadPluginsInternal(plugins);

            // If another ScriptContext (i.e. SharpPagesFeature) is already registered, don't override its IOC registrations.
            if (!Container.Exists<ISharpPages>())
                DefaultScriptContext.Init();

            RunAfterPluginsLoaded(specifiedContentType);

            ConfigurePlugin<MetadataFeature>(
                feature => feature.AddDebugLink("Templates/license.html", "License Info"));

            if (!TestMode && Container.Exists<IAuthSession>())
                throw new Exception(ErrorMessages.ShouldNotRegisterAuthSession);

            if (!Container.Exists<IAppSettings>())
                Container.Register(AppSettings);

            if (!Container.Exists<ICacheClient>())
            {
#if NETCORE
                if (Env.StrictMode && !Container.Exists<ICacheClientAsync>() && Container.Exists<ValueTask<ICacheClientAsync>>())
                {
                    throw new Exception("Invalid attempt to register `ValueTask<ICacheClientAsync>`. Register ICacheClient or ICacheClientAsync instead to use async Cache Client");
                }
#endif
                
                if (Container.Exists<IRedisClientsManager>())
                    Container.Register(c => c.Resolve<IRedisClientsManager>().GetCacheClient());
                else
                    Container.Register<ICacheClient>(DefaultCache);
            }

            if (!Container.Exists<MemoryCacheClient>())
                Container.Register(DefaultCache);

            if (!Container.Exists<ICacheClientAsync>())
            {
                var cache = Container.Resolve<ICacheClient>();
                Container.Register(cache.AsAsync());
            }

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

            if (Config.StrictMode == true && !JsConfig.HasInit)
                JsConfig.Init(); //Ensure JsConfig global config is not mutated after StartUp
            
            if (config.LogUnobservedTaskExceptions)
            {
                TaskScheduler.UnobservedTaskException += this.HandleUnobservedTaskException;
            }
        }

        private void HandleUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
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
        }

        private void RunPreInitPlugin(object instance)
        {
            try
            {
                if (instance is IPreInitPlugin prePlugin)
                    prePlugin.BeforePluginsLoaded(this);
            }
            catch (Exception ex)
            {
                OnStartupException(ex, instance.GetType().Name, nameof(RunPreInitPlugin));
            }
        }

        private void RunPostInitPlugin(object instance)
        {
            try
            {
                if (instance is IPostInitPlugin postPlugin)
                {
                    postPlugin.AfterPluginsLoaded(this);
                
                    if (instance is IPlugin plugin && 
                        OnAfterPluginsLoaded.TryGetValue(instance.GetType(), out var afterLoadedCallbacks))
                        afterLoadedCallbacks.Each(fn => fn(plugin));
                }
            }
            catch (Exception ex)
            {
                OnStartupException(ex, instance.GetType().Name, nameof(RunPostInitPlugin));
            }
        }

        private void RunAfterInitAppHost(object instance)
        {
            try
            {
                if (instance is IAfterInitAppHost afterPlugin)
                    afterPlugin.AfterInit(this);
            }
            catch (Exception ex)
            {
                OnStartupException(ex, instance.GetType().Name, nameof(RunAfterInitAppHost));
            }
        }

        private void RunManagedAction(Action<ServiceStackHost> fn)
        {
            try
            {
                fn(this);
            }
            catch (Exception ex)
            {
                OnStartupException(ex, fn.ToString(), nameof(RunManagedAction));
            }
        }

        private void RunAfterPluginsLoaded(string specifiedContentType)
        {
            if (!string.IsNullOrEmpty(specifiedContentType))
                config.DefaultContentType = specifiedContentType;
            else if (string.IsNullOrEmpty(config.DefaultContentType))
                config.DefaultContentType = MimeTypes.Json;

            Config.PreferredContentTypes.Remove(Config.DefaultContentType);
            Config.PreferredContentTypes.Insert(0, Config.DefaultContentType);

            Config.PreferredContentTypesArray = Config.PreferredContentTypes.ToArray();

            var plugins = Plugins.WithPriority().PriorityOrdered();
            plugins.ForEach(RunPostInitPlugin);
            
            foreach (var action in AfterPluginsLoaded)
            {
                try
                {
                    action(this);
                }
                catch (Exception ex)
                {
                    OnStartupException(ex, action.ToString(), nameof(RunAfterPluginsLoaded));
                }
            }

            ServiceController.AfterInit();
        }

        /// <summary>
        /// Override to intercept releasing this Service or Attribute instance 
        /// </summary>
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
                    using (instance as IDisposable) {}
                }
            }
            catch (Exception ex)
            {
                Log.Error("ServiceStackHost.Release", ex);
            }
        }

        /// <summary>
        /// Override to intercept the final callback after executing this Request 
        /// </summary>
        public virtual void OnEndRequest(IRequest request = null)
        {
            try
            {
                if (request != null)
                {
                    if (request.Items.ContainsKey(nameof(OnEndRequest)))
                        return;

                    request.Items[nameof(OnEndRequest)] = bool.TrueString;
                }
                
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
            finally
            {
                if (request != null)
                {
#if NET472 || NET6_0_OR_GREATER
                    if (ShouldProfileRequest(request))
                    {
                        // Populated in HttpHandlerFactory.InitHandler
                        if (request.GetItem(Keywords.RequestActivity) is System.Diagnostics.Activity activity
                            && activity.GetTagItem(Diagnostics.Activity.OperationId) is Guid id)
                        {
                            var ex = HttpError.GetException(request.Response.Dto);
                            if (ex != null)
                                Diagnostics.ServiceStack.WriteRequestError(id, request, ex);
                            else
                                Diagnostics.ServiceStack.WriteRequestAfter(id, request);
                            
                            Diagnostics.ServiceStack.StopActivity(activity, new ServiceStackActivityArgs { Request = request, Activity = activity });
                        }
                    }
#endif
                    
                    // Release Buffered Streams immediately
                    if (request.UseBufferedStream && request.InputStream is MemoryStream inputMs)
                    {
                        inputMs.Dispose();
                    }
                    var res = request.Response;
                    if (res is { UseBufferedStream: true, OutputStream: MemoryStream outputMs })
                    {
                        try 
                        { 
                            res.AllowSyncIO().Flush();
                            outputMs.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Error disposing Response Buffered OutputStream", ex);
                        }
                    }
                }
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
        /// </summary>
        public T GetPlugin<T>() where T : class, IPlugin
        {
            return Plugins.FirstOrDefault(x => x is T) as T;
        }

        /// <summary>
        /// Returns true if App has this plugin registered 
        /// </summary>
        public bool HasPlugin<T>() where T : class, IPlugin
        {
            return Plugins.FirstOrDefault(x => x is T) != null;
        }

        /// <summary>
        /// Override to use a Custom ServiceRunner to execute this Request DTO
        /// </summary>
        public virtual IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
        {
            //cached per service action
            return new ServiceRunner<TRequest>(this, actionContext);
        }

        /// <summary>
        /// Override to use a localized string for internal routes &amp; text used by ServiceStack 
        /// </summary>
        public virtual string ResolveLocalizedString(string text, IRequest request=null)
        {
            return text;
        }

        /// <summary>
        /// Override to use a localized string for internal routes &amp; text used by ServiceStack 
        /// </summary>
        public virtual string ResolveLocalizedStringFormat(string text, object[] args, IRequest request=null)
        {
            return string.Format(text, args);
        }

        /// <summary>
        /// Override to customize the Absolute URL for this virtualPath for this IRequest
        /// </summary>
        public virtual string ResolveAbsoluteUrl(string virtualPath, IRequest httpReq)
        {
            if (virtualPath.StartsWith("http://") || virtualPath.StartsWith("https://"))
                return virtualPath;
            
            if (httpReq == null)
                return (Config.WebHostUrl ?? "/").CombineWith(virtualPath.TrimStart('~'));

            return httpReq.GetAbsoluteUrl(virtualPath); //Http Listener, TODO: ASP.NET overrides
        }

        /// <summary>
        /// Override to change whether absolute links should use https:// URLs 
        /// </summary>
        public virtual bool UseHttps(IRequest httpReq)
        {
            return Config.UseHttpsLinks || httpReq.GetHeader(HttpHeaders.XForwardedProtocol) == "https";
        }

        /// <summary>
        /// Override to customize the BaseUrl to use for this IRequest 
        /// </summary>
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

        /// <summary>
        /// Override to customize the Physical Path for this virtualPath for this IRequest
        /// </summary>
        public virtual string ResolvePhysicalPath(string virtualPath, IRequest httpReq)
        {
            return VirtualFileSources.CombineVirtualPath(RootDirectory.RealPath, virtualPath);
        }

        public Dictionary<Type, List<Action<IPlugin>>> OnPreRegisterPlugins { get; set; }
        
        /// <summary>
        /// Register a callback to configure a plugin just before it's registered 
        /// </summary>
        public void ConfigurePlugin<T>(Action<T> configure) where T : class, IPlugin
        {
            if (!OnPreRegisterPlugins.TryGetValue(typeof(T), out var actions))
                actions = OnPreRegisterPlugins[typeof(T)] = new();
            actions.Add(plugin => configure((T)plugin));
        }

        public Dictionary<Type, List<Action<IPlugin>>> OnPostRegisterPlugins { get; set; } 
        
        /// <summary>
        /// Register a callback to configure a plugin just after it's registered 
        /// </summary>
        public void PostConfigurePlugin<T>(Action<T> configure) where T : class, IPlugin
        {
            if (!OnPostRegisterPlugins.TryGetValue(typeof(T), out var actions))
                actions = OnPostRegisterPlugins[typeof(T)] = new();
            actions.Add(plugin => configure((T)plugin));
        }

        public Dictionary<Type, List<Action<IPlugin>>> OnAfterPluginsLoaded { get; set; } 
        
        /// <summary>
        /// Register a callback to configure a plugin after AfterPluginsLoaded is run 
        /// </summary>
        public void AfterPluginLoaded<T>(Action<T> configure) where T : class, IPlugin
        {
            if (!OnAfterPluginsLoaded.TryGetValue(typeof(T), out var actions))
                actions = OnAfterPluginsLoaded[typeof(T)] = new();
            actions.Add(plugin => configure((T)plugin));
        }

        private bool delayedLoadPlugin;
        /// <summary>
        /// Manually register Plugin to load
        /// </summary>
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
                    if (OnPreRegisterPlugins.TryGetValue(plugin.GetType(), out var preRegisterCallbacks))
                        preRegisterCallbacks.Each(fn => fn(plugin));
                    
                    plugin.Register(this);
                    
                    if (OnPostRegisterPlugins.TryGetValue(plugin.GetType(), out var postRegisterCallbacks))
                        postRegisterCallbacks.Each(fn => fn(plugin));
                    
                    PluginsLoaded.Add(plugin.GetType().Name);
                }
                catch (Exception ex)
                {
                    OnStartupException(ex, plugin.GetType().Name, nameof(LoadPluginsInternal));
                }
            }
        }

        /// <summary>
        /// Override to intercept Service Requests
        /// </summary>
        public virtual object ExecuteService(object requestDto) => ExecuteService(requestDto, RequestAttributes.None);

        /// <summary>
        /// Override to intercept Service Requests
        /// </summary>
        public virtual object ExecuteService(object requestDto, IRequest req) => ServiceController.Execute(requestDto, req);

        /// <summary>
        /// Override to intercept Async Service Requests
        /// </summary>
        public virtual Task<object> ExecuteServiceAsync(object requestDto, IRequest req) => ServiceController.ExecuteAsync(requestDto, req);

        /// <summary>
        /// Override to intercept Service Requests
        /// </summary>
        public virtual object ExecuteService(object requestDto, RequestAttributes requestAttributes) => ServiceController.Execute(requestDto, new BasicRequest(requestDto, requestAttributes));

        /// <summary>
        /// Override to intercept MQ Requests
        /// </summary>
        public virtual object ExecuteMessage(IMessage mqMessage) => ServiceController.ExecuteMessage(mqMessage, new BasicRequest(mqMessage));

        /// <summary>
        /// Override to intercept MQ Requests
        /// </summary>
        public virtual object ExecuteMessage(IMessage dto, IRequest req) => ServiceController.ExecuteMessage(dto, req);

        /// <summary>
        /// Override to intercept MQ Requests
        /// </summary>
        public Task<object> ExecuteMessageAsync(IMessage mqMessage, CancellationToken token=default) =>
            ServiceController.ExecuteMessageAsync(mqMessage, token);

        /// <summary>
        /// Override to intercept MQ Requests
        /// </summary>
        public Task<object> ExecuteMessageAsync(IMessage mqMessage, IRequest req, CancellationToken token=default) =>
            ServiceController.ExecuteMessageAsync(mqMessage, req, token);

        /// <summary>
        /// Manually register ServiceStack Service at these routes
        /// </summary>
        public virtual void RegisterService<T>(params string[] atRestPaths) where T : IService => RegisterService(typeof(T), atRestPaths);

        /// <summary>
        /// Manually register ServiceStack Service at these routes
        /// </summary>
        public virtual void RegisterService(Type serviceType, params string[] atRestPaths)
        {
            ServiceController.RegisterService(serviceType);
            var reqAttr = serviceType.FirstAttribute<DefaultRequestAttribute>();
            if (reqAttr != null)
            {
                foreach (var atRestPath in atRestPaths)
                {
                    if (atRestPath == null) 
                        continue;
                    var verbs = reqAttr.Verbs ?? serviceType.GetVerbs(); 
                    this.Routes.Add(reqAttr.RequestType, atRestPath, verbs);
                }
            }
        }
        
        /// <summary>
        /// Register all ServiceStack Services found in this Assembly
        /// </summary>
        public void RegisterServicesInAssembly(Assembly assembly)
        {
            ServiceController.RegisterServicesInAssembly(assembly);
        }

        /// <summary>
        /// Return the [Route] attributes for this Request DTO Type
        /// </summary>
        public virtual RouteAttribute[] GetRouteAttributes(Type requestType)
        {
            return requestType.AllAttributes<RouteAttribute>();
        }

        /// <summary>
        /// Override to customize WSDL returned in SOAP /metadata pages
        /// </summary>
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

        /// <summary>
        /// Register Typed Service Request Filter
        /// </summary>
        public void RegisterTypedRequestFilter<T>(Action<IRequest, IResponse, T> filterFn)
        {
            GlobalTypedRequestFilters[typeof(T)] = new TypedFilter<T>(filterFn);
        }

        /// <summary>
        /// Register Async Typed Service Request Filter
        /// </summary>
        public void RegisterTypedRequestFilterAsync<T>(Func<IRequest, IResponse, T, Task> filterFn)
        {
            GlobalTypedRequestFiltersAsync[typeof(T)] = new TypedFilterAsync<T>(filterFn);
        }

        /// <summary>
        /// Register Typed Service Request Filter
        /// </summary>
        public void RegisterTypedRequestFilter<T>(Func<Container, ITypedFilter<T>> filter)
        {
            RegisterTypedFilter(RegisterTypedRequestFilter, filter);
        }

        /// <summary>
        /// Register Async Typed Service Request Filter
        /// </summary>
        public void RegisterTypedRequestFilterAsync<T>(Func<Container, ITypedFilterAsync<T>> filter)
        {
            RegisterTypedFilterAsync(RegisterTypedRequestFilterAsync, filter);
        }

        /// <summary>
        /// Register Typed Service Response Filter
        /// </summary>
        public void RegisterTypedResponseFilter<T>(Action<IRequest, IResponse, T> filterFn)
        {
            GlobalTypedResponseFilters[typeof(T)] = new TypedFilter<T>(filterFn);
        }

        /// <summary>
        /// Register Async Typed Service Response Filter
        /// </summary>
        public void RegisterTypedResponseFilterAsync<T>(Func<IRequest, IResponse, T, Task> filterFn)
        {
            GlobalTypedResponseFiltersAsync[typeof(T)] = new TypedFilterAsync<T>(filterFn);
        }

        /// <summary>
        /// Register Typed Service Response Filter
        /// </summary>
        public void RegisterTypedResponseFilter<T>(Func<Container, ITypedFilter<T>> filter)
        {
            RegisterTypedFilter(RegisterTypedResponseFilter, filter);
        }

        /// <summary>
        /// Register Async Typed Service Response Filter
        /// </summary>
        public void RegisterTypedResponseFilterAsync<T>(Func<Container, ITypedFilterAsync<T>> filter)
        {
            RegisterTypedFilterAsync(RegisterTypedResponseFilterAsync, filter);
        }

        private void RegisterTypedFilter<T>(Action<Action<IRequest, IResponse, T>> registerTypedFilter, Func<Container, ITypedFilter<T>> filter)
        {
            // The filter MUST be resolved inside the RegisterTypedFilter call.
            // Otherwise, the container will not be able to resolve some auto-wired dependencies.
            registerTypedFilter.Invoke((request, response, dto) => filter
                .Invoke(Container)
                .Invoke(request, response, dto));
        }

        private void RegisterTypedFilterAsync<T>(Action<Func<IRequest, IResponse, T, Task>> registerTypedFilter, Func<Container, ITypedFilterAsync<T>> filter)
        {
            // The filter MUST be resolved inside the RegisterTypedFilter call.
            // Otherwise, the container will not be able to resolve some auto-wired dependencies.
            registerTypedFilter((request, response, dto) => filter
                .Invoke(Container)
                .InvokeAsync(request, response, dto));
        }

        /// <summary>
        /// Register Typed MQ Request Filter
        /// </summary>
        public void RegisterTypedMessageRequestFilter<T>(Action<IRequest, IResponse, T> filterFn)
        {
            GlobalTypedMessageRequestFilters[typeof(T)] = new TypedFilter<T>(filterFn);
        }

        /// <summary>
        /// Register Typed MQ Response Filter
        /// </summary>
        public void RegisterTypedMessageResponseFilter<T>(Action<IRequest, IResponse, T> filterFn)
        {
            GlobalTypedMessageResponseFilters[typeof(T)] = new TypedFilter<T>(filterFn);
        }

        /// <summary>
        /// Override to customize the physical path to return for this relativePath 
        /// </summary>
        public virtual string MapProjectPath(string relativePath)
        {
            return relativePath.MapProjectPath();
        }

        /// <summary>
        /// Override to customize normalized /path/info to use for this request 
        /// </summary>
        public virtual string ResolvePathInfo(IRequest request, string originalPathInfo)
        {
            var pathInfo = NormalizePathInfo(originalPathInfo, Config.HandlerFactoryPath);            
            return pathInfo;
        }

        /// <summary>
        /// Normalizes /path/info based on Config.HandlerFactoryPath 
        /// </summary>
        public static string NormalizePathInfo(string pathInfo, string mode)
        {
            if (mode?.Length > 0 && mode[0] == '/')
                mode = mode.Substring(1);

            if (string.IsNullOrEmpty(mode))
                return pathInfo;

            var pathNoPrefix = pathInfo.Length > 0 && pathInfo[0] == '/'
                ? pathInfo.Substring(1)
                : pathInfo;

            var normalizedPathInfo = pathNoPrefix == mode || 
                 (pathNoPrefix.StartsWith(mode) && pathNoPrefix.Length > mode.Length && pathNoPrefix[mode.Length] == '/')
                ? pathNoPrefix.Substring(mode.Length)
                : pathInfo;

            return normalizedPathInfo.Length > 0 && normalizedPathInfo[0] != '/'
                ? '/' + normalizedPathInfo
                : normalizedPathInfo;
        }

        /// <summary>
        /// Override to customize Redirect Responses
        /// </summary>
        public virtual IHttpHandler ReturnRedirectHandler(IHttpRequest httpReq)
        {
            var pathInfo = NormalizePathInfo(httpReq.OriginalPathInfo, Config.HandlerFactoryPath);
            return Config.RedirectPaths.TryGetValue(pathInfo, out string redirectPath)
                ? new RedirectHttpHandler { RelativeUrl = redirectPath }
                : null;
        }
        
        /// <summary>
        /// Override to customize IHttpHandler used to service ?debug=requestinfo requests
        /// </summary>
        public virtual IHttpHandler ReturnRequestInfoHandler(IHttpRequest httpReq)
        {
            if ((Config.DebugMode
                 || Config.AdminAuthSecret != null)
                && httpReq.QueryString[Keywords.Debug] == Keywords.RequestInfo)
            {
                if (Config.DebugMode || HasValidAuthSecret(httpReq))
                {
                    var reqInfo = RequestInfoHandler.GetRequestInfo(httpReq);

                    reqInfo.Host = Config.DebugHttpListenerHostEnvironment + "_v" + Env.VersionString + "_" + ServiceName;
                    reqInfo.PathInfo = httpReq.PathInfo;
                    reqInfo.GetPathUrl = httpReq.GetPathUrl();

                    return new RequestInfoHandler { RequestInfo = reqInfo };
                }
            }

            return null;
        }

        public virtual void OnApplicationStopping()
        {
            Dispose();
        }

        /// <summary>
        /// Executes OnDisposeCallbacks and Disposes IDisposable's dependencies in the IOC &amp; reset singleton states
        /// </summary>
        /// <param name="disposing"></param>
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

                HostContext.Reset();
                AuthenticateService.Reset();
                JS.UnConfigure();
                JsConfig.Reset(); //Clears Runtime Attributes
                Validators.Reset();
                
                TaskScheduler.UnobservedTaskException -= this.HandleUnobservedTaskException;

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
