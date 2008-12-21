using System;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Common.Services.Serialization;
using ServiceStack.Messaging.UseCases.Objects.Serializable;
using ServiceStack.Messaging.UseCases.Support;
using NUnit.Framework;

namespace ServiceStack.Messaging.UseCases
{
    [TestFixture]
    public class PubSubMessaging : TestCaseBase
    {
        public override string DestinationUri
        {
            get { return string.Format("{0}/{1}", BROKER_URI, GetType().Name); }
        }

        [Test]
        public void PubSub_SendMessageToTopicWithOneSubscriber()
        {
            var messagesReceived = new List<ITextMessage>();

            //Create a new connection to the server
            using (var connection = CreateNewConnection())
            {
                //Create a new instance of the client
                using (var listener = connection.CreateListener(DestinationTopic))
                {
                    listener.MessageReceived += delegate(object source, MessageEventArgs e)
                                                    {
                                                        messagesReceived.Add(e.Message);
                                                    };
                    listener.Start();

                    using (var client = connection.CreateClient(DestinationTopic))
                    {
                        client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                        var xml = new XmlSerializableSerializer().Parse(XmlSerializableObject);
                        client.SendOneWay(connection.CreateTextMessage(xml));

                        Thread.Sleep(WaitForListenerToReceiveMessages);
                        Assert.AreEqual(1, messagesReceived.Count);

                        foreach (var message in messagesReceived)
                        {
                            var response = new XmlSerializableDeserializer().Parse<XmlSerializableObject>(message.Text);
                            Assert.AreEqual(XmlSerializableObject.Value, response.Value);
                        }
                    }
                }
            }
        }

        [Test]
        public void PubSub_SendMessageToTopicWithMultipleSubscribers()
        {
            var messagesReceived = new List<ITextMessage>();

            using (var connection = CreateNewConnection())
            {
                var listeners = new List<IGatewayListener>();

                var rnd = new Random();
                var noOfSubscribers = rnd.Next(2, 5);
                for (var i = 0; i < noOfSubscribers; i++)
                {
                    var listener = connection.CreateListener(DestinationTopic);
                    listener.MessageReceived += ((source, e) => messagesReceived.Add(e.Message));
                    listener.Start();
                    listeners.Add(listener);
                }

                using (var client = connection.CreateClient(DestinationTopic))
                {
                    client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                    var xml = new XmlSerializableSerializer().Parse(XmlSerializableObject);
                    client.SendOneWay(connection.CreateTextMessage(xml));

                    Thread.Sleep(WaitForListenerToReceiveMessages);
                    Assert.AreEqual(noOfSubscribers, messagesReceived.Count);

                    foreach (var message in messagesReceived)
                    {
                        var response = new XmlSerializableDeserializer().Parse<XmlSerializableObject>(message.Text);
                        Assert.AreEqual(XmlSerializableObject.Value, response.Value);
                    }
                }

                foreach (var listener in listeners)
                {
                    listener.Dispose();
                }
            }
        }

        [Test]
        public void PubSub_SendMessageToTopicWithOneDurableSubscriber()
        {
            string durableSubscriberId = GetType().Name + "DurableSubscriberId";
            using (var connection = CreateNewConnection())
            {
                IGatewayListener listener = null;
                try
                {
                    listener = connection.CreateRegisteredListener(DestinationTopic, durableSubscriberId);
                    var messagesReceived = new List<ITextMessage>();
                    listener.MessageReceived += ((source, e) => messagesReceived.Add(e.Message));
                    listener.Start();

                    Thread.Sleep(WaitForListenerToReceiveMessages);
                    messagesReceived.Clear(); //drain the previous messages

                    var client = connection.CreateClient(DestinationTopic);
                    client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                    var xml = new XmlSerializableSerializer().Parse(XmlSerializableObject);
                    client.SendOneWay(connection.CreateTextMessage(xml));

                    Thread.Sleep(WaitForListenerToReceiveMessages);
                    Assert.AreEqual(1, messagesReceived.Count);

                    //Take the subscriber offline
                    listener.Dispose();

                    //Send more messages to the topic while the subscriber is offline.
                    client.SendOneWay(connection.CreateTextMessage(xml));
                    client.SendOneWay(connection.CreateTextMessage(xml));
                    client.SendOneWay(connection.CreateTextMessage(xml));
                    client.Dispose();

                    //Bring the subscriber back online
                    listener = connection.CreateRegisteredListener(DestinationTopic, durableSubscriberId);
                    listener.MessageReceived += ((source, e) => messagesReceived.Add(e.Message));
                    listener.Start();

                    Thread.Sleep(WaitForListenerToReceiveMessages);
                    Assert.AreEqual(1 + 3, messagesReceived.Count);

                    foreach (var message in messagesReceived)
                    {
                        var response = new XmlSerializableDeserializer().Parse<XmlSerializableObject>(message.Text);
                        Assert.AreEqual(XmlSerializableObject.Value, response.Value);
                    }
                }
                catch(Exception ex)
                {
                    if (listener != null)
                    {
                        listener.Dispose();
                    }
                }
            }
        }

    }
}