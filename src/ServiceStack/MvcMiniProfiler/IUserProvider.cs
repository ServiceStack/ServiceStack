using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MvcMiniProfiler
{
    /// <summary>
    /// Provides functionality to identify which user is profiling a request.
    /// </summary>
    public interface IUserProvider
    {
        /// <summary>
        /// Returns a string to identify the user profiling the current 'request'.
        /// </summary>
        /// <param name="request">The current HttpRequest being profiled.</param>
        string GetUser(HttpRequest request);
    }
}
