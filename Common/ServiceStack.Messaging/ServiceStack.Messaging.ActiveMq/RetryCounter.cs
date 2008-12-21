using System;
using System.Threading;
using ServiceStack.Logging;

namespace ServiceStack.Messaging.ActiveMq
{
    public class RetryCounter
    {
        protected readonly ILog Log;
        private readonly FailoverSettings failoverSettings;

        private TimeSpan lastDelay;
        private int totalRetryAttempts;
        private int retryAttempts;

        public RetryCounter(FailoverSettings failoverSettings)
        {
            Log = LogManager.GetLogger(GetType());
            this.failoverSettings = failoverSettings;
            totalRetryAttempts = 0;
            retryAttempts = 0;
        }

        public int RetryAttempts
        {
            get { return retryAttempts; }
        }

        public int TotalRetryAttempts
        {
            get { return totalRetryAttempts; }
        }

        public void Reset()
        {
            retryAttempts = 0;
        }

        public bool Retry()
        {
            totalRetryAttempts++;
            retryAttempts++;
            if (retryAttempts >= failoverSettings.MaxReconnectAttempts)
            {
                return false;
            }
            if (failoverSettings.UseExponentialBackOff)
            {
                lastDelay = TimeSpan.FromMilliseconds(lastDelay.TotalMilliseconds * failoverSettings.BackOffMultiplier);
                if (lastDelay.TotalMilliseconds > failoverSettings.MaxReconnectDelay.TotalMilliseconds)
                {
                    lastDelay = failoverSettings.MaxReconnectDelay;
                }
            }
            Log.WarnFormat("Retrying in {0}ms, for the {1} time.", lastDelay.TotalMilliseconds, retryAttempts);
            Thread.Sleep(lastDelay);
            return true;
        }
    }
}