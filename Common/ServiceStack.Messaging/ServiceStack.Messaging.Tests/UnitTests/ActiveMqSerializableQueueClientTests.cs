using System;
using ServiceStack.Common.Services.Serialization;
using ServiceStack.Messaging.Tests.Objects.Serializable;
using ServiceStack.Messaging.Tests.Services.Basic;
using NUnit.Framework;

namespace ServiceStack.Messaging.Tests.UnitTests
{
    [TestFixture]
    public class ActiveMqSerializableQueueClientTests : UnitTestCaseBase
    {
        #region IReplyClient Members

        [Test]
        public void BeginSendTest()
        {
            using (IConnection connection = CreateNewConnection())
            {
                using (IReplyClient client = connection.CreateReplyClient(DestinationQueue))
                {
                    string xml = new XmlSerializableSerializer().Parse(XmlSerializableObject);
                    ITextMessage message = new TextMessage(xml);
                    IAsyncResult asyncResult = client.BeginSend(message);
                    AssertNmsState(1, 1, 1, 1);
                    Assert.AreEqual(1, MockNmsSession.TextMessages.Count);
                    Assert.AreEqual(1, MockNmsProducer.SentMessages.Count);
                    AssertDefaultMessageProperties(MockNmsProducer.SentMessages[0].TextMessage);
                    XmlSerializableObject objMessage =
                        GetXmlSerializableObject(MockNmsProducer.SentMessages[0].TextMessage.Text);
                    Assert.AreEqual(XmlSerializableObject.Value, objMessage.Value);
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
                            AssertCustomMessageProperties(e.Message);
                            XmlSerializableObject objMessage = GetXmlSerializableObject(e.Message.Text);
                            objMessage.Value = SimpleService.Reverse(objMessage.Value);
                            string responseXml = new XmlSerializableSerializer().Parse(objMessage);

                            using (IOneWayClient client = listener.Connection.CreateClient(e.Message.ReplyTo))
                            {
                                client.SendOneWay(CreateTextMessage(responseXml, e.Message.CorrelationId));
                            }
                        };
                    listener.Start();

                    using (IReplyClient client = connection.CreateReplyClient(DestinationQueue))
                    {
                        string xml = new XmlSerializableSerializer().Parse(XmlSerializableObject);
                        IAsyncResult asyncResult = client.BeginSend(CreateTextMessage(xml));
                        ITextMessage messageReceived = client.EndSend(asyncResult, MockWaitTimeOut);
                        XmlSerializableObject objMessage = GetXmlSerializableObject(messageReceived.Text);
                        Assert.AreEqual(SimpleService.Reverse(XmlSerializableObject.Value), objMessage.Value);
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
                    listener.MessageReceived += delegate(object source, MessageEventArgs e)
                        {
                            AssertCustomMessageProperties(e.Message);
                            XmlSerializableObject objMessage = GetXmlSerializableObject(e.Message.Text);
                            objMessage.Value = SimpleService.Reverse(objMessage.Value);
                            string responseXml = new XmlSerializableSerializer().Parse(objMessage);

                            using (IOneWayClient client = listener.Connection.CreateClient(e.Message.ReplyTo))
                            {
                                client.SendOneWay(CreateTextMessage(responseXml, e.Message.CorrelationId));
                            }
                        };
                    listener.Start();

                    using (IReplyClient client = connection.CreateReplyClient(DestinationQueue))
                    {
                        string xml = new XmlSerializableSerializer().Parse(XmlSerializableObject);
                        ITextMessage messageReceived = client.Send(CreateTextMessage(xml), MockWaitTimeOut);
                        XmlSerializableObject objMessage = GetXmlSerializableObject(messageReceived.Text);
                        Assert.AreEqual(SimpleService.Reverse(XmlSerializableObject.Value), objMessage.Value);
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
                    string xml = new XmlSerializableSerializer().Parse(XmlSerializableObject);
                    client.SendOneWay(new TextMessage(xml));
                    AssertNmsState(1, 1, 1, 0);
                    Assert.AreEqual(1, MockNmsSession.TextMessages.Count);
                    Assert.AreEqual(1, MockNmsProducer.SentMessages.Count);
                    AssertDefaultMessageProperties(MockNmsProducer.SentMessages[0].TextMessage);
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
