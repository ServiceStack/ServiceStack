// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Aws.Support
{
    internal static class AwsClientUtils
    {
        internal static JsConfigScope GetJsScope()
        {
            return JsConfig.With(new Config {
                ExcludeTypeInfo = false
            });
        }

        internal static string ToScopedJson<T>(T value)
        {
            using (GetJsScope())
            {
                return JsonSerializer.SerializeToString(value);
            }
        }

        /// <summary>
        /// Sleep using AWS's recommended Exponential BackOff with Full Jitter from:
        /// https://aws.amazon.com/blogs/architecture/exponential-backoff-and-jitter/
        /// </summary>
        /// <param name="retriesAttempted"></param>
        internal static void SleepBackOffMultiplier(this int retriesAttempted) => 
            Thread.Sleep(ExecUtils.CalculateFullJitterBackOffDelay(retriesAttempted));

        internal static async Task SleepBackOffMultiplierAsync(this int retriesAttempted, CancellationToken token=default) => 
            await Task.Delay(ExecUtils.CalculateFullJitterBackOffDelay(retriesAttempted), token);
    }
}