using System;
using System.Xml;
using ServiceStack.Messaging.ActiveMq.Support.Config;

namespace ServiceStack.Messaging.ActiveMq
{
    /// <summary>
    /// Represent a queueHost config item defined in an apps App.config
    /// </summary>
    public class ActiveMqServiceHostConfigQueue : ActiveMqServiceHostConfigBase
    {
        public ActiveMqServiceHostConfigQueue(){}

        public ActiveMqServiceHostConfigQueue(XmlElement el) : base(el)
        {
        }
    }
}
