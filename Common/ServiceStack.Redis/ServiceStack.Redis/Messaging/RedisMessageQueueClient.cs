//
// http://code.google.com/p/servicestack/wiki/ServiceStackRedis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of Redis and ServiceStack: new BSD license.
//

using System;
using ServiceStack.Messaging;

namespace ServiceStack.Redis.Messaging
{
	public class RedisMessageQueueClient
		: IMessageQueueClient
	{
		private readonly Action onPublishedCallback;
		private readonly IRedisClientsManager clientsManager;

		public RedisMessageQueueClient(
			IRedisClientsManager clientsManager, Action onPublishedCallback)
		{
			this.onPublishedCallback = onPublishedCallback;
			this.clientsManager = clientsManager;
		}

		private IRedisNativeClient readWriteClient;
		public IRedisNativeClient ReadWriteClient
		{
			get
			{
				if (this.readWriteClient == null)
				{
					this.readWriteClient = (IRedisNativeClient)clientsManager.GetClient();
				}
				return readWriteClient;
			}
		}

		private IRedisNativeClient readOnlyClient;
		public IRedisNativeClient ReadOnlyClient
		{
			get
			{
				if (this.readOnlyClient == null)
				{
					this.readOnlyClient = (IRedisNativeClient)clientsManager.GetReadOnlyClient();
				}
				return readOnlyClient;
			}
		}

		public void Publish<T>(T messageBody)
		{
			Publish<T>(new Message<T>(messageBody));
		}

		public void Publish<T>(IMessage<T> message)
		{
			var messageBytes = message.ToBytes();
			Publish(message.ToInQueueName(), messageBytes);
		}

		public void Publish(string queueName, byte[] messageBytes)
		{
			this.ReadWriteClient.LPush(queueName, messageBytes);
			
			if (onPublishedCallback != null)
			{
				onPublishedCallback();
			}
		}

		public void Notify(string queueName, byte[] messageBytes)
		{
			const int maxSuccessQueueSize = 1000;
			this.ReadWriteClient.LPush(queueName, messageBytes);
			this.ReadWriteClient.LTrim(queueName, 0, maxSuccessQueueSize);
		}

		public byte[] Get(string queueName, TimeSpan? timeOut)
		{
			throw new NotImplementedException();
		}

		public byte[] GetAsync(string queueName)
		{
			return this.ReadOnlyClient.RPop(queueName);
		}

		public void Dispose()
		{
			if (this.readOnlyClient != null)
			{
				this.readOnlyClient.Dispose();
			}
			if (this.readWriteClient != null)
			{
				this.readWriteClient.Dispose();
			}
		}
	}
}