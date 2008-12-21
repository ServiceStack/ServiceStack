using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Messaging.ActiveMq;
using ServiceStack.Messaging.Tests.Objects.Mock;
using NMS;
using NUnit.Framework;

namespace ServiceStack.Messaging.Tests.UnitTests
{
    [TestFixture]
    public class ActiveMqTopicClientTests : UnitTestCaseBase
    {
        #region IOneWayClient Members

        [Test]
        public void SendOneWayTest()
        {
            using (IConnection connection = CreateNewConnection())
            {
                using (IOneWayClient client = connection.CreateClient(DestinationTopic))
                {
                    client.SendOneWay(TextMessage);
                    AssertNmsState(1, 1, 1, 0);
                    Assert.AreEqual(1, MockNmsSession.TextMessages.Count);
                    Assert.AreEqual(1, MockNmsProducer.SentMessages.Count);
                    AssertCustomMessageProperties(MockNmsProducer.SentMessages[0].TextMessage);
                    Assert.AreEqual(TEXT_MESSAGE, MockNmsProducer.SentMessages[0].TextMessage.Text);
                }
            }
            AssertAllResourcesDisposed(MockFactory);
        }

        #endregion

        [Test]
        public void Failover_ConnectionsAlwaysFailsTest()
        {
            IOneWayClient client = null;
            int maximumReconnectAttempts = new Random().Next(2, 10);
            MockFactory.FactoryConnectionType = typeof (MockNmsConnectionWithSessionFailure);
            try
            {
                using (IConnection connection = CreateNewConnection())
                {
                    client = connection.CreateClient(DestinationTopic);
                    client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                    client.FailoverSettings.MaxReconnectAttempts = maximumReconnectAttempts;
                    client.SendOneWay(TextMessage);
                }
            }
            catch (Exception ex)
            {
                if (client == null)
                {
                    throw;
                }
                Assert.IsNotNull(ex.InnerException as NMSException);
                Assert.AreEqual(maximumReconnectAttempts, MockFactory.Connections.Count - 1);

                int i = 0;
                foreach (MockNmsConnection mockNmsConnection in MockFactory.Connections)
                {
                    if (i == 0)
                    {
                        //Initial configuration
                        Assert.IsTrue(mockNmsConnection.BrokerUri.StartsWith(BROKER_URI));
                    }
                    else
                    {
                        //first failover attempt
                        int brokerIndex = (i - 1)%client.FailoverSettings.BrokerUris.Count;
                        string brokerUri = client.FailoverSettings.BrokerUris[brokerIndex];
                        Assert.AreEqual(brokerUri, mockNmsConnection.BrokerUri);
                    }
                    i++;
                }
                return;
            }
            finally
            {
                if (client != null)
                {
                    client.Dispose();
                }
                AssertAllResourcesDisposed(MockFactory);
            }
            Assert.Fail("Exception should've been thrown");
        }

        [Test]
        public void Failover_ProducerSendAlwaysFailsTest()
        {
            int maximumReconnectAttempts = new Random().Next(2, 10);
            MockFactory.FactoryProducerType = typeof (MockNmsProducerWithSendMessageFailure);
            IOneWayClient client = null;
            try
            {
                using (IConnection connection = CreateNewConnection())
                {
                    client = connection.CreateClient(DestinationTopic);
                    client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                    client.FailoverSettings.MaxReconnectAttempts = maximumReconnectAttempts;
                    client.SendOneWay(TextMessage);
                }
            }
            catch (Exception ex)
            {
                if (client == null)
                {
                    throw;
                }
                Assert.IsNotNull(ex.InnerException as NMSException);
                Assert.AreEqual(maximumReconnectAttempts, MockFactory.Connections.Count - 1);

                int i = 0;
                foreach (MockNmsConnection mockNmsConnection in MockFactory.Connections)
                {
                    if (i == 0)
                    {
                        //Initial configuration
                        Assert.IsTrue(mockNmsConnection.BrokerUri.StartsWith(BROKER_URI));
                    }
                    else
                    {
                        //first failover attempt
                        int brokerIndex = (i - 1)%client.FailoverSettings.BrokerUris.Count;
                        string brokerUri = client.FailoverSettings.BrokerUris[brokerIndex];
                        Assert.AreEqual(brokerUri, mockNmsConnection.BrokerUri);
                    }
                    i++;
                }
                return;
            }
            finally
            {
                if (client != null)
                {
                    client.Dispose();
                }
                AssertAllResourcesDisposed(MockFactory);
            }
            Assert.Fail("Exception should've been thrown");
        }

    }
}
