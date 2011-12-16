using System;
using System.IO;
using ServiceStack.Service;

namespace ServiceStack.ServiceClient.Web
{


#if MONOTOUCH

	public class Soap12ServiceClient  : IServiceClient
	{
		public Soap12ServiceClient(string uri)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public void SetCredentials(string userName, string password)
		{
			throw new NotImplementedException();
		}

		public void GetAsync<TResponse>(string relativeOrAbsoluteUrl, Action<TResponse> onSuccess, 
			Action<TResponse, Exception> onError)
		{
			throw new NotImplementedException();
		}

		public void DeleteAsync<TResponse>(string relativeOrAbsoluteUrl, Action<TResponse> onSuccess, 
			Action<TResponse, Exception> onError)
		{
			throw new NotImplementedException();
		}

		public void PostAsync<TResponse>(string relativeOrAbsoluteUrl, object request, 
			Action<TResponse> onSuccess, Action<TResponse,Exception> onError)
		{
			throw new NotImplementedException();
		}

		public void PutAsync<TResponse>(string relativeOrAbsoluteUrl, object request, Action<TResponse> onSuccess, 
			Action<TResponse,Exception> onError)
		{
			throw new NotImplementedException();
		}

		public void SendAsync<TResponse>(object request, Action<TResponse> onSuccess, Action<TResponse, Exception> onError)
		{
			throw new NotImplementedException();
		}

		public void SendOneWay(object request)
		{
			throw new NotImplementedException();
		}

		public void SendOneWay(string relativeOrAbsoluteUrl, object request)
		{
			throw new NotImplementedException();
		}

		public TResponse Send<TResponse>(object request)
		{
			throw new NotImplementedException();
		}

		public TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, string mimeType)
		{
			throw new NotImplementedException();
		}
	}

#else

	using System.ServiceModel;
	using System.ServiceModel.Channels;
	using ServiceStack.Text;

	public class Soap12ServiceClient : WcfServiceClient
	{
		public Soap12ServiceClient(string uri)
		{
			this.Uri = uri.WithTrailingSlash() + "Soap12";
		}

		private WSHttpBinding binding;

		private Binding WsHttpBinding
		{
			get
			{
				if (this.binding == null)
				{
					this.binding = new WSHttpBinding {
						MaxReceivedMessageSize = int.MaxValue,
						HostNameComparisonMode = HostNameComparisonMode.StrongWildcard,						
						MaxBufferPoolSize = 524288,
					};
					this.binding.Security.Mode = SecurityMode.None;
				}
				return this.binding;
			}
		}

		protected override Binding Binding
		{
			get { return this.WsHttpBinding; }
		}

		protected override MessageVersion MessageVersion
		{
			get { return MessageVersion.Default; }
		}

		public override void SetProxy(Uri proxyAddress)
		{
			var wsHttpBinding = (WSHttpBinding)Binding;

			wsHttpBinding.ProxyAddress = proxyAddress;
			wsHttpBinding.UseDefaultWebProxy = false;
			wsHttpBinding.BypassProxyOnLocal = false;
			return;
		}
	}

#endif
}
