using System;
using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests
{
	public class Incr
	{
		public int Value { get; set; }
	}

	[TestFixture]
	public class MessagingTests
	{
		[Test]
		public void Can_serialize_IMessage_into_typed_Message()
		{
			var dto = new Incr { Value = 1 };
			var iMsg = Message.Create(dto);
			var json = ((object) iMsg).ToJson();
			var typedMessage = json.FromJson<Message<Incr>>();

			Assert.That(typedMessage.Body.Value, Is.EqualTo(dto.Value));
		}

		[Test]
		public void Can_serialize_IMessage_ToBytes_into_typed_Message()
		{
			var dto = new Incr { Value = 1 };
			var iMsg = Message.Create(dto);
			var bytes = iMsg.ToBytes();
			var typedMessage = bytes.ToMessage<Incr>();

			Assert.That(typedMessage.Body.Value, Is.EqualTo(dto.Value));
		}

	}
}