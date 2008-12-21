using System;
using System.Text;

namespace ServiceStack.Messaging.ActiveMq.Support.Config
{
    /// <summary>
    /// Container used to hold all the available failover uris
    /// 
    /// failover://(tcp://localhost:61616,tcp://remotehost:61616)?initialReconnectDelay=100
    /// http://activemq.apache.org/failover-transport-reference.html
    /// </summary>
    public class FailoverUri
    {
        private const string ERROR_INVALID_FORMAT = "Invalid format should be: failover://(tcp://localhost:61616,tcp://remotehost:61616)?initialReconnectDelay=100";
        private const string ERROR_OPTION_NOT_RECOGNISED = "Failover option not recognized: {0}";
        
        private const string FAILOVER_PREFIX = "failover://(";
        private const string FAILOVER_HOST_SUFFIX = ")";
        private const char FAILOVER_HOST_SEPERATOR = ',';
        private const char FAILOVER_OPTIONS_PREFIX = '?';
        private const char FAILOVER_OPTIONS_SEPERATOR = '&';

        private readonly FailoverSettings failoverSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="FailoverUri"/> class.
        /// </summary>
        public FailoverUri() : this(new FailoverSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FailoverUri"/> class.
        /// </summary>
        public FailoverUri(FailoverSettings failoverSettings)
        {
            this.failoverSettings = failoverSettings;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FailoverUri"/> class.
        /// </summary>
        /// <param name="failoverUris">The failover uris.</param>
        public FailoverUri(string failoverUris) : this()
        {
            Load(failoverUris);
        }

        /// <summary>
        /// Gets the failover settings.
        /// </summary>
        /// <value>The failover settings.</value>
        public FailoverSettings FailoverSettings
        {
            get { return failoverSettings; }
        }

        /// <summary>
        /// Parses the specified failover uris.
        /// </summary>
        /// <param name="failoverUris">The failover uris.</param>
        /// <returns></returns>
        public static FailoverUri Parse(string failoverUris)
        {
            return new FailoverUri(failoverUris);
        }

        /// <summary>
        /// Loads the specified failover uris.
        /// </summary>
        /// <param name="failoverUris">The failover uris.</param>
        private void Load(string failoverUris)
        {
            if (string.IsNullOrEmpty(failoverUris))
            {
                throw new ArgumentNullException();
            }
            if (!failoverUris.StartsWith(FAILOVER_PREFIX))
            {
                throw new ArgumentException(ERROR_INVALID_FORMAT);
            }
            failoverUris = failoverUris.Substring(FAILOVER_PREFIX.Length);
            int endHostsPos = failoverUris.IndexOf(FAILOVER_HOST_SUFFIX);
            string hosts = failoverUris.Substring(0, endHostsPos);
            string[] failoverHosts = hosts.Split(FAILOVER_HOST_SEPERATOR);
            failoverSettings.BrokerUris.AddRange(failoverHosts);
            int startFailoverOptionsPos = failoverUris.IndexOf(FAILOVER_OPTIONS_PREFIX, endHostsPos);
            if (startFailoverOptionsPos != -1)
            {
                LoadOptions(failoverUris.Substring(startFailoverOptionsPos + 1));
            }
        }

        /// <summary>
        /// Loads the options.
        /// </summary>
        /// <param name="failoverOptsUri">The failover opts URI.</param>
        private void LoadOptions(string failoverOptsUri)
        {
            string[] failoverOpts = failoverOptsUri.Split(FAILOVER_OPTIONS_SEPERATOR);
            foreach (string failoverOpt in failoverOpts)
            {
                string[] failoverOptParts = failoverOpt.Split('=');
                string key = failoverOptParts[0];
                string value = failoverOptParts[1];
                switch (key)
                {
                    case "initialReconnectDelay":
                        failoverSettings.InitialReconnectDelay = TimeSpan.FromMilliseconds(long.Parse(value));
                        break;
                    case "maxReconnectDelay":
                        failoverSettings.MaxReconnectDelay = TimeSpan.FromMilliseconds(long.Parse(value));
                        break;
                    case "backOffMultiplier":
                        failoverSettings.BackOffMultiplier = int.Parse(value);
                        break;
                    case "maxReconnectAttempts":
                        failoverSettings.MaxReconnectAttempts = int.Parse(value);
                        break;
                    case "randomize":
                        //failoverSettings.Randomize = bool.Parse(value);
                        break;
                    default:
                        throw new ArgumentException(string.Format(ERROR_OPTION_NOT_RECOGNISED, key));
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(FAILOVER_PREFIX);
            int i = 0;
            foreach (string brokerUri in failoverSettings.BrokerUris)
            {
                if (i++ > 0)
                {
                    sb.Append(FAILOVER_HOST_SEPERATOR);
                }
                sb.Append(brokerUri);
            }
            sb.Append(FAILOVER_HOST_SUFFIX);
            sb.AppendFormat("{0}{1}={2}", FAILOVER_OPTIONS_PREFIX, "initialReconnectDelay", failoverSettings.InitialReconnectDelay);
            sb.AppendFormat("{0}{1}={2}", FAILOVER_OPTIONS_SEPERATOR, "maxReconnectDelay", failoverSettings.MaxReconnectDelay);
            sb.AppendFormat("{0}{1}={2}", FAILOVER_OPTIONS_SEPERATOR, "backOffMultiplier", failoverSettings.BackOffMultiplier);
            sb.AppendFormat("{0}{1}={2}", FAILOVER_OPTIONS_SEPERATOR, "maxReconnectAttempts", failoverSettings.MaxReconnectAttempts);
            return sb.ToString();
        }
    }
}
