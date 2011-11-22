using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MvcMiniProfiler
{
    partial class WebRequestProfilerProvider
    {
        /// <summary>
        /// WebRequestProfilerProvider specific configurations
        /// </summary>
        public static class Settings
        {

            /// <summary>
            /// Provides user identification for a given profiling request.
            /// </summary>
            public static IUserProvider UserProvider
            {
                get;
                set;
            }
        }
    }
}
