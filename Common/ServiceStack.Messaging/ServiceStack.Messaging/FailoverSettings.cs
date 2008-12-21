using System;
using System.Collections.Generic;

namespace ServiceStack.Messaging
{
    /// <summary>
    /// Contains a list of failover brokers as well as options to determine
    /// the behaviour of reconnect attempts
    /// </summary>
    public class FailoverSettings
    {
        private List<string> brokerUris;
        private TimeSpan initialReconnectDelay;
        private TimeSpan maxReconnectDelay;
        private bool useExponentialBackOff;
        private int backOffMultiplier;
        private int maxReconnectAttempts;

        public FailoverSettings ()
        {
            brokerUris = new List<string>();
            initialReconnectDelay = TimeSpan.FromMilliseconds(100);
            maxReconnectDelay = TimeSpan.FromSeconds(10);
            useExponentialBackOff = true;
            backOffMultiplier = 2;
            maxReconnectAttempts = 0;
        }

        /// <summary>
        /// Gets or sets the broker uris.
        /// e.g. tcp://localhost, tcp://wwvis7020
        /// </summary>
        /// <value>The broker uris.</value>
        public List<string> BrokerUris
        {
            get
            {
                return brokerUris;
            }
        }

        /// <summary>
        /// Gets or sets the initial reconnect delay.
        /// </summary>
        /// <value>The initial reconnect delay.</value>
        public TimeSpan InitialReconnectDelay
        {
            get
            {
                return initialReconnectDelay;
            }
            set
            {
                initialReconnectDelay = value;
            }
        }

        /// <summary>
        /// Gets or sets the max reconnect delay.
        /// </summary>
        /// <value>The max reconnect delay.</value>
        public TimeSpan MaxReconnectDelay
        {
            get
            {
                return maxReconnectDelay;
            }
            set
            {
                maxReconnectDelay = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use an exponential back off.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [use exponential back off]; otherwise, <c>false</c>.
        /// </value>
        public bool UseExponentialBackOff
        {
            get
            {
                return useExponentialBackOff;
            }
            set
            {
                useExponentialBackOff = value;
            }
        }

        /// <summary>
        /// Gets or sets the back off multiplier.
        /// </summary>
        /// <value>The back off multiplier.</value>
        public int BackOffMultiplier
        {
            get
            {
                return backOffMultiplier;
            }
            set
            {
                backOffMultiplier = value;
            }
        }

        /// <summary>
        /// Gets or sets the max reconnect attempts.
        /// </summary>
        /// <value>The max reconnect attempts.</value>
        public int MaxReconnectAttempts
        {
            get
            {
                return maxReconnectAttempts;
            }
            set
            {
                maxReconnectAttempts = value;
            }
        }

        /// <summary>
        /// Loads the specified settings.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public void Load(FailoverSettings settings)
        {
            this.BackOffMultiplier = settings.backOffMultiplier;
            this.brokerUris = settings.BrokerUris;
            this.InitialReconnectDelay = settings.initialReconnectDelay;
            this.MaxReconnectAttempts = settings.MaxReconnectAttempts;
            this.MaxReconnectDelay = settings.MaxReconnectDelay;
            maxReconnectAttempts = 0;
        }

    }
}
