using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using ServiceStack.Service;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
	public abstract class ServiceClientBase 
		: IServiceClient
	{
		protected ServiceClientBase()
		{
		}

		protected ServiceClientBase(string syncReplyBaseUri, string asyncOneWayBaseUri)
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

		public abstract void SerializeToStream(object request, Stream stream);

		public abstract T DeserializeFromStream<T>(Stream stream);

		public TResponse Send<TResponse>(object request)
		{
			var requestUri = this.SyncReplyBaseUri.WithTrailingSlash() + request.GetType().Name;
			var client = WebRequest.Create(requestUri);
			try
			{
				if (this.Timeout.HasValue)
				{
					client.Timeout = (int)this.Timeout.Value.TotalMilliseconds;
				}

				client.Method = "POST";
				client.ContentType = ContentType;

				using (var requestStream = client.GetRequestStream())				
				{
					SerializeToStream(request, requestStream);
				}
			}
			catch (AuthenticationException ex)
			{
				throw WebRequestUtils.CreateCustomException(requestUri, ex) ?? ex;
			}

			using (var responseStream = client.GetResponse().GetResponseStream())
			{
				var response = DeserializeFromStream<TResponse>(responseStream);
				return response;
			}
		}

		public void SendOneWay(object request)
		{
			var requestUri = this.AsyncOneWayBaseUri.WithTrailingSlash() + request.GetType().Name;
			var client = WebRequest.Create(requestUri);
			try
			{
				if (this.Timeout.HasValue)
				{
					client.Timeout = (int)this.Timeout.Value.TotalMilliseconds;
				}

				client.Method = "POST";
				client.ContentType = ContentType;

				using (var requestStream = client.GetRequestStream())
				{
					SerializeToStream(request, requestStream);
				}
			}
			catch (AuthenticationException ex)
			{
				throw WebRequestUtils.CreateCustomException(requestUri, ex) ?? ex;
			}
		}

		public void Dispose() {}
	}
}