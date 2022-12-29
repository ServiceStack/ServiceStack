using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration")]
    public class RedisPubSubTests
        : RedisClientTestsBase
    {
        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();
            Redis.NamespacePrefix = "RedisPubSubTests";
        }

        [Test]
        public void Can_Subscribe_and_Publish_single_message()
        {
            var channelName = PrefixedKey("CHANNEL1");
            const string message = "Hello, World!";
            var key = PrefixedKey("Can_Subscribe_and_Publish_single_message");

            Redis.IncrementValue(key);

            using (var subscription = Redis.CreateSubscription())
            {
                subscription.OnSubscribe = channel =>
                {
                    Log("Subscribed to '{0}'", channel);
                    Assert.That(channel, Is.EqualTo(channelName));
                };
                subscription.OnUnSubscribe = channel =>
                {
                    Log("UnSubscribed from '{0}'", channel);
                    Assert.That(channel, Is.EqualTo(channelName));
                };
                subscription.OnMessage = (channel, msg) =>
                {
                    Log("Received '{0}' from channel '{1}'", msg, channel);
                    Assert.That(channel, Is.EqualTo(channelName));
                    Assert.That(msg, Is.EqualTo(message));
                    subscription.UnSubscribeFromAllChannels();
                };

                ThreadPool.QueueUserWorkItem(x =>
                {
                    Thread.Sleep(100); // to be sure that we have subscribers
                    using (var redisClient = CreateRedisClient())
                    {
                        Log("Publishing '{0}' to '{1}'", message, channelName);
                        redisClient.PublishMessage(channelName, message);
                    }
                });

                Log("Start Listening On " + channelName);
                subscription.SubscribeToChannels(channelName); //blocking
            }

            Log("Using as normal client again...");
            Redis.IncrementValue(key);
            Assert.That(Redis.Get<int>(key), Is.EqualTo(2));
        }

        [Test]
        public void Can_Subscribe_and_Publish_single_message_using_wildcard()
        {
            var channelWildcard = PrefixedKey("CHANNEL.*");
            var channelName = PrefixedKey("CHANNEL.1");
            const string message = "Hello, World!";
            var key = PrefixedKey("Can_Subscribe_and_Publish_single_message");

            Redis.IncrementValue(key);

            using (var subscription = Redis.CreateSubscription())
            {
                subscription.OnSubscribe = channel =>
                {
                    Log("Subscribed to '{0}'", channelWildcard);
                    Assert.That(channel, Is.EqualTo(channelWildcard));
                };
                subscription.OnUnSubscribe = channel =>
                {
                    Log("UnSubscribed from '{0}'", channelWildcard);
                    Assert.That(channel, Is.EqualTo(channelWildcard));
                };
                subscription.OnMessage = (channel, msg) =>
                {
                    Log("Received '{0}' from channel '{1}'", msg, channel);
                    Assert.That(channel, Is.EqualTo(channelName));
                    Assert.That(msg, Is.EqualTo(message), "we should get the message, not the channel");
                    subscription.UnSubscribeFromChannelsMatching();
                };

                ThreadPool.QueueUserWorkItem(x =>
                {
                    Thread.Sleep(100); // to be sure that we have subscribers
                    using (var redisClient = CreateRedisClient())
                    {
                        Log("Publishing '{0}' to '{1}'", message, channelName);
                        redisClient.PublishMessage(channelName, message);
                    }
                });

                Log("Start Listening On " + channelName);
                subscription.SubscribeToChannelsMatching(channelWildcard); //blocking
            }

            Log("Using as normal client again...");
            Redis.IncrementValue(key);
            Assert.That(Redis.Get<int>(key), Is.EqualTo(2));
        }

        [Test]
        public void Can_Subscribe_and_Publish_multiple_message()
        {
            var channelName = PrefixedKey("CHANNEL2");
            const string messagePrefix = "MESSAGE ";
            string key = PrefixedKey("Can_Subscribe_and_Publish_multiple_message");
            const int publishMessageCount = 5;
            var messagesReceived = 0;

            Redis.IncrementValue(key);

            using (var subscription = Redis.CreateSubscription())
            {
                subscription.OnSubscribe = channel =>
                {
                    Log("Subscribed to '{0}'", channel);
                    Assert.That(channel, Is.EqualTo(channelName));
                };
                subscription.OnUnSubscribe = channel =>
                {
                    Log("UnSubscribed from '{0}'", channel);
                    Assert.That(channel, Is.EqualTo(channelName));
                };
                subscription.OnMessage = (channel, msg) =>
                {
                    Log("Received '{0}' from channel '{1}'", msg, channel);
                    Assert.That(channel, Is.EqualTo(channelName));
                    Assert.That(msg, Is.EqualTo(messagePrefix + messagesReceived++));

                    if (messagesReceived == publishMessageCount)
                    {
                        subscription.UnSubscribeFromAllChannels();
                    }
                };

                ThreadPool.QueueUserWorkItem(x =>
                {
                    Thread.Sleep(100); // to be sure that we have subscribers

                    using (var redisClient = CreateRedisClient())
                    {
                        for (var i = 0; i < publishMessageCount; i++)
                        {
                            var message = messagePrefix + i;
                            Log("Publishing '{0}' to '{1}'", message, channelName);
                            redisClient.PublishMessage(channelName, message);
                        }
                    }
                });

                Log("Start Listening On");
                subscription.SubscribeToChannels(channelName); //blocking
            }

            Log("Using as normal client again...");
            Redis.IncrementValue(key);
            Assert.That(Redis.Get<int>(key), Is.EqualTo(2));

            Assert.That(messagesReceived, Is.EqualTo(publishMessageCount));
        }

        [Test]
        public void Can_Subscribe_and_Publish_message_to_multiple_channels()
        {
            var channelPrefix = PrefixedKey("CHANNEL3 ");
            const string message = "MESSAGE";
            const int publishChannelCount = 5;
            var key = PrefixedKey("Can_Subscribe_and_Publish_message_to_multiple_channels");

            var channels = new List<string>();
            publishChannelCount.Times(i => channels.Add(channelPrefix + i));

            var messagesReceived = 0;
            var channelsSubscribed = 0;
            var channelsUnSubscribed = 0;

            Redis.IncrementValue(key);

            using (var subscription = Redis.CreateSubscription())
            {
                subscription.OnSubscribe = channel =>
                {
                    Log("Subscribed to '{0}'", channel);
                    Assert.That(channel, Is.EqualTo(channelPrefix + channelsSubscribed++));
                };
                subscription.OnUnSubscribe = channel =>
                {
                    Log("UnSubscribed from '{0}'", channel);
                    Assert.That(channel, Is.EqualTo(channelPrefix + channelsUnSubscribed++));
                };
                subscription.OnMessage = (channel, msg) =>
                {
                    Log("Received '{0}' from channel '{1}'", msg, channel);
                    Assert.That(channel, Is.EqualTo(channelPrefix + messagesReceived++));
                    Assert.That(msg, Is.EqualTo(message));

                    subscription.UnSubscribeFromChannels(channel);
                };

                ThreadPool.QueueUserWorkItem(x =>
                {
                    Thread.Sleep(100); // to be sure that we have subscribers

                    using (var redisClient = CreateRedisClient())
                    {
                        foreach (var channel in channels)
                        {
                            Log("Publishing '{0}' to '{1}'", message, channel);
                            redisClient.PublishMessage(channel, message);
                        }
                    }
                });

                Log("Start Listening On");
                subscription.SubscribeToChannels(channels.ToArray()); //blocking
            }

            Log("Using as normal client again...");
            Redis.IncrementValue(key);
            Assert.That(Redis.Get<int>(key), Is.EqualTo(2));

            Assert.That(messagesReceived, Is.EqualTo(publishChannelCount));
            Assert.That(channelsSubscribed, Is.EqualTo(publishChannelCount));
            Assert.That(channelsUnSubscribed, Is.EqualTo(publishChannelCount));
        }

        [Test]
        public void Can_Subscribe_to_channel_pattern()
        {
            int msgs = 0;
            using (var subscription = Redis.CreateSubscription())
            {
                subscription.OnMessage = (channel, msg) =>
                {
                    Debug.WriteLine(String.Format("{0}: {1}", channel, msg + msgs++));
                    subscription.UnSubscribeFromChannelsMatching(PrefixedKey("CHANNEL4:TITLE*"));
                };

                ThreadPool.QueueUserWorkItem(x =>
                {
                    Thread.Sleep(100); // to be sure that we have subscribers

                    using (var redisClient = CreateRedisClient())
                    {
                        Log("Publishing msg...");
                        redisClient.Publish(PrefixedKey("CHANNEL4:TITLE1"), "hello".ToUtf8Bytes());
                    }
                });

                Log("Start Listening On");
                subscription.SubscribeToChannelsMatching(PrefixedKey("CHANNEL4:TITLE*"));
            }
        }

        [Test]
        public void Can_Subscribe_to_multiplechannel_pattern()
        {
            var channels = new[] { PrefixedKey("CHANNEL5:TITLE*"), PrefixedKey("CHANNEL5:BODY*") };
            int msgs = 0;
            using (var subscription = Redis.CreateSubscription())
            {
                subscription.OnMessage = (channel, msg) =>
                {
                    Debug.WriteLine(String.Format("{0}: {1}", channel, msg + msgs++));
                    subscription.UnSubscribeFromChannelsMatching(channels);
                };

                ThreadPool.QueueUserWorkItem(x =>
                {
                    Thread.Sleep(100); // to be sure that we have subscribers

                    using (var redisClient = CreateRedisClient())
                    {
                        Log("Publishing msg...");
                        redisClient.Publish(PrefixedKey("CHANNEL5:BODY"), "hello".ToUtf8Bytes());
                    }
                });

                Log("Start Listening On");
                subscription.SubscribeToChannelsMatching(channels);
            }
        }

    }
}