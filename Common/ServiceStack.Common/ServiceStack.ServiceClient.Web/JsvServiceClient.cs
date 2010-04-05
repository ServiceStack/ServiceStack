using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using ServiceStack.Service;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
	public class JsvServiceClient 
		: IServiceClient
	{
		private const string ContentType = "text/jsv";

		public JsvServiceClient(string baseUri)
			: this(baseUri + "Jsv/SyncReply", baseUri + "Jsv/AsyncOneWay")
		{
		}

		public JsvServiceClient(string syncReplyBaseUri, string asyncOneWayBaseUri)
		{
			this.SyncReplyBaseUri = syncReplyBaseUri;
			this.AsyncOneWayBaseUri = asyncOneWayBaseUri;
		}

		public string SyncReplyBaseUri { get; set; }
		public string AsyncOneWayBaseUri { get; set; }

		public TimeSpan? Timeout { get; set; }

		public T Send<T>(object request)
		{
			var requestUri = this.SyncReplyBaseUri + "/" + request.GetType().Name;
			var client = WebRequest.Create(requestUri);
			try
			{
				if (this.Timeout.HasValue)
				{
					client.Timeout = (int)this.Timeout.Value.TotalMilliseconds;
				}

				client.Method = "POST";
				client.ContentType = ContentType;

				using (var writer = new StreamWriter(client.GetRequestStream()))
				{
					TypeSerializer.SerializeToWriter(request, writer);
				}
			}
			catch (AuthenticationException ex)
			{
				throw WebRequestUtils.CreateCustomException(requestUri, ex) ?? ex;
			}

			using (var responseStream = client.GetResponse().GetResponseStream())
			using (var reader = new StreamReader(responseStream))
			{
				var response = TypeSerializer.DeserializeFromReader<T>(reader);
				return response;
			}
		}

		public void SendOneWay(object request)
		{
			var requestUri = this.AsyncOneWayBaseUri + "/" + request.GetType().Name;
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
				using (var writer = new StreamWriter(requestStream))
				{
					TypeSerializer.SerializeToWriter(request, writer);
				}
			}
			catch (AuthenticationException ex)
			{
				throw WebRequestUtils.CreateCustomException(requestUri, ex) ?? ex;
			}
		}

		public void Dispose() { }
	}
}