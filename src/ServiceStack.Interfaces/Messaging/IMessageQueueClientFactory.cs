using System;

namespace ServiceStack.Messaging
{
	public interface IMessageQueueClientFactory
		: IDisposable
	{
		IMessageQueueClient CreateMessageQueueClient();
	}
}