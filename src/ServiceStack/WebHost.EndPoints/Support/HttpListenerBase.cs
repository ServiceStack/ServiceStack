using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Funq;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;

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
		private static readonly ILog Log = LogManager.GetLogger(typeof(HttpListenerBase));

		private const int RequestThreadAbortedException = 995;

		protected HttpListener Listener;
		protected bool IsStarted = false;

		private readonly DateTime startTime;

		public static HttpListenerBase Instance { get; protected set; }

		private static readonly AutoResetEvent ListenForNextRequest = new AutoResetEvent(false);

		public event DelReceiveWebRequest ReceiveWebRequest;

		protected HttpListenerBase()
		{
			this.startTime = DateTime.Now;
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
			if (serviceManager != null)
			{
				//Required for adhoc services added in Configure()
				serviceManager.ReloadServiceOperations();
				EndpointHost.SetOperationTypes(
					serviceManager.ServiceOperations,
					serviceManager.AllServiceOperations
				);
			}

			EndpointHost.AfterInit();

			var elapsed = DateTime.Now - this.startTime;
			Log.InfoFormat("Initializing Application took {0}ms", elapsed.TotalMilliseconds);
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
			var isListening = listener.IsListening;

			try
			{
				if (!isListening)
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
				var errMsg = ex + ": " + isListening;
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

			//System.Diagnostics.Debug.WriteLine("Start: " + requestNumber + " at " + DateTime.Now);
			//var request = context.Request;

			//if (request.HasEntityBody)

			if (this.ReceiveWebRequest != null)
				this.ReceiveWebRequest(context);

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

			//System.Diagnostics.Debug.WriteLine("End: " + requestNumber + " at " + DateTime.Now);
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
			var autoWire = new AutoWireContainer(this.Container);
			autoWire.RegisterAs<T, TAs>();
		}

		public void Register<T>(T instance)
		{
			this.Container.Register(instance);
		}

		public T TryResolve<T>()
		{
			return this.Container.TryResolve<T>();
		}

		public IServiceRoutes Routes
		{
			get { return EndpointHost.Config.ServiceController.Routes; }
		}

		public IContentTypeFilter ContentTypeFilters
		{
			get
			{
				return EndpointHost.ContentTypeFilter;
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

		public List<StreamSerializerResolverDelegate> HtmlProviders
		{
			get
			{
				return EndpointHost.HtmlProviders;
			}
		}

		public List<HttpHandlerResolverDelegate> CatchAllHandlers
		{
			get { return EndpointHost.CatchAllHandlers; }
		}

		public EndpointHostConfig Config
		{
			get { return EndpointHost.Config; }
		}

		public void RegisterService(Type serviceType, params string[] atRestPaths)
		{
			var genericService = EndpointHost.Config.ServiceManager.RegisterService(serviceType);
			var requestType = genericService.GetGenericArguments()[0];
			foreach (var atRestPath in atRestPaths)
			{
				this.Routes.Add(requestType, atRestPath, null, null);
			}
		}

		public virtual void Dispose()
		{
			this.Stop();

			if (EndpointHost.Config.ServiceManager != null)
			{
				EndpointHost.Config.ServiceManager.Dispose();
			}

			Instance = null;
		}
	}
}
