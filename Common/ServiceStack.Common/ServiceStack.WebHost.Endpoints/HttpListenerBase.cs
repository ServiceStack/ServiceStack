using System;
using System.IO;
using System.Net;
using ServiceStack.Logging;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.WebHost.Endpoints
{
	public delegate void DelReceiveWebRequest(HttpListenerContext Context);

	/// <summary>
	/// Wrapper class for the HTTPListener to allow easier access to the
	/// server, for start and stop management and event routing of the actual
	/// inbound requests.
	/// </summary>
	public abstract class HttpListenerBase : IServiceHost, IDisposable
	{
		private readonly ILog log = LogManager.GetLogger(typeof(EndpointHostBase));

		private const int RequestThreadAbortedException = 995;

		protected HttpListener listener;
		protected bool isStarted = false;

		public event DelReceiveWebRequest ReceiveWebRequest;

		/// <summary>
		/// Starts the Web Service
		/// </summary>
		/// <param name="urlBase">
		/// A Uri that acts as the base that the server is listening on.
		/// Format should be: http://127.0.0.1:8080/ or http://127.0.0.1:8080/somevirtual/
		/// Note: the trailing backslash is required! For more info see the
		/// HttpListener.Prefixes property on MSDN.
		/// </param>
		public void Start(string urlBase)
		{
			// *** Already running - just leave it in place
			if (this.isStarted)
				return;

			if (this.listener == null)
			{
				this.listener = new HttpListener();
			}

			this.listener.Prefixes.Add(urlBase);

			this.isStarted = true;
			this.listener.Start();

			IAsyncResult result = this.listener.BeginGetContext(WebRequestCallback, this.listener);
		}

		/// <summary>
		/// Shut down the Web Service
		/// </summary>
		public void Stop()
		{
			if (listener == null) return;

			try
			{
				this.listener.Close();
			}
			catch (HttpListenerException ex)
			{
				if (ex.ErrorCode != RequestThreadAbortedException)
					throw;

				log.ErrorFormat("Swallowing HttpListenerException({0}) Thread exit or aborted request",
					RequestThreadAbortedException);
			}
			this.listener = null;
			this.isStarted = false;
		}

		protected void WebRequestCallback(IAsyncResult result)
		{
			if (this.listener == null)
				return;

			// Get out the context object
			HttpListenerContext context = this.listener.EndGetContext(result);

			// *** Immediately set up the next context
			this.listener.BeginGetContext(WebRequestCallback, this.listener);

			if (this.ReceiveWebRequest != null)
				this.ReceiveWebRequest(context);

			try
			{
				this.ProcessRequest(context);
			}
			catch (Exception ex)
			{
				var errorMessage = string.Format("Error occured while Processing Request: {0}", ex.Message);
				log.Error(errorMessage, ex);

				var responseXml = string.Format("<Error>\n\t<Message>{0}</Message>\n\t<StackTrace>\n\t\t{1}\n\t</StackTrace>\n</Error>", 
					errorMessage, ex.StackTrace);
				WriteXmlToResponse(context.Response, responseXml);
			}
		}

		/// <summary>
		/// Overridable method that can be used to implement a custom hnandler
		/// </summary>
		/// <param name="context"></param>
		protected abstract void ProcessRequest(HttpListenerContext context);

		protected static object CreateRequest(HttpListenerRequest request, string operationName)
		{
			var operationType = EndpointHost.ServiceOperations.GetOperationType(operationName);
			if (operationType == null)
			{
				throw new NotImplementedException("Could not find handler for: " + operationName);
			}

			//AssertOperationExists(operationName, operationType);
			if (request.HttpMethod == "GET")
			{
				try
				{
					return KeyValueDataContractDeserializer.Instance.Parse(request.QueryString, operationType);
				}
				catch (Exception ex)
				{
					var log = EndpointHost.Config.LogFactory.GetLogger(typeof(HttpListenerBase));
					log.ErrorFormat("Could not deserialize '{0}' request using KeyValueDataContractDeserializer: '{1}'.\nError: '{2}'",
							operationType, request.QueryString, ex);
					throw;
				}
			}

			var xml = new StreamReader(request.InputStream).ReadToEnd();
			try
			{
				return DataContractDeserializer.Instance.Parse(xml, operationType);
			}
			catch (System.Exception ex)
			{
				var log = EndpointHost.Config.LogFactory.GetLogger(typeof(HttpListenerBase));
				log.ErrorFormat("Could not deserialize '{0}' request using DataContractDeserializer: '{1}'.\nError: '{2}'",
						operationType, xml, ex);
				throw;
			}
		}

		protected void WriteXmlToResponse(HttpListenerResponse response, string xml)
		{
			try
			{
				var bOutput = System.Text.Encoding.UTF8.GetBytes(xml);

				response.ContentType = "text/xml";
				response.ContentLength64 = bOutput.Length;

				var outputStream = response.OutputStream;
				outputStream.Write(bOutput, 0, bOutput.Length);
				outputStream.Close();

			}
			catch (Exception ex)
			{
				log.Error("Could not WriteTextToResponse: " + ex.Message, ex);
				throw;
			}
			finally
			{
				response.Close();
			}
		}

		protected void SetConfig(EndpointHostConfig config)
		{
			if (config.ServiceHost == null)
			{
				config.ServiceHost = this;
			}
			
			EndpointHost.Config = config;
		}

		protected abstract IOperationContext CreateOperationContext(object requestDto, EndpointAttributes endpointAttributes);
		
		public virtual object ExecuteService(object request, EndpointAttributes endpointAttributes)
		{
			var operationContext = CreateOperationContext(request, endpointAttributes);
			return EndpointHost.Config.ServiceController.Execute(operationContext);
		}

		public virtual string ExecuteXmlService(string xmlRequest, EndpointAttributes endpointAttributes)
		{
			var operationContext = CreateOperationContext(new XmlRequestDto(xmlRequest), endpointAttributes);
			return EndpointHost.Config.ServiceController.ExecuteXml(operationContext);
		}

		public virtual void Dispose()
		{
			this.Stop();
		}
	}
}