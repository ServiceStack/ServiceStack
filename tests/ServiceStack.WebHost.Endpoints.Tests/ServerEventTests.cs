using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/channels/{Channel}/chat")]
    public class PostChatToChannel : IReturn<ChatMessage>
    {
        public string From { get; set; }
        public string ToUserId { get; set; }
        public string Channel { get; set; }
        public string Message { get; set; }
        public string Selector { get; set; }
    }

    public class ChatMessage
    {
        public long Id { get; set; }
        public string FromUserId { get; set; }
        public string FromName { get; set; }
        public string DisplayName { get; set; }
        public string Message { get; set; }
        public string UserAuthId { get; set; }
        public bool Private { get; set; }
    }

    [Route("/channels/{Channel}/raw")]
    public class PostRawToChannel : IReturnVoid
    {
        public string From { get; set; }
        public string ToUserId { get; set; }
        public string Channel { get; set; }
        public string Message { get; set; }
        public string Selector { get; set; }
    }

    [Route("/channels/{Channel}/object")]
    public class PostObjectToChannel
    {
        public string ToUserId { get; set; }
        public string Channel { get; set; }
        public string Selector { get; set; }

        public CustomType CustomType { get; set; }
        public SetterType SetterType { get; set; }
    }

    public class CustomType
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SetterType
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ServerEventsService : Service
    {
        private static long msgId;

        public IServerEvents ServerEvents { get; set; }

        public object Any(PostChatToChannel request)
        {
            var sub = ServerEvents.GetSubscriptionInfo(request.From);
            if (sub == null)
                throw HttpError.NotFound("Subscription {0} does not exist".Fmt(request.From));

            var msg = new ChatMessage
            {
                Id = Interlocked.Increment(ref msgId),
                FromUserId = sub.UserId,
                FromName = sub.DisplayName,
                Message = request.Message,
            };

            if (request.ToUserId != null)
            {
                msg.Private = true;
                ServerEvents.NotifyUserId(request.ToUserId, request.Selector, msg);
                var toSubs = ServerEvents.GetSubscriptionInfosByUserId(request.ToUserId);
                foreach (var toSub in toSubs)
                {
                    msg.Message = "@{0}: {1}".Fmt(toSub.DisplayName, msg.Message);
                    ServerEvents.NotifySubscription(request.From, request.Selector, msg);
                }
            }
            else
            {
                ServerEvents.NotifyChannel(request.Channel, request.Selector, msg);
            }

            return msg;
        }

        public void Any(PostRawToChannel request)
        {
            var sub = ServerEvents.GetSubscriptionInfo(request.From);
            if (sub == null)
                throw HttpError.NotFound("Subscription {0} does not exist".Fmt(request.From));

            if (request.ToUserId != null)
            {
                ServerEvents.NotifyUserId(request.ToUserId, request.Selector, request.Message);
            }
            else
            {
                ServerEvents.NotifyChannel(request.Channel, request.Selector, request.Message);
            }
        }

        public void Any(PostObjectToChannel request)
        {
            if (request.ToUserId != null)
            {
                if (request.CustomType != null)
                    ServerEvents.NotifyUserId(request.ToUserId, request.Selector ?? Selector.Id<CustomType>(), request.CustomType);
                if (request.SetterType != null)
                    ServerEvents.NotifyUserId(request.ToUserId, request.Selector ?? Selector.Id<SetterType>(), request.SetterType);
            }
            else
            {
                if (request.CustomType != null)
                    ServerEvents.NotifyChannel(request.Channel, request.Selector ?? Selector.Id<CustomType>(), request.CustomType);
                if (request.SetterType != null)
                    ServerEvents.NotifyChannel(request.Channel, request.Selector ?? Selector.Id<SetterType>(), request.SetterType);
            }
        }
    }

    public class ServerEventsAppHost : AppSelfHostBase
    {
        public ServerEventsAppHost()
            : base(typeof(ServerEventsAppHost).Name, typeof(ServerEventsAppHost).Assembly) { }

        public bool UseRedisServerEvents { get; set; }

        public override void Configure(Container container)
        {
            Plugins.Add(new ServerEventsFeature
            {
                HeartbeatInterval = TimeSpan.FromMilliseconds(200),
            });

            if (UseRedisServerEvents)
            {
                container.Register<IRedisClientsManager>(new PooledRedisClientManager());

                container.Register<IServerEvents>(c =>
                    new RedisServerEvents(c.Resolve<IRedisClientsManager>()));

                container.Resolve<IServerEvents>().Start();
            }
        }
    }

    [TestFixture]
    public class MemoryServerEventsTests : ServerEventsTests
    {
        protected override ServiceStackHost CreateAppHost()
        {
            return new ServerEventsAppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }
    }

    //[Explicit("Remove from autorunning in CI for now")]
    [TestFixture]
    public class RedisServerEventsTests : ServerEventsTests
    {
        protected override ServiceStackHost CreateAppHost()
        {
            return new ServerEventsAppHost { UseRedisServerEvents = true }
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }
    }

    public abstract class ServerEventsTests
    {
        private ServiceStackHost appHost;

        public ServerEventsTests()
        {
            //LogManager.LogFactory = new ConsoleLogFactory();
            appHost = CreateAppHost();
        }

        protected abstract ServiceStackHost CreateAppHost();

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            var serverEvents = appHost.TryResolve<IServerEvents>();
            serverEvents.Reset();
        }

        private static ServerEventsClient CreateServerEventsClient()
        {
            var client = new ServerEventsClient(Config.AbsoluteBaseUri);
            return client;
        }

        [Test]
        public async void Can_connect_to_ServerEventsStream()
        {
            using (var client = CreateServerEventsClient().Start())
            {
                var task = client.Connect();
                var connectMsg = await task.WaitAsync();

                Assert.That(connectMsg.HeartbeatUrl, Is.StringStarting(Config.AbsoluteBaseUri));
                Assert.That(connectMsg.UnRegisterUrl, Is.StringStarting(Config.AbsoluteBaseUri));
                Assert.That(connectMsg.HeartbeatIntervalMs, Is.GreaterThan(0));
            }
        }

        [Test]
        public async void Does_fire_onJoin_events()
        {
            using (var client = CreateServerEventsClient().Start())
            {
                var taskConnect = client.Connect();
                var taskMsg = client.WaitForNextCommand();

                var connectMsg = await taskConnect.WaitAsync();
                Assert.That(connectMsg.HeartbeatUrl, Is.StringStarting(Config.AbsoluteBaseUri));

                var joinMsg = (ServerEventJoin)await taskMsg.WaitAsync();
                Assert.That(joinMsg.DisplayName, Is.EqualTo(client.ConnectionInfo.DisplayName));
            }
        }

        [Test]
        public async void Does_fire_all_callbacks()
        {
            using (var client1 = CreateServerEventsClient())
            {
                ServerEventConnect connectMsg = null;
                var msgs = new List<ServerEventMessage>();
                var commands = new List<ServerEventMessage>();
                var errors = new List<Exception>();

                client1.OnConnect = e => connectMsg = e;
                client1.OnCommand = commands.Add;
                client1.OnMessage = msgs.Add;
                client1.OnException = errors.Add;

                //Pop Connect + onJoin messages off
                var taskConnect = client1.Connect();
                var taskCmd = client1.WaitForNextCommand();

                await taskConnect.WaitAsync();
                await taskCmd.WaitAsync();

                var joinMsg = commands.OfType<ServerEventJoin>().FirstOrDefault();

                Assert.That(connectMsg, Is.Not.Null, "connectMsg == null");
                Assert.That(joinMsg, Is.Not.Null, "joinMsg == null");

                Assert.That(msgs.Count, Is.EqualTo(0));
                Assert.That(errors.Count, Is.EqualTo(0));
                Assert.That(commands.Count, Is.EqualTo(1)); //join

                commands.Clear();

                "New Client....".Print();
                taskCmd = client1.WaitForNextCommand();

                using (var client2 = CreateServerEventsClient())
                {
                    var connectMsg2 = await client2.Connect();

                    if (taskCmd != await Task.WhenAny(taskCmd, Task.Delay(2000)))
                        throw new TimeoutException();

                    joinMsg = commands.OfType<ServerEventJoin>().FirstOrDefault();

                    taskCmd = client1.WaitForNextCommand();

                    connectMsg2.UnRegisterUrl.GetStringFromUrl(); //unsubscribe 2nd client
                }

                await taskCmd.WaitAsync();

                var leaveMsg = commands.OfType<ServerEventLeave>().FirstOrDefault();

                Assert.That(joinMsg, Is.Not.Null, "joinMsg == null");  //2nd connection
                Assert.That(leaveMsg, Is.Not.Null, "leaveMsg == null");
                Assert.That(commands.Count, Is.EqualTo(2)); //join + leave
                Assert.That(errors.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public async Task Does_receive_messages()
        {
            using (var client1 = CreateServerEventsClient())
            using (var client2 = CreateServerEventsClient())
            {
                var msgs1 = new List<ServerEventMessage>();
                var msgs2 = new List<ServerEventMessage>();

                client1.OnMessage = msgs1.Add;
                client2.OnMessage = msgs2.Add;

                await Task.WhenAll(client1.Connect(), client1.WaitForNextCommand()); //connect1 + join1

                "client2.Connect()...".Print();
                await Task.WhenAll(
                    client2.Connect(), client2.WaitForNextCommand(), //connect2 + join2
                    client1.WaitForNextCommand()); //join2

                "Waiting for Msg1...".Print();
                var taskMsg1 = client1.WaitForNextMessage();
                var taskMsg2 = client2.WaitForNextMessage();

                var info1 = client1.ConnectionInfo;
                client1.PostChat("hello from client1");

                var msg1 = await taskMsg1.WaitAsync();
                var msg2 = await taskMsg2.WaitAsync();

                Assert.That(msg1.EventId, Is.GreaterThan(0));
                Assert.That(msg2.EventId, Is.GreaterThan(0));
                Assert.That(msg1.Selector, Is.EqualTo("cmd.chat"));
                Assert.That(msg2.Selector, Is.EqualTo("cmd.chat"));

                var chatMsg1 = msg1.Json.FromJson<ChatMessage>();
                Assert.That(chatMsg1.Id, Is.GreaterThan(0));
                Assert.That(chatMsg1.FromUserId, Is.EqualTo(info1.UserId)); //-1 / anon user
                Assert.That(chatMsg1.FromName, Is.EqualTo(info1.DisplayName)); //user1 / anon user
                Assert.That(chatMsg1.Message, Is.EqualTo("hello from client1"));

                var chatMsg2 = msg2.Json.FromJson<ChatMessage>();
                Assert.That(chatMsg2.Id, Is.GreaterThan(0));
                Assert.That(chatMsg2.FromUserId, Is.EqualTo(info1.UserId));
                Assert.That(chatMsg2.FromName, Is.EqualTo(info1.DisplayName));
                Assert.That(chatMsg2.Message, Is.EqualTo("hello from client1"));

                Assert.That(msgs1.Count, Is.EqualTo(1));
                Assert.That(msgs2.Count, Is.EqualTo(1));

                "Waiting for Msg2...".Print();
                taskMsg1 = client1.WaitForNextMessage();
                taskMsg2 = client2.WaitForNextMessage();

                var info2 = client2.ConnectionInfo;
                client2.PostChat("hello from client2");

                msg1 = await taskMsg1.WaitAsync();
                msg2 = await taskMsg2.WaitAsync();

                chatMsg1 = msg1.Json.FromJson<ChatMessage>();
                Assert.That(chatMsg1.FromUserId, Is.EqualTo(info2.UserId));
                Assert.That(chatMsg1.FromName, Is.EqualTo(info2.DisplayName));
                Assert.That(chatMsg1.Message, Is.EqualTo("hello from client2"));

                chatMsg2 = msg2.Json.FromJson<ChatMessage>();
                Assert.That(chatMsg2.FromUserId, Is.EqualTo(info2.UserId));
                Assert.That(chatMsg2.FromName, Is.EqualTo(info2.DisplayName));
                Assert.That(chatMsg2.Message, Is.EqualTo("hello from client2"));

                Assert.That(msgs1.Count, Is.EqualTo(2));
                Assert.That(msgs2.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task Does_send_multiple_heartbeats()
        {
            using (var client1 = CreateServerEventsClient())
            {
                var heartbeats = 0;
                var tcs = new TaskCompletionSource<object>();
                client1.OnHeartbeat = () =>
                {
                    //configured to 1s interval in AppHost
                    if (heartbeats++ == 2)
                        tcs.SetResult(null);
                };
                client1.Start();

                await tcs.Task.WaitAsync();

                Assert.That(heartbeats, Is.GreaterThanOrEqualTo(2));
            }
        }

        private static void EnsureSynchronizationContext()
        {
            if (SynchronizationContext.Current != null) return;

            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        }

        [Test]
        public async Task GetStringFromUrlAsync_does_throw_error()
        {
            EnsureSynchronizationContext();

            var heartbeatUrl = Config.AbsoluteBaseUri.CombineWith("event-heartbeat")
                .AddQueryParam("id", "unknown");

            var task = heartbeatUrl.GetStringFromUrlAsync()
            .Success(t =>
            {
                "Was success".Print();
                Assert.Fail("Should Error");
            })
            .Error(ex =>
            {
                "Was error".Print();
            })
            .ContinueWith(t =>
            {
                "was cancelled".Print();
                Assert.Fail("Should Error");
            }, TaskContinuationOptions.OnlyOnCanceled)
            ;

            if (task != await Task.WhenAny(task, Task.Delay(2000)))
                throw new TimeoutException();
        }

        [Test]
        public async Task Does_reconnect_on_lost_connection()
        {
            try
            {
                using (var client1 = CreateServerEventsClient())
                {
                    var serverEvents = appHost.TryResolve<IServerEvents>();
                    var msgs = new List<ServerEventMessage>();

                    client1.OnMessage = msgs.Add;

                    await client1.Connect();

                    var msgTask = client1.WaitForNextMessage();

                    client1.PostChat("msg1 from client1");

                    var msg1 = await msgTask.WaitAsync();

                    msgTask = client1.WaitForNextMessage();

                    serverEvents.Reset(); //Dispose all existing subscriptions

                    using (var client2 = CreateServerEventsClient())
                    {
                        await client2.Connect();

                        await Task.WhenAny(client1.Connect(), Task.Delay(1000));

                        client2.PostChat("msg2 from client2");
                    }

                    "Waiting for 30s...".Print();
                    var msg2 = await msgTask.WaitAsync(2000);

                    var chatMsg2 = msg2.Json.FromJson<ChatMessage>();

                    Assert.That(chatMsg2.Message, Is.EqualTo("msg2 from client2"));
                }
            }
            catch (Exception ex)
            {
                ex.Message.Print();
                throw;
            }
        }

        [Test]
        public async Task Does_send_message_to_Handler()
        {
            using (var client1 = CreateServerEventsClient())
            {
                await client1.Connect();

                ChatMessage chatMsg = null;
                client1.Handlers["chat"] = (client, msg) =>
                {
                    chatMsg = msg.Json.FromJson<ChatMessage>();
                };

                var msgTask = client1.WaitForNextMessage();
                client1.PostChat("msg1");
                await msgTask.WaitAsync();

                Assert.That(chatMsg, Is.Not.Null);
                Assert.That(chatMsg.Message, Is.EqualTo("msg1"));

                msgTask = client1.WaitForNextMessage();
                client1.PostChat("msg2");
                await msgTask.WaitAsync();

                Assert.That(chatMsg, Is.Not.Null);
                Assert.That(chatMsg.Message, Is.EqualTo("msg2"));
            }
        }

        [Test]
        public async Task Does_send_message_to_named_receiver()
        {
            using (var client1 = CreateServerEventsClient())
            {
                client1.RegisterNamedReceiver<TestNamedReceiver>("test");

                await client1.Connect();

                var msgTask = client1.WaitForNextMessage();
                client1.Post(new CustomType { Id = 1, Name = "Foo" }, "test.FooMethod");
                await msgTask.WaitAsync();

                var foo = TestNamedReceiver.FooMethodReceived;
                Assert.That(foo, Is.Not.Null);
                Assert.That(foo.Id, Is.EqualTo(1));
                Assert.That(foo.Name, Is.EqualTo("Foo"));

                msgTask = client1.WaitForNextMessage();
                client1.Post(new CustomType { Id = 2, Name = "Bar" }, "test.BarMethod");
                await msgTask.WaitAsync();

                var bar = TestNamedReceiver.BarMethodReceived;
                Assert.That(bar, Is.Not.Null);
                Assert.That(bar.Id, Is.EqualTo(2));
                Assert.That(bar.Name, Is.EqualTo("Bar"));

                msgTask = client1.WaitForNextMessage();
                client1.Post(new CustomType { Id = 3, Name = "Baz" }, "test.BazMethod");
                await msgTask.WaitAsync();

                var baz = TestNamedReceiver.NoSuchMethodReceived;
                Assert.That(baz, Is.Not.Null);
                Assert.That(baz.Id, Is.EqualTo(3));
                Assert.That(baz.Name, Is.EqualTo("Baz"));
                Assert.That(TestNamedReceiver.NoSuchMethodSelector, Is.EqualTo("BazMethod"));
            }
        }

        [Test]
        public async Task Does_send_message_to_global_receiver()
        {
            using (var client1 = CreateServerEventsClient())
            {
                client1.RegisterReceiver<TestGlobalReceiver>();

                await client1.Connect();

                var msgTask = client1.WaitForNextMessage();
                client1.Post(new CustomType { Id = 1, Name = "Foo" });
                await msgTask.WaitAsync();

                var foo = TestGlobalReceiver.FooMethodReceived;
                Assert.That(foo, Is.Not.Null);
                Assert.That(foo.Id, Is.EqualTo(1));
                Assert.That(foo.Name, Is.EqualTo("Foo"));
            }
        }

        [Test]
        public async Task Does_set_properties_on_global_receiver()
        {
            using (var client1 = CreateServerEventsClient())
            {
                client1.RegisterReceiver<TestGlobalReceiver>();

                await client1.Connect();

                var msgTask = client1.WaitForNextMessage();
                client1.Post(new SetterType { Id = 1, Name = "Foo" });
                await msgTask.WaitAsync();

                var foo = TestGlobalReceiver.AnyNamedSetterReceived;
                Assert.That(foo, Is.Not.Null);
                Assert.That(foo.Id, Is.EqualTo(1));
                Assert.That(foo.Name, Is.EqualTo("Foo"));
            }
        }

        [Test]
        public async Task Does_send_raw_string_messages()
        {
            using (var client1 = CreateServerEventsClient())
            {
                client1.RegisterReceiver<TestJavaScriptReceiver>();
                client1.RegisterNamedReceiver<TestJavaScriptReceiver>("css");

                await client1.Connect();

                var msgTask = client1.WaitForNextMessage();
                client1.PostChat("chat msg");
                await msgTask.WaitAsync();

                var chatMsg = TestJavaScriptReceiver.ChatReceived;
                Assert.That(chatMsg, Is.Not.Null);
                Assert.That(chatMsg.Message, Is.EqualTo("chat msg"));

                msgTask = client1.WaitForNextMessage();
                client1.PostRaw("cmd.announce", "This is your captain speaking...");
                await msgTask.WaitAsync();

                var announce = TestJavaScriptReceiver.AnnounceReceived;
                Assert.That(announce, Is.EqualTo("This is your captain speaking..."));

                msgTask = client1.WaitForNextMessage();
                client1.PostRaw("cmd.toggle$#channels", null);
                await msgTask.WaitAsync();

                var toggle = TestJavaScriptReceiver.ToggleReceived;
                Assert.That(toggle, Is.EqualTo(""));
                var toggleRequest = TestJavaScriptReceiver.ToggleRequestReceived;
                Assert.That(toggleRequest.Selector, Is.EqualTo("cmd.toggle$#channels"));
                Assert.That(toggleRequest.Op, Is.EqualTo("cmd"));
                Assert.That(toggleRequest.Target, Is.EqualTo("toggle"));
                Assert.That(toggleRequest.CssSelector, Is.EqualTo("#channels"));

                msgTask = client1.WaitForNextMessage();
                client1.PostRaw("css.background-image$#top", "url(http://bit.ly/1yIJOBH)");
                await msgTask.WaitAsync();

                var bgImage = TestJavaScriptReceiver.BackgroundImageReceived;
                Assert.That(bgImage, Is.EqualTo("url(http://bit.ly/1yIJOBH)"));
                var bgImageRequest = TestJavaScriptReceiver.BackgroundImageRequestReceived;
                Assert.That(bgImageRequest.Selector, Is.EqualTo("css.background-image$#top"));
                Assert.That(bgImageRequest.Op, Is.EqualTo("css"));
                Assert.That(bgImageRequest.Target, Is.EqualTo("background-image"));
                Assert.That(bgImageRequest.CssSelector, Is.EqualTo("#top"));
            }
        }

        [Test]
        public async Task Can_reuse_same_instance()
        {
            using (var client1 = CreateServerEventsClient())
            {
                client1.RegisterReceiver<TestJavaScriptReceiver>();
                client1.RegisterNamedReceiver<TestJavaScriptReceiver>("css");
                client1.Resolver = new SingletonInstanceResolver();

                await client1.Connect();

                var msgTask = client1.WaitForNextMessage();
                client1.PostRaw("cmd.announce", "This is your captain speaking...");
                await msgTask.WaitAsync();

                var instance = client1.Resolver.TryResolve<TestJavaScriptReceiver>();
                Assert.That(instance.AnnounceInstance, Is.EqualTo("This is your captain speaking..."));

                msgTask = client1.WaitForNextMessage();
                client1.PostRaw("cmd.announce", "2nd Announcement");
                await msgTask.WaitAsync();

                Assert.That(instance.AnnounceInstance, Is.EqualTo("2nd Announcement"));
            }
        }

        [Test]
        public async Task Can_use_IOC_to_autowire_Receivers()
        {
            using (var client1 = CreateServerEventsClient())
            {
                client1.RegisterReceiver<TestContainerReceiver>();

                var container = new Container();
                container.RegisterAs<Dependency, IDependency>();
                container.RegisterAutoWiredTypes(client1.ReceiverTypes);

                client1.Resolver = container;

                await client1.Connect();

                var msgTask = client1.WaitForNextMessage();
                client1.Post(new CustomType { Id = 1, Name = "Foo" });
                await msgTask.WaitAsync();

                var instance = (Dependency)container.Resolve<IDependency>();
                var customType = instance.CustomTypeReceived;
                Assert.That(customType, Is.Not.Null);
                Assert.That(customType.Id, Is.EqualTo(1));
                Assert.That(customType.Name, Is.EqualTo("Foo"));

                msgTask = client1.WaitForNextMessage();
                client1.Post(new SetterType { Id = 2, Name = "Bar" });
                await msgTask.WaitAsync();

                var setterType = instance.SetterTypeReceived;
                Assert.That(setterType, Is.Not.Null);
                Assert.That(setterType.Id, Is.EqualTo(2));
                Assert.That(setterType.Name, Is.EqualTo("Bar"));
            }
        }
    }

    public class TestNamedReceiver : ServerEventReceiver
    {
        public static CustomType FooMethodReceived;
        public static CustomType BarMethodReceived;
        public static CustomType NoSuchMethodReceived;
        public static string NoSuchMethodSelector;

        public void FooMethod(CustomType request)
        {
            FooMethodReceived = request;
        }

        public CustomType BarMethod(CustomType request)
        {
            BarMethodReceived = request;
            return request;
        }

        public override void NoSuchMethod(string selector, object message)
        {
            var msg = (ServerEventMessage)message;
            NoSuchMethodReceived = msg.Json.FromJson<CustomType>();
            NoSuchMethodSelector = selector;
        }
    }

    public class TestGlobalReceiver : ServerEventReceiver
    {
        public static CustomType FooMethodReceived;
        public static CustomType NoSuchMethodReceived;
        public static string NoSuchMethodSelector;

        internal static SetterType AnyNamedSetterReceived;

        public SetterType AnyNamedSetter
        {
            set { AnyNamedSetterReceived = value; }
        }

        public void AnyNamedMethod(CustomType request)
        {
            FooMethodReceived = request;
        }

        public override void NoSuchMethod(string selector, object message)
        {
            var msg = (ServerEventMessage)message;
            NoSuchMethodReceived = msg.Json.FromJson<CustomType>();
            NoSuchMethodSelector = selector;
        }
    }

    public class TestJavaScriptReceiver : ServerEventReceiver
    {
        public static ChatMessage ChatReceived;
        public static string AnnounceReceived;
        public string AnnounceInstance;
        public static string ToggleReceived;
        public static ServerEventMessage ToggleRequestReceived;
        public static string BackgroundImageReceived;
        public static ServerEventMessage BackgroundImageRequestReceived;

        public void Chat(ChatMessage message)
        {
            ChatReceived = message;
        }

        public void Announce(string message)
        {
            AnnounceReceived = message;
            AnnounceInstance = message;
        }

        public void Toggle(string message)
        {
            ToggleReceived = message;
            ToggleRequestReceived = Request;
        }

        public void BackgroundImage(string cssRule)
        {
            BackgroundImageReceived = cssRule;
            BackgroundImageRequestReceived = Request;
        }
    }

    public class ContainerResolver : IResolver
    {
        private readonly Container container;

        public ContainerResolver(Container container)
        {
            this.container = container;
        }

        public T TryResolve<T>()
        {
            return container.TryResolve<T>();
        }
    }

    public interface IDependency
    {
        void Record(CustomType msg);
        void Record(SetterType msg);
    }

    class Dependency : IDependency
    {
        public CustomType CustomTypeReceived;
        public SetterType SetterTypeReceived;

        public void Record(CustomType msg)
        {
            CustomTypeReceived = msg;
        }

        public void Record(SetterType msg)
        {
            SetterTypeReceived = msg;
        }
    }

    public class TestContainerReceiver : ServerEventReceiver
    {
        public IDependency Dependency { get; set; }

        public void AnyNamedMethod(CustomType request)
        {
            Dependency.Record(request);
        }

        public void AnySetter(SetterType request)
        {
            Dependency.Record(request);
        }
    }

    public static class ServerClientExtensions
    {
        public static void PostChat(this ServerEventsClient client,
            string message, string channel = null)
        {
            client.ServiceClient.Post(new PostChatToChannel
            {
                From = client.SubscriptionId,
                Message = message,
                Channel = channel ?? EventSubscription.UnknownChannel,
                Selector = "cmd.chat",
            });
        }

        public static void PostRaw(this ServerEventsClient client, string selector, string message, string channel = null)
        {
            client.ServiceClient.Post(new PostRawToChannel
            {
                From = client.SubscriptionId,
                Message = message,
                Channel = channel ?? EventSubscription.UnknownChannel,
                Selector = selector,
            });
        }

        public static void Post(this ServerEventsClient client,
            CustomType message, string selector = null, string channel = null)
        {
            client.ServiceClient.Post(new PostObjectToChannel
            {
                CustomType = message,
                Channel = channel ?? EventSubscription.UnknownChannel,
                Selector = selector,
            });
        }

        public static void Post(this ServerEventsClient client,
            SetterType message, string selector = null, string channel = null)
        {
            client.ServiceClient.Post(new PostObjectToChannel
            {
                SetterType = message,
                Channel = channel ?? EventSubscription.UnknownChannel,
                Selector = selector,
            });
        }

        public static async Task<T> WaitAsync<T>(this Task<T> task, int timeMs = 1000)
        {
            if (task != await Task.WhenAny(task, Task.Delay(timeMs)))
                throw new TimeoutException();

            return await task;
        }
    }
}