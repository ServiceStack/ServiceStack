using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Extensions;

namespace ServiceStack.ServiceInterface
{
    /// <summary>
    /// Class represents HttpContext helper methods.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Converts current <see cref="HttpContext"/> into <see cref="HttpRequestContext" />.
        /// </summary>
        /// <param name="httpContext">Current context.</param>
        /// <returns>Created ServiceStack RequestContext.</returns>
        public static HttpRequestContext ToRequestContext(this HttpContext httpContext)
        {
            return new HttpRequestContext(httpContext.Request.ToRequest(), httpContext.Response.ToResponse(), null);
        }
    }
}
