    using System;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Messaging.ActiveMq;
using ServiceStack.Messaging.Tests.Objects.Serializable;
using NUnit.Framework;

namespace ServiceStack.Messaging.Tests.Support
{
    public class TestCaseBase
    {
        public const string TEXT_MESSAGE = "Hello, World!";
        public const string BROKER_URI = "tcp://wwvis7020:61616";
        private IMessagingFactory factory;
        private ITextMessage textMessage;

        [SetUp]
        public virtual void SetUp()
        {
            factory = new ActiveMqMessagingFactory();
        }

        [TearDown]
        public virtual void TearDown()
        {
            factory = null;
        }

        public virtual string[] FailoverUris
        {
            get
            {
                return new string[] { "tcp://wwvis7020:61616" };
            }
        }

        public string LargeXmlPath
        {
            get { return @"C:\Projects\PoToPe\Utopia\trunk\src\Common\ServiceStack.Messaging\Lib\eastenders.xml"; }
        }

        public IMessagingFactory Factory
        {
            get
            {
                return factory;
            }
        }

        protected virtual IConnection CreateNewConnection()
        {
            return Factory.CreateConnection(DestinationUri);
        }

        protected ITextMessage TextMessage
        {
            get
            {
                if (textMessage == null)
                {
                    textMessage = new TextMessage(TEXT_MESSAGE);
                    textMessage.CorrelationId = "Custom_CorrelationId";
                    textMessage.Persist = false;
                    textMessage.SessionId = "Custom_SessionId";
                    textMessage.Expiration = TimeSpan.FromSeconds(11);
                }
                return textMessage;
            }
        }

        public virtual string DestinationUri
        {
            get { return string.Format("{0}/{1}", BROKER_URI, GetType().Name); }
        }

        protected IDestination DestinationQueue
        {
            get
            {
                return new Destination(DestinationType.Queue, DestinationUri);
            }
        }

        protected IDestination DestinationTopic
        {
            get
            {
                return new Destination(DestinationType.Topic, DestinationUri);
            }
        }

        public virtual TimeSpan DefaultReplyTimeout
        {
            get { return TimeSpan.FromMilliseconds(3000); }
        }

        public virtual TimeSpan WaitForListenerToReceiveMessages
        {
            get { return TimeSpan.FromMilliseconds(3000); }
        }

        public DataContractObject DataContractObject
        {
            get
            {
                DataContractObject obj = new DataContractObject();
                obj.Value = TEXT_MESSAGE;
                return obj;
            }
        }

        public XmlSerializableObject XmlSerializableObject
        {
            get
            {
                XmlSerializableObject obj = new XmlSerializableObject();
                obj.Value = TEXT_MESSAGE;
                return obj;
            }
        }

        public List<ITextMessage> GetTextMessagesInQueue(IDestination destination)
        {
            List<ITextMessage> messages = new List<ITextMessage>();
            using (IConnection connection = Factory.CreateConnection(destination.Uri))
            {
                using (IGatewayListener listener = connection.CreateListener(destination))
                {
                    listener.MessageReceived += delegate(object source, MessageEventArgs e)
                                                    {
                                                        messages.Add(e.Message);
                                                    };
                    listener.Start();
                    Thread.Sleep(WaitForListenerToReceiveMessages);
                    return messages;
                }
            }
        }
    }
}