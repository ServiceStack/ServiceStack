using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Text;
using ServiceStack.Common.Web;
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

		/// <summary>
		/// The request filter is called before any request.
		/// This request filter is executed globally.
		/// </summary>
		public static Action<HttpWebRequest> HttpWebRequestFilter { get; set; }

		public const string DefaultHttpMethod = "POST";

		readonly AsyncServiceClient asyncClient;

		protected ServiceClientBase()
		{
			this.StoreCookies = true;
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

		/// <summary>
		/// The user name for basic authentication
		/// </summary>
		public string UserName { get; set; }

		/// <summary>
		/// The password for basic authentication
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// Sets the username and the password for basic authentication.
		/// </summary>
		public void SetCredentials(string userName, string password)
		{
			this.UserName = userName;
			this.Password = password;
		}

		public string BaseUri { get; set; }

		public string SyncReplyBaseUri { get; set; }

		public string AsyncOneWayBaseUri { get; set; }

		private TimeSpan? timeout;
		public TimeSpan? Timeout
		{
			get { return this.timeout; }
			set
			{
				this.timeout = value;
				this.asyncClient.Timeout = value;
			}
		}

		public abstract string ContentType { get; }

		public string HttpMethod { get; set; }

        public IWebProxy Proxy { get; set; }

		private ICredentials credentials;

		/// <summary>
		/// Gets or sets authentication information for the request.
		/// Warning: It's recommened to use <see cref="UserName"/> and <see cref="Password"/> for basic auth.
		/// This property is only used for IIS level authentication.
		/// </summary>
		public ICredentials Credentials
		{
			set
			{
				this.credentials = value;
				this.asyncClient.Credentials = value;
			}
		}

		/// <summary>
		/// Determines if the basic auth header should be sent with every request.
		/// By default, the basic auth header is only sent when "401 Unauthorized" is returned.
		/// </summary>
		public bool AlwaysSendBasicAuthHeader { get; set; }

		/// <summary>
		/// Specifies if cookies should be stored
		/// </summary>
		public bool StoreCookies { get; set; }

		public CookieContainer CookieContainer { get; set; }

		/// <summary>
		/// The request filter is called before any request.
		/// This request filter only works with the instance where it was set (not global).
		/// </summary>
		public Action<HttpWebRequest> LocalHttpWebRequestFilter { get; set; }

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
				TResponse response;
				
				if (!HandleResponseException(ex, Web.HttpMethod.Post, requestUri, request, out response))
				{
					throw;
				}

				return response;
			}
		}

		private bool HandleResponseException<TResponse>(Exception ex, string httpMethod, string requestUri, object request, out TResponse response)
		{
			try
			{
				if (WebRequestUtils.ShouldAuthenticate(ex, this.UserName, this.Password))
				{
					var client = SendRequest(httpMethod, requestUri, request);
					client.AddBasicAuth(this.UserName, this.Password);

					try
					{
						using (var responseStream = client.GetResponse().GetResponseStream())
						{
							response = DeserializeFromStream<TResponse>(responseStream);
							return true;
						}
					}
					catch { /* Ignore deserializing error exceptions */ }
				}
			}
			catch (Exception subEx)
			{
				// Since we are effectively re-executing the call, 
				// the new exception should be shown to the caller rather
				// than the old one.
				// The new exception is either this one or the one thrown
				// by the following method.
				HandleResponseException<TResponse>(subEx, requestUri);
				throw;
			}

			// If this doesn't throw, the calling method 
			// should rethrow the original exception upon
			// return value of false.
			HandleResponseException<TResponse>(ex, requestUri);

			response = default(TResponse);
			return false;
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
					StatusDescription = errorResponse.StatusDescription,
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
						StatusDescription = errorResponse.StatusDescription,
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
				client.Accept = ContentType;
				client.Method = httpMethod;

                if (Proxy != null) client.Proxy = Proxy;
                if (this.Timeout.HasValue) client.Timeout = (int)this.Timeout.Value.TotalMilliseconds;
                if (this.credentials != null) client.Credentials = this.credentials;
				if (this.AlwaysSendBasicAuthHeader) client.AddBasicAuth(this.UserName, this.Password);

				if (StoreCookies)
				{
					if (CookieContainer == null)
						CookieContainer = new CookieContainer();

					client.CookieContainer = CookieContainer;
				}

				if (this.LocalHttpWebRequestFilter != null)
					LocalHttpWebRequestFilter(client);

				if (HttpWebRequestFilter != null)
                    HttpWebRequestFilter(client);

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

        private byte[] DownloadBytes(string requestUri, object request)
        {
            var webRequest = SendRequest(requestUri, request);
            using (var response = webRequest.GetResponse())
            using (var stream = response.GetResponseStream())
                return stream.ReadFully();
        }

		public void SendOneWay(object request)
		{
			var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + request.GetType().Name;
            DownloadBytes(requestUri, request);
		}

	    public void SendOneWay(string relativeOrAbsoluteUrl, object request)
	    {
            var requestUri = GetUrl(relativeOrAbsoluteUrl);
            DownloadBytes(requestUri, request);
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
				TResponse response;

				if (!HandleResponseException(ex, httpMethod, requestUri, request, out response))
				{
					throw;
				}

				return response;
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
            if (Proxy != null) webRequest.Proxy = Proxy;

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