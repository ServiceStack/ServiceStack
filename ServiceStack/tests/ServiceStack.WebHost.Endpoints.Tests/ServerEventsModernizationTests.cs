using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class ServerEventsModernizationTests
{
    private class CustomEventSubscriptionMock : IEventSubscription
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastPulseAt { get; set; } = DateTime.UtcNow;
        public long LastMessageId => 0;
        public string[] Channels { get; set; } = Array.Empty<string>();
        public string[] MergedChannels { get; set; } = Array.Empty<string>();
        public string UserId => "user1";
        public string UserName => "user1";
        public string DisplayName => "User 1";
        public string SessionId => "sess1";
        public string SubscriptionId => "sub1";
        public string UserAddress { get; set; } = "127.0.0.1";
        public bool IsAuthenticated { get; set; } = true;
        public bool IsClosed => false;

        public void UpdateChannels(string[] channels) => Channels = channels;
        public Func<IEventSubscription, Task> OnUnsubscribeAsync { get; set; }
        public Action<IEventSubscription> OnUnsubscribe { get; set; }
        public void Unsubscribe() { }
        public Task UnsubscribeAsync() => TypeConstants.EmptyTask;
        public void Publish(string selector, string message) { }
        public Task PublishAsync(string selector, string message, CancellationToken token = default) => TypeConstants.EmptyTask;
        public void PublishRaw(string frame) { }
        public Task PublishRawAsync(string frame, CancellationToken token = default) => TypeConstants.EmptyTask;
        public void Pulse() => LastPulseAt = DateTime.UtcNow;

        public ConcurrentDictionary<string, string> Meta { get; set; } = new();
        public Dictionary<string, string> ServerArgs { get; set; } = new();
        public Dictionary<string, string> ConnectArgs { get; set; } = new();
        public string JsonArgs => "{}";

        public void Dispose() { }
    }

    [Test]
    public void ServerEventsFeature_Register_handles_null_appHost()
    {
        var feature = new ServerEventsFeature();
        Assert.DoesNotThrow(() => feature.Register(null));
    }

    [Test]
    public void ServerEventsFeature_CanAccessSubscription_guards_null_inputs()
    {
        var feature = new ServerEventsFeature { ValidateUserAddress = true };
        Assert.That(feature.CanAccessSubscription(null, null), Is.True);

        var sub = new SubscriptionInfo { UserAddress = null };
        Assert.That(feature.CanAccessSubscription(null, sub), Is.True);
    }

    [Test]
    public void EventSubscription_SerializeDictionary_handles_null_and_edge_cases()
    {
        Assert.That(EventSubscription.SerializeDictionary(null), Is.Null);

        var map = new Dictionary<string, string>
        {
            { "a", "1" },
            { "b", null },
            { "c", "3" }
        };
        var json = EventSubscription.SerializeDictionary(map);
        Assert.That(json, Does.Contain("\"a\":\"1\""));
        Assert.That(json, Does.Contain("\"c\":\"3\""));
        Assert.That(json, Does.Not.Contain("\"b\""));

        var empty = EventSubscription.SerializeDictionary(new Dictionary<string, string>());
        Assert.That(empty, Is.EqualTo("{}"));
    }

    [Test]
    public void IEventSubscription_IsGrpc_handles_non_EventSubscription_without_throwing()
    {
        var mock = new CustomEventSubscriptionMock();
        Assert.DoesNotThrow(() =>
        {
            var isGrpc = mock.IsGrpc();
            Assert.That(isGrpc, Is.False);
        });

        IEventSubscription nullSub = null;
        Assert.DoesNotThrow(() =>
        {
            var isGrpc = nullSub.IsGrpc();
            Assert.That(isGrpc, Is.False);
        });
    }

    [Test]
    public void IEventSubscription_HasChannel_and_HasAnyChannel_handle_nulls()
    {
        IEventSubscription nullSub = null;
        Assert.That(nullSub.HasChannel("ch1"), Is.False);
        Assert.That(nullSub.HasAnyChannel(new[] { "ch1" }), Is.False);

        var mock = new CustomEventSubscriptionMock { Channels = null };
        Assert.That(mock.HasChannel("ch1"), Is.False);
        Assert.That(mock.HasAnyChannel(new[] { "ch1" }), Is.False);
        Assert.That(mock.HasAnyChannel(null), Is.False);

        mock.Channels = new[] { "ch1", "ch2" };
        Assert.That(mock.HasChannel(null), Is.True);
        Assert.That(mock.HasChannel("ch1"), Is.True);
        Assert.That(mock.HasChannel("ch3"), Is.False);
        Assert.That(mock.HasAnyChannel(new[] { "ch3", "ch2" }), Is.True);
        Assert.That(mock.HasAnyChannel(new string[] { null }), Is.False);
    }

    [Test]
    public async Task MemoryServerEvents_Pulse_and_PulseAsync_handle_null_or_empty()
    {
        using var mse = new MemoryServerEvents();
        Assert.That(mse.Pulse(null), Is.False);
        Assert.That(mse.Pulse(""), Is.False);
        Assert.That(await mse.PulseAsync(null), Is.False);
        Assert.That(await mse.PulseAsync(""), Is.False);
    }

    [Test]
    public async Task MemoryServerEvents_NotifyChannelsAsync_handles_null_or_empty_channels()
    {
        using var mse = new MemoryServerEvents();
        Assert.DoesNotThrowAsync(async () => await mse.NotifyChannelsAsync(null, "sel", "msg"));
        Assert.DoesNotThrowAsync(async () => await mse.NotifyChannelsAsync(Array.Empty<string>(), "sel", "msg"));
        Assert.DoesNotThrowAsync(async () => await mse.NotifyChannelsAsync(new string[] { null, "" }, "sel", "msg"));
    }

    [Test]
    public async Task MemoryServerEvents_FlushNopToChannelsAsync_handles_null_or_empty_channels()
    {
        using var mse = new MemoryServerEvents();
        Assert.DoesNotThrowAsync(async () => await mse.FlushNopToChannelsAsync(null));
        Assert.DoesNotThrowAsync(async () => await mse.FlushNopToChannelsAsync(Array.Empty<string>()));
        Assert.DoesNotThrowAsync(async () => await mse.FlushNopToChannelsAsync(new string[] { null, "" }));
    }

    [Test]
    public void MemoryServerEvents_GetSubscriptionsDetails_handles_null_or_empty_channels()
    {
        using var mse = new MemoryServerEvents();
        var res1 = mse.GetSubscriptionsDetails(null);
        Assert.That(res1, Is.Not.Null);
        Assert.That(res1.Count, Is.EqualTo(0));

        var res2 = mse.GetSubscriptionsDetails(new string[] { null, "" });
        Assert.That(res2, Is.Not.Null);
        Assert.That(res2.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task MemoryServerEvents_UnRegister_and_UnRegisterAsync_handle_null_or_empty()
    {
        using var mse = new MemoryServerEvents();
        Assert.DoesNotThrow(() => mse.UnRegister(null));
        Assert.DoesNotThrow(() => mse.UnRegister(""));
        Assert.DoesNotThrowAsync(async () => await mse.UnRegisterAsync(null));
        Assert.DoesNotThrowAsync(async () => await mse.UnRegisterAsync(""));
    }

    [Test]
    public async Task MemoryServerEvents_RegisterAsync_guards_null_subscription()
    {
        using var mse = new MemoryServerEvents();
        Assert.DoesNotThrowAsync(async () => await mse.RegisterAsync(null));
    }

    [Test]
    public void MemoryServerEvents_SubscribeToChannels_and_UnsubscribeFromChannels_guard_null_arguments()
    {
        using var mse = new MemoryServerEvents();
        Assert.Throws<ArgumentNullException>(() => mse.SubscribeToChannels(null, new[] { "ch1" }));
        Assert.Throws<ArgumentNullException>(() => mse.SubscribeToChannels("sub1", null));
        Assert.Throws<ArgumentNullException>(() => mse.UnsubscribeFromChannels(null, new[] { "ch1" }));
        Assert.Throws<ArgumentNullException>(() => mse.UnsubscribeFromChannels("sub1", null));

        // Sub does not exist: should safely return without error
        Assert.DoesNotThrow(() => mse.SubscribeToChannels("nonexistent", new[] { "ch1", null, "" }));
        Assert.DoesNotThrow(() => mse.UnsubscribeFromChannels("nonexistent", new[] { "ch1", null, "" }));
    }

    [Test]
    public async Task IServerEvents_Notify_extensions_guard_null_server_and_message()
    {
        IServerEvents nullServer = null;
        Assert.DoesNotThrow(() => nullServer.NotifyAll("msg"));
        Assert.DoesNotThrowAsync(async () => await nullServer.NotifyAllAsync("msg"));
        Assert.DoesNotThrow(() => nullServer.NotifyChannel("ch", "msg"));
        Assert.DoesNotThrowAsync(async () => await nullServer.NotifyChannelAsync("ch", "msg"));
        Assert.DoesNotThrow(() => nullServer.NotifySubscription("id", "msg"));
        Assert.DoesNotThrowAsync(async () => await nullServer.NotifySubscriptionAsync("id", "msg"));
        Assert.DoesNotThrow(() => nullServer.NotifyUserId("u", "msg"));
        Assert.DoesNotThrowAsync(async () => await nullServer.NotifyUserIdAsync("u", "msg"));
        Assert.DoesNotThrow(() => nullServer.NotifyUserName("u", "msg"));
        Assert.DoesNotThrowAsync(async () => await nullServer.NotifyUserNameAsync("u", "msg"));
        Assert.DoesNotThrow(() => nullServer.NotifySession("s", "msg"));
        Assert.DoesNotThrowAsync(async () => await nullServer.NotifySessionAsync("s", "msg"));

        using var mse = new MemoryServerEvents();
        Assert.DoesNotThrow(() => mse.NotifyAll(null));
        Assert.DoesNotThrowAsync(async () => await mse.NotifyAllAsync(null));
        Assert.DoesNotThrow(() => mse.NotifyChannel("ch", null));
        Assert.DoesNotThrowAsync(async () => await mse.NotifyChannelAsync("ch", null));
        Assert.DoesNotThrow(() => mse.NotifySubscription("id", null));
        Assert.DoesNotThrowAsync(async () => await mse.NotifySubscriptionAsync("id", null));
        Assert.DoesNotThrow(() => mse.NotifyUserId("u", null));
        Assert.DoesNotThrowAsync(async () => await mse.NotifyUserIdAsync("u", null));
        Assert.DoesNotThrow(() => mse.NotifyUserName("u", null));
        Assert.DoesNotThrowAsync(async () => await mse.NotifyUserNameAsync("u", null));
        Assert.DoesNotThrow(() => mse.NotifySession("s", null));
        Assert.DoesNotThrowAsync(async () => await mse.NotifySessionAsync("s", null));
    }

    [Test]
    public void ServerEventsServices_guard_null_requests()
    {
        using var mse = new MemoryServerEvents();
        var subscribersService = new ServerEventsSubscribersService(mse);
        var subscribers = subscribersService.Any(null);
        Assert.That(subscribers, Is.Not.Null);

        var unRegisterService = new ServerEventsUnRegisterService(mse);
        Assert.ThrowsAsync<ArgumentNullException>(async () => await unRegisterService.Any(null));
        Assert.ThrowsAsync<ArgumentNullException>(async () => await unRegisterService.Any(new UnRegisterEventSubscriber { Id = null }));

        var updateService = new UpdateEventSubscriberService(mse);
        Assert.ThrowsAsync<ArgumentNullException>(async () => await updateService.Any(null));
        Assert.ThrowsAsync<ArgumentNullException>(async () => await updateService.Any(new UpdateEventSubscriber { Id = null }));
    }
}
