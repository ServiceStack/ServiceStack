using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ServiceStack.Common.Support;

#if NETFX_CORE
using Windows.System.Threading;
#endif

namespace ServiceStack.Common
{
    public static class ActionExecExtensions
    {
        public static void ExecAllAndWait(this ICollection<Action> actions, TimeSpan timeout)
        {
            var waitHandles = new WaitHandle[actions.Count];
            var i = 0;
            foreach (var action in actions)
            {
                waitHandles[i++] = action.BeginInvoke(null, null).AsyncWaitHandle;
            }

            WaitAll(waitHandles, timeout);
        }

        public static List<WaitHandle> ExecAsync(this IEnumerable<Action> actions)
        {
            var waitHandles = new List<WaitHandle>();
            foreach (var action in actions)
            {
                var waitHandle = new AutoResetEvent(false);
                waitHandles.Add(waitHandle);
                var commandExecsHandler = new ActionExecHandler(action, waitHandle);
#if NETFX_CORE
                ThreadPool.RunAsync(new WorkItemHandler((IAsyncAction) => commandExecsHandler.Execute()));
#else
                ThreadPool.QueueUserWorkItem(x => ((ActionExecHandler)x).Execute(), commandExecsHandler);
#endif
            }
            return waitHandles;
        }

        public static bool WaitAll(this List<WaitHandle> waitHandles, int timeoutMs)
        {
            return WaitAll(waitHandles.ToArray(), timeoutMs);
        }

        public static bool WaitAll(this ICollection<WaitHandle> waitHandles, int timeoutMs)
        {
            return WaitAll(waitHandles.ToArray(), timeoutMs);
        }

        public static bool WaitAll(this ICollection<WaitHandle> waitHandles, TimeSpan timeout)
        {
            return WaitAll(waitHandles.ToArray(), (int)timeout.TotalMilliseconds);
        }

#if !SILVERLIGHT && !MONOTOUCH && !XBOX
        public static bool WaitAll(this List<IAsyncResult> asyncResults, TimeSpan timeout)
        {
            var waitHandles = asyncResults.ConvertAll(x => x.AsyncWaitHandle);
            return WaitAll(waitHandles.ToArray(), (int)timeout.TotalMilliseconds);
        }
        
        public static bool WaitAll(WaitHandle[] waitHandles, TimeSpan timeout)
        {
            return WaitAll(waitHandles, (int)timeout.TotalMilliseconds);
        }

        public static bool WaitAll(WaitHandle[] waitHandles, int timeOutMs)
        {
            // throws an exception if there are no wait handles
            if (waitHandles == null) throw new ArgumentNullException("waitHandles");
            if (waitHandles.Length == 0) return true;

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                // WaitAll for multiple handles on an STA thread is not supported.
                // CurrentThread is ApartmentState.STA when run under unit tests
                var successfullyComplete = true;
                foreach (var waitHandle in waitHandles)
                {
                    successfullyComplete = successfullyComplete 
                        && waitHandle.WaitOne(timeOutMs, false);
                }
                return successfullyComplete;
            }

            return WaitHandle.WaitAll(waitHandles, timeOutMs, false);
        }
#endif

    }

}