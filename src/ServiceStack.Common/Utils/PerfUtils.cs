#if !SILVERLIGHT && !MONOTOUCH && !XBOX

using System;
using System.Diagnostics;

namespace ServiceStack.Common.Utils
{
    public static class PerfUtils
    {
        public static TimeSpan ToTimeSpan(this long fromTicks)
        {
            return TimeSpan.FromSeconds(fromTicks * 1d / Stopwatch.Frequency);
        }

        public static long Measure(long iterations, Action action)
        {
            GC.Collect();
            var begin = Stopwatch.GetTimestamp();

            for (var i = 0; i < iterations; i++)
            {
                action();
            }

            var end = Stopwatch.GetTimestamp();

            return (end - begin);
        }
    }
}

#endif