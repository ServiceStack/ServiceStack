using System;

namespace ServiceStack.Messaging.Tests
{
	public class InMemoryMessagingHostTests
		: TransientServiceMessagingTests
	{
		InMemoryMessageService messageService;

		protected override IMessageFactory CreateMessageFactory()
		{
			messageService = new InMemoryMessageService();
			return new InMemoryMessageFactory(messageService);
		}

		protected override TransientMessageServiceBase CreateMessagingService()
		{
			return messageService;
		}
	}

}