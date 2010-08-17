using System;
using System.IO;
using System.Net;
using System.Security.Authentication;

namespace ServiceStack.Client
{
	public class JsonServiceClient : IServiceClient
	{
		private const string ContentType = "application/json";

		public JsonServiceClient(string baseUri)
		{
			this.BaseUri = baseUri;
		}

		public string BaseUri { get; set; }

		public TimeSpan? Timeout { get; set; }

		public T Send<T>(object request)
		{
			var jsonRequest = BclJsonDataContractSerializer.Instance.Parse(request);
			var requestUri = this.BaseUri + "/" + request.GetType().Name;
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
					writer.Write(jsonRequest);
				}
			}
			catch (AuthenticationException ex)
			{
				throw WebRequestUtils.CreateCustomException(requestUri, ex) ?? ex;
			}

			using (var responseStream = client.GetResponse().GetResponseStream())
			using (var reader = new StreamReader(responseStream))
			{
				var json = reader.ReadToEnd();
				var response = (T)BclJsonDataContractDeserializer.Instance.Parse(json, typeof(T));
				return response;
			}
		}

		public void SendOneWay(object request)
		{
			var jsonRequest = BclJsonDataContractSerializer.Instance.Parse(request);
			var requestUri = this.BaseUri + "/" + request.GetType().Name;
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
					writer.Write(jsonRequest);
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