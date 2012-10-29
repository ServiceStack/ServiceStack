using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Funq;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.Configuration;
using ServiceStack.Html;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.VirtualPath;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public delegate void DelReceiveWebRequest(HttpListenerContext context);

	/// <summary>
	/// Wrapper class for the HTTPListener to allow easier access to the
	/// server, for start and stop management and event routing of the actual
	/// inbound requests.
	/// </summary>
	public abstract class HttpListenerBase : IDisposable, IAppHost
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(HttpListenerBase));

		private const int RequestThreadAbortedException = 995;

		protected HttpListener Listener;
		protected bool IsStarted = false;

		private readonly DateTime _startTime;
		private bool _runAsNamedInstance;
		private string _serviceName; //also the name of this instance if running as named instance
		public static HttpListenerBase Instance { get; protected set; }

		private readonly AutoResetEvent _listenForNextRequest = new AutoResetEvent(false);

		public event DelReceiveWebRequest ReceiveWebRequest;

		protected HttpListenerBase()
		{
			_startTime = DateTime.Now;
			_log.Info("Begin Initializing Application...");
			_runAsNamedInstance = false;
			EndpointHostConfig.SkipPathValidation = true;
		}

		protected HttpListenerBase(string serviceName,  params Assembly[] assembliesWithServices)
			: this()
		{
			_runAsNamedInstance = false;
			_serviceName = serviceName;
			OurEndpointHost.ConfigureHost(this, serviceName, CreateServiceManager(assembliesWithServices));
		}

		protected HttpListenerBase(string serviceName, bool runAsNamedInstance, params Assembly[] assembliesWithServices)
			: this()
		{
			if (runAsNamedInstance && string.IsNullOrEmpty(serviceName)) throw new ArgumentException("Must provide a service name of named instances.", "serviceName");
			_runAsNamedInstance = runAsNamedInstance;
			_serviceName = serviceName;
			OurEndpointHost.ConfigureHost(this, serviceName, CreateServiceManager(assembliesWithServices));
		}

		protected EndpointHostInstance OurEndpointHost
		{
			get
			{
				if (_runAsNamedInstance)
				{
					return EndpointHost.GetNamedHost(_serviceName);
				}
				else
				{
					return EndpointHost.Instance;
				}
			}

		}

		private static Dictionary<string, HttpListenerBase> _namedListener = new Dictionary<string, HttpListenerBase>();
		private readonly static object _syncRoot = new object();

		/// <summary>
		/// Gets a <see cref="EndpointHostConfig"/> by name, and creates a new one if one doesn't exist by that name.
		/// </summary>
		/// <param name="name">The name of the config to return or create.</param>
		/// <returns>Returns the instance.</returns>
		/// <remarks>This method is thread safe.</remarks>
		internal static HttpListenerBase GetNamedConfig(string name)
		{
			HttpListenerBase listener;
			if (_namedListener.TryGetValue(name, out listener))
			{
				return listener;
			}

			lock (_syncRoot)
			{
				if (_namedListener.TryGetValue(name, out listener)) //double checked locking works in .Net 
				{
					return listener;
				}

				var namedListener = new Dictionary<string, HttpListenerBase>(_namedListener);
				namedListener.Add(name, listener);
				_namedListener = namedListener;
				return listener;
			}
		}

		protected virtual ServiceManager CreateServiceManager(params Assembly[] assembliesWithServices)
		{		
			return new ServiceManager(assembliesWithServices);
		}

		public void Init()
		{
			if (_runAsNamedInstance == false)
			{
				if (Instance != null)
				{
					throw new InvalidDataException("HttpListenerBase.Instance has already been set");
				}

				Instance = this;
			}

			var serviceManager = OurEndpointHost.Config.ServiceManager;
			if (serviceManager != null)
			{
				serviceManager.Init();
				Configure(OurEndpointHost.Config.ServiceManager.Container);
			}
			else
			{
				Configure(null);
			}
			if (serviceManager != null)
			{
				//Required for adhoc services added in Configure()
				serviceManager.ReloadServiceOperations();
				OurEndpointHost.SetOperationTypes(
					serviceManager.ServiceOperations,
					serviceManager.AllServiceOperations
				);
			}

			OurEndpointHost.AfterInit();

			var elapsed = DateTime.Now - _startTime;
			_log.InfoFormat("Initializing Application took {0}ms", elapsed.TotalMilliseconds);
		}

		public abstract void Configure(Container container);

		/// <summary>
		/// Starts the Web Service
		/// </summary>
		/// <param name="urlBase">
		/// A Uri that acts as the base that the server is listening on.
		/// Format should be: http://127.0.0.1:8080/ or http://127.0.0.1:8080/somevirtual/
		/// Note: the trailing backslash is required! For more info see the
		/// HttpListener.Prefixes property on MSDN.
		/// </param>
		public virtual void Start(string urlBase)
		{
			// *** Already running - just leave it in place
			if (this.IsStarted)
				return;

			if (this.Listener == null)
			{
				this.Listener = new HttpListener();
			}

            OurEndpointHost.Config.ServiceStackHandlerFactoryPath = HttpListenerRequestWrapper.GetHandlerPathIfAny(urlBase);

			this.Listener.Prefixes.Add(urlBase);

			this.IsStarted = true;
			this.Listener.Start();

			ThreadPool.QueueUserWorkItem(Listen);
		}

		// Loop here to begin processing of new requests.
		private void Listen(object state)
		{
			while (this.Listener.IsListening)
			{
				if (this.Listener == null) return;

				try
				{
					this.Listener.BeginGetContext(ListenerCallback, this.Listener);
					_listenForNextRequest.WaitOne();
				}
				catch (Exception ex)
				{
					_log.Error("Listen()", ex);
					return;
				}
				if (this.Listener == null) return;
			}
		}

		// Handle the processing of a request in here.
		private void ListenerCallback(IAsyncResult asyncResult)
		{
			var listener = asyncResult.AsyncState as HttpListener;
			HttpListenerContext context = null;

			if (listener == null) return;
			var isListening = listener.IsListening;

			try
			{
				if (!isListening)
				{
					_log.DebugFormat("Ignoring ListenerCallback() as HttpListener is no longer listening");
					return;
				}
				// The EndGetContext() method, as with all Begin/End asynchronous methods in the .NET Framework,
				// blocks until there is a request to be processed or some type of data is available.
				context = listener.EndGetContext(asyncResult);				
			}
			catch (Exception ex)
			{
				// You will get an exception when httpListener.Stop() is called
				// because there will be a thread stopped waiting on the .EndGetContext()
				// method, and again, that is just the way most Begin/End asynchronous
				// methods of the .NET Framework work.
				var errMsg = ex + ": " + isListening;
				_log.Warn(errMsg);
				return;
			}
			finally
			{
				// Once we know we have a request (or exception), we signal the other thread
				// so that it calls the BeginGetContext() (or possibly exits if we're not
				// listening any more) method to start handling the next incoming request
				// while we continue to process this request on a different thread.
				_listenForNextRequest.Set();
			}

			if (context == null) return;

            _log.InfoFormat("{0} Request : {1}", context.Request.UserHostAddress, context.Request.RawUrl);

			//System.Diagnostics.Debug.WriteLine("Start: " + requestNumber + " at " + DateTime.Now);
			//var request = context.Request;

			//if (request.HasEntityBody)

			RaiseReceiveWebRequest(context);

			try
			{
				this.ProcessRequest(context);
			}
			catch (Exception ex)
			{
				var error = string.Format("Error this.ProcessRequest(context): [{0}]: {1}", ex.GetType().Name, ex.Message);
				_log.ErrorFormat(error);

				try
				{
					var sb = new StringBuilder();
					sb.AppendLine("{");
					sb.AppendLine("\"ResponseStatus\":{");
					sb.AppendFormat(" \"ErrorCode\":{0},\n", ex.GetType().Name.EncodeJson());
					sb.AppendFormat(" \"Message\":{0},\n", ex.Message.EncodeJson());
					sb.AppendFormat(" \"StackTrace\":{0}\n", ex.StackTrace.EncodeJson());
					sb.AppendLine("}");
					sb.AppendLine("}");

					context.Response.StatusCode = 500;
					context.Response.ContentType = ContentType.Json;
					var sbBytes = sb.ToString().ToUtf8Bytes();
					context.Response.OutputStream.Write(sbBytes, 0, sbBytes.Length);
					context.Response.Close();
				}
				catch (Exception errorEx)
				{
					error = string.Format("Error this.ProcessRequest(context)(Exception while writing error to the response): [{0}]: {1}", errorEx.GetType().Name, errorEx.Message);
					_log.ErrorFormat(error);

				}
			}			

			//System.Diagnostics.Debug.WriteLine("End: " + requestNumber + " at " + DateTime.Now);
		}

	    protected void RaiseReceiveWebRequest(HttpListenerContext context)
	    {
	        if (this.ReceiveWebRequest != null)
	            this.ReceiveWebRequest(context);
	    }


	    /// <summary>
		/// Shut down the Web Service
		/// </summary>
		public virtual void Stop()
		{
			if (Listener == null) return;

			try
			{
				this.Listener.Close();
			}
			catch (HttpListenerException ex)
			{
				if (ex.ErrorCode != RequestThreadAbortedException) throw;

				_log.ErrorFormat("Swallowing HttpListenerException({0}) Thread exit or aborted request", RequestThreadAbortedException);
			}
			this.Listener = null;
			this.IsStarted = false;
		}

		/// <summary>
		/// Overridable method that can be used to implement a custom hnandler
		/// </summary>
		/// <param name="context"></param>
		protected abstract void ProcessRequest(HttpListenerContext context);

		protected void SetConfig(EndpointHostConfig config)
		{
			if (config.ServiceName == null)
				config.ServiceName = OurEndpointHost.Config.ServiceName;

			if (config.ServiceManager == null)
				config.ServiceManager = OurEndpointHost.Config.ServiceManager;

			config.ServiceManager.ServiceController.EnableAccessRestrictions = config.EnableAccessRestrictions;

			OurEndpointHost.Config = config;

			//only one serializer perr app domain, so these values will get overwritten for all
			JsonDataContractSerializer.Instance.UseBcl = config.UseBclJsonSerializers;
			JsonDataContractDeserializer.Instance.UseBcl = config.UseBclJsonSerializers;
		}

		public Container Container
		{
			get
			{
				return OurEndpointHost.Config.ServiceManager.Container;
			}
		}

		public void RegisterAs<T, TAs>() where T : TAs
		{
			this.Container.RegisterAutoWiredAs<T, TAs>();
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
                    if (disposable != null)
                        disposable.Dispose();
                }
            }
            catch {/*ignore*/}
        }

        public virtual void OnEndRequest()
        {
            foreach (var item in HostContext.Instance.Items.Values)
            {
                Release(item);
            }

            HostContext.Instance.EndRequest();
        }

	    public void Register<T>(T instance)
		{
			this.Container.Register(instance);
		}

		public T TryResolve<T>()
		{
			return this.Container.TryResolve<T>();
		}

        protected IServiceController ServiceController
        {
            get
            {
                return OurEndpointHost.Config.ServiceController;
            }
        }

		public IServiceRoutes Routes
		{
			get { return OurEndpointHost.Config.ServiceController.Routes; }
		}

		public Dictionary<Type, Func<IHttpRequest, object>> RequestBinders
		{
			get { return OurEndpointHost.ServiceManager.ServiceController.RequestTypeFactoryMap; }
		}

		public IContentTypeFilter ContentTypeFilters
		{
			get
			{
				return OurEndpointHost.ContentTypeFilter;
			}
		}

		public List<Action<IHttpRequest, IHttpResponse>> PreRequestFilters
		{
			get
			{
				return OurEndpointHost.RawRequestFilters;
			}
		}

		public List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters
		{
			get
			{
				return OurEndpointHost.RequestFilters;
			}
		}

		public List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters
		{
			get
			{
				return OurEndpointHost.ResponseFilters;
			}
		}

        public List<IViewEngine> ViewEngines
        {
            get
            {
                return OurEndpointHost.ViewEngines;
            }
        }

        public HandleUncaughtExceptionDelegate ExceptionHandler
        {
            get { return OurEndpointHost.ExceptionHandler; }
            set { OurEndpointHost.ExceptionHandler = value; }
        }

        public HandleServiceExceptionDelegate ServiceExceptionHandler
        {
            get { return OurEndpointHost.ServiceExceptionHandler; }
            set { OurEndpointHost.ServiceExceptionHandler = value; }
        }

        public List<HttpHandlerResolverDelegate> CatchAllHandlers
		{
			get { return OurEndpointHost.CatchAllHandlers; }
		}

		public EndpointHostConfig Config
		{
			get { return OurEndpointHost.Config; }
		}

        ///TODO: plugin added with .Add method after host initialization won't be configured. Each plugin should have state so we can invoke Register method if host was already started.  
		public List<IPlugin> Plugins
		{
			get { return OurEndpointHost.Plugins; }
		}
		
		public IVirtualPathProvider VirtualPathProvider
		{
			get { return OurEndpointHost.VirtualPathProvider; }
			set { OurEndpointHost.VirtualPathProvider = value; }
		}

        public virtual IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
        {
            return new ServiceRunner<TRequest>(this, actionContext);
        }

	    public virtual void LoadPlugin(params IPlugin[] plugins)
		{
			foreach (var plugin in plugins)
			{
				try
				{
					plugin.Register(this);
				}
				catch (Exception ex)
				{
					_log.Warn("Error loading plugin " + plugin.GetType().Name, ex);
				}
			}
		}

		public void RegisterService(Type serviceType, params string[] atRestPaths)
		{
            var genericService = OurEndpointHost.Config.ServiceManager.RegisterService(serviceType);
            if (genericService != null)
            {
                var requestType = genericService.GetGenericArguments()[0];
                foreach (var atRestPath in atRestPaths)
                {
                    this.Routes.Add(requestType, atRestPath, null);
                }
            }
            else
            {
                var reqAttr = serviceType.GetCustomAttributes(true).OfType<DefaultRequestAttribute>().FirstOrDefault();
                if (reqAttr != null)
                {
                    foreach (var atRestPath in atRestPaths)
                    {
                        this.Routes.Add(reqAttr.RequestType, atRestPath, null);
                    }
                }
            }
        }

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					// Dispose managed resources.
					this.Stop();

					if (OurEndpointHost.Config.ServiceManager != null)
					{
						OurEndpointHost.Config.ServiceManager.Dispose();
					}

					if (_runAsNamedInstance)
					{
						//remove named instances
						EndpointHostConfig.RemoveNamedConfig(_serviceName);
						EndpointHost.RemoveNamedHost(_serviceName);
					}
					else
					{
						Instance = null;
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}
			_disposed = true;

		}
		private bool _disposed;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}	
	}
}
