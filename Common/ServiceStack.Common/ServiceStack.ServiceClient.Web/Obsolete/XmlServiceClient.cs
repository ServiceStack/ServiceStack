using System;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Threading;
using ServiceStack.Service;
using ServiceStack.ServiceModel.Serialization;

namespace ServiceStack.ServiceClient.Web.Obsolete
{
	[Obsolete]
	public class XmlServiceClient 
		: IServiceClient
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
			try
			{
				client.Method = "POST";
				if (this.Timeout.HasValue)
				{
					client.Timeout = (int)this.Timeout.Value.TotalMilliseconds;
				}

				client.ContentType = "application/xml";
				using (var writer = new StreamWriter(client.GetRequestStream()))
				{
					writer.Write(xmlRequest);
				}
			}
			catch (AuthenticationException ex)
			{
				throw WebRequestUtils.CreateCustomException(requestUri, ex) ?? ex;
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
			try
			{
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
			catch (AuthenticationException ex)
			{
				throw WebRequestUtils.CreateCustomException(requestUri, ex) ?? ex;
			}
		}

		public void Dispose() { }
	}
}