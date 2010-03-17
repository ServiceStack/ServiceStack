using System;

namespace ServiceStack.Messaging.Tests
{
	public class InMemoryMessagingHostTests
		: BasicServiceMessagingTests
	{
		InMemoryMessageService messageService;

		protected override IMessageFactory CreateMessageFactory()
		{
			messageService = new InMemoryMessageService();
			return new InMemoryMessageFactory(messageService);
		}

		protected override MessageServiceBase CreateMessagingService()
		{
			return messageService;
		}
	}

}