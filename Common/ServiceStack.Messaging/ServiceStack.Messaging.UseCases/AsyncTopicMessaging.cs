using System.Collections.Generic;
using System.IO;
using System.Threading;
using ServiceStack.Common.Services.Serialization;
using ServiceStack.Messaging.UseCases.Support;
using NUnit.Framework;

namespace ServiceStack.Messaging.UseCases
{
    [TestFixture]
    public class AsyncTopicMessaging : TestCaseBase
    {

        [Test]
        public void AsyncTopic_SendTextMessage()
        {
            var messagesReceived = new List<ITextMessage>();
            using (var connection = CreateNewConnection())
            {
                using (var listener = connection.CreateListener(DestinationTopic))
                {
                    listener.MessageReceived += ((source, e) => messagesReceived.Add(e.Message));
                    listener.Start();

                    using (var client = connection.CreateClient(DestinationTopic))
                    {
                        client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                        client.SendOneWay(connection.CreateTextMessage(TEXT_MESSAGE));
                    }

                    Thread.Sleep(WaitForListenerToReceiveMessages);
                }
            }
            Assert.AreEqual(1, messagesReceived.Count);
        }

        [Test]
        public void AsyncQueue_SendLargeTextMessage()
        {
            var messagesReceived = new List<ITextMessage>();
            using (var connection = CreateNewConnection())
            {
                using (var listener = connection.CreateListener(DestinationTopic))
                {
                    listener.MessageReceived += ((source, e) => messagesReceived.Add(e.Message));
                    listener.Start();

                    using (var client = connection.CreateClient(DestinationTopic))
                    {
                        client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                        client.SendOneWay(connection.CreateTextMessage(File.ReadAllText(LargeXmlPath)));
                    }

                    Thread.Sleep(WaitForListenerToReceiveLargeMessages);
                }
            }
            Assert.AreEqual(1, messagesReceived.Count);
        }

        [Test]
        public void AsyncTopic_SendXmlSerializable()
        {
            var messagesReceived = new List<ITextMessage>();
            using (var connection = CreateNewConnection())
            {
                using (var listener = connection.CreateListener(DestinationTopic))
                {
                    listener.MessageReceived += ((source, e) => messagesReceived.Add(e.Message));
                    listener.Start();

                    using (var client = connection.CreateClient(DestinationTopic))
                    {
                        client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                        var xml = new XmlSerializableSerializer().Parse(XmlSerializableObject);
                        client.SendOneWay(connection.CreateTextMessage(xml));
                    }
                    Thread.Sleep(WaitForListenerToReceiveMessages);
                }
            }
            Assert.AreEqual(1, messagesReceived.Count);
        }

        [Test]
        public void AsyncTopic_SendDataContract()
        {
            var messagesReceived = new List<ITextMessage>();
            using (var connection = CreateNewConnection())
            {
                using (var listener = connection.CreateListener(DestinationTopic))
                {
                    listener.MessageReceived += ((source, e) => messagesReceived.Add(e.Message));
                    listener.Start();

                    using (var client = connection.CreateClient(DestinationTopic))
                    {
                        client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                        var xml = new DataContractSerializer().Parse(DataContractObject);
                        client.SendOneWay(connection.CreateTextMessage(xml));
                    }

                    Thread.Sleep(WaitForListenerToReceiveMessages);
                }
            }
            Assert.AreEqual(1, messagesReceived.Count);
        }
    }
}