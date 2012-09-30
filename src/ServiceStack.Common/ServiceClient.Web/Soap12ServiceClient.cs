using System;
using System.IO;
using ServiceStack.Service;
using System.Net;

namespace ServiceStack.ServiceClient.Web
{
#if SILVERLIGHT || MONOTOUCH || XBOX || ANDROID

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

        TResponse IReplyClient.Send<TResponse>(object request)
        {
            throw new NotImplementedException();
        }

        public TResponse Send<TResponse>(ServiceStack.ServiceHost.IReturn<TResponse> request)
        {
            throw new NotImplementedException();
        }

        public void Send(ServiceStack.ServiceHost.IReturnVoid request)
        {
            throw new NotImplementedException();
        }

        TResponse IReplyClient.PostFile<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, string mimeType)
        {
            throw new NotImplementedException();
        }

        TResponse IReplyClient.PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType)
        {
            throw new NotImplementedException();
        }

        TResponse IReplyClient.PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, object request)
        {
            throw new NotImplementedException();
        }

        TResponse IReplyClient.PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, object request)
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

        public TResponse PostFile<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, string mimeType)
        {
            throw new NotImplementedException();
        }

        public TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, FileInfo fileToUpload, object request)
        {
            throw new NotImplementedException();
        }
        
        public TResponse PostFileWithRequest<TResponse>(string relativeOrAbsoluteUrl, Stream fileToUpload, string fileName, object request)
        {
            throw new NotImplementedException();
        }
    }

#else

    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using ServiceStack.Text;
    using ServiceStack.Service;

    public class Soap12ServiceClient : WcfServiceClient
    {
        public Soap12ServiceClient(string uri)
        {
            this.Uri = uri.WithTrailingSlash() + "Soap12";
            this.StoreCookies = true;
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
                    // CCB Custom
                    // Yes, you need this to manage cookies yourself.  Seems counterintutive, but set to true,
                    // it only means that the framework will manage cookie propagation for the same call, which is
                    // not what we want.
                    if (StoreCookies)
                        this.binding.AllowCookies = false;
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
