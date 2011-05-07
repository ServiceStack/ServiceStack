using System;

namespace ServiceStack.ServiceClient.Web
{

#if MONOTOUCH

	public class Soap11ServiceClient
	{
		public Soap11ServiceClient(string uri)
		{
			throw new NotImplementedException();
		}
	}

#else

	using System.ServiceModel;
	using System.ServiceModel.Channels;
	using ServiceStack.Text;

	public class Soap11ServiceClient : WcfServiceClient
	{
		private BasicHttpBinding binding;

		public Soap11ServiceClient(string uri)
		{
			this.Uri = uri.WithTrailingSlash() + "Soap11";
		}

		private Binding BasicHttpBinding
		{
			get
			{
				if (this.binding == null)
				{
					this.binding = new BasicHttpBinding {
						MaxReceivedMessageSize = int.MaxValue,
						HostNameComparisonMode = HostNameComparisonMode.StrongWildcard
					};
				}
				return this.binding;
			}
		}

		protected override Binding Binding
		{
			get { return this.BasicHttpBinding; }
		}

		protected override MessageVersion MessageVersion
		{
			get { return this.BasicHttpBinding.MessageVersion; }
		}

		public override void SetProxy(Uri proxyAddress)
		{
			var basicBinding = (BasicHttpBinding)Binding;

			basicBinding.ProxyAddress = proxyAddress;
			basicBinding.UseDefaultWebProxy = false;
			basicBinding.BypassProxyOnLocal = false;
			return;
		}
	}

#endif

}