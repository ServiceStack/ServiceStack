using System;
using System.Collections.Generic;

namespace ServiceStack
{
    public static class AsyncExtensions
    {
        public static List<IAsyncResult> TimesAsync(this int times, Action<int> actionFn)
        {
            var asyncResults = new List<IAsyncResult>(times);
            for (var i = 0; i < times; i++)
            {
                asyncResults.Add(actionFn.BeginInvoke(i, null, null));
            }
            return asyncResults;
        }

        public static List<IAsyncResult> TimesAsync(this int times, Action actionFn)
        {
            var asyncResults = new List<IAsyncResult>(times);
            for (var i = 0; i < times; i++)
            {
                asyncResults.Add(actionFn.BeginInvoke(null, null));
            }
            return asyncResults;
        }
    }
}