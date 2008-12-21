using System.Collections.Generic;
using System.Configuration;
using System.Xml;

namespace ServiceStack.Messaging.ActiveMq
{
    /// <summary>
    /// Creates a factory instance from an object definition stored in the applications .config file.
    /// <![CDATA[
    ///   <configuration>
    ///     <configSections>
    ///       <section name="serviceHostsConfigTest" type="ServiceStack.Messaging.ActiveMq.ActiveMqServiceHostsConfigurationSectionHandler, ServiceStack.Messaging.ActiveMq" />
    ///     </configSections>
    ///
    ///     <serviceHostsConfigTest>
    ///       <queueHost uri="tcp://localhost:61616/Test.Queue" serviceType="ServiceStack.Messaging.Tests.Services.Messaging.TestService, ServiceStack.Messaging.Tests" />
    ///     </serviceHostsConfigTest>
    ///   </configuration>
    /// ]]>
    /// </summary>
    public class ActiveMqServiceHostsConfigurationSectionHandler : IConfigurationSectionHandler
    {
        private const string QUEUE_HOST_ELEMENT = "queueHost";
        private const string SUBSCRIBER_HOST_ELEMENT = "subscriberHost";

        /// <summary>
        /// Creates a configuration section handler.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="configContext">Configuration context object.</param>
        /// <param name="section"></param>
        /// <returns>The created section handler object.</returns>
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            List<IServiceHostConfig> serviceHosts = new List<IServiceHostConfig>();
            
            XmlNodeList nodeObjects = section.SelectNodes(QUEUE_HOST_ELEMENT);
            foreach (XmlNode nodeObject in nodeObjects)
            {
                ActiveMqServiceHostConfigQueue serviceHostConfigBase = new ActiveMqServiceHostConfigQueue((XmlElement)nodeObject);
                serviceHosts.Add(serviceHostConfigBase);
            }
            nodeObjects = section.SelectNodes(SUBSCRIBER_HOST_ELEMENT);
            foreach (XmlNode nodeObject in nodeObjects)
            {
                ActiveMqServiceHostConfigTopic serviceHostConfigBase = new ActiveMqServiceHostConfigTopic((XmlElement)nodeObject);
                serviceHosts.Add(serviceHostConfigBase);
            }
            return serviceHosts;
        }
    }
}