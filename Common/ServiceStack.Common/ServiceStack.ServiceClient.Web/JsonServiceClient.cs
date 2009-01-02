using System.IO;
using System.Net;
using ServiceStack.Service;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.ServiceClient.Web
{
	public class JsonServiceClient : IServiceClient
	{
		public JsonServiceClient(string baseUri)
		{
			this.BaseUri = baseUri;
		}

		public string BaseUri { get; set; }

		public T Send<T>(object request)
		{
			var xmlRequest = JsonDataContractSerializer.Instance.Parse(request);
			var requestUri = this.BaseUri + "/" + request.GetType().Name;
			var client = WebRequest.Create(requestUri);
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
			client.Method = "POST";
			client.ContentType = "application/json";
			using (var writer = new StreamWriter(client.GetRequestStream()))
			{
				writer.Write(xmlRequest);
			}
		}

		public void Dispose() {}
	}
}