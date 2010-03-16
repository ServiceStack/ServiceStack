using System;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.Messaging.Tests.Services;
using ServiceStack.Text;

namespace ServiceStack.Messaging.Tests
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
		public void Can_Serialize_basic_IMessage()
		{
			var message = new Message<Greet>(new Greet { Name = "Test" });
			var messageString = TypeSerializer.SerializeToString(message);
			Assert.That(messageString, Is.Not.Null);

			try
			{
				var fromMessageString = TypeSerializer.DeserializeFromString<IMessage<Greet>>(messageString);
			}
			catch (NotSupportedException expectedException)
			{
				return;
			}
			Assert.Fail("Should've thrown NotSupportedException: Cannot deserialize an interface type");
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
				Error = new MessagingException(
					"Test Error", new ArgumentNullException("Test")).ToMessageError()
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
	}
}