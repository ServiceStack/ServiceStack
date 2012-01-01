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
        /// The response filter is executed after the service
        /// </summary>
        /// <param name="req">The http request wrapper</param>
        /// <param name="res">The http response wrapper</param>
        /// <param name="requestDto">The response DTO</param>
        void ResponseFilter(IHttpRequest req, IHttpResponse res, object responseDto);
    }
}
