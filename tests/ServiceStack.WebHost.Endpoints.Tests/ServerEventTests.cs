using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Logging;
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

    public class ServerEventsService : Service
    {
        private static long msgId;

        public IServerEvents ServerEvents { get; set; }

        public object Any(PostChatToChannel request)
        {
            var sub = ServerEvents.GetSubscription(request.From);
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
                var toSubs = ServerEvents.GetSubscriptionsByUserId(request.ToUserId);
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
            var sub = ServerEvents.GetSubscription(request.From);
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
    }

    public class ServerEventsAppHost : AppSelfHostBase
    {
        public ServerEventsAppHost()
            : base(typeof(ServerEventsAppHost).Name, typeof(ServerEventsAppHost).Assembly) {}

        public override void Configure(Container container)
        {
            Plugins.Add(new ServerEventsFeature());
        }
    }


    [TestFixture]
    public class ServerEventsTests
    {
        private ServiceStackHost appHost;

        public ServerEventsTests()
        {
            //LogManager.LogFactory = new ConsoleLogFactory();

            appHost = new ServerEventsAppHost()
                .Init()
                .Start(Config.AbsoluteBaseUri);
        }

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
            return new ServerEventsClient(Config.AbsoluteBaseUri);
        }

        [Test]
        public async void Can_connect_to_ServerEventsStream()
        {
            var client = CreateServerEventsClient()
                .Start();

            var task = client.Connect();
            if (task != await Task.WhenAny(task, Task.Delay(2000)))
                throw new TimeoutException();
            
            var connectMsg = await task;
            Assert.That(connectMsg.HeartbeatUrl, Is.StringStarting(Config.AbsoluteBaseUri));
            Assert.That(connectMsg.UnRegisterUrl, Is.StringStarting(Config.AbsoluteBaseUri));
            Assert.That(connectMsg.HeartbeatIntervalMs, Is.GreaterThan(0));
        }

        [Test]
        public async void Does_fire_onJoin_events()
        {
            var client = CreateServerEventsClient()
                .Start();

            var taskConnect = client.Connect();
            var taskMsg = client.WaitForNextCommand();

            if (taskConnect != await Task.WhenAny(taskConnect, Task.Delay(2000)))
                throw new TimeoutException();

            if (taskMsg != await Task.WhenAny(taskMsg, Task.Delay(2000)))
                throw new TimeoutException();

            var connectMsg = await taskConnect;
            Assert.That(connectMsg.HeartbeatUrl, Is.StringStarting(Config.AbsoluteBaseUri));

            var joinMsg = (ServerEventJoin)await taskMsg;
            Assert.That(joinMsg.DisplayName, Is.EqualTo(client.ConnectionInfo.DisplayName));
        }

        [Test]
        public async void Does_fire_all_callbacks()
        {
            ServerEventConnect connectMsg = null;

            var client1 = CreateServerEventsClient();

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

            if (taskConnect != await Task.WhenAny(taskConnect, Task.Delay(2000)))
                throw new TimeoutException();
            if (taskCmd != await Task.WhenAny(taskCmd, Task.Delay(2000)))
                throw new TimeoutException();

            var joinMsg = commands.OfType<ServerEventJoin>().FirstOrDefault();

            Assert.That(connectMsg, Is.Not.Null, "connectMsg == null");
            Assert.That(joinMsg, Is.Not.Null, "joinMsg == null");

            Assert.That(msgs.Count, Is.EqualTo(0));
            Assert.That(errors.Count, Is.EqualTo(0));
            Assert.That(commands.Count, Is.EqualTo(1)); //join

            commands.Clear();

            "New Client....".Print();
            taskCmd = client1.WaitForNextCommand();

            var client2 = CreateServerEventsClient();
            var connectMsg2 = await client2.Connect();

            if (taskCmd != await Task.WhenAny(taskCmd, Task.Delay(2000)))
                throw new TimeoutException();

            joinMsg = commands.OfType<ServerEventJoin>().FirstOrDefault();

            taskCmd = client1.WaitForNextCommand();

            connectMsg2.UnRegisterUrl.GetStringFromUrl(); //unsubscribe 2nd client

            if (taskCmd != await Task.WhenAny(taskCmd, Task.Delay(2000)))
                throw new TimeoutException();

            var leaveMsg = commands.OfType<ServerEventLeave>().FirstOrDefault();

            Assert.That(joinMsg, Is.Not.Null, "joinMsg == null");  //2nd connection
            Assert.That(leaveMsg, Is.Not.Null, "leaveMsg == null");
            Assert.That(commands.Count, Is.EqualTo(2)); //join + leave
            Assert.That(errors.Count, Is.EqualTo(0)); 
        }

        [Test]
        public async Task Does_receive_messages()
        {
            var msgs1 = new List<ServerEventMessage>();
            var msgs2 = new List<ServerEventMessage>();

            var client1 = CreateServerEventsClient();
            var client2 = CreateServerEventsClient();
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
            client1.ServiceClient.Post(new PostChatToChannel {
                From = client1.SubscriptionId,
                Message = "hello from client1",
                Channel = EventSubscription.UnknownChannel,
                Selector = "cmd.chat",
            });

            if (taskMsg1 != await Task.WhenAny(taskMsg1, Task.Delay(2000)))
                throw new TimeoutException();
            if (taskMsg2 != await Task.WhenAny(taskMsg2, Task.Delay(2000)))
                throw new TimeoutException();

            var msg1 = await taskMsg1;
            var msg2 = await taskMsg2;

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
            client2.ServiceClient.Post(new PostChatToChannel
            {
                From = client2.SubscriptionId,
                Message = "hello from client2",
                Channel = EventSubscription.UnknownChannel,
                Selector = "cmd.chat",
            });

            if (taskMsg1 != await Task.WhenAny(taskMsg1, Task.Delay(2000)))
                throw new TimeoutException();
            if (taskMsg2 != await Task.WhenAny(taskMsg2, Task.Delay(2000)))
                throw new TimeoutException();

            msg1 = await taskMsg1;
            msg2 = await taskMsg2;

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
}