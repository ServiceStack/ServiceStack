//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.Messaging
{
	public class RedisMessageQueueClient
		: IMessageQueueClient
	{
		private readonly Action onPublishedCallback;
		private readonly IRedisClientsManager clientsManager;

        public int MaxSuccessQueueSize { get; set; }

		public RedisMessageQueueClient(IRedisClientsManager clientsManager)
			: this(clientsManager, null) {}

		public RedisMessageQueueClient(
			IRedisClientsManager clientsManager, Action onPublishedCallback)
		{
			this.onPublishedCallback = onPublishedCallback;
			this.clientsManager = clientsManager;
		    this.MaxSuccessQueueSize = 100;
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
            if (typeof(IMessage).IsAssignableFrom(typeof(T)))
                Publish((IMessage)messageBody);
            else
                Publish<T>(new Message<T>(messageBody));
        }

        public void Publish(IMessage message)
        {
            var messageBytes = message.ToBytes();
            Publish(message.ToInQueueName(), messageBytes);
        }

        public void Publish<T>(IMessage<T> message)
        {
            var messageBytes = message.ToBytes();
            Publish(message.ToInQueueName(), messageBytes);
        }

		public void Publish(string queueName, byte[] messageBytes)
		{
			this.ReadWriteClient.LPush(queueName, messageBytes);
			this.ReadWriteClient.Publish(QueueNames.TopicIn, queueName.ToUtf8Bytes());

			if (onPublishedCallback != null)
			{
				onPublishedCallback();
			}
		}

		public void Notify(string queueName, byte[] messageBytes)
		{
			this.ReadWriteClient.LPush(queueName, messageBytes);
            this.ReadWriteClient.LTrim(queueName, 0, this.MaxSuccessQueueSize);
			this.ReadWriteClient.Publish(QueueNames.TopicOut, queueName.ToUtf8Bytes());
		}

		public byte[] Get(string queueName, TimeSpan? timeOut)
		{
			var unblockingKeyAndValue = this.ReadOnlyClient.BRPop(queueName, (int) timeOut.GetValueOrDefault().TotalSeconds);
            return unblockingKeyAndValue.Length != 2 
                ? null 
                : unblockingKeyAndValue[1];
		}

		public byte[] GetAsync(string queueName)
		{
			return this.ReadOnlyClient.RPop(queueName);
		}

		public string WaitForNotifyOnAny(params string[] channelNames)
		{
			string result = null;
            var subscription = readOnlyClient.CreateSubscription();
			subscription.OnMessage = (channel, msg) => {
				result = msg;
				subscription.UnSubscribeFromAllChannels();
			};
			subscription.SubscribeToChannels(channelNames); //blocks
			return result;
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