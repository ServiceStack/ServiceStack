using System;
using System.Threading;
using NUnit.Framework;

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
			var redis = new RedisClient(TestConfig.SingleHost);

			using (var subscription = redis.CreateSubscription())
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

					if (++messagesReceived == PublishMessageCount)
					{
						subscription.UnSubscribeFromAllChannels();
					}
				};

				ThreadPool.QueueUserWorkItem(x =>
				{
					Thread.Sleep(200); 

					using (var redisClient = new RedisClient(TestConfig.SingleHost))
					{
						for (var i = 0; i < PublishMessageCount; i++)
						{
							var message = MessagePrefix + i;
							Console.WriteLine("Publishing '{0}' to '{1}'", message, ChannelName);
							redisClient.PublishMessage(ChannelName, message);
						}
					}
				});

				Console.WriteLine("Start Listening On " + ChannelName);
				subscription.SubscribeToChannels(ChannelName); //blocking
			}

			Console.WriteLine("EOF");

			/*Output: 
			
			== WITH Thread.Sleep(200) ==

			Start Listening On CHANNEL
			Subscribed to 'CHANNEL'
			Publishing 'MESSAGE 0' to 'CHANNEL'
			Received 'MESSAGE 0' from channel 'CHANNEL'
			Publishing 'MESSAGE 1' to 'CHANNEL'
			Received 'MESSAGE 1' from channel 'CHANNEL'
			Publishing 'MESSAGE 2' to 'CHANNEL'
			Received 'MESSAGE 2' from channel 'CHANNEL'
			Publishing 'MESSAGE 3' to 'CHANNEL'
			Received 'MESSAGE 3' from channel 'CHANNEL'
			Publishing 'MESSAGE 4' to 'CHANNEL'
			Received 'MESSAGE 4' from channel 'CHANNEL'
			UnSubscribed from 'CHANNEL'
			EOF
			
			== WITHOUT Thread.Sleep() ==

			Start Listening On CHANNEL
			Publishing 'MESSAGE 0' to 'CHANNEL'
			Publishing 'MESSAGE 1' to 'CHANNEL'
			Publishing 'MESSAGE 2' to 'CHANNEL'
			Publishing 'MESSAGE 3' to 'CHANNEL'
			Publishing 'MESSAGE 4' to 'CHANNEL'
			Subscribed to 'CHANNEL'
			Received 'MESSAGE 0' from channel 'CHANNEL'
			Received 'MESSAGE 1' from channel 'CHANNEL'
			Received 'MESSAGE 2' from channel 'CHANNEL'
			Received 'MESSAGE 3' from channel 'CHANNEL'
			Received 'MESSAGE 4' from channel 'CHANNEL'
			UnSubscribed from 'CHANNEL'
			EOF
			 */
		}
		
	}
}