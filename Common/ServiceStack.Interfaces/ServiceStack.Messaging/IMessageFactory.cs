using System;

namespace ServiceStack.Messaging
{
	public interface IMessageFactory
		: IDisposable
	{
		IMessageProducer CreateMessageProducer();
		IMessageService CreateMessageService();
	}

	public interface IMessageQueueClientFactory
		: IDisposable
	{
		IMessageQueueClient CreateMessageQueueClient();
	}

	public interface IMessageQueueClient
		: IMessageProducer
	{
		void Publish(string queueName, byte[] messageBytes);
		void Notify(string queueName, byte[] messageBytes);

		byte[] Get(string queueName);
		byte[] GetAsync(string queueName);
	}
}