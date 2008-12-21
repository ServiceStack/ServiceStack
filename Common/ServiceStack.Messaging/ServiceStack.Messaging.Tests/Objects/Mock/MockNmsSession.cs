using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ActiveMQ.Commands;

namespace ServiceStack.Messaging.Tests.Objects.Mock
{
    public class MockNmsSession : NMS.ISession
    {
        private readonly List<MockNmsProducer> producers;
        private readonly List<MockNmsConsumer> consumers;
        private readonly List<MockNmsTextMessage> textMessages;
        private KeyValuePair<NMS.IDestination, NMS.ITextMessage>? lastMessageSent;
        private int commitNo;
        private int rollbackNo;
        private int disposedNo;
        private MockNmsConnection connection;
        private NMS.AcknowledgementMode acknowledgementMode;

        public MockNmsSession(MockNmsConnection connection)
        {
            producers = new List<MockNmsProducer>();
            consumers = new List<MockNmsConsumer>();
            textMessages = new List<MockNmsTextMessage>();
            lastMessageSent = null;
            this.connection = connection;
            commitNo = 0;
            rollbackNo = 0;
            disposedNo = 0;
        }

        public List<MockNmsProducer> Producers
        {
            get { return producers; }
        }

        public List<MockNmsConsumer> Consumers
        {
            get { return consumers; }
        }

        public List<MockNmsTextMessage> TextMessages
        {
            get { return textMessages; }
        }

        public KeyValuePair<NMS.IDestination, NMS.ITextMessage>? LastMessageSent
        {
            get { return lastMessageSent; }
            set { lastMessageSent = value; }
        }

        public Dictionary<NMS.IDestination, List<MockNmsConsumer>> ConsumerMap
        {
            get { return connection.Factory.ConsumerMap; }
        }

        public Dictionary<NMS.IDestination, List<NMS.ITextMessage>> UnsentMessages
        {
            get { return connection.Factory.UnsentMessages; }
        }

        public int CommitNo
        {
            get { return commitNo; }
        }

        public int RollbackNo
        {
            get { return rollbackNo; }
        }

        public int DisposedNo
        {
            get { return disposedNo; }
        }

        public NMS.IConnection Connection
        {
            get { return connection; }
        }

        public void SendMessage(NMS.IDestination destination, NMS.ITextMessage message)
        {
            List<MockNmsConsumer> consumers = GetRegisteredConsumer(destination);
            if (consumers.Count > 0)
            {
                foreach (MockNmsConsumer consumer in consumers)
                {
                    consumer.Deliver(message);
                }
            }
            else
            {
                StoreUnsentMessage(destination, message);
            }
        }

        private List<MockNmsConsumer> GetRegisteredConsumer(NMS.IDestination destination)
        {
            if (ConsumerMap.ContainsKey(destination))
            {
                return ConsumerMap[destination];
            }
            return new List<MockNmsConsumer>();
        }

        private void StoreUnsentMessage(NMS.IDestination destination, NMS.ITextMessage message)
        {
            if (!UnsentMessages.ContainsKey(destination))
            {
                UnsentMessages[destination] = new List<NMS.ITextMessage>();
            }
            UnsentMessages[destination].Add(message);
        }

        public void SendUnsentMessages()
        {
            foreach (MockNmsConsumer consumer in consumers)
            {
                consumer.Deliver(PopUnsentMessages(consumer.Destination));
            }
        }

        private List<NMS.ITextMessage> PopUnsentMessages(NMS.IDestination destination)
        {
            List<NMS.ITextMessage> messages = new List<NMS.ITextMessage>();
            if (UnsentMessages.ContainsKey(destination))
            {
                messages.AddRange(UnsentMessages[destination]);
                UnsentMessages[destination].Clear();
            }
            return messages;
        }

        private MockNmsProducer CreateProducerFromType()
        {
            ConstructorInfo ci = connection.Factory.FactoryProducerType.GetConstructor(new Type[] { GetType() });
            MockNmsProducer mockNmsProducer = (MockNmsProducer)ci.Invoke(new object[] { this });
            return mockNmsProducer;
        }

        #region ISession Members

        public NMS.AcknowledgementMode AcknowledgementMode
        {
            get { return acknowledgementMode; }
            set { acknowledgementMode = value; }
        }

        public void Close()
        {
            Dispose();
        }

        public void Commit()
        {
            lastMessageSent = null;
            commitNo++;
        }

        public NMS.IBytesMessage CreateBytesMessage(byte[] body)
        {
            MockNmsTextMessage mock = new MockNmsTextMessage();
            mock.Content = body;
            textMessages.Add(mock);
            return mock;
        }

        public NMS.IBytesMessage CreateBytesMessage()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public NMS.IMessageConsumer CreateConsumer(NMS.IDestination destination, string selector, bool noLocal)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public NMS.IMessageConsumer CreateConsumer(NMS.IDestination destination, string selector)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public NMS.IMessageConsumer CreateConsumer(NMS.IDestination destination)
        {
            MockNmsConsumer mock = new MockNmsConsumer(this, destination);
            if (!ConsumerMap.ContainsKey(destination))
            {
                ConsumerMap[destination] = new List<MockNmsConsumer>();
            }
            ConsumerMap[destination].Add(mock);
            consumers.Add(mock);
            return mock;
        }

        public NMS.IMessageConsumer CreateDurableConsumer(NMS.ITopic destination, string name, string selector, bool noLocal)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public NMS.IMapMessage CreateMapMessage()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public NMS.IMessage CreateMessage()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public NMS.IObjectMessage CreateObjectMessage(object body)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public NMS.IMessageProducer CreateProducer(NMS.IDestination destination)
        {
            MockNmsProducer mock = CreateProducerFromType();
            mock.Destination = destination;
            producers.Add(mock);
            return mock;
        }

        public NMS.IMessageProducer CreateProducer()
        {
            return CreateProducer(null);
        }

        public NMS.ITemporaryQueue CreateTemporaryQueue()
        {
            return new ActiveMQTempQueue(Guid.NewGuid().ToString());
        }

        public NMS.ITemporaryTopic CreateTemporaryTopic()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public NMS.ITextMessage CreateTextMessage(string text)
        {
            MockNmsTextMessage mock = new MockNmsTextMessage();
            mock.Text = text;
            textMessages.Add(mock);
            return mock;
        }

        public NMS.ITextMessage CreateTextMessage()
        {
            MockNmsTextMessage mock = new MockNmsTextMessage();
            textMessages.Add(mock);
            return mock;
        }

        public NMS.IQueue GetQueue(string name)
        {
            return new ActiveMQQueue(name);
        }

        public NMS.ITopic GetTopic(string name)
        {
            return new ActiveMQTopic(name);
        }

        public void Rollback()
        {
            if (lastMessageSent.HasValue)
            {
                SendMessage(lastMessageSent.Value.Key, lastMessageSent.Value.Value);
            }
            rollbackNo++;
        }

        public bool Transacted
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            disposedNo++;
        }

        #endregion
    }
}
