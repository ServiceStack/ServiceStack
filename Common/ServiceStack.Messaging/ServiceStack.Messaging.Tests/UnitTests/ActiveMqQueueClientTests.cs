using System;
using System.Collections.Generic;
using System.Text;
using ActiveMQ;
using ServiceStack.Messaging.ActiveMq;
using ServiceStack.Messaging.Tests.Objects.Mock;
using ServiceStack.Messaging.Tests.Services.Basic;
using NMS;
using NUnit.Framework;

namespace ServiceStack.Messaging.Tests.UnitTests
{
    [TestFixture]
    public class ActiveMqQueueClientTests : UnitTestCaseBase
    {
        #region IReplyClient Members

        [Test]
        public void BeginSendTest()
        {
            using (IConnection connection = CreateNewConnection())
            {
                using (IReplyClient client = connection.CreateReplyClient(DestinationQueue))
                {
                    IAsyncResult asyncResult = client.BeginSend(TextMessage);
                    AssertNmsState(1, 1, 1, 1);
                    Assert.AreEqual(1, MockNmsSession.TextMessages.Count);
                    Assert.AreEqual(1, MockNmsProducer.SentMessages.Count);
                    AssertCustomMessageProperties(MockNmsProducer.SentMessages[0].TextMessage);
                    Assert.AreEqual(TEXT_MESSAGE, MockNmsProducer.SentMessages[0].TextMessage.Text);
                }
            }
            AssertAllResourcesDisposed(MockFactory);
        }

        [Test]
        public void EndSendTest()
        {
            using (IConnection connection = CreateNewConnection())
            {
                using (IGatewayListener listener = connection.CreateListener(DestinationQueue))
                {
                    listener.MessageReceived += delegate(object source, MessageEventArgs e)
                    {
                        TextMessage message = new TextMessage(SimpleService.Reverse(e.Message.Text));
                        message.CorrelationId = e.Message.CorrelationId;
                        using (IOneWayClient client = listener.Connection.CreateClient(e.Message.ReplyTo))
                        {
                            client.SendOneWay(message);
                        }
                    };
                    listener.Start();
                    using (IReplyClient client = connection.CreateReplyClient(DestinationQueue))
                    {
                        IAsyncResult asyncResult = client.BeginSend(TextMessage);
                        ITextMessage messageReceived = client.EndSend(asyncResult, MockWaitTimeOut);
                        Assert.AreEqual(SimpleService.Reverse(TEXT_MESSAGE), messageReceived.Text);
                    }
                }
            }
            AssertAllResourcesDisposed(MockFactory);
        }

        [Test]
        public void SendTest()
        {
            using (IConnection connection = CreateNewConnection())
            {
                using (IGatewayListener listener = connection.CreateListener(DestinationQueue))
                {
                    listener.MessageReceived += 
                        delegate(object source, MessageEventArgs e) 
                        {
                            TextMessage message = new TextMessage(SimpleService.Reverse(e.Message.Text));
                            message.CorrelationId = e.Message.CorrelationId;
                            using (IOneWayClient client = listener.Connection.CreateClient(e.Message.ReplyTo))
                            {
                                client.SendOneWay(message);
                            }
                        };
                    listener.Start();
                    using (IReplyClient client = connection.CreateReplyClient(DestinationQueue))
                    {
                        ITextMessage messageReceived = client.Send(TextMessage, MockWaitTimeOut);
                        Assert.AreEqual(SimpleService.Reverse(TEXT_MESSAGE), messageReceived.Text);
                    }
                }
            }
            AssertAllResourcesDisposed(MockFactory);
        }

        #endregion

        #region IOneWayClient Members

        [Test]
        public void SendOneWayTest()
        {
            using (IConnection connection = CreateNewConnection())
            {
                using (IReplyClient client = connection.CreateReplyClient(DestinationQueue))
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
            int maximumReconnectAttempts = new Random().Next(2, 10);
            MockFactory.FactoryConnectionType = typeof(MockNmsConnectionWithSessionFailure);
            IConnection connection = CreateNewConnection();
            IOneWayClient client = null;
            try
            {
                client = connection.CreateReplyClient(DestinationQueue);
                client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                client.FailoverSettings.MaxReconnectAttempts = maximumReconnectAttempts;
                client.SendOneWay(TextMessage);
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
                        int brokerIndex = (i - 1) % client.FailoverSettings.BrokerUris.Count;
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
                connection.Dispose();
                AssertAllResourcesDisposed(MockFactory);
            }
            Assert.Fail("Exception should've been thrown");
        }

        [Test]
        public void Failover_ProducerSendAlwaysFailsTest()
        {

            int maximumReconnectAttempts = new Random().Next(2, 10);
            MockFactory.FactoryProducerType = typeof(MockNmsProducerWithSendMessageFailure);
            IConnection connection = CreateNewConnection();
            IOneWayClient client = null;
            try
            {
                client = connection.CreateReplyClient(DestinationQueue);
                client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                client.FailoverSettings.MaxReconnectAttempts = maximumReconnectAttempts;
                client.SendOneWay(TextMessage);
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
                        int brokerIndex = (i - 1) % client.FailoverSettings.BrokerUris.Count;
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
                connection.Dispose();
                AssertAllResourcesDisposed(MockFactory);
            }
            Assert.Fail("Exception should've been thrown");
        }

    }
}
