using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using ServiceStack.Logging;
using ServiceStack.Service;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
	/**
	 * Need to provide async request options
	 * http://msdn.microsoft.com/en-us/library/86wf6409(VS.71).aspx
	 */
	public abstract class ServiceClientBase
		: IServiceClient, IRestClient
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(ServiceClientBase));

		public static Action<HttpWebRequest> HttpWebRequestFilter { get; set; }

		public const string DefaultHttpMethod = "POST";

		readonly AsyncServiceClient asyncClient;

		protected ServiceClientBase()
		{
			this.HttpMethod = DefaultHttpMethod;
			asyncClient = new AsyncServiceClient {
				ContentType = ContentType,
				StreamSerializer = SerializeToStream,
				StreamDeserializer = StreamDeserializer
			};
		}

		protected ServiceClientBase(string syncReplyBaseUri, string asyncOneWayBaseUri)
			: this()
		{
			this.SyncReplyBaseUri = syncReplyBaseUri;
			this.AsyncOneWayBaseUri = asyncOneWayBaseUri;
		}

		public void SetBaseUri(string baseUri, string format)
		{
			this.BaseUri = baseUri;
			this.SyncReplyBaseUri = baseUri.WithTrailingSlash() + format + "/syncreply/";
			this.AsyncOneWayBaseUri = baseUri.WithTrailingSlash() + format + "/asynconeway/";
		}

		public string BaseUri { get; set; }

		public string SyncReplyBaseUri { get; set; }

		public string AsyncOneWayBaseUri { get; set; }

		public TimeSpan? Timeout { get; set; }

		public abstract string ContentType { get; }

		public string HttpMethod { get; set; }

		private ICredentials credentials;
		public ICredentials Credentials
		{
			set
			{
				this.credentials = value;
				this.asyncClient.Credentials = value;
			}
		}

		public abstract void SerializeToStream(IRequestContext requestContext, object request, Stream stream);

		public abstract T DeserializeFromStream<T>(Stream stream);

		public abstract StreamDeserializerDelegate StreamDeserializer { get; }

		public virtual TResponse Send<TResponse>(object request)
		{
			var requestUri = this.SyncReplyBaseUri.WithTrailingSlash() + request.GetType().Name;
			var client = SendRequest(requestUri, request);

			try
			{
				using (var responseStream = client.GetResponse().GetResponseStream())
				{
					var response = DeserializeFromStream<TResponse>(responseStream);
					return response;
				}
			}
			catch (Exception ex)
			{
				HandleResponseException<TResponse>(ex, requestUri);
				throw;
			}
		}

		private void HandleResponseException<TResponse>(Exception ex, string requestUri)
		{
			var webEx = ex as WebException;
			if (webEx != null && webEx.Status == WebExceptionStatus.ProtocolError)
			{
				var errorResponse = ((HttpWebResponse)webEx.Response);
				log.Error(webEx);
				log.DebugFormat("Status Code : {0}", errorResponse.StatusCode);
				log.DebugFormat("Status Description : {0}", errorResponse.StatusDescription);

				var serviceEx = new WebServiceException(errorResponse.StatusDescription) {
					StatusCode = (int)errorResponse.StatusCode,
				};

				try
				{
					using (var stream = errorResponse.GetResponseStream())
					{
						serviceEx.ResponseDto = DeserializeFromStream<TResponse>(stream);
					}
				}
				catch (Exception innerEx)
				{
					// Oh, well, we tried
					throw new WebServiceException(errorResponse.StatusDescription, innerEx) {
						StatusCode = (int)errorResponse.StatusCode,
					};
				}

				//Escape deserialize exception handling and throw here
				throw serviceEx;
			}

			var authEx = ex as AuthenticationException;
			if (authEx != null)
			{
				throw WebRequestUtils.CreateCustomException(requestUri, authEx);
			}

			throw ex;
		}

		private WebRequest SendRequest(string requestUri, object request)
		{
			var isHttpGet = HttpMethod != null && HttpMethod.ToUpper() == "GET";
			if (isHttpGet)
			{
				var queryString = QueryStringSerializer.SerializeToString(request);
				if (!string.IsNullOrEmpty(queryString))
				{
					requestUri += "?" + queryString;
				}
			}

			return SendRequest(HttpMethod ?? DefaultHttpMethod, requestUri, request);
		}

		private WebRequest SendRequest(string httpMethod, string requestUri, object request)
		{
			if (httpMethod == null)
				throw new ArgumentNullException("httpMethod");

			var client = (HttpWebRequest)WebRequest.Create(requestUri);
			try
			{
				if (this.Timeout.HasValue)
				{
					client.Timeout = (int)this.Timeout.Value.TotalMilliseconds;
				}

				client.Accept = ContentType;
				client.Method = httpMethod;
				if (this.credentials != null)
				{
					client.Credentials = this.credentials;
				}

				if (HttpWebRequestFilter != null)
				{
					HttpWebRequestFilter(client);
				}

				if (httpMethod != Web.HttpMethod.Get
					&& httpMethod != Web.HttpMethod.Delete)
				{
					client.ContentType = ContentType;

					using (var requestStream = client.GetRequestStream())
					{
						SerializeToStream(null, request, requestStream);
					}
				}
			}
			catch (AuthenticationException ex)
			{
				throw WebRequestUtils.CreateCustomException(requestUri, ex) ?? ex;
			}
			return client;
		}

		private string GetUrl(string relativeOrAbsoluteUrl)
		{
			return relativeOrAbsoluteUrl.StartsWith("http:")
				|| relativeOrAbsoluteUrl.StartsWith("https:")
					 ? relativeOrAbsoluteUrl
					 : this.BaseUri + relativeOrAbsoluteUrl;
		}

		public void SendOneWay(object request)
		{
			var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + request.GetType().Name;
			var client = SendRequest(requestUri, request);
		}

		public void SendAsync<TResponse>(object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			var requestUri = this.SyncReplyBaseUri.WithTrailingSlash() + request.GetType().Name;
			asyncClient.SendAsync(Web.HttpMethod.Post, requestUri, request, onSuccess, onError);
		}

		public void GetAsync<TResponse>(string relativeOrAbsoluteUrl, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			asyncClient.SendAsync(Web.HttpMethod.Get, GetUrl(relativeOrAbsoluteUrl), null, onSuccess, onError);
		}

		public void DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			asyncClient.SendAsync(Web.HttpMethod.Delete, GetUrl(relativeOrAbsoluteUrl), null, onSuccess, onError);
		}

		public void PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			asyncClient.SendAsync(Web.HttpMethod.Post, GetUrl(relativeOrAbsoluteUrl), request, onSuccess, onError);
		}

		public void PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			asyncClient.SendAsync(Web.HttpMethod.Put, GetUrl(relativeOrAbsoluteUrl), request, onSuccess, onError);
		}


		public virtual TResponse Send<TResponse>(string httpMethod, string relativeOrAbsoluteUrl, object request)
		{
			var requestUri = GetUrl(relativeOrAbsoluteUrl);
			var client = SendRequest(httpMethod, requestUri, request);

			try
			{
				using (var responseStream = client.GetResponse().GetResponseStream())
				{
					var response = DeserializeFromStream<TResponse>(responseStream);
					return response;
				}
			}
			catch (Exception ex)
			{
				HandleResponseException<TResponse>(ex, requestUri);
				throw;
			}
		}

		public TResponse Get<TResponse>(string relativeOrAbsoluteUrl)
		{
			return Send<TResponse>(Web.HttpMethod.Get, relativeOrAbsoluteUrl, null);
		}

		public TResponse Delete<TResponse>(string relativeOrAbsoluteUrl)
		{
			return Send<TResponse>(Web.HttpMethod.Delete, relativeOrAbsoluteUrl, null);
		}

		public TResponse Post<TResponse>(string relativeOrAbsoluteUrl, object request)
		{
			return Send<TResponse>(Web.HttpMethod.Post, relativeOrAbsoluteUrl, request);
		}

		public TResponse Put<TResponse>(string relativeOrAbsoluteUrl, object request)
		{
			return Send<TResponse>(Web.HttpMethod.Put, relativeOrAbsoluteUrl, request);
		}

		public TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, string mimeType)
		{
			var requestUri = GetUrl(relativeOrAbsoluteUrl);
			var webRequest = (HttpWebRequest)WebRequest.Create(requestUri);
			webRequest.Method = Web.HttpMethod.Post;
			webRequest.Accept = ContentType;

			try
			{
				var webResponse = webRequest.UploadFile(fileToUpload, mimeType);
				using (var responseStream = webResponse.GetResponseStream())
				{
					var response = DeserializeFromStream<TResponse>(responseStream);
					return response;
				}
			}
			catch (Exception ex)
			{
				HandleResponseException<TResponse>(ex, requestUri);
				throw;
			}
		}

		public void Dispose() { }
	}
}