using System;
using System.Configuration;
using System.Xml;
using ServiceStack.Messaging.ActiveMq.Support.Utils;

namespace ServiceStack.Messaging.ActiveMq.Support.Config
{
    public class ActiveMqServiceHostConfigBase : IServiceHostConfig
    {
        private const string ERROR_TYPE_NOT_FOUND = "Type: {0} could not be found";

        private const string URI_ATTR = "uri";
        private const string SERVICE_TYPE_ATTR = "serviceType";
        private const string DEAD_LETTER_QUEUE = "deadLetterQueue";
        private const string MAX_REDELIVERY_COUNT = "maxRedeliveryCount";
        private const string FAILOVER_URI = "failoverUri";

        private string uri;
        private Type serviceType;
        private string deadLetterQueue;
        private FailoverSettings failoverSettings;
        private int maxRedeliveryCount;

        public ActiveMqServiceHostConfigBase()
        {
            failoverSettings = new FailoverSettings();
            maxRedeliveryCount = 0;
        }

        public ActiveMqServiceHostConfigBase(XmlElement el)
        {
            uri = el.GetAttribute(URI_ATTR);
            if (string.IsNullOrEmpty(uri))
            {
                throw new ArgumentNullException(URI_ATTR);
            }
            string serviceTypeName = el.GetAttribute(SERVICE_TYPE_ATTR);
            serviceType = AssemblyUtils.FindType(serviceTypeName);
            if (serviceType == null)
            {
                throw new ConfigurationErrorsException(
                    string.Format(ERROR_TYPE_NOT_FOUND, serviceTypeName));
            }
            deadLetterQueue = el.GetAttribute(DEAD_LETTER_QUEUE);
            FailoverUri failoverUri = new FailoverUri();
            if (!string.IsNullOrEmpty(el.GetAttribute(FAILOVER_URI)))
            {
                failoverUri = FailoverUri.Parse(el.GetAttribute(FAILOVER_URI));
            }
            failoverSettings = failoverUri.FailoverSettings;
            maxRedeliveryCount = 0;
            if (!string.IsNullOrEmpty(el.GetAttribute(MAX_REDELIVERY_COUNT)))
            {
                maxRedeliveryCount = int.Parse(el.GetAttribute(MAX_REDELIVERY_COUNT));
            }
        }

        #region IServiceHostConfig Members

        public string Uri
        {
            get { return uri; }
            set { uri = value; }
        }

        public Type ServiceType
        {
            get { return serviceType; }
            set { serviceType = value; }
        }

        public string DeadLetterQueue
        {
            get { return deadLetterQueue; }
            set { deadLetterQueue = value; }
        }

        public FailoverSettings FailoverSettings
        {
            get { return failoverSettings; }
            set { failoverSettings = value; }
        }

        public int MaxRedeliveryCount
        {
            get { return maxRedeliveryCount; }
            set { maxRedeliveryCount = value; }
        }

        #endregion
    }
}
