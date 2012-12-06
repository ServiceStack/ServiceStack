using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ServiceStack.Logging;

namespace ServiceStack.Common
{
    public static class ExecExtensions
    {
        public static void LogError(Type declaringType, string clientMethodName, Exception ex)
        {
            var log = LogManager.GetLogger(declaringType);
            log.Error(string.Format("'{0}' threw an error on {1}: {2}", declaringType.FullName, clientMethodName, ex.Message), ex);
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

            return default(TReturn);
        }

        public static void RetryUntilTrue(Func<bool> action, TimeSpan? timeOut)
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

            throw new TimeoutException(string.Format("Exceeded timeout of {0}", timeOut.Value));
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

            throw new TimeoutException(string.Format("Exceeded timeout of {0}", timeOut.Value), lastEx);
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

        private static void SleepBackOffMultiplier(int i)
        {
            //exponential/random retry back-off.
            var rand = new Random(Guid.NewGuid().GetHashCode());
            var nextTry = rand.Next(
                (int)Math.Pow(i, 2), (int)Math.Pow(i + 1, 2) + 1);

            Thread.Sleep(nextTry);
        }
    }
}
