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
using ServiceStack.IO;
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
    public abstract class HttpListenerBase : IDisposable, IAppHost, IHasContainer
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(HttpListenerBase));

		private const int RequestThreadAbortedException = 995;

		protected HttpListener Listener;
		protected bool IsStarted = false;

		private readonly DateTime startTime;

		public static HttpListenerBase Instance { get; protected set; }

		private readonly AutoResetEvent ListenForNextRequest = new AutoResetEvent(false);

		public event DelReceiveWebRequest ReceiveWebRequest;

		protected HttpListenerBase()
		{
            this.startTime = DateTime.UtcNow;
			Log.Info("Begin Initializing Application...");

			EndpointHostConfig.SkipPathValidation = true;
		}

		protected HttpListenerBase(string serviceName, params Assembly[] assembliesWithServices)
			: this()
		{
			EndpointHost.ConfigureHost(this, serviceName, CreateServiceManager(assembliesWithServices));
		}

		protected virtual ServiceManager CreateServiceManager(params Assembly[] assembliesWithServices)
		{
			return new ServiceManager(assembliesWithServices);
		}

		public void Init()
		{
			if (Instance != null)
			{
				throw new InvalidDataException("HttpListenerBase.Instance has already been set");
			}

			Instance = this;

			var serviceManager = EndpointHost.Config.ServiceManager;
			if (serviceManager != null)
			{
				serviceManager.Init();
				Configure(EndpointHost.Config.ServiceManager.Container);
			}
			else
			{
				Configure(null);
			}

			EndpointHost.AfterInit();

            var elapsed = DateTime.UtcNow - this.startTime;
			Log.InfoFormat("Initializing Application took {0}ms", elapsed.TotalMilliseconds);
		}

		public abstract void Configure(Container container);

		/// <summary>
		/// Starts the Web Service
		/// </summary>
		/// <param name="urlBase">
		/// A Uri that acts as the base that the server is listening on.
		/// Format should be: http://127.0.0.1:8080/ or http://127.0.0.1:8080/somevirtual/
		/// Note: the trailing slash is required! For more info see the
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

            EndpointHost.Config.ServiceStackHandlerFactoryPath = HttpListenerRequestWrapper.GetHandlerPathIfAny(urlBase);

			this.Listener.Prefixes.Add(urlBase);

			this.IsStarted = true;
			this.Listener.Start();

			ThreadPool.QueueUserWorkItem(Listen);
		}

	    private bool IsListening
	    {
            get { return this.IsStarted && this.Listener != null && this.Listener.IsListening; }
	    }

		// Loop here to begin processing of new requests.
		private void Listen(object state)
		{
			while (this.IsListening)
			{
				if (this.Listener == null) return;

				try
				{
					this.Listener.BeginGetContext(ListenerCallback, this.Listener);
					ListenForNextRequest.WaitOne();
				}
				catch (Exception ex)
				{
					Log.Error("Listen()", ex);
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

			try
			{
				if (!IsListening)
				{
					Log.DebugFormat("Ignoring ListenerCallback() as HttpListener is no longer listening");
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
                var errMsg = ex + ": " + IsListening;
				Log.Warn(errMsg);
				return;
			}
			finally
			{
				// Once we know we have a request (or exception), we signal the other thread
				// so that it calls the BeginGetContext() (or possibly exits if we're not
				// listening any more) method to start handling the next incoming request
				// while we continue to process this request on a different thread.
				ListenForNextRequest.Set();
			}

			if (context == null) return;

            Log.InfoFormat("{0} Request : {1}", context.Request.UserHostAddress, context.Request.RawUrl);

            //System.Diagnostics.Debug.WriteLine("Start: " + requestNumber + " at " + DateTime.UtcNow);
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
				Log.ErrorFormat(error);

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
					Log.ErrorFormat(error);

				}
			}

            //System.Diagnostics.Debug.WriteLine("End: " + requestNumber + " at " + DateTime.UtcNow);
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

				Log.ErrorFormat("Swallowing HttpListenerException({0}) Thread exit or aborted request", RequestThreadAbortedException);
			}
            this.IsStarted = false;
            this.Listener = null;
		}

		/// <summary>
		/// Overridable method that can be used to implement a custom hnandler
		/// </summary>
		/// <param name="context"></param>
		protected abstract void ProcessRequest(HttpListenerContext context);

		protected void SetConfig(EndpointHostConfig config)
		{
			if (config.ServiceName == null)
				config.ServiceName = EndpointHost.Config.ServiceName;

			if (config.ServiceManager == null)
				config.ServiceManager = EndpointHost.Config.ServiceManager;

			config.ServiceManager.ServiceController.EnableAccessRestrictions = config.EnableAccessRestrictions;

			EndpointHost.Config = config;

			JsonDataContractSerializer.Instance.UseBcl = config.UseBclJsonSerializers;
			JsonDataContractDeserializer.Instance.UseBcl = config.UseBclJsonSerializers;
		}

		public Container Container
		{
			get
			{
				return EndpointHost.Config.ServiceManager.Container;
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

        /// <summary>
        /// Resolves from IoC container a specified type instance.
        /// </summary>
        /// <typeparam name="T">Type to be resolved.</typeparam>
        /// <returns>Instance of <typeparamref name="T"/>.</returns>
        public static T Resolve<T>()
        {
            if (Instance == null) throw new InvalidOperationException("AppHostBase is not initialized.");
            return Instance.Container.Resolve<T>();
        }

        /// <summary>
        /// Resolves and auto-wires a ServiceStack Service
        /// </summary>
        /// <typeparam name="T">Type to be resolved.</typeparam>
        /// <returns>Instance of <typeparamref name="T"/>.</returns>
        public static T ResolveService<T>(HttpListenerContext httpCtx) where T : class, IRequiresRequestContext
        {
            if (Instance == null) throw new InvalidOperationException("AppHostBase is not initialized.");
            var service = Instance.Container.Resolve<T>();
            if (service == null) return null;
            service.RequestContext = httpCtx.ToRequestContext();
            return service;
        }

        protected IServiceController ServiceController
        {
            get
            {
                return EndpointHost.Config.ServiceController;
            }
        }

		public IServiceRoutes Routes
		{
			get { return EndpointHost.Config.ServiceController.Routes; }
		}

		public Dictionary<Type, Func<IHttpRequest, object>> RequestBinders
		{
			get { return EndpointHost.ServiceManager.ServiceController.RequestTypeFactoryMap; }
		}

		public IContentTypeFilter ContentTypeFilters
		{
			get
			{
				return EndpointHost.ContentTypeFilter;
			}
		}

		public List<Action<IHttpRequest, IHttpResponse>> PreRequestFilters
		{
			get
			{
				return EndpointHost.RawRequestFilters;
			}
		}

		public List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters
		{
			get
			{
				return EndpointHost.RequestFilters;
			}
		}

		public List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters
		{
			get
			{
				return EndpointHost.ResponseFilters;
			}
		}

        public List<IViewEngine> ViewEngines
        {
            get
            {
                return EndpointHost.ViewEngines;
            }
        }

        public HandleUncaughtExceptionDelegate ExceptionHandler
        {
            get { return EndpointHost.ExceptionHandler; }
            set { EndpointHost.ExceptionHandler = value; }
        }

        public HandleServiceExceptionDelegate ServiceExceptionHandler
        {
            get { return EndpointHost.ServiceExceptionHandler; }
            set { EndpointHost.ServiceExceptionHandler = value; }
        }

        public List<HttpHandlerResolverDelegate> CatchAllHandlers
		{
			get { return EndpointHost.CatchAllHandlers; }
		}

		public EndpointHostConfig Config
		{
			get { return EndpointHost.Config; }
		}

        ///TODO: plugin added with .Add method after host initialization won't be configured. Each plugin should have state so we can invoke Register method if host was already started.  
		public List<IPlugin> Plugins
		{
			get { return EndpointHost.Plugins; }
		}
		
		public IVirtualPathProvider VirtualPathProvider
		{
			get { return EndpointHost.VirtualPathProvider; }
			set { EndpointHost.VirtualPathProvider = value; }
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
					Log.Warn("Error loading plugin " + plugin.GetType().Name, ex);
				}
			}
		}

		public void RegisterService(Type serviceType, params string[] atRestPaths)
		{
            var genericService = EndpointHost.Config.ServiceManager.RegisterService(serviceType);
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

        private bool disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            lock (this)
            {
                if (disposed) return;

                if (disposing)
                {
                    this.Stop();

                    if (EndpointHost.Config.ServiceManager != null)
                    {
                        EndpointHost.Config.ServiceManager.Dispose();
                    }

                    Instance = null;
                }

                //release unmanaged resources here...

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
