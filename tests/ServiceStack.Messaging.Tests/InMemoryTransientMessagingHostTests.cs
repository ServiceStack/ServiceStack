using System;

namespace ServiceStack.Messaging.Tests
{
	public class InMemoryTransientMessagingHostTests
		: TransientServiceMessagingTests
	{
		InMemoryTransientMessageService messageService;

		protected override IMessageFactory CreateMessageFactory()
		{
			messageService = new InMemoryTransientMessageService();
			return new InMemoryTransientMessageFactory(messageService);
		}

		protected override TransientMessageServiceBase CreateMessagingService()
		{
			return messageService;
		}
	}
}