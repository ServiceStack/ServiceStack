using ServiceStack.Messaging;

namespace ServiceStack.Server.Tests.Messaging
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