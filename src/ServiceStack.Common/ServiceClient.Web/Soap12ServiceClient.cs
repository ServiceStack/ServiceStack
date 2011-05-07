using System;

namespace ServiceStack.ServiceClient.Web
{


#if MONOTOUCH

	public class Soap12ServiceClient 
	{
		public Soap12ServiceClient(string uri)
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
