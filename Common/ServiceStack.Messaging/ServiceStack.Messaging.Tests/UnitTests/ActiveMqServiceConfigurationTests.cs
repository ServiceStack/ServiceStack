using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using ServiceStack.Messaging.ActiveMq;
using ServiceStack.Messaging.Tests.Services.Messaging;
using NUnit.Framework;

namespace ServiceStack.Messaging.Tests.UnitTests
{
    [TestFixture]
    public class ActiveMqServiceConfigurationTests : UnitTestCaseBase
    {
        private ActiveMqServiceHostConfigQueue queueHostConfig;

        private List<IServiceHost> queueHosts;

        public IServiceHostConfig QueueHostConfig
        {
            get
            {
                if (queueHostConfig == null)
                {
                    queueHostConfig = new ActiveMqServiceHostConfigQueue();
                    queueHostConfig.Uri = "tcp://localhost:61616/Test.Queue";
                    queueHostConfig.FailoverSettings.BrokerUris.AddRange(new string[] { "tcp://localhost", "tcp://mq-broker.service.systest" });
                    queueHostConfig.FailoverSettings.InitialReconnectDelay = TimeSpan.FromMilliseconds(1001);
                    queueHostConfig.FailoverSettings.MaxReconnectDelay = TimeSpan.FromMilliseconds(11);
                    queueHostConfig.ServiceType = typeof(TestService);
                }
                return queueHostConfig;
            }
        }

        private List<IServiceHost> QueueHosts
        {
            get
            {
                if (queueHosts == null)
                {
                    queueHosts = new List<IServiceHost>();
                    IDestination destination =
                        new Destination(DestinationType.Queue, "tcp://localhost:61616/Test.Queue");
                    IGatewayListener listener = MockFactory.CreateConnection(destination.Uri).CreateListener(destination);
                    IServiceHost queueHost = MockFactory.CreateServiceHost(listener, QueueHostConfig);
                    queueHosts.Add(queueHost);
                }
                return queueHosts;
            }
        }

        [Test]
        public void CreateServiceHostsFromConfigTest()
        {
            //<serviceHostsConfigTest>
            //    <queueHost uri="tcp://localhost:61616/Test.Queue" 
            //               failoverUri="failover://(tcp://localhost,tcp://mq-broker.service.systest)?initialReconnectDelay=1001&amp;maxReconnectDelay=11" 
            //               serviceType="ServiceStack.Messaging.Tests.Services.Messaging.TestService, ServiceStack.Messaging.Tests" />
            //</serviceHostsConfigTest>

            IList<IServiceHost> serviceHosts = ActiveMqServiceConfiguration.
                CreateServiceHostsFromConfig(MockFactory, SERVICE_HOSTS_CONFIG_TEST);
            Assert.AreEqual(QueueHosts.Count,serviceHosts.Count);
            IServiceHost testServiceHost = QueueHosts[0];
            IServiceHost serviceHost = serviceHosts[0];
            Assert.AreEqual(serviceHost.Config.Uri, testServiceHost.Config.Uri);
            Assert.AreEqual(serviceHost.Config.FailoverSettings.BrokerUris.Count, testServiceHost.Config.FailoverSettings.BrokerUris.Count);
            Assert.AreEqual(serviceHost.Config.FailoverSettings.BrokerUris[0], testServiceHost.Config.FailoverSettings.BrokerUris[0]);
            Assert.AreEqual(serviceHost.Config.FailoverSettings.BrokerUris[1], testServiceHost.Config.FailoverSettings.BrokerUris[1]);
            Assert.AreEqual(serviceHost.Config.FailoverSettings.InitialReconnectDelay, testServiceHost.Config.FailoverSettings.InitialReconnectDelay);
            Assert.AreEqual(serviceHost.Config.FailoverSettings.MaxReconnectDelay, testServiceHost.Config.FailoverSettings.MaxReconnectDelay);
            Assert.AreEqual(serviceHost.Config.ServiceType, testServiceHost.Config.ServiceType);
        }
        
        [Test]
        public void CreateServiceHostsTest()
        {
            List<IServiceHostConfig> serviceHostsConfig = new List<IServiceHostConfig>(new IServiceHostConfig[] { QueueHostConfig });
            List<IServiceHost> serviceHosts = ActiveMqServiceConfiguration.CreateServiceHosts(serviceHostsConfig, MockFactory);
            Assert.AreEqual(1, serviceHosts.Count);
            Assert.AreEqual(QueueHostConfig.Uri, serviceHosts[0].GatewayListener.Destination.Uri);
            serviceHosts[0].GatewayListener.Dispose();
        }
    }
}
