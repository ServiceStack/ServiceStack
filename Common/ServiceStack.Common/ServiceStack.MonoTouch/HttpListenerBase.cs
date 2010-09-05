using System;
using System.IO;
using System.Net;
using System.Reflection;
using Funq;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Support
{
	public delegate void DelReceiveWebRequest(HttpListenerContext context);

	/// <summary>
	/// Wrapper class for the HTTPListener to allow easier access to the
	/// server, for start and stop management and event routing of the actual
	/// inbound requests.
	/// </summary>
	public abstract class HttpListenerBase : IDisposable
	{
		private readonly ILog log = LogManager.GetLogger(typeof(HttpListenerBase));

		private const int RequestThreadAbortedException = 995;

		protected HttpListener Listener;
		protected bool IsStarted = false;

		private readonly DateTime startTime;
		private readonly ServiceManager serviceManager;
		public static HttpListenerBase Instance { get; protected set; }

		public event DelReceiveWebRequest ReceiveWebRequest;

		protected HttpListenerBase()
		{
			this.startTime = DateTime.Now;
			log.Info("Begin Initializing Application...");
		}

		protected HttpListenerBase(string serviceName, params Assembly[] assembliesWithServices)
			: this()
		{
			this.serviceManager = new ServiceManager(assembliesWithServices);

			SetConfig(new EndpointHostConfig
			          	{
			          		ServiceName = serviceName,
			          		ServiceController = serviceManager.ServiceController,
			          	});
		}

		public void Init()
		{
			if (Instance != null)
			{
				throw new InvalidDataException("HttpListenerBase.Instance has already been set");
			}

			Instance = this;

			if (this.serviceManager != null)
			{
				serviceManager.Init();
				Configure(serviceManager.Container);
			}
			else
			{
				Configure(null);
			}

			EndpointHost.SetOperationTypes(
				EndpointHost.Config.ServiceController.OperationTypes,
				EndpointHost.Config.ServiceController.AllOperationTypes
				);

			var elapsed = DateTime.Now - this.startTime;
			log.InfoFormat("Initializing Application took {0}ms", elapsed.TotalMilliseconds);
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

			var result = this.Listener.BeginGetContext(
				WebRequestCallback, this.Listener);
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
				if (ex.ErrorCode != RequestThreadAbortedException)
					throw;

				log.ErrorFormat("Swallowing HttpListenerException({0}) Thread exit or aborted request",
				                RequestThreadAbortedException);
			}
			this.Listener = null;
			this.IsStarted = false;
		}

		protected void WebRequestCallback(IAsyncResult result)
		{
			if (this.Listener == null)
				return;

			try
			{
				// Get out the context object
				var context = this.Listener.EndGetContext(result);

				// *** Immediately set up the next context
				this.Listener.BeginGetContext(WebRequestCallback, this.Listener);

				if (this.ReceiveWebRequest != null)
					this.ReceiveWebRequest(context);

				this.ProcessRequest(context);

			}
			catch (HttpListenerException ex)
			{
				if (ex.ErrorCode != RequestThreadAbortedException)
					throw;

				log.ErrorFormat("Swallowing HttpListenerException({0}) Thread exit or aborted request",
				                RequestThreadAbortedException);
			}
		}

		/// <summary>
		/// Overridable method that can be used to implement a custom hnandler
		/// </summary>
		/// <param name="context"></param>
		protected abstract void ProcessRequest(HttpListenerContext context);

		protected void SetConfig(EndpointHostConfig config)
		{
			EndpointHost.Config = config;
			this.serviceManager.ServiceController.EnableAccessRestrictions = config.EnableAccessRestrictions;
		}

		public virtual object ExecuteService(object request, EndpointAttributes endpointAttributes)
		{
			return EndpointHost.Config.ServiceController.Execute(
				request, new HttpRequestContext(request, endpointAttributes));
		}

		public virtual string ExecuteXmlService(string xmlRequest, EndpointAttributes endpointAttributes)
		{
			return (string)EndpointHost.Config.ServiceController.ExecuteText(
			               	xmlRequest, new HttpRequestContext(xmlRequest, endpointAttributes));
		}

		public virtual void Dispose()
		{
			this.Stop();

			if (serviceManager != null)
			{
				serviceManager.Dispose();
			}
		}
	}
}