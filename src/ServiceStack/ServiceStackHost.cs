﻿// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
        public Assembly[] ServiceAssemblies { get; private set; }

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
            ServiceAssemblies = assembliesWithServices;
            ServiceController = CreateServiceController(assembliesWithServices);

            ContentTypes = Host.ContentTypes.Instance;
            RestPaths = new List<RestPath>();
            Routes = new ServiceRoutes(this);
            Metadata = new ServiceMetadata(RestPaths);
            PreRequestFilters = new List<Action<IRequest, IResponse>>();
            RequestConverters = new List<Func<IRequest, object, object>>();
            ResponseConverters = new List<Func<IRequest, object, object>>();
            GlobalRequestFilters = new List<Action<IRequest, IResponse, object>>();
            GlobalTypedRequestFilters = new Dictionary<Type, ITypedFilter>();
            GlobalResponseFilters = new List<Action<IRequest, IResponse, object>>();
            GlobalTypedResponseFilters = new Dictionary<Type, ITypedFilter>();
            GlobalMessageRequestFilters = new List<Action<IRequest, IResponse, object>>();
            GlobalTypedMessageRequestFilters = new Dictionary<Type, ITypedFilter>();
            GlobalMessageResponseFilters = new List<Action<IRequest, IResponse, object>>();
            GlobalTypedMessageResponseFilters = new Dictionary<Type, ITypedFilter>();
            GatewayRequestFilters = new List<Action<IRequest, object>>();
            GatewayResponseFilters = new List<Action<IRequest, object>>();
            ViewEngines = new List<IViewEngine>();
            ServiceExceptionHandlers = new List<HandleServiceExceptionDelegate>();
            UncaughtExceptionHandlers = new List<HandleUncaughtExceptionDelegate>();
            AfterInitCallbacks = new List<Action<IAppHost>>();
            OnDisposeCallbacks = new List<Action<IAppHost>>();
            OnEndRequestCallbacks = new List<Action<IRequest>>();
            RawHttpHandlers = new List<Func<IHttpRequest, IHttpHandler>> {
                 HttpHandlerFactory.ReturnRequestInfo,
#if !NETSTANDARD1_6
                 ServiceStack.MiniProfiler.UI.MiniProfilerHandler.MatchesRequest,
#endif
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
                new MarkdownFormat(),
                new PredefinedRoutesFeature(),
                new MetadataFeature(),
                new NativeTypesFeature(),
                new HttpCacheFeature(),
            };
            ExcludeAutoRegisteringServiceTypes = new HashSet<Type> {
                typeof(AuthenticateService),
                typeof(RegisterService),
                typeof(AssignRolesService),
                typeof(UnAssignRolesService),
                typeof(NativeTypesService),
                typeof(PostmanService),
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

            Config = HostConfig.ResetInstance();
            OnConfigLoad();

            Config.DebugMode = GetType().GetAssembly().IsDebugBuild();
            if (Config.DebugMode)
            {
                Plugins.Add(new RequestInfoFeature());
            }

            OnBeforeInit();
            ServiceController.Init();
            Configure(Container);

            ConfigurePlugins();

            List<IVirtualPathProvider> pathProviders = null;
            if (VirtualFileSources == null)
            {
                pathProviders = GetVirtualFileSources().Where(x => x != null).ToList();

                VirtualFileSources = pathProviders.Count > 1
                    ? new MultiVirtualPathProvider(this, pathProviders.ToArray())
                    : pathProviders.First();
            }

            if (VirtualFiles == null)
                VirtualFiles = pathProviders?.FirstOrDefault(x => x is FileSystemVirtualPathProvider) as IVirtualFiles
                    ?? GetVirtualFileSources().FirstOrDefault(x => x is FileSystemVirtualPathProvider) as IVirtualFiles;

            OnAfterInit();

            LogInitComplete();

            return this;
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
            }
            else
            {
                Log.InfoFormat(
                    "Initializing Application {0} took {1}ms. No errors detected.",
                    ServiceName,
                    elapsed.TotalMilliseconds);
            }
        }

        [Obsolete("Renamed to GetVirtualFileSources")]
        public virtual List<IVirtualPathProvider> GetVirtualPathProviders()
        {
            return GetVirtualFileSources();
        }

        /// <summary>
        /// Gets Full Directory Path of where the app is running
        /// </summary>
        public virtual string GetWebRootPath() => Config.WebHostPhysicalPath;

        public virtual List<IVirtualPathProvider> GetVirtualFileSources()
        {
            var pathProviders = new List<IVirtualPathProvider> {
                new FileSystemVirtualPathProvider(this, GetWebRootPath())
            };

            pathProviders.AddRange(Config.EmbeddedResourceBaseTypes.Distinct()
                .Map(x => new ResourceVirtualPathProvider(this, x)));

            pathProviders.AddRange(Config.EmbeddedResourceSources.Distinct()
                .Map(x => new ResourceVirtualPathProvider(this, x)));

            return pathProviders;
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

        /// <summary>
        /// Retain the same behavior as ASP.NET and redirect requests to directores 
        /// without a trailing '/'
        /// </summary>
        public virtual IHttpHandler RedirectDirectory(IHttpRequest request)
        {
            var dir = request.GetVirtualNode() as IVirtualDirectory;
            if (dir != null)
            {
                //Only redirect GET requests for directories which don't have services registered at the same path
                if (!request.PathInfo.EndsWith("/")
                    && request.Verb == HttpMethods.Get
                    && ServiceController.GetRestPathForRequest(request.Verb, request.PathInfo) == null)
                {
                    return new RedirectHttpHandler
                    {
                        RelativeUrl = request.PathInfo + "/",
                    };
                }
            }
            return null;
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

        /// <summary>
        /// Collection of RequestConverters.
        /// Can be used to convert/change Input Dto
        /// Called after routing and model binding, but before request filters.
        /// All request converters are called unless <see cref="IResponse.IsClosed"></see>
        /// Converter can return null, orginal model will be used.
        /// 
        /// Note one converter could influence the input for the next converter!
        /// </summary>
        public List<Func<IRequest, object, object>> RequestConverters { get; set; }

        /// <summary>
        /// Collection of ResponseConverters.
        /// Can be used to convert/change Output Dto
        /// 
        /// Called directly after response is handled, even before <see cref="ApplyResponseFilters"></see>!
        /// </summary>
        public List<Func<IRequest, object, object>> ResponseConverters { get; set; }

        public List<Action<IRequest, IResponse, object>> GlobalRequestFilters { get; set; }

        public Dictionary<Type, ITypedFilter> GlobalTypedRequestFilters { get; set; }

        public List<Action<IRequest, IResponse, object>> GlobalResponseFilters { get; set; }

        public Dictionary<Type, ITypedFilter> GlobalTypedResponseFilters { get; set; }

        public List<Action<IRequest, IResponse, object>> GlobalMessageRequestFilters { get; }

        public Dictionary<Type, ITypedFilter> GlobalTypedMessageRequestFilters { get; set; }

        public List<Action<IRequest, IResponse, object>> GlobalMessageResponseFilters { get; }

        public Dictionary<Type, ITypedFilter> GlobalTypedMessageResponseFilters { get; set; }

        /// <summary>
        /// Lists of view engines for this app.
        /// If view is needed list is looped until view is found.
        /// </summary>
        public List<IViewEngine> ViewEngines { get; set; }

        public List<HandleServiceExceptionDelegate> ServiceExceptionHandlers { get; set; }

        public List<HandleUncaughtExceptionDelegate> UncaughtExceptionHandlers { get; set; }

        public List<Action<IAppHost>> AfterInitCallbacks { get; set; }

        public List<Action<IAppHost>> OnDisposeCallbacks { get; set; }

        public List<Action<IRequest>> OnEndRequestCallbacks { get; set; }

        public List<Func<IHttpRequest, IHttpHandler>> RawHttpHandlers { get; set; }

        public List<HttpHandlerResolverDelegate> CatchAllHandlers { get; set; }

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

        public List<Action<IRequest, object>> GatewayRequestFilters { get; set; }

        public List<Action<IRequest, object>> GatewayResponseFilters { get; set; }

        [Obsolete("Renamed to VirtualFileSources")]
        public IVirtualPathProvider VirtualPathProvider
        {
            get { return VirtualFileSources; }
            set { VirtualFileSources = value; }
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
        /// Occurs when the Service throws an Exception.
        /// </summary>
        public virtual object OnServiceException(IRequest httpReq, object request, Exception ex)
        {
            object lastError = null;
            foreach (var errorHandler in ServiceExceptionHandlers)
            {
                lastError = errorHandler(httpReq, request, ex) ?? lastError;
            }
            return lastError;
        }

        /// <summary>
        /// Occurs when an exception is thrown whilst processing a request.
        /// </summary>
        public virtual void OnUncaughtException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
        {
            if (UncaughtExceptionHandlers.Count > 0)
            {
                foreach (var errorHandler in UncaughtExceptionHandlers)
                {
                    errorHandler(httpReq, httpRes, operationName, ex);
                }
            }
        }

        public virtual void HandleUncaughtException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
        {
            //Only add custom error messages to StatusDescription
            var httpError = ex as IHttpError;
            var errorMessage = httpError?.Message;
            var statusCode = ex.ToStatusCode();

            //httpRes.WriteToResponse always calls .Close in it's finally statement so 
            //if there is a problem writing to response, by now it will be closed
            httpRes.WriteErrorToResponse(httpReq, httpReq.ResponseContentType, operationName, errorMessage, ex, statusCode);
        }

        public virtual void OnStartupException(Exception ex)
        {
            this.StartUpErrors.Add(DtoUtils.CreateErrorResponse(null, ex).GetResponseStatus());
        }

        private HostConfig config;
        public HostConfig Config
        {
            get
            {
                return config;
            }
            set
            {
                config = value;
                OnAfterConfigChanged();
            }
        }

        public virtual void OnConfigLoad()
        {
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
            Container.Register<IHashProvider>(c => new SaltedHash());
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
                }
                if ((Feature.Json & config.EnableFeatures) != Feature.Json)
                {
                    config.IgnoreFormatsInMetadata.Add("json");
                    Config.PreferredContentTypes.Remove(MimeTypes.Json);
                }
                if ((Feature.Jsv & config.EnableFeatures) != Feature.Jsv)
                {
                    config.IgnoreFormatsInMetadata.Add("jsv");
                    Config.PreferredContentTypes.Remove(MimeTypes.Jsv);
                }
                if ((Feature.Csv & config.EnableFeatures) != Feature.Csv)
                {
                    config.IgnoreFormatsInMetadata.Add("csv");
                    Config.PreferredContentTypes.Remove(MimeTypes.Csv);
                }
                if ((Feature.Html & config.EnableFeatures) != Feature.Html)
                {
                    config.IgnoreFormatsInMetadata.Add("html");
                    Config.PreferredContentTypes.Remove(MimeTypes.Html);
                }
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
            delayLoadPlugin = true;
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
                    Container.Register<ICacheClient>(ServiceExtensions.DefaultCache);
            }

            if (!Container.Exists<MemoryCacheClient>())
                Container.Register(ServiceExtensions.DefaultCache);

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

            if (config.LogUnobservedTaskExceptions)
            {
                System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, args) =>
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
                try
                {
                    callback(this);
                }
                catch (Exception ex)
                {
                    OnStartupException(ex);
                }
            }

            ReadyAt = DateTime.UtcNow;
        }

        private void ConfigurePlugins()
        {
            //Some plugins need to initialize before other plugins are registered.
            foreach (var plugin in Plugins)
            {
                var preInitPlugin = plugin as IPreInitPlugin;
                if (preInitPlugin != null)
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
                var preInitPlugin = plugin as IPostInitPlugin;
                if (preInitPlugin != null)
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
                var iocAdapterReleases = Container.Adapter as IRelease;
                if (iocAdapterReleases != null)
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
            var baseUrl = HttpHandlerFactory.GetBaseUrl();
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

        public virtual IVirtualFile ResolveVirtualFile(string virtualPath, IRequest httpReq)
        {
            return VirtualFileSources.GetFile(virtualPath);
        }

        public virtual IVirtualDirectory ResolveVirtualDirectory(string virtualPath, IRequest httpReq)
        {
            return virtualPath == VirtualFileSources.VirtualPathSeparator
                ? VirtualFileSources.RootDirectory
                : VirtualFileSources.GetDirectory(virtualPath);
        }

        public virtual IVirtualNode ResolveVirtualNode(string virtualPath, IRequest httpReq)
        {
            return (IVirtualNode)ResolveVirtualFile(virtualPath, httpReq)
                ?? ResolveVirtualDirectory(virtualPath, httpReq);
        }

        private bool delayLoadPlugin;
        public virtual void LoadPlugin(params IPlugin[] plugins)
        {
            if (delayLoadPlugin)
            {
                LoadPluginsInternal(plugins);
                Plugins.AddRange(plugins);
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

        public virtual object ExecuteService(object requestDto)
        {
            return ExecuteService(requestDto, RequestAttributes.None);
        }

        public virtual object ExecuteService(object requestDto, IRequest req)
        {
            return ServiceController.Execute(requestDto, req);
        }

        public virtual object ExecuteService(object requestDto, RequestAttributes requestAttributes)
        {
            return ServiceController.Execute(requestDto, new BasicRequest(requestDto, requestAttributes));
        }

        public virtual object ExecuteMessage(IMessage mqMessage)
        {
            return ServiceController.ExecuteMessage(mqMessage, new BasicRequest(mqMessage));
        }

        public virtual object ExecuteMessage(IMessage dto, IRequest req)
        {
            return ServiceController.ExecuteMessage(dto, req);
        }

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

        public void RegisterTypedResponseFilter<T>(Action<IRequest, IResponse, T> filterFn)
        {
            GlobalTypedResponseFilters[typeof(T)] = new TypedFilter<T>(filterFn);
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
