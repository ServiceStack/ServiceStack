using System;
using System.Xml;
using ServiceStack.Messaging.ActiveMq.Support.Config;

namespace ServiceStack.Messaging.ActiveMq
{
    /// <summary>
    /// Represent a topicHost config item defined in an apps App.config
    /// </summary>
    public class ActiveMqServiceHostConfigTopic : ActiveMqServiceHostConfigBase, IRegisteredServiceHostConfig
    {
        private const string DURABLE_SUBSCRIBER_ID = "durableSubscriberId";
        private readonly string durableSubscriberId;

        public ActiveMqServiceHostConfigTopic(){}

        public ActiveMqServiceHostConfigTopic(XmlElement el)
            : base(el)
        {
            durableSubscriberId = el.GetAttribute(DURABLE_SUBSCRIBER_ID);
        }

        #region IServiceHostTopicConfig Members

        public string DurableSubscriberId
        {
            get { return durableSubscriberId; }
        }

        #endregion
    }
}
