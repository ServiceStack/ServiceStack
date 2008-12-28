using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ServiceStack.ServiceClient.Web
{
	public class Soap11ServiceClient : WcfServiceClient
	{
		private BasicHttpBinding binding;

		public Soap11ServiceClient(string uri)
		{
			this.Uri = uri;
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
}