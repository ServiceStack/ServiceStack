using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack;

public static class ExecUtils
{
    public static void LogError(Type declaringType, string clientMethodName, Exception ex)
    {
        var log = LogManager.GetLogger(declaringType);
        log.Error($"'{declaringType.FullName}' threw an error on {clientMethodName}: {ex.Message}", ex);
    }

    public static void ExecAll<T>(this IEnumerable<T> instances, Action<T> action)
    {
        foreach (var instance in instances)
        {
            try
            {
                action(instance);
            }
            catch (Exception ex)
            {
                LogError(instance.GetType(), action.GetType().Name, ex);
            }
        }
    }

    public static async Task ExecAllAsync<T>(this IEnumerable<T> instances, Func<T,Task> action)
    {
        foreach (var instance in instances)
        {
            try
            {
                await action(instance).ConfigAwait();
            }
            catch (Exception ex)
            {
                LogError(instance.GetType(), action.GetType().Name, ex);
            }
        }
    }

    public static async Task<TReturn> ExecAllReturnFirstAsync<T,TReturn>(this IEnumerable<T> instances, Func<T,Task<TReturn>> action)
    {
        TReturn firstResult = default;
        var i = 0; 
        foreach (var instance in instances)
        {
            try
            {
                var ret = await action(instance).ConfigAwait();
                if (i++ == 0)
                    firstResult = ret;
            }
            catch (Exception ex)
            {
                LogError(instance.GetType(), action.GetType().Name, ex);
            }
        }
        return firstResult;
    }

    public static void ExecAllWithFirstOut<T, TReturn>(this IEnumerable<T> instances, Func<T, TReturn> action, ref TReturn firstResult)
    {
        foreach (var instance in instances)
        {
            try
            {
                var result = action(instance);
                if (!Equals(firstResult, default(TReturn)))
                {
                    firstResult = result;
                }
            }
            catch (Exception ex)
            {
                LogError(instance.GetType(), action.GetType().Name, ex);
            }
        }
    }

    public static TReturn ExecReturnFirstWithResult<T, TReturn>(this IEnumerable<T> instances, Func<T, TReturn> action)
    {
        foreach (var instance in instances)
        {
            try
            {
                var result = action(instance);
                if (!Equals(result, default(TReturn)))
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                LogError(instance.GetType(), action.GetType().Name, ex);
            }
        }

        return default;
    }

    public static async Task<TReturn> ExecReturnFirstWithResultAsync<T, TReturn>(this IEnumerable<T> instances, Func<T, Task<TReturn>> action)
    {
        foreach (var instance in instances)
        {
            try
            {
                var result = await action(instance).ConfigAwait();
                if (!Equals(result, default(TReturn)))
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                LogError(instance.GetType(), action.GetType().Name, ex);
            }
        }

        return default;
    }

    public static void RetryUntilTrue(Func<bool> action, TimeSpan? timeOut=null)
    {
        var i = 0;
        var firstAttempt = DateTime.UtcNow;

        while (timeOut == null || DateTime.UtcNow - firstAttempt < timeOut.Value)
        {
            i++;
            if (action())
            {
                return;
            }
            SleepBackOffMultiplier(i);
        }

        throw new TimeoutException($"Exceeded timeout of {timeOut.Value}");
    }

    public static async Task RetryUntilTrueAsync(Func<Task<bool>> action, TimeSpan? timeOut=null)
    {
        var i = 0;
        var firstAttempt = DateTime.UtcNow;

        while (timeOut == null || DateTime.UtcNow - firstAttempt < timeOut.Value)
        {
            i++;
            if (await action().ConfigAwait())
            {
                return;
            }
            await DelayBackOffMultiplierAsync(i).ConfigAwait();
        }

        throw new TimeoutException($"Exceeded timeout of {timeOut.Value}");
    }

    public static void RetryOnException(Action action, TimeSpan? timeOut)
    {
        var i = 0;
        Exception lastEx = null;
        var firstAttempt = DateTime.UtcNow;

        while (timeOut == null || DateTime.UtcNow - firstAttempt < timeOut.Value)
        {
            i++;
            try
            {
                action();
                return;
            }
            catch (Exception ex)
            {
                lastEx = ex;

                SleepBackOffMultiplier(i);
            }
        }

        throw new TimeoutException($"Exceeded timeout of {timeOut.Value}", lastEx);
    }

    public static async Task RetryOnExceptionAsync(Func<Task> action, TimeSpan? timeOut)
    {
        var i = 0;
        Exception lastEx = null;
        var firstAttempt = DateTime.UtcNow;

        while (timeOut == null || DateTime.UtcNow - firstAttempt < timeOut.Value)
        {
            i++;
            try
            {
                await action().ConfigAwait();
                return;
            }
            catch (Exception ex)
            {
                lastEx = ex;

                await DelayBackOffMultiplierAsync(i).ConfigAwait();
            }
        }

        throw new TimeoutException($"Exceeded timeout of {timeOut.Value}", lastEx);
    }

    public static void RetryOnException(Action action, int maxRetries)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                action();
                break;
            }
            catch
            {
                if (i == maxRetries - 1) throw;

                SleepBackOffMultiplier(i);
            }
        }
    }

    public static T RetryOnException<T>(Func<T> action, int maxRetries)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                return action();
            }
            catch
            {
                if (i == maxRetries - 1) throw;

                SleepBackOffMultiplier(i);
            }
        }
        return action();
    }

    public static async Task RetryOnExceptionAsync(Func<Task> action, int maxRetries)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                await action().ConfigAwait();
                break;
            }
            catch
            {
                if (i == maxRetries - 1) throw;

                await DelayBackOffMultiplierAsync(i).ConfigAwait();
            }
        }
    }

    public static async Task<T> RetryOnExceptionAsync<T>(Func<Task<T>> action, int maxRetries)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                return await action().ConfigAwait();
            }
            catch
            {
                if (i == maxRetries - 1) throw;

                await DelayBackOffMultiplierAsync(i).ConfigAwait();
            }
        }
        return await action().ConfigAwait();
    }

    /// <summary>
    /// Default base sleep time (milliseconds).
    /// </summary>
    public static int BaseDelayMs { get; set; } = 100;

    /// <summary>
    /// Default maximum back-off time before retrying a request
    /// </summary>
    public static int MaxBackOffMs { get; set; } = 1000 * 20;

    /// <summary>
    /// Maximum retry limit. Avoids integer overflow issues.
    /// </summary>
    public static int MaxRetries { get; set; } = 30;

    /// <summary>
    /// How long to sleep before next retry using Exponential BackOff delay with Full Jitter.
    /// </summary>
    /// <param name="retriesAttempted"></param>
    public static void SleepBackOffMultiplier(int retriesAttempted) => TaskUtils.Sleep(CalculateFullJitterBackOffDelay(retriesAttempted));

    /// <summary>
    /// How long to wait before next retry using Exponential BackOff delay with Full Jitter.
    /// </summary>
    /// <param name="retriesAttempted"></param>
    public static Task DelayBackOffMultiplierAsync(int retriesAttempted) => Task.Delay(CalculateFullJitterBackOffDelay(retriesAttempted));

    /// <summary>
    /// Exponential BackOff Delay with Full Jitter
    /// </summary>
    /// <param name="retriesAttempted"></param>
    /// <returns></returns>
    public static int CalculateFullJitterBackOffDelay(int retriesAttempted) => CalculateFullJitterBackOffDelay(retriesAttempted, BaseDelayMs, MaxBackOffMs);

    /// <summary>
    /// Exponential BackOff Delay with Full Jitter from:
    /// https://github.com/aws/aws-sdk-java/blob/master/aws-java-sdk-core/src/main/java/com/amazonaws/retry/PredefinedBackoffStrategies.java
    /// </summary>
    /// <param name="retriesAttempted"></param>
    /// <param name="baseDelay"></param>
    /// <param name="maxBackOffMs"></param>
    /// <returns></returns>
    public static int CalculateFullJitterBackOffDelay(int retriesAttempted, int baseDelay, int maxBackOffMs)
    {
        var random = new Random(Guid.NewGuid().GetHashCode());
        var ceil = CalculateExponentialDelay(retriesAttempted, baseDelay, maxBackOffMs);
        return random.Next(ceil);
    }

    /// <summary>
    /// Calculate exponential retry back-off.
    /// </summary>
    /// <param name="retriesAttempted"></param>
    /// <returns></returns>
    public static int CalculateExponentialDelay(int retriesAttempted) => CalculateExponentialDelay(retriesAttempted, BaseDelayMs, MaxBackOffMs);

    /// <summary>
    /// Calculate exponential retry back-off.
    /// </summary>
    /// <param name="retriesAttempted"></param>
    /// <param name="baseDelay"></param>
    /// <param name="maxBackOffMs"></param>
    /// <returns></returns>
    public static int CalculateExponentialDelay(int retriesAttempted, int baseDelay, int maxBackOffMs)
    {
        if (retriesAttempted <= 0)
            return baseDelay;

        var retries = Math.Min(retriesAttempted, MaxRetries);
        return (int)Math.Min((1L << retries) * baseDelay, maxBackOffMs);
    }

    public static class StaticRandom
    {
#if NET6_0_OR_GREATER
        public static int Next(int maxValue) => Random.Shared.Next(maxValue);
#else
        static int seed = Environment.TickCount;
        static readonly ThreadLocal<Random> random = new(() => new Random(Interlocked.Increment(ref seed)));
        public static int Next(int maxValue)
        {
            return random.Value.Next(maxValue);
        }
#endif
    }
    
    /// <summary>
    /// Calculates delay for retrying an operation as per the retryPolicy
    /// </summary>
    /// <param name="retryAttempt"></param>
    /// <param name="retry"></param>
    /// <returns></returns>
    public static int CalculateRetryDelayMs(int retryAttempt, RetryPolicy retry)
    {
        var retryDelayMs = retry.DelayMs < 0 ? BaseDelayMs : retry.DelayMs;
        var retryMaxDelayMs = retry.MaxDelayMs < 0 ? MaxBackOffMs : retry.MaxDelayMs;
        
        var delayMs = !retry.DelayFirst && retryAttempt == 1
            ? 0
            : retry.Behavior switch {
                RetryBehavior.LinearBackoff => retryAttempt * retryDelayMs,
                RetryBehavior.ExponentialBackoff => 
                    (int)Math.Min((1L << retryAttempt) * retryDelayMs, retryMaxDelayMs),
                RetryBehavior.FullJitterBackoff => 
                    StaticRandom.Next((int)Math.Min((1L << retryAttempt) * retryDelayMs, retryMaxDelayMs)),
                _ => retryDelayMs
            };
        return Math.Min(delayMs, retryMaxDelayMs);
    }

    /// <summary>
    /// Calculate back-off logic for obtaining an in memory lock 
    /// </summary>
    /// <param name="retries"></param>
    /// <returns></returns>
    public static int CalculateMemoryLockDelay(int retries) => retries < 10
        ? CalculateExponentialDelay(retries, baseDelay:5, maxBackOffMs:1000)
        : CalculateFullJitterBackOffDelay(retries, baseDelay:10, maxBackOffMs:10000);

    public static string ShellExec(string command, Dictionary<string, object> args=null) =>
        new ProtectedScripts().sh(default, command, args);
}
