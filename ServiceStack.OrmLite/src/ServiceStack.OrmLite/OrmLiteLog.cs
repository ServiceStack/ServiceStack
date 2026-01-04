using System;
using System.Data;
using ServiceStack.Logging;

namespace ServiceStack; // Reduced Type Name in Logging

/// <summary>
/// Adding logging in own class to delay premature static log configuration
/// </summary>
public static class OrmLiteLog
{
    public static ILog Log = LogManager.GetLogger(typeof(OrmLiteLog));
    
    internal static T WithLog<T>(this IDbCommand cmd, T result, ILog log = null)
    {
        log ??= Log;
#if NET8_0_OR_GREATER
        if (log.IsDebugEnabled)
        {
            if (cmd is OrmLite.OrmLiteCommand { StartTimestamp: > 0 } ormCmd)
            {
                var elapsed = ormCmd.GetElapsedTime();
                if (elapsed == TimeSpan.Zero)
                    elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(ormCmd.StartTimestamp, System.Diagnostics.Stopwatch.GetTimestamp());
                if (elapsed != TimeSpan.Zero)
                {
                    log.DebugFormat("TIME: {0:N3}ms", elapsed.TotalMilliseconds);
                }
            }
        }
#endif
        return result;
    }
    
}