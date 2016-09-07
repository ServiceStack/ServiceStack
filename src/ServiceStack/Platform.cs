using System.Collections.Generic;
using ServiceStack.Platforms;

namespace ServiceStack
{
    public class Platform
    {
        public static Platform Instance =
#if NETSTANDARD1_3
            new PlatformNetCore();
#else
            new PlatformNet45();
#endif

        public virtual HashSet<string> GetRazorNamespaces()
        {
            return new HashSet<string>();
        }

        public virtual void InitHostConifg(HostConfig config) {}
    }
}