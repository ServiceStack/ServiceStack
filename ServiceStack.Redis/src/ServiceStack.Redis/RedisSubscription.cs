using System;
using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
    public partial class RedisSubscription
        : IRedisSubscription
    {
        private readonly IRedisNativeClient redisClient;
        private List<string> activeChannels;
		public long SubscriptionCount { get; private set; }
        public bool IsPSubscription { get; private set; }

        private const int MsgIndex = 2;
        private static readonly byte[] SubscribeWord = "subscribe".ToUtf8Bytes();
        private static readonly byte[] PSubscribeWord = "psubscribe".ToUtf8Bytes();
        private static readonly byte[] UnSubscribeWord = "unsubscribe".ToUtf8Bytes();
        private static readonly byte[] PUnSubscribeWord = "punsubscribe".ToUtf8Bytes();
        private static readonly byte[] MessageWord = "message".ToUtf8Bytes();
        private static readonly byte[] PMessageWord = "pmessage".ToUtf8Bytes();

        public RedisSubscription(IRedisNativeClient redisClient)
        {
            this.redisClient = redisClient;

            this.SubscriptionCount = 0;
            this.activeChannels = new List<string>();
        }

        public Action<string> OnSubscribe { get; set; }
        public Action<string, string> OnMessage { get; set; }
        public Action<string, byte[]> OnMessageBytes { get; set; }
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
            var multiBytes = redisClient.PSubscribe(patterns);
            ParseSubscriptionResults(multiBytes);

            while (this.SubscriptionCount > 0)
            {
                multiBytes = redisClient.ReceiveMessages();
                ParseSubscriptionResults(multiBytes);
            }
        }

        private void ParseSubscriptionResults(byte[][] multiBytes)
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

                    this.OnSubscribe?.Invoke(channel);
                }
                else if (UnSubscribeWord.AreEqual(messageType)
                    || PUnSubscribeWord.AreEqual(messageType))
                {
                    this.SubscriptionCount = int.Parse(multiBytes[i + 2].FromUtf8Bytes());

                    activeChannels.Remove(channel);

                    this.OnUnSubscribe?.Invoke(channel);
                }
                else if (MessageWord.AreEqual(messageType))
                {
                    var msgBytes = multiBytes[i + MsgIndex];
                    this.OnMessageBytes?.Invoke(channel, msgBytes);

                    var message = msgBytes.FromUtf8Bytes();
                    this.OnMessage?.Invoke(channel, message);
                }
                else if (PMessageWord.AreEqual(messageType))
                {
                    channel = multiBytes[i + 2].FromUtf8Bytes();
                    var msgBytes = multiBytes[i + MsgIndex + 1];
                    this.OnMessageBytes?.Invoke(channel, msgBytes);

                    var message = msgBytes.FromUtf8Bytes();
                    this.OnMessage?.Invoke(channel, message);
                }
                else
                {
                    throw new RedisException(
                        "Invalid state. Expected [[p]subscribe|[p]unsubscribe|message] got: " + messageType.FromUtf8Bytes());
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

        public void UnSubscribeFromAllChannelsMatchingAnyPatterns()
        {
            if (activeChannels.Count == 0) return;

            var multiBytes = redisClient.PUnSubscribe();
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
            var multiBytes = redisClient.PUnSubscribe(patterns);
            ParseSubscriptionResults(multiBytes);
        }

        public void Dispose()
        {
            if (IsPSubscription)
            {
                UnSubscribeFromAllChannelsMatchingAnyPatterns();
            }
            else
            {
                UnSubscribeFromAllChannels();
            }
        }
    }
}