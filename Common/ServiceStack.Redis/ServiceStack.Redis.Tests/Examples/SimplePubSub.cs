using System;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Common.Extensions;

namespace ServiceStack.Redis.Tests.Examples
{
	[TestFixture]
	public class SimplePubSub
	{
		const string ChannelName = "CHANNEL";
		const string MessagePrefix = "MESSAGE ";
		const int PublishMessageCount = 5;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			using (var redis = new RedisClient(TestConfig.SingleHost))
			{
				redis.FlushAll();
			}
		}

		[Test]
		public void Publish_and_receive_5_messages()
		{
			var messagesReceived = 0;
			
			using (var redisConsumer = new RedisClient(TestConfig.SingleHost))
			using (var subscription = redisConsumer.CreateSubscription())
			{
				subscription.OnSubscribe = channel =>
				{
					Console.WriteLine("Subscribed to '{0}'", channel);
				};
				subscription.OnUnSubscribe = channel =>
				{
					Console.WriteLine("UnSubscribed from '{0}'", channel);
				};
				subscription.OnMessage = (channel, msg) =>
				{
					Console.WriteLine("Received '{0}' from channel '{1}'", msg, channel);

					//As soon as we've received all 5 messages, disconnect by unsubscribing to all channels
					if (++messagesReceived == PublishMessageCount)
					{
						subscription.UnSubscribeFromAllChannels();
					}
				};

				ThreadPool.QueueUserWorkItem(x =>
				{
					Thread.Sleep(200);
					Console.WriteLine("Begin publishing messages...");

					using (var redisPublisher = new RedisClient(TestConfig.SingleHost))
					{
						for (var i = 1; i <= PublishMessageCount; i++)
						{
							var message = MessagePrefix + i;
							Console.WriteLine("Publishing '{0}' to '{1}'", message, ChannelName);
							redisPublisher.PublishMessage(ChannelName, message);
						}
					}
				});

				Console.WriteLine("Started Listening On '{0}'", ChannelName);
				subscription.SubscribeToChannels(ChannelName); //blocking
			}

			Console.WriteLine("EOF");

			/*Output: 
			Started Listening On 'CHANNEL'
			Subscribed to 'CHANNEL'
			Begin publishing messages...
			Publishing 'MESSAGE 1' to 'CHANNEL'
			Received 'MESSAGE 1' from channel 'CHANNEL'
			Publishing 'MESSAGE 2' to 'CHANNEL'
			Received 'MESSAGE 2' from channel 'CHANNEL'
			Publishing 'MESSAGE 3' to 'CHANNEL'
			Received 'MESSAGE 3' from channel 'CHANNEL'
			Publishing 'MESSAGE 4' to 'CHANNEL'
			Received 'MESSAGE 4' from channel 'CHANNEL'
			Publishing 'MESSAGE 5' to 'CHANNEL'
			Received 'MESSAGE 5' from channel 'CHANNEL'
			UnSubscribed from 'CHANNEL'
			EOF
			 */
		}

		[Test]
		public void Publish_5_messages_to_3_clients()
		{
			const int noOfClients = 3;

			for (var i = 1; i <= noOfClients; i++)
			{
				var clientNo = i;
				ThreadPool.QueueUserWorkItem(x =>
				{
					using (var redisConsumer = new RedisClient(TestConfig.SingleHost))
					using (var subscription = redisConsumer.CreateSubscription())
					{
						var messagesReceived = 0;
						subscription.OnSubscribe = channel =>
						{
							Console.WriteLine("Client #{0} Subscribed to '{1}'", clientNo, channel);
						};
						subscription.OnUnSubscribe = channel =>
						{
							Console.WriteLine("Client #{0} UnSubscribed from '{1}'", clientNo, channel);
						};
						subscription.OnMessage = (channel, msg) =>
						{
							Console.WriteLine("Client #{0} Received '{1}' from channel '{2}'", 
								clientNo, msg, channel);

							if (++messagesReceived == PublishMessageCount)
							{
								subscription.UnSubscribeFromAllChannels();
							}
						};

						Console.WriteLine("Client #{0} started Listening On '{1}'", clientNo, ChannelName);
						subscription.SubscribeToChannels(ChannelName); //blocking
					}

					Console.WriteLine("Client #{0} EOF", clientNo);
				});
			}

			using (var redisClient = new RedisClient(TestConfig.SingleHost))
			{
				Thread.Sleep(500);
				Console.WriteLine("Begin publishing messages...");

				for (var i = 1; i <= PublishMessageCount; i++)
				{
					var message = MessagePrefix + i;
					Console.WriteLine("Publishing '{0}' to '{1}'", message, ChannelName);
					redisClient.PublishMessage(ChannelName, message);
				}
			}

			Thread.Sleep(500);

			/*Output:
			Client #1 started Listening On 'CHANNEL'
			Client #2 started Listening On 'CHANNEL'
			Client #1 Subscribed to 'CHANNEL'
			Client #2 Subscribed to 'CHANNEL'
			Client #3 started Listening On 'CHANNEL'
			Client #3 Subscribed to 'CHANNEL'
			Begin publishing messages...
			Publishing 'MESSAGE 1' to 'CHANNEL'
			Client #1 Received 'MESSAGE 1' from channel 'CHANNEL'
			Client #2 Received 'MESSAGE 1' from channel 'CHANNEL'
			Publishing 'MESSAGE 2' to 'CHANNEL'
			Client #1 Received 'MESSAGE 2' from channel 'CHANNEL'
			Client #2 Received 'MESSAGE 2' from channel 'CHANNEL'
			Publishing 'MESSAGE 3' to 'CHANNEL'
			Client #3 Received 'MESSAGE 1' from channel 'CHANNEL'
			Client #3 Received 'MESSAGE 2' from channel 'CHANNEL'
			Client #3 Received 'MESSAGE 3' from channel 'CHANNEL'
			Client #1 Received 'MESSAGE 3' from channel 'CHANNEL'
			Client #2 Received 'MESSAGE 3' from channel 'CHANNEL'
			Publishing 'MESSAGE 4' to 'CHANNEL'
			Client #1 Received 'MESSAGE 4' from channel 'CHANNEL'
			Client #3 Received 'MESSAGE 4' from channel 'CHANNEL'
			Publishing 'MESSAGE 5' to 'CHANNEL'
			Client #1 Received 'MESSAGE 5' from channel 'CHANNEL'
			Client #3 Received 'MESSAGE 5' from channel 'CHANNEL'
			Client #1 UnSubscribed from 'CHANNEL'
			Client #1 EOF
			Client #3 UnSubscribed from 'CHANNEL'
			Client #3 EOF
			Client #2 Received 'MESSAGE 4' from channel 'CHANNEL'
			Client #2 Received 'MESSAGE 5' from channel 'CHANNEL'
			Client #2 UnSubscribed from 'CHANNEL'
			Client #2 EOF
			 */
		}
	}
}