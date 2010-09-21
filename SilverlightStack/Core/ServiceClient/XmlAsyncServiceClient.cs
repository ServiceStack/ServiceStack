using System;
using System.Net;
using System.Threading;
using ServiceStack.Service;
using SilverlightStack.Serialization;

namespace SilverlightStack.ServiceClient
{
	public class XmlAsyncServiceClient : IAsyncServiceClient
	{
		public TimeSpan? Timeout { get; set; }

		private int TimeoutMs
		{
			get
			{
				return Timeout.HasValue ? (int)Timeout.Value.TotalMilliseconds : System.Threading.Timeout.Infinite;
			}
		}

		public XmlAsyncServiceClient(string baseUri)
		{
			this.BaseUri = baseUri;
			var uri = new Uri(this.BaseUri);
		}

		public string BaseUri { get; set; }

		public void Send<TResponse>(object request, Action<TResponse> callback)
		{
			var xmlRequest = DataContractSerializer.Instance.Parse(request);
			var requestUri = this.BaseUri + "/" + request.GetType().Name;

			string responseXml;
			var client = new WebClient();

			//client.DownloadStringCompleted += (DownloadStringCompletedEventHandler)
			//    delegate(object sender, DownloadStringCompletedEventArgs e) {
			//        responseXml = e.Result;
			//        waitHandle.Set();
			//    };

			client.UploadStringCompleted += (UploadStringCompletedEventHandler)
				delegate(object sender, UploadStringCompletedEventArgs e) {
					responseXml = e.Result;
					var response = DataContractDeserializer.Instance.Parse<TResponse>(responseXml);
					callback(response);
				};

			client.UploadStringAsync(new Uri(requestUri), "POST", xmlRequest);
		}

		public void SendOneWay(object request)
		{
			var xmlRequest = DataContractSerializer.Instance.Parse(request);
			var requestUri = this.BaseUri + "/" + request.GetType().Name;

			var client = new WebClient();
			client.UploadStringAsync(new Uri(requestUri), "POST", xmlRequest);
		}

		public void Dispose()
		{
		}

	}
}