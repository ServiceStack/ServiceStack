using System;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints
{
    /// <summary>
    /// Context to capture IService action
    /// </summary>
    public class ActionContext
    {
        public ActionInvokerFn ServiceAction { get; set; }
        public IHasRequestFilter[] RequestFilters { get; set; }
        public IHasResponseFilter[] ResponseFilters { get; set; }
    }
}