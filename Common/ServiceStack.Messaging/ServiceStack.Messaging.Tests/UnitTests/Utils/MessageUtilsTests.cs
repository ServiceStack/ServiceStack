using System.IO;
using ServiceStack.Common.Services.Serialization;
using ServiceStack.Messaging.ActiveMq;
using ServiceStack.Messaging.ActiveMq.Support.Utils;
using ServiceStack.Messaging.Tests.Objects.Serializable;
using NUnit.Framework;
using Rhino.Mocks;

namespace ServiceStack.Messaging.Tests.UnitTests.Utils
{
    [TestFixture]
    public class MessageUtilsTests : UnitTestCaseBase
    {
        private string testXml = null;

        public string TestXml
        {
            get
            {
                if (testXml == null)
                {
                    var xmlObj = new XmlSerializableObject();
                    testXml = new XmlSerializableSerializer().Parse(xmlObj);
                }
                return testXml;
            }
        }

        [Test]
        public void CreateNmsMessageTest()
        {
            var mocks = new MockRepository();
            var session = mocks.CreateMock<NMS.ISession>();
            var textMessage = mocks.CreateMock<NMS.ITextMessage>();
            textMessage.NMSType = ActiveMqMessageType.Text.ToString();
            Expect.Call(session.CreateTextMessage(TestXml)).Return(textMessage);
            mocks.ReplayAll();

            var message = MessageUtils.CreateNmsMessage(session, TestXml);

            Assert.IsNotNull(message as NMS.ITextMessage);

            mocks.VerifyAll();
        }

        [Test]
        public void CreateLargeNmsMessageTest()
        {
            var mocks = new MockRepository();
            var session = mocks.CreateMock<NMS.ISession>();
            var bytesMessage = mocks.CreateMock<NMS.IBytesMessage>();
            bytesMessage.NMSType = ActiveMqMessageType.Bytes.ToString();
            Expect.Call(session.CreateBytesMessage(null)).IgnoreArguments().Return(bytesMessage);
            mocks.ReplayAll();

            var message = MessageUtils.CreateNmsMessage(session, File.ReadAllText(LargeXmlPath));

            Assert.IsNotNull(message as NMS.IBytesMessage);

            mocks.VerifyAll();
        }
    }
}
