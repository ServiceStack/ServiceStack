using System.Collections.Generic;
using System.Configuration;

namespace ServiceStack.Messaging.ActiveMq
{
    /// <summary>
    /// Configuration class that creates a list of IGatewayListeners from the defintion defined in App.Config
    /// </summary>
    public class ActiveMqServiceConfiguration
    {
        private const string ERROR_CONFIG_SECTION_NOT_FOUND = "The config section {0} is not defined";
        private const string DEFAULT_SECTION_NAME = "serviceHosts";

        /// <summary>
        /// Creates the service hosts from config using the default tagName &lt;serviceHosts /&gt;
        /// </summary>
        /// <param name="messagingFactory">The connection factory.</param>
        /// <returns></returns>
        public static List<IServiceHost> CreateServiceHostsFromConfig(IMessagingFactory messagingFactory)
        {
            return CreateServiceHostsFromConfig(messagingFactory, DEFAULT_SECTION_NAME);
        }

        /// <summary>
        /// Creates the service hosts from config.
        /// </summary>
        /// <param name="messagingFactory">The connection factory.</param>
        /// <param name="configSectionName">Name of the config section.</param>
        /// <returns></returns>
        public static List<IServiceHost> CreateServiceHostsFromConfig(IMessagingFactory messagingFactory, string configSectionName)
        {
            List<IServiceHostConfig> serviceHostConfigs = ConfigurationManager.GetSection(configSectionName) as List<IServiceHostConfig>;
            if (serviceHostConfigs == null)
            {
                throw new ConfigurationErrorsException(
                    string.Format(ERROR_CONFIG_SECTION_NOT_FOUND, configSectionName));
            }
            return CreateServiceHosts(serviceHostConfigs, messagingFactory);
        }

        /// <summary>
        /// Creates the service hosts.
        /// </summary>
        /// <param name="serviceHostConfigs">The service host configs.</param>
        /// <param name="messagingFactory">The connection factory.</param>
        /// <returns></returns>
        public static List<IServiceHost> CreateServiceHosts(List<IServiceHostConfig> serviceHostConfigs, IMessagingFactory messagingFactory)
        {
            List<IServiceHost> serviceHosts = new List<IServiceHost>();
            foreach (IServiceHostConfig serviceHostConfig in serviceHostConfigs)
            {
                IGatewayListener gatewayListener;
                IRegisteredServiceHostConfig subscriberHostServiceHostConfig = serviceHostConfig as IRegisteredServiceHostConfig;
                IConnection connection = messagingFactory.CreateConnection(serviceHostConfig.Uri);
                if (subscriberHostServiceHostConfig != null)
                {
                    IDestination destination = new Destination(DestinationType.Topic, serviceHostConfig.Uri);
                    if (subscriberHostServiceHostConfig.DurableSubscriberId != null)
                    {
                        gatewayListener = connection.CreateRegisteredListener(
                            destination, subscriberHostServiceHostConfig.DurableSubscriberId);
                    }
                    else
                    {
                        gatewayListener = connection.CreateListener(destination);
                    }
                }
                else
                {
                    IDestination destination = new Destination(DestinationType.Queue, serviceHostConfig.Uri);
                    gatewayListener = connection.CreateListener(destination);
                }

                IServiceHost serviceHost = messagingFactory.CreateServiceHost(gatewayListener, serviceHostConfig);
                serviceHosts.Add(serviceHost);
            }
            return serviceHosts;
        }

    }
}
