using System;

namespace ServiceStack.Messaging.Tests
{
	public class InMemoryMessagingHostTests
		: BasicServiceMessagingTests
	{
		InMemoryMessagingService messagingService;

		protected override IMessageFactory CreateMessageFactory()
		{
			messagingService = new InMemoryMessagingService();
			return new InMemoryMessageFactory(messagingService);
		}

		protected override MessagingServiceBase CreateMessagingService()
		{
			return messagingService;
		}
	}

}