using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using ServiceStack.Logging;
using ServiceStack.Service;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
	public abstract class ServiceClientBase
		: IServiceClient
	{
		private static ILog log = LogManager.GetLogger(typeof (ServiceClientBase));

		public static Action<HttpWebRequest> HttpWebRequestFilter { get; set; }

		public const string DefaultHttpMethod = "POST";

		protected ServiceClientBase()
		{
			this.HttpMethod = DefaultHttpMethod;
		}

		protected ServiceClientBase(string syncReplyBaseUri, string asyncOneWayBaseUri)
			: this()
		{
			this.SyncReplyBaseUri = syncReplyBaseUri;
			this.AsyncOneWayBaseUri = asyncOneWayBaseUri;
		}

		public string BaseUri
		{
			set
			{
				var baseUri = value.WithTrailingSlash();
				this.SyncReplyBaseUri = baseUri + "SyncReply/";
				this.AsyncOneWayBaseUri = baseUri + "AsyncOneWay/";
			}
		}

		public string SyncReplyBaseUri { get; set; }

		public string AsyncOneWayBaseUri { get; set; }

		public TimeSpan? Timeout { get; set; }

		public abstract string ContentType { get; }

		public string HttpMethod { get; set; }

		public abstract void SerializeToStream(object request, Stream stream);

		public abstract T DeserializeFromStream<T>(Stream stream);

		public virtual TResponse Send<TResponse>(object request)
		{
			var requestUri = this.SyncReplyBaseUri.WithTrailingSlash() + request.GetType().Name;
			var client = SendRequest(request, requestUri);

			try
			{
				using (var responseStream = client.GetResponse().GetResponseStream())
				{
					var response = DeserializeFromStream<TResponse>(responseStream);
					return response;
				}
			}
			catch (WebException webEx)
			{
				if (webEx.Status == WebExceptionStatus.ProtocolError)
				{
					var errorResponse = ((HttpWebResponse)webEx.Response);
					log.Error(webEx);
					log.DebugFormat("Status Code : {0}", errorResponse.StatusCode);
					log.DebugFormat("Status Description : {0}", errorResponse.StatusDescription);

					try
					{
						using (var stream = errorResponse.GetResponseStream())
						{
							var response = DeserializeFromStream<TResponse>(stream);
							return response;
						}
					}
					catch (WebException ex)
					{
						// Oh, well, we tried
						throw;
					}
				}

				throw;
			}
		}

		private WebRequest SendRequest(object request, string requestUri)
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

			var client = (HttpWebRequest)WebRequest.Create(requestUri);
			try
			{
				if (this.Timeout.HasValue)
				{
					client.Timeout = (int)this.Timeout.Value.TotalMilliseconds;
				}

				client.Accept = string.Format("{0}, */*", ContentType);
				client.Method = HttpMethod ?? DefaultHttpMethod;

				if (HttpWebRequestFilter != null)
				{
					HttpWebRequestFilter(client);
				}

				if (!isHttpGet)
				{
					client.ContentType = ContentType;
				
					using (var requestStream = client.GetRequestStream())
					{
						SerializeToStream(request, requestStream);
					}
				}

			}
			catch (AuthenticationException ex)
			{
				throw WebRequestUtils.CreateCustomException(requestUri, ex) ?? ex;
			}
			return client;
		}

		public void SendOneWay(object request)
		{
			var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + request.GetType().Name;
			var client = SendRequest(request, requestUri);
		}

		public void Dispose() { }
	}
}