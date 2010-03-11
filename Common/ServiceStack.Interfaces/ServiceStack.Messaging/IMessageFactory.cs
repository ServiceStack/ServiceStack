using System;

namespace ServiceStack.Messaging
{
	public interface IMessageFactory
		: IDisposable
	{
		IMessageProducer CreateMessageProducer();
		IMessageService CreateMessageService();
	}
}