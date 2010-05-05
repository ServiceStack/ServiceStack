using System;
using System.Collections.Generic;
using ServiceStack.Common.Extensions;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
	public class RedisSubscription
		: IRedisSubscription
	{
		private readonly RedisClient redisClient;
		private List<string> activeChannels;
		public int SubscriptionCount { get; private set; }

		private static readonly byte[] SubscribeWord = "subscribe".ToUtf8Bytes();
		private static readonly byte[] UnSubscribeWord = "unsubscribe".ToUtf8Bytes();
		private static readonly byte[] MessageWord = "message".ToUtf8Bytes();

		public RedisSubscription(RedisClient redisClient)
		{
			this.redisClient = redisClient;

			this.SubscriptionCount = 0;
			this.activeChannels = new List<string>();
		}

		public Action<string> OnSubscribe { get; set; }
		public Action<string, string> OnMessage { get; set; }
		public Action<string> OnUnSubscribe { get; set; }

		public void SubscribeToChannels(params string[] channels)
		{
			var multiBytes = redisClient.Subscribe(channels);
			ParseSubscriptionResults(multiBytes);
			
			while (this.SubscriptionCount > 0)
			{
				multiBytes = redisClient.ReceiveMessages();
				ParseSubscriptionResults(multiBytes);
			}
		}

		public void SubscribeToChannelsMatching(params string[] patterns)
		{
			var multiBytes = redisClient.Subscribe(patterns);
			ParseSubscriptionResults(multiBytes);

			while (this.SubscriptionCount > 0)
			{
				multiBytes = redisClient.ReceiveMessages();
				ParseSubscriptionResults(multiBytes);
			}
		}

		private void ParseSubscriptionResults(byte[][] multiBytes)
		{
			for (var i = 0; i < multiBytes.Length; i += 3)
			{
				var messageType = multiBytes[i];
				var channel = multiBytes[i + 1].FromUtf8Bytes();

				if (SubscribeWord.AreEqual(messageType))
				{
					this.SubscriptionCount = int.Parse(multiBytes[i + 2].FromUtf8Bytes());

					activeChannels.Add(channel);

					if (this.OnSubscribe != null)
					{
						this.OnSubscribe(channel);
					}
				}
				else if (UnSubscribeWord.AreEqual(messageType))
				{
					this.SubscriptionCount = int.Parse(multiBytes[i + 2].FromUtf8Bytes());

					activeChannels.Remove(channel);

					if (this.OnUnSubscribe != null)
					{
						this.OnUnSubscribe(channel);
					}
				}
				else if (MessageWord.AreEqual(messageType))
				{
					var message = multiBytes[i + 2].FromUtf8Bytes();
					
					if (this.OnMessage != null)
					{
						this.OnMessage(channel, message);
					}
				}
				else
				{
					throw new RedisException(
						"Invalid state. Expected [subscribe|unsubscribe|message] got: " + messageType);
				}
			}
		}

		public void UnSubscribeFromAllChannels()
		{
			if (activeChannels.Count == 0) return;

			var multiBytes = redisClient.UnSubscribe();
			ParseSubscriptionResults(multiBytes);

			this.activeChannels = new List<string>();
		}

		public void UnSubscribeFromChannels(params string[] channels)
		{
			var multiBytes = redisClient.UnSubscribe(channels);
			ParseSubscriptionResults(multiBytes);
		}

		public void UnSubscribeFromChannelsMatching(params string[] patterns)
		{
			var multiBytes = redisClient.UnSubscribe(patterns);
			ParseSubscriptionResults(multiBytes);
		}

		public void Dispose()
		{
			UnSubscribeFromAllChannels();
		}
	}
}