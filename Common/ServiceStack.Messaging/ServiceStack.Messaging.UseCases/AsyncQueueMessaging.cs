using System.Collections.Generic;
using System.IO;
using ServiceStack.Common.Services.Serialization;
using ServiceStack.Messaging.UseCases.Support;
using NUnit.Framework;

namespace ServiceStack.Messaging.UseCases
{
    [TestFixture]
    public class AsyncQueueMessaging : TestCaseBase
    {
        public override string DestinationUri
        {
            get { return string.Format("{0}/{1}", BROKER_URI, GetType().Name); } 
        }

        [Test]
        public void AsyncQueue_SendTextMessage()
        {
            //Create a new connection to the server
            using (IConnection connection = CreateNewConnection())
            {
                //Create a new instance of the client
                using (IOneWayClient client = connection.CreateClient(DestinationQueue))
                {
                    //Set failover brokers to send to if the primary broker is down
                    client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                    //Send an Asynchronus text message
                    client.SendOneWay(connection.CreateTextMessage(TEXT_MESSAGE));
                }
            }

            List<ITextMessage> messagesReceived = GetTextMessagesInQueue(DestinationQueue);
            Assert.AreEqual(1, messagesReceived.Count);
        }

        [Test]
        public void AsyncQueue_SendLargeTextMessage()
        {
            //Create a new connection to the server
            using (IConnection connection = CreateNewConnection())
            {
                //Create a new instance of the client
                using (IOneWayClient client = connection.CreateClient(DestinationQueue))
                {
                    //Set failover brokers to send to if the primary broker is down
                    client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                    //Send an Asynchronus text message
                    client.SendOneWay(connection.CreateTextMessage(File.ReadAllText(LargeXmlPath)));
                }
            }

            List<ITextMessage> messagesReceived = GetTextMessagesInQueue(DestinationQueue, WaitForListenerToReceiveLargeMessages);
            Assert.AreEqual(1, messagesReceived.Count);
        }

        [Test]
        public void AsyncQueue_SendXmlSerializable()
        {
            //Create a new connection to the server
            using (IConnection connection = CreateNewConnection())
            {
                //Create a new instance of the client
                using (IOneWayClient client = connection.CreateClient(DestinationQueue))
                {
                    //Set failover brokers to send to if the primary broker is down
                    client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                    //Send an Asynchronus serializable object
                    string xml = new XmlSerializableSerializer().Parse(XmlSerializableObject);
                    client.SendOneWay(connection.CreateTextMessage(xml));
                }
            }

            var messagesReceived = GetTextMessagesInQueue(DestinationQueue);
            Assert.AreEqual(1, messagesReceived.Count);
        }

        [Test]
        public void AsyncQueue_SendDataContract()
        {
            //Create a new connection to the server
            using (var connection = CreateNewConnection())
            {
                //Create a new instance of the client
                using (var client = connection.CreateClient(DestinationQueue))
                {
                    //Set failover brokers to send to if the primary broker is down
                    client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                    //Send an Asynchronus serializable object
                    var xml = new DataContractSerializer().Parse(DataContractObject);
                    client.SendOneWay(connection.CreateTextMessage(xml));
                }
            }

            var messagesReceived = GetTextMessagesInQueue(DestinationQueue);
            Assert.AreEqual(1, messagesReceived.Count);
        }
    }
}