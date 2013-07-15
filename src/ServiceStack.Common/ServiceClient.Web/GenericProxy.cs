#if !SILVERLIGHT && !MONOTOUCH && !XBOX && !ANDROIDINDIE
using System.ServiceModel;
using System.ServiceModel.Description;

namespace ServiceStack.ServiceClient.Web
{
    /// <summary>
    /// Generic Proxy for service calls.
    /// </summary>
    /// <typeparam name="T">The service Contract</typeparam>
    public class GenericProxy<T> : ClientBase<T> where T : class
    {
        public GenericProxy()
            : base()
        {
            Initialize();
        }

        public GenericProxy(string endpoint)
            : base(endpoint)
        {
            Initialize();
        }

        public GenericProxy(ServiceEndpoint endpoint)
            : base(endpoint.Binding, endpoint.Address)
        {
            Initialize();
        }

        public void Initialize()
        {
            //this.Endpoint.Behaviors.Add(new ServiceEndpointBehaviour());
        }

        /// <summary>
        /// Returns the transparent proxy for the service call
        /// </summary>
        public T Proxy
        {
            get
            {
                return base.Channel;
            }
        }
    }
}
#endif