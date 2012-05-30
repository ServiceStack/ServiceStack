#if !SILVERLIGHT && !MONOTOUCH && !XBOX
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.ServiceHost
{
    /// <summary>
    /// This interface can be implemented by an attribute
    /// which adds an response filter for the specific response DTO the attribute marked.
    /// </summary>
    public interface IHasResponseFilter
    {
        /// <summary>
        /// Order in which Response Filters are executed. 
        /// &lt;0 Executed before global response filters
        /// &gt;0 Executed after global response filters
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// The response filter is executed after the service
        /// </summary>
        /// <param name="req">The http request wrapper</param>
        /// <param name="res">The http response wrapper</param>
        /// <param name="requestDto">The response DTO</param>
        void ResponseFilter(IHttpRequest req, IHttpResponse res, object response);
    }
}
#endif