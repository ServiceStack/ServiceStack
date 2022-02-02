using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Integration")]
    public class RedisPubSubTestsAsync
        : RedisClientTestsBaseAsync
    {
        public override void OnBeforeEachTest()
        {
            base.OnBeforeEachTest();
            RedisRaw.NamespacePrefix = "RedisPubSubTests";
        }

        [Test]
        public async Task Can_Subscribe_and_Publish_single_message()
        {
            var channelName = PrefixedKey("CHANNEL1");
            const string message = "Hello, World!";
            var key = PrefixedKey("Can_Subscribe_and_Publish_single_message");

            await RedisAsync.IncrementValueAsync(key);

            await using (var subscription = await RedisAsync.CreateSubscriptionAsync())
            {
                subscription.OnSubscribeAsync += channel =>
                {
                    Log("Subscribed to '{0}'", channel);
                    Assert.That(channel, Is.EqualTo(channelName));
                    return default;
                };
                subscription.OnUnSubscribeAsync += channel =>
                {
                    Log("UnSubscribed from '{0}'", channel);
                    Assert.That(channel, Is.EqualTo(channelName));
                    return default;
                };
                subscription.OnMessageAsync += async (channel, msg) =>
                {
                    Log("Received '{0}' from channel '{1}'", msg, channel);
                    Assert.That(channel, Is.EqualTo(channelName));
                    Assert.That(msg, Is.EqualTo(message));
                    await subscription.UnSubscribeFromAllChannelsAsync();
                };

                ThreadPool.QueueUserWorkItem(async x =>
                {
                    await Task.Delay(100); // to be sure that we have subscribers
                    await using var redisClient = CreateRedisClient().ForAsyncOnly();
                    Log("Publishing '{0}' to '{1}'", message, channelName);
                    await redisClient.PublishMessageAsync(channelName, message);
                });

                Log("Start Listening On " + channelName);
                await subscription.SubscribeToChannelsAsync(new[] { channelName }); //blocking
            }

            Log("Using as normal client again...");
            await RedisAsync.IncrementValueAsync(key);
            Assert.That(await RedisAsync.GetAsync<int>(key), Is.EqualTo(2));
        }

        [Test]
        public async Task Can_Subscribe_and_Publish_single_message_using_wildcard()
        {
            var channelWildcard = PrefixedKey("CHANNEL.*");
            var channelName = PrefixedKey("CHANNEL.1");
            const string message = "Hello, World!";
            var key = PrefixedKey("Can_Subscribe_and_Publish_single_message");

            await RedisAsync.IncrementValueAsync(key);

            await using (var subscription = await RedisAsync.CreateSubscriptionAsync())
            {
                subscription.OnSubscribeAsync += channel =>
                {
                    Log("Subscribed to '{0}'", channelWildcard);
                    Assert.That(channel, Is.EqualTo(channelWildcard));
                    return default;
                };
                subscription.OnUnSubscribeAsync += channel =>
                {
                    Log("UnSubscribed from '{0}'", channelWildcard);
                    Assert.That(channel, Is.EqualTo(channelWildcard));
                    return default;
                };
                subscription.OnMessageAsync += async (channel, msg) =>
                {
                    Log("Received '{0}' from channel '{1}'", msg, channel);
                    Assert.That(channel, Is.EqualTo(channelName));
                    Assert.That(msg, Is.EqualTo(message), "we should get the message, not the channel");
                    await subscription.UnSubscribeFromChannelsMatchingAsync(new string[0]);
                };

                ThreadPool.QueueUserWorkItem(async x =>
                {
                    await Task.Delay(100); // to be sure that we have subscribers
                    await using var redisClient = CreateRedisClient().ForAsyncOnly();
                    Log("Publishing '{0}' to '{1}'", message, channelName);
                    await redisClient.PublishMessageAsync(channelName, message);
                });

                Log("Start Listening On " + channelName);
                await subscription.SubscribeToChannelsMatchingAsync(new[] { channelWildcard }); //blocking
            }

            Log("Using as normal client again...");
            await RedisAsync.IncrementValueAsync(key);
            Assert.That(await RedisAsync.GetAsync<int>(key), Is.EqualTo(2));
        }

        [Test]
        public async Task Can_Subscribe_and_Publish_multiple_message()
        {
            var channelName = PrefixedKey("CHANNEL2");
            const string messagePrefix = "MESSAGE ";
            string key = PrefixedKey("Can_Subscribe_and_Publish_multiple_message");
            const int publishMessageCount = 5;
            var messagesReceived = 0;

            await RedisAsync.IncrementValueAsync(key);

            await using (var subscription = await RedisAsync.CreateSubscriptionAsync())
            {
                subscription.OnSubscribeAsync += channel =>
                {
                    Log("Subscribed to '{0}'", channel);
                    Assert.That(channel, Is.EqualTo(channelName));
                    return default;
                };
                subscription.OnUnSubscribeAsync += channel =>
                {
                    Log("UnSubscribed from '{0}'", channel);
                    Assert.That(channel, Is.EqualTo(channelName));
                    return default;
                };
                subscription.OnMessageAsync += async (channel, msg) =>
                {
                    Log("Received '{0}' from channel '{1}'", msg, channel);
                    Assert.That(channel, Is.EqualTo(channelName));
                    Assert.That(msg, Is.EqualTo(messagePrefix + messagesReceived++));

                    if (messagesReceived == publishMessageCount)
                    {
                        await subscription.UnSubscribeFromAllChannelsAsync();
                    }
                };

                ThreadPool.QueueUserWorkItem(async x =>
                {
                    await Task.Delay(100); // to be sure that we have subscribers

                    await using var redisClient = CreateRedisClient().ForAsyncOnly();
                    for (var i = 0; i < publishMessageCount; i++)
                    {
                        var message = messagePrefix + i;
                        Log("Publishing '{0}' to '{1}'", message, channelName);
                        await redisClient.PublishMessageAsync(channelName, message);
                    }
                });

                Log("Start Listening On");
                await subscription.SubscribeToChannelsAsync(new[] { channelName }); //blocking
            }

            Log("Using as normal client again...");
            await RedisAsync.IncrementValueAsync(key);
            Assert.That(await RedisAsync.GetAsync<int>(key), Is.EqualTo(2));

            Assert.That(messagesReceived, Is.EqualTo(publishMessageCount));
        }

        [Test]
        public async Task Can_Subscribe_and_Publish_message_to_multiple_channels()
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

            await RedisAsync.IncrementValueAsync(key);

            await using (var subscription = await RedisAsync.CreateSubscriptionAsync())
            {
                subscription.OnSubscribeAsync += channel =>
                {
                    Log("Subscribed to '{0}'", channel);
                    Assert.That(channel, Is.EqualTo(channelPrefix + channelsSubscribed++));
                    return default;
                };
                subscription.OnUnSubscribeAsync += channel =>
                {
                    Log("UnSubscribed from '{0}'", channel);
                    Assert.That(channel, Is.EqualTo(channelPrefix + channelsUnSubscribed++));
                    return default;
                };
                subscription.OnMessageAsync += async (channel, msg) =>
                {
                    Log("Received '{0}' from channel '{1}'", msg, channel);
                    Assert.That(channel, Is.EqualTo(channelPrefix + messagesReceived++));
                    Assert.That(msg, Is.EqualTo(message));

                    await subscription.UnSubscribeFromChannelsAsync(new[] { channel });
                };

                ThreadPool.QueueUserWorkItem(async x =>
                {
                    await Task.Delay(100); // to be sure that we have subscribers

                    await using var redisClient = CreateRedisClient().ForAsyncOnly();
                    foreach (var channel in channels)
                    {
                        Log("Publishing '{0}' to '{1}'", message, channel);
                        await redisClient.PublishMessageAsync(channel, message);
                    }
                });

                Log("Start Listening On");
                await subscription.SubscribeToChannelsAsync(channels.ToArray()); //blocking
            }

            Log("Using as normal client again...");
            await RedisAsync.IncrementValueAsync(key);
            Assert.That(await RedisAsync.GetAsync<int>(key), Is.EqualTo(2));

            Assert.That(messagesReceived, Is.EqualTo(publishChannelCount));
            Assert.That(channelsSubscribed, Is.EqualTo(publishChannelCount));
            Assert.That(channelsUnSubscribed, Is.EqualTo(publishChannelCount));
        }

        [Test]
        public async Task Can_Subscribe_to_channel_pattern()
        {
            int msgs = 0;
            await using var subscription = await RedisAsync.CreateSubscriptionAsync();
            subscription.OnMessageAsync += async (channel, msg) =>
            {
                Debug.WriteLine(String.Format("{0}: {1}", channel, msg + msgs++));
                await subscription.UnSubscribeFromChannelsMatchingAsync(new[] { PrefixedKey("CHANNEL4:TITLE*") });
            };

            ThreadPool.QueueUserWorkItem(async x =>
            {
                await Task.Delay(100); // to be sure that we have subscribers

                await using var redisClient = CreateRedisClient().ForAsyncOnly();
                Log("Publishing msg...");
                await redisClient.PublishMessageAsync(PrefixedKey("CHANNEL4:TITLE1"), "hello"); // .ToUtf8Bytes()
            });

            Log("Start Listening On");
            await subscription.SubscribeToChannelsMatchingAsync(new[] { PrefixedKey("CHANNEL4:TITLE*") });
        }

        [Test]
        public async Task Can_Subscribe_to_multiplechannel_pattern()
        {
            var channels = new[] { PrefixedKey("CHANNEL5:TITLE*"), PrefixedKey("CHANNEL5:BODY*") };
            int msgs = 0;
            await using var subscription = await RedisAsync.CreateSubscriptionAsync();
            subscription.OnMessageAsync += async (channel, msg) =>
            {
                Debug.WriteLine(String.Format("{0}: {1}", channel, msg + msgs++));
                await subscription.UnSubscribeFromChannelsMatchingAsync(channels);
            };

            ThreadPool.QueueUserWorkItem(async x =>
            {
                await Task.Delay(100); // to be sure that we have subscribers

                await using var redisClient = CreateRedisClient().ForAsyncOnly();
                Log("Publishing msg...");
                await redisClient.PublishMessageAsync(PrefixedKey("CHANNEL5:BODY"), "hello"); // .ToUtf8Bytes()
            });

            Log("Start Listening On");
            await subscription.SubscribeToChannelsMatchingAsync(channels);
        }

    }
}