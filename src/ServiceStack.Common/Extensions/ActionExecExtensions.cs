using System;
using System.Collections.Generic;
using System.Threading;

using Proxy = ServiceStack.Common.ActionExecExtensions;

namespace ServiceStack.Common.Extensions
{
    [Obsolete("Use ServiceStack.Common.ActionExecExtensions")]
    public static class ExtensionsProxy
    {
        public static void ExecAllAndWait(this ICollection<Action> actions, TimeSpan timeout)
        {
            Proxy.ExecAllAndWait(actions, timeout);
        }

        public static List<WaitHandle> ExecAsync(this IEnumerable<Action> actions)
        {
            return Proxy.ExecAsync(actions);
        }

        public static bool WaitAll(this List<WaitHandle> waitHandles, int timeoutMs)
        {
            return Proxy.WaitAll(waitHandles, timeoutMs);
        }

        public static bool WaitAll(this ICollection<WaitHandle> waitHandles, int timeoutMs)
        {
            return Proxy.WaitAll(waitHandles, timeoutMs);
        }

        public static bool WaitAll(this ICollection<WaitHandle> waitHandles, TimeSpan timeout)
        {
            return Proxy.WaitAll(waitHandles, timeout);
        }

#if !SILVERLIGHT && !MONOTOUCH && !XBOX
        public static bool WaitAll(this List<IAsyncResult> asyncResults, TimeSpan timeout)
        {
            return Proxy.WaitAll(asyncResults, timeout);
        }
#endif

        public static bool WaitAll(WaitHandle[] waitHandles, TimeSpan timeout)
        {
            return Proxy.WaitAll(waitHandles, timeout);
        }

        public static bool WaitAll(WaitHandle[] waitHandles, int timeOutMs)
        {
            return Proxy.WaitAll(waitHandles, timeOutMs);
        }	
    }
}