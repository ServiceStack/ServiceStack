using System;
using System.Net;
using System.Threading;
using ServiceStack.Service;
using SilverlightStack.Serialization;

namespace SilverlightStack.ServiceClient
{
	public class XmlServiceClient : IServiceClient
	{
		public TimeSpan? Timeout { get; set; }

		private int TimeoutMs
		{
			get
			{
				return Timeout.HasValue ? (int)Timeout.Value.TotalMilliseconds : System.Threading.Timeout.Infinite;
			}
		}

		public XmlServiceClient(string baseUri)
		{
			this.BaseUri = baseUri;
			var uri = new Uri(this.BaseUri);
		}

		public string BaseUri { get; set; }

		public T Send<T>(object request)
		{
			var xmlRequest = DataContractSerializer.Instance.Parse(request);
			var requestUri = this.BaseUri + "/" + request.GetType().Name;

			var waitHandle = new ManualResetEvent(false);
			string responseXml = null;
			var client = new WebClient();

			//client.DownloadStringCompleted += (DownloadStringCompletedEventHandler)
			//    delegate(object sender, DownloadStringCompletedEventArgs e) {
			//        responseXml = e.Result;
			//        waitHandle.Set();
			//    };

			client.UploadStringCompleted += (UploadStringCompletedEventHandler)
				delegate(object sender, UploadStringCompletedEventArgs e) {
					responseXml = e.Result;
					waitHandle.Set();
				};

			client.UploadStringAsync(new Uri(requestUri), "POST", xmlRequest);

			waitHandle.WaitOne(TimeoutMs);

			var response = (T)DataContractDeserializer.Instance.Parse(responseXml, typeof(T));
			return response;
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