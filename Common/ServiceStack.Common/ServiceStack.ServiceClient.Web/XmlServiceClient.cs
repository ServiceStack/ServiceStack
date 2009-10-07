using System;
using System.IO;
using System.Net;
using System.Threading;
using ServiceStack.Service;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.ServiceClient.Web
{
	public class XmlServiceClient : IServiceClient
	{
		public XmlServiceClient(string baseUri)
		{
			this.BaseUri = baseUri;
		}

		public string BaseUri { get; set; }

		public TimeSpan? Timeout { get; set; }

		public T Send<T>(object request)
		{
			var xmlRequest = DataContractSerializer.Instance.Parse(request);
			var requestUri = this.BaseUri + "/" + request.GetType().Name;
			var client = WebRequest.Create(requestUri);
			client.Method = "POST";
			if (this.Timeout.HasValue)
			{
				client.Timeout = (int) this.Timeout.Value.TotalMilliseconds;
			}

			client.ContentType = "application/xml";
			using (var writer = new StreamWriter(client.GetRequestStream()))
			{
				writer.Write(xmlRequest);
			}

			var xml = new StreamReader(client.GetResponse().GetResponseStream()).ReadToEnd();
			var response = (T)DataContractDeserializer.Instance.Parse(xml, typeof(T));
			return response;
		}

		public void SendOneWay(object request)
		{
			var xmlRequest = DataContractSerializer.Instance.Parse(request);
			var requestUri = this.BaseUri + "/" + request.GetType().Name;
			var client = WebRequest.Create(requestUri);
			if (this.Timeout.HasValue)
			{
				client.Timeout = (int)this.Timeout.Value.TotalMilliseconds;
			}

			client.Method = "POST";
			client.ContentType = "application/xml";
			using (var writer = new StreamWriter(client.GetRequestStream()))
			{
				writer.Write(xmlRequest);
			}
		}

		public void Dispose() { }
	}

	//public class InterruptableXmlServiceClient : IServiceClient
	//{
	//    public InterruptableXmlServiceClient(string baseUri)
	//    {
	//        this.BaseUri = baseUri;
	//    }

	//    public string BaseUri { get; set; }

	//    public T Send<T>(object request, ref Action interruptFunction)
	//    {
	//        var xmlRequest = DataContractSerializer.Instance.Parse(request);
	//        var requestUri = this.BaseUri + "/" + request.GetType().Name;
	//        var client = WebRequest.Create(requestUri);
			
	//        client.Method = "POST";
	//        client.ContentType = "application/xml";

	//        using (var writer = new StreamWriter(client.GetRequestStream()))
	//        {
	//            writer.Write(xmlRequest);
	//        }

	//        var stream = client.GetResponse().GetResponseStream();

	//        var totalRead = 0;
	//        var buffer = new byte[4096];
	//        WaitHandle currentWaitHandle = null;

	//        using (stream)
	//        {
	//            var asyncResult = stream.BeginRead(buffer, 0, buffer.Length - totalRead, null, null);

	//            currentWaitHandle = asyncResult.AsyncWaitHandle;

	//            asyncResult.AsyncWaitHandle.WaitOne(Timeout.Infinite);
	//        }

	//        var xml = new StreamReader(client.GetResponse().GetResponseStream()).ReadToEnd();


	//        var response = (T)DataContractDeserializer.Instance.Parse(xml, typeof(T));
			
	//        return response;
	//    }

	//    public void SendOneWay(object request)
	//    {
	//        var xmlRequest = DataContractSerializer.Instance.Parse(request);
	//        var requestUri = this.BaseUri + "/" + request.GetType().Name;
	//        var client = WebRequest.Create(requestUri);
	//        client.Method = "POST";
	//        client.ContentType = "application/xml";
	//        using (var writer = new StreamWriter(client.GetRequestStream()))
	//        {
	//            writer.Write(xmlRequest);
	//        }
	//    }

	//    public void Dispose() { }
	//}


}