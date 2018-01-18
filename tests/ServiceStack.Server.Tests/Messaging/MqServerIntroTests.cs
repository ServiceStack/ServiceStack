using System;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using ServiceStack.RabbitMq;
using ServiceStack.Redis;
using ServiceStack.Server.Tests.Caching;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.Server.Tests.Messaging
{
    public class RabbitMqServerIntroTests : MqServerIntroTests
    {
        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            return new RabbitMqServer(connectionString: Config.RabbitMQConnString) { RetryCount = retryCount };
        }
    }

    public class RedisMqServerIntroTests : MqServerIntroTests
    {
        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            var redisManager = new BasicRedisClientManager();
            using (var redis = redisManager.GetClient())
            {
                redis.FlushAll();
            }
            return new RedisMqServer(redisManager) { RetryCount = retryCount };
        }
    }

    public class InMemoryMqServerIntroTests : MqServerIntroTests
    {
        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            return new InMemoryTransientMessageService { RetryCount = retryCount };
        }
    }

    public class HelloIntro : IReturn<HelloIntroResponse>
    {
        public string Name { get; set; }
    }

    public class HelloIntroResponse
    {
        public string Result { get; set; }
    }

    public class HelloService : Service
    {
        public object Any(HelloIntro request)
        {
            return new HelloIntroResponse { Result = "Hello, {0}!".Fmt(request.Name) };
        }
    }

    public class MqAuthOnly : IReturn<MqAuthOnlyResponse>
    {
        public string Name { get; set; }
        public string SessionId { get; set; }
    }

    public class MqAuthOnlyResponse
    {
        public string Result { get; set; }
    }

    public class MqAuthOnlyService : Service
    {
        [Authenticate]
        public object Any(MqAuthOnly request)
        {
            var session = base.SessionAs<AuthUserSession>();
            return new MqAuthOnlyResponse
            {
                Result = "Hello, {0}! Your UserName is {1}"
                    .Fmt(request.Name, session.UserAuthName)
            };
        }
    }

    [Restrict(RequestAttributes.MessageQueue)]
    public class MqRestriction : IReturn<MqRestrictionResponse>
    {
        public string Name { get; set; }
    }

    public class MqRestrictionResponse
    {
        public string Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class MqRestrictionService : Service
    {
        public object Any(MqRestriction request) =>
            new MqRestrictionResponse { Result = request.Name };
    }

    public class AppHost : AppSelfHostBase
    {
        private readonly Func<IMessageService> createMqServerFn;

        public AppHost(Func<IMessageService> createMqServerFn)
            : base("Rabbit MQ Test Host", typeof(HelloService).Assembly)
        {
            this.createMqServerFn = createMqServerFn;
        }

        public override void Configure(Container container)
        {
            Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                new IAuthProvider[] {
                    new CredentialsAuthProvider(AppSettings),
                }));

            container.Register<IAuthRepository>(c => new InMemoryAuthRepository());
            var authRepo = container.Resolve<IAuthRepository>();

            try
            {
                ((IClearable)authRepo).Clear();
            }
            catch { /*ignore*/ }

            authRepo.CreateUserAuth(new UserAuth
            {
                Id = 1,
                UserName = "mythz",
                FirstName = "First",
                LastName = "Last",
                DisplayName = "Display",
            }, "p@55word");

            container.Register(c => createMqServerFn());

            var mqServer = container.Resolve<IMessageService>();

            mqServer.RegisterHandler<HelloIntro>(ExecuteMessage);
            mqServer.RegisterHandler<MqAuthOnly>(m =>
            {
                var req = new BasicRequest
                {
                    Verb = HttpMethods.Post,
                    Headers = { ["X-ss-id"] = m.GetBody().SessionId }
                };
                var response = ExecuteMessage(m, req);
                return response;
            });
            mqServer.RegisterHandler<MqRestriction>(ExecuteMessage);
            mqServer.Start();
        }

        protected override void Dispose(bool disposable)
        {
            var mqServer = TryResolve<IMessageService>();
            mqServer?.Dispose();

            base.Dispose(disposable);
        }
    }

    [TestFixture]
    public abstract class MqServerIntroTests
    {
        public abstract IMessageService CreateMqServer(int retryCount = 1);

        [Test]
        public void Messages_with_no_responses_are_published_to_Request_outq_topic()
        {
            using (var mqServer = CreateMqServer())
            {
                mqServer.RegisterHandler<HelloIntro>(m =>
                {
                    "Hello, {0}!".Print(m.GetBody().Name);
                    return null;
                });
                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    mqClient.Publish(new HelloIntro { Name = "World" });

                    IMessage<HelloIntro> msgCopy = mqClient.Get<HelloIntro>(QueueNames<HelloIntro>.Out);
                    mqClient.Ack(msgCopy);
                    Assert.That(msgCopy.GetBody().Name, Is.EqualTo("World"));
                }
            }
        }

        [Test]
        public void Message_with_response_are_published_to_Response_inq()
        {
            using (var mqServer = CreateMqServer())
            {
                mqServer.RegisterHandler<HelloIntro>(m =>
                    new HelloIntroResponse { Result = "Hello, {0}!".Fmt(m.GetBody().Name) });
                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    mqClient.Publish(new HelloIntro { Name = "World" });

                    IMessage<HelloIntroResponse> responseMsg = mqClient.Get<HelloIntroResponse>(QueueNames<HelloIntroResponse>.In);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, World!"));
                }
            }
        }

        [Test]
        public void Message_with_exceptions_are_retried_then_published_to_Request_dlq()
        {
            using (var mqServer = CreateMqServer(retryCount: 1))
            {
                var called = 0;
                mqServer.RegisterHandler<HelloIntro>(m =>
                {
                    Interlocked.Increment(ref called);
                    throw new ArgumentException("Name");
                });
                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    mqClient.Publish(new HelloIntro { Name = "World" });

                    IMessage<HelloIntro> dlqMsg = mqClient.Get<HelloIntro>(QueueNames<HelloIntro>.Dlq);
                    mqClient.Ack(dlqMsg);

                    Assert.That(called, Is.EqualTo(2));
                    Assert.That(dlqMsg.GetBody().Name, Is.EqualTo("World"));
                    Assert.That(dlqMsg.Error.ErrorCode, Is.EqualTo(typeof(ArgumentException).Name));
                    Assert.That(dlqMsg.Error.Message, Is.EqualTo("Name"));
                }
            }
        }

        [Test]
        public void Message_with_ReplyTo_that_throw_exceptions_are_retried_then_published_to_Request_dlq()
        {
            using (var mqServer = CreateMqServer(retryCount: 1))
            {
                var called = 0;
                mqServer.RegisterHandler<HelloIntro>(m =>
                {
                    Interlocked.Increment(ref called);
                    throw new ArgumentException("Name");
                });
                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    const string replyToMq = "mq:Hello.replyto";
                    mqClient.Publish(new Message<HelloIntro>(new HelloIntro { Name = "World" })
                    {
                        ReplyTo = replyToMq
                    });

                    IMessage<HelloIntro> dlqMsg = mqClient.Get<HelloIntro>(QueueNames<HelloIntro>.Dlq);
                    mqClient.Ack(dlqMsg);

                    Assert.That(called, Is.EqualTo(2));
                    Assert.That(dlqMsg.GetBody().Name, Is.EqualTo("World"));
                    Assert.That(dlqMsg.Error.ErrorCode, Is.EqualTo(typeof(ArgumentException).Name));
                    Assert.That(dlqMsg.Error.Message, Is.EqualTo("Name"));
                }
            }
        }

        [Test]
        public void Message_with_ReplyTo_are_published_to_the_ReplyTo_queue()
        {
            using (var mqServer = CreateMqServer())
            {
                mqServer.RegisterHandler<HelloIntro>(m =>
                    new HelloIntroResponse { Result = "Hello, {0}!".Fmt(m.GetBody().Name) });
                mqServer.Start();

                using (var mqClient = mqServer.CreateMessageQueueClient())
                {
                    const string replyToMq = "mq:Hello.replyto";
                    mqClient.Publish(new Message<HelloIntro>(new HelloIntro { Name = "World" })
                    {
                        ReplyTo = replyToMq
                    });

                    IMessage<HelloIntroResponse> responseMsg = mqClient.Get<HelloIntroResponse>(replyToMq);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, World!"));
                }
            }
        }

        [Test]
        public void Does_process_messages_in_HttpListener_AppHost()
        {
            using (var appHost = new AppHost(() => CreateMqServer()).Init().Start(Config.ListeningOn))
            {
                using (var mqClient = appHost.Resolve<IMessageService>().CreateMessageQueueClient())
                {
                    mqClient.Publish(new HelloIntro { Name = "World" });

                    IMessage<HelloIntroResponse> responseMsg = mqClient.Get<HelloIntroResponse>(QueueNames<HelloIntroResponse>.In);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, World!"));
                }
            }
        }

        [Test]
        public void Does_process_multi_messages_in_HttpListener_AppHost()
        {
            using (var appHost = new AppHost(() => CreateMqServer()).Init().Start(Config.ListeningOn))
            {
                using (var mqClient = appHost.Resolve<IMessageService>().CreateMessageQueueClient())
                {
                    var requests = new[]
                    {
                        new HelloIntro { Name = "Foo" },
                        new HelloIntro { Name = "Bar" },
                    };

                    var client = (IOneWayClient)mqClient;
                    client.SendAllOneWay(requests);

                    var responseMsg = mqClient.Get<HelloIntroResponse>(QueueNames<HelloIntroResponse>.In);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, Foo!"));

                    responseMsg = mqClient.Get<HelloIntroResponse>(QueueNames<HelloIntroResponse>.In);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, Bar!"));
                }
            }
        }

        [Test]
        public void Does_allow_MessageQueue_restricted_Services()
        {
            using (var appHost = new AppHost(() => CreateMqServer()).Init().Start(Config.ListeningOn))
            {
                using (var mqClient = appHost.Resolve<IMessageService>().CreateMessageQueueClient())
                {
                    mqClient.Publish(new MqRestriction
                    {
                        Name = "MQ Restriction",
                    });

                    var responseMsg = mqClient.Get<MqRestrictionResponse>(QueueNames<MqRestrictionResponse>.In);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result,
                        Is.EqualTo("MQ Restriction"));
                }
            }
        }

        [Test]
        public void Can_make_authenticated_requests_with_MQ()
        {
            using (var appHost = new AppHost(() => CreateMqServer()).Init())
            {
                appHost.Start(Config.ListeningOn);

                var client = new JsonServiceClient(Config.ListeningOn);

                var response = client.Post(new Authenticate
                {
                    UserName = "mythz",
                    Password = "p@55word"
                });

                var sessionId = response.SessionId;

                using (var mqClient = appHost.Resolve<IMessageService>().CreateMessageQueueClient())
                {

                    mqClient.Publish(new MqAuthOnly
                    {
                        Name = "MQ Auth",
                        SessionId = sessionId,
                    });

                    var responseMsg = mqClient.Get<MqAuthOnlyResponse>(QueueNames<MqAuthOnlyResponse>.In);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result,
                        Is.EqualTo("Hello, MQ Auth! Your UserName is mythz"));
                }
            }
        }

        [Test]
        public void Does_process_messages_in_BasicAppHost()
        {
            using (var appHost = new BasicAppHost(typeof(HelloService).Assembly)
            {
                ConfigureAppHost = host =>
                {
                    host.Container.Register(c => CreateMqServer());

                    var mqServer = host.Container.Resolve<IMessageService>();

                    mqServer.RegisterHandler<HelloIntro>(host.ExecuteMessage);
                    mqServer.Start();
                }
            }.Init())
            {
                using (var mqClient = appHost.Resolve<IMessageService>().CreateMessageQueueClient())
                {
                    mqClient.Publish(new HelloIntro { Name = "World" });

                    IMessage<HelloIntroResponse> responseMsg = mqClient.Get<HelloIntroResponse>(QueueNames<HelloIntroResponse>.In);
                    mqClient.Ack(responseMsg);
                    Assert.That(responseMsg.GetBody().Result, Is.EqualTo("Hello, World!"));
                }
                appHost.Resolve<IMessageService>().Dispose();
            }
        }
    }

    public class RabbitMqServerPostMessageTests : MqServerPostMessageTests
    {
        public override IMessageService CreateMqServer(IAppHost host, int retryCount = 1)
        {
            return new RabbitMqServer(connectionString: Config.RabbitMQConnString)
            {
                RetryCount = retryCount,
                ResponseFilter = r => { host.OnEndRequest(null); return r; }
            };
        }
    }

    public class RedisMqServerPostMessageTests : MqServerPostMessageTests
    {
        public override IMessageService CreateMqServer(IAppHost host, int retryCount = 1)
        {
            return new RedisMqServer(new BasicRedisClientManager())
            {
                RetryCount = retryCount,
                ResponseFilter = r => { host.OnEndRequest(null); return r; }
            };
        }
    }

    public class HelloIntroWithDep
    {
        public string Name { get; set; }
    }

    public class HelloWithDepService : Service
    {
        public IDisposableDependency Dependency { get; set; }

        public object Any(HelloIntroWithDep request)
        {
            return new HelloIntroResponse { Result = "Hello, {0}!".Fmt(request.Name) };
        }
    }

    public interface IDisposableDependency : IDisposable
    {

    }

    public class DisposableDependency : IDisposableDependency
    {
        private readonly Action onDispose;

        public DisposableDependency(Action onDispose)
        {
            this.onDispose = onDispose;
        }

        public void Dispose()
        {
            if (this.onDispose != null)
                this.onDispose();
        }
    }

    [TestFixture]
    public abstract class MqServerPostMessageTests
    {
        public abstract IMessageService CreateMqServer(IAppHost host, int retryCount = 1);

        [Test]
        public void Does_dispose_request_scope_dependency_in_PostMessageHandler()
        {
            var disposeCount = 0;
            using (var appHost = new BasicAppHost(typeof(HelloWithDepService).Assembly)
            {
                ConfigureAppHost = host =>
                {
#if !NETCORE_SUPPORT
                    RequestContext.UseThreadStatic = true;
#endif
                    host.Container.Register<IDisposableDependency>(c => new DisposableDependency(() =>
                    {
                        Interlocked.Increment(ref disposeCount);
                    }))
                        .ReusedWithin(ReuseScope.Request);
                    host.Container.Register(c => CreateMqServer(host));

                    var mqServer = host.Container.Resolve<IMessageService>();

                    mqServer.RegisterHandler<HelloIntroWithDep>(host.ExecuteMessage);
                    mqServer.Start();
                }
            }.Init())
            {
                using (var mqClient = appHost.Resolve<IMessageService>().CreateMessageQueueClient())
                {
                    mqClient.Publish(new HelloIntroWithDep { Name = "World" });

                    IMessage<HelloIntroResponse> responseMsg = mqClient.Get<HelloIntroResponse>(QueueNames<HelloIntroResponse>.In);
                    mqClient.Ack(responseMsg);

                    Assert.That(disposeCount, Is.EqualTo(1));
                }
                appHost.Resolve<IMessageService>().Dispose();
            }
        }
    }
}