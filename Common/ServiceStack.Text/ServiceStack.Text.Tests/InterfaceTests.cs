using NUnit.Framework;
using ServiceStack.Messaging;

namespace ServiceStack.Text.Tests
{
	[TestFixture]
	public class InterfaceTests
		: TestBase
	{
		[Test]
		public void Can_serialize_Message()
		{
			var message = new Message<string> {
				Body = "test"
			};
			var messageString = TypeSerializer.SerializeToString(message);

			Assert.That(messageString, Is.EqualTo("{Id:00000000000000000000000000000000,CreatedDate:0001-01-01,Priority:0,RetryAttempts:0,Body:test}"));

			Serialize(message);
		}

		[Test]
		public void Can_serialize_IMessage()
		{
			var message = new Message<string> {
				Body = "test"
			};
			var messageString = TypeSerializer.SerializeToString((IMessage<string>)message);

			Assert.That(messageString, Is.EqualTo("{Id:00000000000000000000000000000000,CreatedDate:0001-01-01,Priority:0,RetryAttempts:0,Body:test}"));
		}

	}
}