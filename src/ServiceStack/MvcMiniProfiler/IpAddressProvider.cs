using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace MvcMiniProfiler
{
    /// <summary>
    /// Identifies users based on ip address.
    /// </summary>
    public class IpAddressIdentity : IUserProvider
    {
        /// <summary>
        /// Returns the paramter HttpRequest's client ip address.
        /// </summary>
        public string GetUser(HttpRequest request)
        {
            return request.ServerVariables["REMOTE_ADDR"] ?? "";
        }
    }
}
