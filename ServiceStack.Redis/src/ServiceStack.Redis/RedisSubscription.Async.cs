using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis
{
    partial class RedisSubscription
        : IRedisSubscriptionAsync
    {
        // private events here for +/- semantics
        private event Func<string, ValueTask> OnSubscribeAsync;
        private event Func<string, string, ValueTask> OnMessageAsync;
        private event Func<string, byte[], ValueTask> OnMessageBytesAsync;
        private event Func<string, ValueTask> OnUnSubscribeAsync;

        event Func<string, ValueTask> IRedisSubscriptionAsync.OnSubscribeAsync
        {
            add => OnSubscribeAsync += value;
            remove => OnSubscribeAsync -= value;
        }
        event Func<string, string, ValueTask> IRedisSubscriptionAsync.OnMessageAsync
        {
            add => OnMessageAsync += value;
            remove => OnMessageAsync -= value;
        }
        event Func<string, byte[], ValueTask> IRedisSubscriptionAsync.OnMessageBytesAsync
        {
            add => OnMessageBytesAsync += value;
            remove => OnMessageBytesAsync -= value;
        }
        event Func<string, ValueTask> IRedisSubscriptionAsync.OnUnSubscribeAsync
        {
            add => OnUnSubscribeAsync += value;
            remove => OnUnSubscribeAsync -= value;
        }

        private IRedisSubscriptionAsync AsAsync() => this;
        private IRedisNativeClientAsync NativeAsync
        {
            get
            {
                return redisClient as IRedisNativeClientAsync ?? NotAsync();
                static IRedisNativeClientAsync NotAsync() => throw new InvalidOperationException("The underlying client is not async");
            }
        }

        private async ValueTask UnSubscribeFromAllChannelsMatchingAnyPatternsAsync(CancellationToken token = default)
        {
            if (activeChannels.Count == 0) return;

            var multiBytes = await NativeAsync.PUnSubscribeAsync(Array.Empty<string>(), token).ConfigureAwait(false);
            await ParseSubscriptionResultsAsync(multiBytes).ConfigureAwait(false);

            this.activeChannels = new List<string>();
        }

        ValueTask IAsyncDisposable.DisposeAsync() => IsPSubscription
                ? UnSubscribeFromAllChannelsMatchingAnyPatternsAsync()
                : AsAsync().UnSubscribeFromAllChannelsAsync();

        async ValueTask IRedisSubscriptionAsync.SubscribeToChannelsAsync(string[] channels, CancellationToken token)
        {
            var multiBytes = await NativeAsync.SubscribeAsync(channels, token).ConfigureAwait(false);
            await ParseSubscriptionResultsAsync(multiBytes).ConfigureAwait(false);

            while (this.SubscriptionCount > 0)
            {
                multiBytes = await NativeAsync.ReceiveMessagesAsync(token).ConfigureAwait(false);
                await ParseSubscriptionResultsAsync(multiBytes).ConfigureAwait(false);
            }
        }

        async ValueTask IRedisSubscriptionAsync.SubscribeToChannelsMatchingAsync(string[] patterns, CancellationToken token)
        {
            var multiBytes = await NativeAsync.PSubscribeAsync(patterns, token).ConfigureAwait(false);
            await ParseSubscriptionResultsAsync(multiBytes).ConfigureAwait(false);

            while (this.SubscriptionCount > 0)
            {
                multiBytes = await NativeAsync.ReceiveMessagesAsync(token).ConfigureAwait(false);
                await ParseSubscriptionResultsAsync(multiBytes).ConfigureAwait(false);
            }
        }

        async ValueTask IRedisSubscriptionAsync.UnSubscribeFromAllChannelsAsync(CancellationToken token)
        {
            if (activeChannels.Count == 0) return;

            var multiBytes = await NativeAsync.UnSubscribeAsync(Array.Empty<string>(), token).ConfigureAwait(false);
            await ParseSubscriptionResultsAsync(multiBytes).ConfigureAwait(false);

            this.activeChannels = new List<string>();
        }

        async ValueTask IRedisSubscriptionAsync.UnSubscribeFromChannelsAsync(string[] channels, CancellationToken token)
        {
            var multiBytes = await NativeAsync.UnSubscribeAsync(channels, token).ConfigureAwait(false);
            await ParseSubscriptionResultsAsync(multiBytes).ConfigureAwait(false);
        }

        async ValueTask IRedisSubscriptionAsync.UnSubscribeFromChannelsMatchingAsync(string[] patterns, CancellationToken token)
        {
            var multiBytes = await NativeAsync.PUnSubscribeAsync(patterns, token).ConfigureAwait(false);
            await ParseSubscriptionResultsAsync(multiBytes).ConfigureAwait(false);
        }

        private async ValueTask ParseSubscriptionResultsAsync(byte[][] multiBytes)
        {
            int componentsPerMsg = IsPSubscription ? 4 : 3;
            for (var i = 0; i < multiBytes.Length; i += componentsPerMsg)
            {
                var messageType = multiBytes[i];
                var channel = multiBytes[i + 1].FromUtf8Bytes();
                if (SubscribeWord.AreEqual(messageType)
                    || PSubscribeWord.AreEqual(messageType))
                {
                    IsPSubscription = PSubscribeWord.AreEqual(messageType);

                    this.SubscriptionCount = int.Parse(multiBytes[i + MsgIndex].FromUtf8Bytes());

                    activeChannels.Add(channel);

                    var tmp = OnSubscribeAsync;
                    if (tmp is object) await tmp.Invoke(channel).ConfigureAwait(false);
                }
                else if (UnSubscribeWord.AreEqual(messageType)
                    || PUnSubscribeWord.AreEqual(messageType))
                {
                    this.SubscriptionCount = int.Parse(multiBytes[i + 2].FromUtf8Bytes());

                    activeChannels.Remove(channel);

                    var tmp = OnUnSubscribeAsync;
                    if (tmp is object) await tmp.Invoke(channel).ConfigureAwait(false);
                }
                else if (MessageWord.AreEqual(messageType))
                {
                    var msgBytes = multiBytes[i + MsgIndex];
                    var tmp1 = OnMessageBytesAsync;
                    if (tmp1 is object) await tmp1.Invoke(channel, msgBytes).ConfigureAwait(false);

                    var tmp2 = OnMessageAsync;
                    if (tmp2 is object)
                    {
                        var message = msgBytes.FromUtf8Bytes();
                        await tmp2.Invoke(channel, message).ConfigureAwait(false);
                    }
                }
                else if (PMessageWord.AreEqual(messageType))
                {
                    channel = multiBytes[i + 2].FromUtf8Bytes();
                    var msgBytes = multiBytes[i + MsgIndex + 1];
                    var tmp1 = OnMessageBytesAsync;
                    if (tmp1 is object) await tmp1.Invoke(channel, msgBytes).ConfigureAwait(false);

                    var tmp2 = OnMessageAsync;
                    if (tmp2 is object)
                    {
                        var message = msgBytes.FromUtf8Bytes();
                        await tmp2.Invoke(channel, message).ConfigureAwait(false);
                    }
                }
                else
                {
                    throw new RedisException(
                        "Invalid state. Expected [[p]subscribe|[p]unsubscribe|message] got: " + messageType.FromUtf8Bytes());
                }
            }
        }

        ValueTask IRedisSubscriptionAsync.SubscribeToChannelsAsync(params string[] channels)
            => AsAsync().SubscribeToChannelsAsync(channels, token: default);

        ValueTask IRedisSubscriptionAsync.SubscribeToChannelsMatchingAsync(params string[] patterns)
            => AsAsync().SubscribeToChannelsMatchingAsync(patterns, token: default);

        ValueTask IRedisSubscriptionAsync.UnSubscribeFromChannelsAsync(params string[] channels)
            => AsAsync().UnSubscribeFromChannelsAsync(channels, token: default);

        ValueTask IRedisSubscriptionAsync.UnSubscribeFromChannelsMatchingAsync(params string[] patterns)
            => AsAsync().UnSubscribeFromChannelsMatchingAsync(patterns, token: default);
    }
}