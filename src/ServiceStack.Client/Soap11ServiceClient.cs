#if !(SL5 || XBOX || ANDROID || __IOS__  || __MAC__|| PCL)
namespace ServiceStack
{
    using System;
    
    public class Soap11ServiceClient : WcfServiceClient
    {
        private System.ServiceModel.BasicHttpBinding binding;

        public Soap11ServiceClient(string uri)
        {
            this.Uri = uri.WithTrailingSlash() + "Soap11";
        }

        private System.ServiceModel.Channels.Binding BasicHttpBinding
        {
            get
            {
                if (this.binding == null)
                {
                    this.binding = new System.ServiceModel.BasicHttpBinding
                    {
                        MaxReceivedMessageSize = int.MaxValue,
                        HostNameComparisonMode = System.ServiceModel.HostNameComparisonMode.StrongWildcard,
                    };
                }
                return this.binding;
            }
        }

        protected override System.ServiceModel.Channels.Binding Binding
        {
            get { return this.BasicHttpBinding; }
        }

        protected override System.ServiceModel.Channels.MessageVersion MessageVersion
        {
            get { return this.BasicHttpBinding.MessageVersion; }
        }

        public override void SetProxy(Uri proxyAddress)
        {
            var basicBinding = (System.ServiceModel.BasicHttpBinding)Binding;

            basicBinding.ProxyAddress = proxyAddress;
            basicBinding.UseDefaultWebProxy = false;
            basicBinding.BypassProxyOnLocal = false;
            return;
        }
    }
}

#endif
