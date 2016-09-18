#if !NETSTANDARD1_6

using System.Web;

namespace ServiceStack.MiniProfiler
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

#endif