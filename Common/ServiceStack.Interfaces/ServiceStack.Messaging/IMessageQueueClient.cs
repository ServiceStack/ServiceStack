using System;

namespace ServiceStack.Messaging
{
	public interface IMessageQueueClient
		: IMessageProducer
	{
		/// <summary>
		/// Publish the specified message into the durable queue @queueName
		/// </summary>
		/// <param name="queueName"></param>
		/// <param name="messageBytes"></param>
		void Publish(string queueName, byte[] messageBytes);
		
		/// <summary>
		/// Publish the specified message into the transient queue @queueName
		/// </summary>
		/// <param name="queueName"></param>
		/// <param name="messageBytes"></param>
		void Notify(string queueName, byte[] messageBytes);

		/// <summary>
		/// Synchronous blocking get.
		/// </summary>
		/// <param name="queueName"></param>
		/// <param name="timeOut"></param>
		/// <returns></returns>
		byte[] Get(string queueName, TimeSpan? timeOut);
		
		/// <summary>
		/// Non blocking get message
		/// </summary>
		/// <param name="queueName"></param>
		/// <returns></returns>
		byte[] GetAsync(string queueName);
	}
}