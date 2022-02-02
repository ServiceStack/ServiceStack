using NUnit.Framework;
using ServiceStack.Messaging;

namespace ServiceStack.Text.Tests
{
    public class Incr
    {
        public int Value { get; set; }
    }

    public class Ping { }

    [TestFixture]
    public class MessagingTests : TestBase
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
        public void Can_serialize_IMessage_into_typed_Message_Ping()
        {
            var dto = new Ping();
            IMessage iMsg = MessageFactory.Create(dto);
            var json = iMsg.ToJson();
            var typedMessage = json.FromJson<Message<Ping>>();

            Assert.That(typedMessage.GetBody(), Is.Not.Null);
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

#if !NETCORE
        [Test]
        public void Can_serialize_IMessage_ToBytes_into_typed_Message()
        {
            var dto = new Incr { Value = 1 };
            var iMsg = MessageFactory.Create(dto);
            var bytes = iMsg.ToBytes();
            var typedMessage = bytes.ToMessage<Incr>();

            Assert.That(typedMessage.GetBody().Value, Is.EqualTo(dto.Value));
        }
#endif

        public class DtoWithInterface
        {
            public IMessage<string> Results { get; set; }
        }

        [Test]
        public void Can_deserialize_interface_into_concrete_type()
        {
            var dto = Serialize(new DtoWithInterface { Results = new Message<string>("Body") }, includeXml: false);
            Assert.That(dto.Results, Is.Not.Null);
            Assert.That(dto.Results.GetBody(), Is.Not.Null);
        }
    }
}