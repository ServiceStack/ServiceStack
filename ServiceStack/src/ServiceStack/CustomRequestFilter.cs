using System;
using ServiceStack.Web;

namespace ServiceStack
{
    public class CustomRequestFilter : IPlugin
    {
        private readonly Action<IRequest, IResponse, object> filter;

        public bool ApplyToMessaging { get; set; }

        public CustomRequestFilter(Action<IRequest, IResponse, object> filter)
        {
            this.filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        public void Register(IAppHost appHost)
        {
            appHost.GlobalRequestFilters.Add(filter);

            if (ApplyToMessaging)
                appHost.GlobalMessageRequestFilters.Add(filter);
        }
    }

    public class CustomResponseFilter : IPlugin
    {
        private readonly Action<IRequest, IResponse, object> filter;

        public bool ApplyToMessaging { get; set; }

        public CustomResponseFilter(Action<IRequest, IResponse, object> filter)
        {
            this.filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        public void Register(IAppHost appHost)
        {
            appHost.GlobalResponseFilters.Add(filter);

            if (ApplyToMessaging)
                appHost.GlobalMessageResponseFilters.Add(filter);
        }
    }
}