using System;
using System.IO;
using System.Net;

namespace ServiceStack.Client
{
	public class JsonServiceClient : IServiceClient
	{
		public JsonServiceClient(string baseUri)
		{
			this.BaseUri = baseUri;
		}

		public string BaseUri { get; set; }

		public TimeSpan? Timeout { get; set; }

		public T Send<T>(object request)
		{
			var xmlRequest = JsonDataContractSerializer.Instance.Parse(request);
			var requestUri = this.BaseUri + "/" + request.GetType().Name;
			var client = WebRequest.Create(requestUri);
			if (this.Timeout.HasValue)
			{
				client.Timeout = (int)this.Timeout.Value.TotalMilliseconds;
			}

			client.Method = "POST";
			client.ContentType = "application/json";
			using (var writer = new StreamWriter(client.GetRequestStream()))
			{
				writer.Write(xmlRequest);
			}

			var xml = new StreamReader(client.GetResponse().GetResponseStream()).ReadToEnd();
			var response = (T)JsonDataContractDeserializer.Instance.Parse(xml, typeof(T));
			return response;
		}

		public void SendOneWay(object request)
		{
			var xmlRequest = JsonDataContractSerializer.Instance.Parse(request);
			var requestUri = this.BaseUri + "/" + request.GetType().Name;
			var client = WebRequest.Create(requestUri);
			if (this.Timeout.HasValue)
			{
				client.Timeout = (int)this.Timeout.Value.TotalMilliseconds;
			}

			client.Method = "POST";
			client.ContentType = "application/json";
			using (var writer = new StreamWriter(client.GetRequestStream()))
			{
				writer.Write(xmlRequest);
			}
		}

		public void Dispose() { }
	}
}