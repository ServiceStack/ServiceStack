using ServiceStack.Common.Services.Serialization;
using ServiceStack.Messaging.Tests.Objects.Serializable;
using NUnit.Framework;

namespace ServiceStack.Messaging.Tests.UnitTests
{
    [TestFixture]
    public class ActiveMqSerializableTopicClientTests : UnitTestCaseBase
    {

        #region IOneWayClient Members

        [Test]
        public void SendOneWayTest()
        {
            using (IConnection connection = CreateNewConnection())
            {
                using (IOneWayClient client = connection.CreateClient(DestinationTopic))
                {
                    var xml = new XmlSerializableSerializer().Parse(XmlSerializableObject);
                    client.SendOneWay(CreateTextMessage(xml));
                    AssertNmsState(1, 1, 1, 0);
                    Assert.AreEqual(1, MockNmsSession.TextMessages.Count);
                    Assert.AreEqual(1, MockNmsProducer.SentMessages.Count);
                    AssertCustomMessageProperties(MockNmsProducer.SentMessages[0].TextMessage);
                    XmlSerializableObject objMessage =
                        GetXmlSerializableObject(MockNmsProducer.SentMessages[0].TextMessage.Text);
                    Assert.AreEqual(XmlSerializableObject.Value, objMessage.Value);
                }
            }
            AssertAllResourcesDisposed(MockFactory);
        }

        [Test]
        public void SendOneWay_WithPropertiesTest()
        {
            using (IConnection connection = CreateNewConnection())
            {
                using (IOneWayClient client = connection.CreateClient(DestinationTopic))
                {
                    string xml = new XmlSerializableSerializer().Parse(XmlSerializableObject);
                    client.SendOneWay(CreateTextMessage(xml));
                    AssertNmsState(1, 1, 1, 0);
                    Assert.AreEqual(1, MockNmsSession.TextMessages.Count);
                    Assert.AreEqual(1, MockNmsProducer.SentMessages.Count);
                    AssertCustomMessageProperties(MockNmsProducer.SentMessages[0].TextMessage);
                    XmlSerializableObject objMessage =
                        GetXmlSerializableObject(MockNmsProducer.SentMessages[0].TextMessage.Text);
                    Assert.AreEqual(XmlSerializableObject.Value, objMessage.Value);
                }
            }
            AssertAllResourcesDisposed(MockFactory);
        }

        #endregion
    }
}
