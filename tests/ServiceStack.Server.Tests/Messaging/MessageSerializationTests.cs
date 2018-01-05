using System;
using System.Reflection;
using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.Model;
using ServiceStack.Server.Tests.Services;
using ServiceStack.Text;

namespace ServiceStack.Server.Tests.Messaging
{
	[TestFixture]
	public class MessageSerializationTests
	{
		[Test]
		public void Can_Serialize_and_basic_Message()
		{
			var message = new Message<Greet>(new Greet { Name = "Test" });
			Serialize(message);
		}

		[Test]
		public void Serializing_basic_IMessage_returns_null()
		{
			var message = new Message<Greet>(new Greet { Name = "Test" });
			var messageString = TypeSerializer.SerializeToString(message);
			Assert.That(messageString, Is.Not.Null);

			var result = TypeSerializer.DeserializeFromString<IMessage<Greet>>(messageString);
			Assert.That(result, Is.Null);
		}

		[Test]
		public void Can_Serialize_IMessage_and_Deserialize_into_Message()
		{
			var message = new Message<Greet>(new Greet { Name = "Test" });
			var messageString = TypeSerializer.SerializeToString((IMessage<Greet>)message);
			Assert.That(messageString, Is.Not.Null);

			var fromMessageString = TypeSerializer.DeserializeFromString<Message<Greet>>(
				messageString);

			Assert.That(fromMessageString, Is.Not.Null);
			Assert.That(fromMessageString.Id, Is.EqualTo(message.Id));
		}

		[Test]
		public void Can_Serialize_and_Message_with_Error()
		{
			var message = new Message<Greet>(new Greet { Name = "Test" }) {
                Error = new ArgumentNullException("Test").ToResponseStatus()
			};
			Serialize(message);
		}


		private static void Serialize<T>(T message)
			where T : IHasId<Guid>
		{
			var messageString = TypeSerializer.SerializeToString(message);
			Assert.That(messageString, Is.Not.Null);

			var fromMessageString = TypeSerializer.DeserializeFromString<T>(messageString);

			Assert.That(fromMessageString, Is.Not.Null);
			Assert.That(fromMessageString.Id, Is.EqualTo(message.Id));
		}

        [Test]
        public void Does_serialize_to_correct_MQ_name()
        {
            var message = new Message<Greet>(new Greet { Name = "Test" }) {};
            var message2 = new Message<Greet> { Body = new Greet { Name = "Test" }, };

            const string expected = "mq:Greet.inq";

            Assert.That(QueueNames<Greet>.In, Is.EqualTo(expected));
            Assert.That(message.ToInQueueName(), Is.EqualTo(expected));
            Assert.That(((IMessage<Greet>)message).ToInQueueName(), Is.EqualTo(expected));

            Assert.That(message2.ToInQueueName(), Is.EqualTo(expected));
            Assert.That(((IMessage<Greet>)message2).ToInQueueName(), Is.EqualTo(expected));
            Assert.That(((IMessage<Greet>)(object)message2).ToInQueueName(), Is.EqualTo(expected));
        }

        [Test]
        public void Cast_Tests()
        {
            var message = new Message<Greet>(new Greet { Name = "Test" }) { };

            Assert.That(message is IMessage<Greet>, Is.True);
            Assert.That(typeof(IMessage<Greet>).IsAssignableFrom(message.GetType()), Is.True);
            Assert.That(message.GetType().IsAssignableFrom(typeof(IMessage<Greet>)), Is.False);
        }
	}
}