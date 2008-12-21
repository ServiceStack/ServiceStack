using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using ServiceStack.Messaging.ActiveMq;
using ServiceStack.Messaging.Tests.Objects.Mock;
using NUnit.Framework;

namespace ServiceStack.Messaging.Tests.UnitTests
{
    [TestFixture]
    public class ActiveMqQueueListenerTests : UnitTestCaseBase
    {
        private IMessagingFactory factory;

        public IMessagingFactory Factory
        {
            get
            {
                if (factory == null)
                {
                    factory = new ActiveMqMessagingFactory();
                }
                return factory;
            }
        }

        private IDestination DlqDestination
        {
            get
            {
                return new Destination(DestinationType.Queue, DlqDestinationUri);
            }
        }

        [Test]
        public void ReceiveMessageTest()
        {
            List<ITextMessage> messagesReceived = new List<ITextMessage>();
            using (IConnection connection = CreateNewConnection())
            {
                using (IGatewayListener listener = connection.CreateListener(DestinationQueue))
                {
                    listener.MessageReceived += delegate(object source, MessageEventArgs e)
                        {
                            messagesReceived.Add(e.Message);
                        };
                    listener.Start();
                    NMS.ITextMessage message = MockNmsSession.CreateTextMessage(TEXT_MESSAGE);
                    MockNmsSession.SendMessage(new ActiveMQ.Commands.ActiveMQQueue(DestinationName), message);
                    Thread.Sleep(MockWaitToReceiveMessage);
                }
            }
            Assert.AreEqual(1, messagesReceived.Count);
            Assert.AreEqual(1, MockNmsSession.CommitNo);
            AssertAllResourcesDisposed(base.MockFactory);
        }
        
        [Test]
        public void DeadLetterQueueTest()
        {
            
            List<ITextMessage> messagesReceived = new List<ITextMessage>();
            List<ITextMessage> dlqMessagesReceived = new List<ITextMessage>();
            using (IConnection connection = CreateNewConnection())
            {
                using (IGatewayListener dlqListener = connection.CreateListener(DlqDestination))
                {
                    dlqListener.MessageReceived += delegate(object source, MessageEventArgs e)
                       {
                           dlqMessagesReceived.Add(e.Message);
                       };
                    dlqListener.Start();

                    using (IGatewayListener listener = connection.CreateListener(DestinationQueue))
                    {
                        listener.DeadLetterQueue = new DestinationUri(DlqDestinationUri).Name;
                        listener.MessageReceived += delegate(object source, MessageEventArgs e)
                            {
                                ActiveMqQueueListener srcListener = (ActiveMqQueueListener) source;
                                messagesReceived.Add(e.Message);
                                srcListener.Rollback();
                            };
                        listener.Start();

                        using (IOneWayClient client = connection.CreateClient(DestinationQueue))
                        {
                            client.SendOneWay(new TextMessage(TEXT_MESSAGE));
                        }
                        Thread.Sleep(MockWaitToReceiveMessage);
                    }
                }
            }
            Assert.AreEqual(1, messagesReceived.Count);
            Assert.AreEqual(1, dlqMessagesReceived.Count);
            AssertAllResourcesDisposed(base.MockFactory);
        }

        [Test]
        public void MessageRedelivery_FailureTest()
        {
            
            int maximumRedeliveryCount = new Random().Next(2, 10);
            List<ITextMessage> messagesReceived = new List<ITextMessage>();
            List<ITextMessage> dlqMessagesReceived = new List<ITextMessage>();
            using (IConnection connection = CreateNewConnection())
            {
                using (IGatewayListener dlqListener = connection.CreateListener(DlqDestination))
                {
                    dlqListener.MessageReceived += delegate(object source, MessageEventArgs e)
                       {
                           dlqMessagesReceived.Add(e.Message);
                       };
                    dlqListener.Start();

                    using (IGatewayListener listener = connection.CreateListener(DestinationQueue))
                    {
                        listener.DeadLetterQueue = new DestinationUri(DlqDestinationUri).Name;
                        listener.MaximumRedeliveryCount = maximumRedeliveryCount;
                        listener.MessageReceived += delegate(object source, MessageEventArgs e)
                            {
                                ActiveMqQueueListener srcListener = (ActiveMqQueueListener) source;
                                messagesReceived.Add(e.Message);
                                srcListener.Rollback();
                            };
                        listener.Start();

                        using (IOneWayClient client = connection.CreateClient(DestinationQueue))
                        {
                            client.SendOneWay(new TextMessage(TEXT_MESSAGE));
                        }
                        Thread.Sleep(MockWaitToReceiveMessage);
                    }
                }
            }
            Assert.AreEqual(maximumRedeliveryCount, messagesReceived.Count - 1);
            Assert.AreEqual(1, dlqMessagesReceived.Count);
            AssertAllResourcesDisposed(base.MockFactory);
        }

        [Test]
        public void MessageRedelivery_SuccessTest()
        {
            int maximumRedeliveryCount = new Random().Next(2, 10);
            List<ITextMessage> messagesReceived = new List<ITextMessage>();
            List<ITextMessage> dlqMessagesReceived = new List<ITextMessage>();
            using (IConnection connection = CreateNewConnection())
            {
                using (IGatewayListener dlqListener = connection.CreateListener(DlqDestination))
                {
                    dlqListener.MessageReceived += delegate(object source, MessageEventArgs e)
                       {
                           dlqMessagesReceived.Add(e.Message);
                       };
                    dlqListener.Start();

                    using (IGatewayListener listener = connection.CreateListener(DestinationQueue))
                    {
                        listener.DeadLetterQueue = new DestinationUri(DlqDestinationUri).Name;
                        listener.MaximumRedeliveryCount = maximumRedeliveryCount;
                        listener.MessageReceived += delegate(object source, MessageEventArgs e)
                            {
                                IGatewayListener srcListener = (IGatewayListener) source;
                                messagesReceived.Add(e.Message);
                                if (messagesReceived.Count < maximumRedeliveryCount)
                                {
                                    srcListener.Rollback();
                                }
                            };
                        listener.Start();

                        using (IOneWayClient client = connection.CreateClient(DestinationQueue))
                        {
                            client.SendOneWay(new TextMessage(TEXT_MESSAGE));
                        }
                        Thread.Sleep(MockWaitToReceiveMessage);
                    }
                }
            }
            Assert.AreEqual(0, dlqMessagesReceived.Count);
            AssertAllResourcesDisposed(base.MockFactory);
        }

        [Test]
        public void Failover_ConnectionAlwaysFailsTest()
        {
            int maximumReconnectAttempts = new Random().Next(2, 10);
            List<ITextMessage> messagesReceived = new List<ITextMessage>();
            base.MockFactory.FactoryConnectionType = typeof (MockNmsConnectionWithSessionFailure);
            try
            {
                using (IConnection connection = CreateNewConnection())
                {
                    using (IGatewayListener listener = connection.CreateListener(DestinationQueue))
                    {
                        listener.DeadLetterQueue = new DestinationUri(DlqDestinationUri).Name;
                        listener.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                        listener.FailoverSettings.MaxReconnectAttempts = maximumReconnectAttempts;
                        listener.MessageReceived += delegate(object source, MessageEventArgs e)
                            {
                                messagesReceived.Add(e.Message);
                            };
                        listener.Start();

                        using (IOneWayClient client = connection.CreateClient(DestinationQueue))
                        {
                            client.SendOneWay(new TextMessage(TEXT_MESSAGE));
                        }
                        Thread.Sleep(MockWaitToReceiveMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(NMS.NMSConnectionException), ex.InnerException.GetType());
                Assert.AreEqual(maximumReconnectAttempts, base.MockFactory.Connections.Count - 1);
                Assert.AreEqual(0, messagesReceived.Count);
                AssertAllResourcesDisposed(base.MockFactory);
                return;
            }
            Assert.Fail("Exception should've been thrown");
        }
    }
}
