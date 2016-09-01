#if !NETCORE_SUPPORT
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Messaging;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
    public class Incr
    {
        public int Value { get; set; }
    }

    public class TestUserSession : AuthUserSession
    {
    }

    [TestFixture]
    public class MessagingTests
    {
        [Test]
        public void Can_serialize_IMessage_into_typed_Message()
        {
            var dto = new Incr { Value = 1 };
            IMessage iMsg = MessageFactory.Create(dto);
            var json = iMsg.ToJson();
            var typedMessage = json.FromJson<Message<Incr>>();

            Assert.That(typedMessage.GetBody().Value, Is.EqualTo(dto.Value));
        }

        [Test]
        public void Can_serialize_object_IMessage_into_typed_Message()
        {
            var dto = new Incr { Value = 1 };
            var iMsg = MessageFactory.Create(dto);
            var json = ((object)iMsg).ToJson();
            var typedMessage = json.FromJson<Message<Incr>>();

            Assert.That(typedMessage.GetBody().Value, Is.EqualTo(dto.Value));
        }

        [Test]
        public void Can_serialize_IMessage_ToBytes_into_typed_Message()
        {
            var dto = new Incr { Value = 1 };
            var iMsg = MessageFactory.Create(dto);
            var bytes = iMsg.ToBytes();
            var typedMessage = bytes.ToMessage<Incr>();

            Assert.That(typedMessage.GetBody().Value, Is.EqualTo(dto.Value));
        }

        [Test]
        public void Can_deserialize_concrete_type_into_IOAuthSession()
        {
            var json = "{\"__type\":\"ServiceStack.Common.Tests.TestUserSession, ServiceStack.Common.Tests\",\"ReferrerUrl\":\"http://localhost:4629/oauth\",\"Id\":\"0412cc4654484111b2e7162a24a83753\",\"RequestToken\":\"dw4U1RUBr8r5Bx1oBZfdmNiocsMrAtBmSoFHYCZrr4\",\"RequestTokenSecret\":\"HNvCiD1a61CrutnxZoiJXQlLKNN1GAtWn7pRuafYN0\",\"CreatedAt\":\"\\/Date(1320221243138+0000)\\/\",\"LastModified\":\"\\/Date(1320221243138+0000)\\/\",\"Items\":{}}";
            var fromJson = json.FromJson<IAuthSession>();
            Assert.That(fromJson, Is.Not.Null);
        }
    }
}
#endif
