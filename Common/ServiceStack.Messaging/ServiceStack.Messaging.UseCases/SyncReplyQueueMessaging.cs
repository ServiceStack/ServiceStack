using ServiceStack.Common.Services.Serialization;
using ServiceStack.Messaging.UseCases.Objects.Serializable;
using ServiceStack.Messaging.UseCases.Services.Basic;
using ServiceStack.Messaging.UseCases.Support;
using NUnit.Framework;

namespace ServiceStack.Messaging.UseCases
{
    [TestFixture]
    public class SyncReplyQueueMessaging : TestCaseBase
    {
        const string SERVICE_HOSTS_SECTION_NAME = "SyncReplyQueueServiceHosts";

        public override string DestinationUri
        {
            get { return string.Format("{0}/{1}", BROKER_URI, GetType().Name); } 
        }

        [Test]
        public void SyncReplyQueue_SendTextMessage()
        {
            var queueUri = DestinationUri + ".Text";
            using (var service = new ReplyQueueTextService(Factory, queueUri))
            {
                service.Start();
                using (var connection = CreateNewConnection())
                {
                    var destination = new Destination(DestinationType.Queue, queueUri);
                    using (var client = connection.CreateReplyClient(destination))
                    {
                        client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                        var response = client.Send(connection.CreateTextMessage(TEXT_MESSAGE), DefaultReplyTimeout);
                        Assert.AreEqual(SimpleService.Reverse(TEXT_MESSAGE), response.Text);
                    }
                }
            }
        }

        [Test]
        public void SyncReplyQueue_SendXmlSerializable()
        {
            string queueUri = DestinationUri + ".XmlSerializable";
            using (ReplyQueueXmlSerializableService service = new ReplyQueueXmlSerializableService(Factory, queueUri))
            {
                service.Start();
                using (var connection = CreateNewConnection())
                {
                    IDestination destination = new Destination(DestinationType.Queue, queueUri);
                    using (var client = connection.CreateReplyClient(destination))
                    {
                        client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                        var xml = new XmlSerializableSerializer().Parse(XmlSerializableObject);
                        var responseXml = client.Send(connection.CreateTextMessage(xml), DefaultReplyTimeout);
                        var response = new XmlSerializableDeserializer().Parse<XmlSerializableObject>(responseXml.Text);
                        Assert.AreEqual(SimpleService.Reverse(XmlSerializableObject.Value), response.Value);
                    }
                }
            }
        }

        [Test]
        public void SyncReplyQueue_SendDataContract()
        {
            string queueUri = DestinationUri + ".DataContract";
            using (ReplyQueueDataContractService service = new ReplyQueueDataContractService(Factory, queueUri))
            {
                service.Start();
                using (IConnection connection = CreateNewConnection())
                {
                    IDestination destination = new Destination(DestinationType.Queue, queueUri);
                    using (IReplyClient client = connection.CreateReplyClient(destination))
                    {
                        client.FailoverSettings.BrokerUris.AddRange(FailoverUris);
                        var xml = new DataContractSerializer().Parse(DataContractObject);
                        var responseXml = client.Send(connection.CreateTextMessage(xml), DefaultReplyTimeout);
                        var response = new DataContractDeserializer().Parse<DataContractObject>(responseXml.Text);
                        Assert.AreEqual(SimpleService.Reverse(DataContractObject.Value), response.Value);
                    }
                }
            }
        }
    }
}