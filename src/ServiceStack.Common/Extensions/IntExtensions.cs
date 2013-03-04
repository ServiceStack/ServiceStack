using System;
using System.Collections.Generic;

using Proxy = ServiceStack.Common.IntExtensions;

namespace ServiceStack.Common.Extensions
{
    [Obsolete("Use ServiceStack.Common.IntExtensions")]
    public static class IntExtensions
    {
        public static IEnumerable<int> Times(this int times)
        {
            return Proxy.Times(times);
        }

        public static void Times(this int times, Action<int> actionFn)
        {
            Proxy.Times(times, actionFn);
        }

        public static void Times(this int times, Action actionFn)
        {
            Proxy.Times(times, actionFn);
        }

        public static List<IAsyncResult> TimesAsync(this int times, Action<int> actionFn)
        {
            return Proxy.TimesAsync(times, actionFn);
        }

        public static List<IAsyncResult> TimesAsync(this int times, Action actionFn)
        {
            return Proxy.TimesAsync(times, actionFn);
        }

        public static List<T> Times<T>(this int times, Func<T> actionFn)
        {
            return Proxy.Times(times, actionFn);
        }

        public static List<T> Times<T>(this int times, Func<int, T> actionFn)
        {
            return Proxy.Times(times, actionFn);
        }
    }
}