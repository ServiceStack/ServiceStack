using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Messaging;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.Azure.Messaging;
using System.Threading.Tasks;
using ServiceStack.Configuration;
#if NETCORE
using QueueClient = Microsoft.Azure.ServiceBus.QueueClient;
#else
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
#endif
using ServiceStack.Logging;


namespace ServiceStack.Azure.Tests.Messaging
{
    public class AzureServiceBusMqServerIntroTests : MqServerIntroTests
    {
        static string ConnectionString
        {
            get
            {
                var connString = Environment.GetEnvironmentVariable("AZURE_BUS_CONNECTION_STRING");
                if (connString != null)
                    return connString;
                
                var assembly = typeof(AzureServiceBusMqServerIntroTests).Assembly;
                var path = new Uri(assembly.CodeBase).LocalPath;
                var configFile = Path.Combine(Path.GetDirectoryName(path), "settings.config");

                return new TextFileSettings(configFile).Get("ConnectionString");
            }
        }

        public AzureServiceBusMqServerIntroTests()
        {
#if !NETCORE            
            NamespaceManager nm = NamespaceManager.CreateFromConnectionString(ConnectionString);
            Parallel.ForEach(nm.GetQueues(), qd =>
            {
                var sbClient = QueueClient.CreateFromConnectionString(ConnectionString, qd.Path, ReceiveMode.ReceiveAndDelete);
                BrokeredMessage msg = null;
                while ((msg = sbClient.Receive(new TimeSpan(0, 0, 1))) != null)
                {
                }
            });
#endif
        }

        public override IMessageService CreateMqServer(int retryCount = 1)
        {
            return new ServiceBusMqServer(ConnectionString) { };
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

            container.Register<IUserAuthRepository>(c => new InMemoryAuthRepository());

            var authRepo = container.Resolve<IUserAuthRepository>();

            if (authRepo.GetUserAuthByUserName("mythz") == null)
            {
                authRepo.CreateUserAuth(new UserAuth
                {
                    Id = 1,
                    UserName = "mythz",
                    FirstName = "First",
                    LastName = "Last",
                    DisplayName = "Display",
                }, "p@55word");
            }

            container.Register(c => createMqServerFn());

            var mqServer = container.Resolve<IMessageService>();

            mqServer.RegisterHandler<HelloIntro>(ServiceController.ExecuteMessage);
            mqServer.RegisterHandler<MqAuthOnly>(m =>
            {
                var req = new BasicRequest { Verb = HttpMethods.Post };
                req.Headers["X-ss-id"] = m.GetBody().SessionId;
                var response = ServiceController.ExecuteMessage(m, req);
                return response;
            });
            mqServer.Start();
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
        [Ignore("Fix test")]
        public void Message_with_exceptions_are_retried_then_published_to_Request_dlq()
        {
            using (var mqServer = CreateMqServer(retryCount: 1))
            {
                var called = 0;
                mqServer.RegisterHandler<HelloIntro>(m =>
                {
                    Interlocked.Increment(ref called);
                    Console.WriteLine("{0}:{1}:{2}", m.Id, m.RetryAttempts, m.Body);
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
            LogManager.LogFactory = null;
            using (var appHost = new AppHost(() => CreateMqServer()).Init())
            {
#if NETCORE
                appHost.Start(Config.ListeningOn);
#endif
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
            LogManager.LogFactory = null;
            using (var appHost = new AppHost(() => CreateMqServer()).Init())
            {
#if NETCORE
                appHost.Start(Config.ListeningOn);
#endif
                
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
        public void Can_make_authenticated_requests_with_MQ()
        {
            LogManager.LogFactory = null;
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
            LogManager.LogFactory = null;
            using (var appHost = new BasicAppHost(typeof(HelloService).Assembly)
            {
                ConfigureAppHost = host =>
                {
                    host.Container.Register(c => CreateMqServer());

                    var mqServer = host.Container.Resolve<IMessageService>();

                    mqServer.RegisterHandler<HelloIntro>(host.ServiceController.ExecuteMessage);
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
            }
        }
    }

    public class AzureServiceBusMqServerPostMessageTests : MqServerPostMessageTests
    {
        public override IMessageService CreateMqServer(IAppHost host, int retryCount = 1)
        {
            var assembly = typeof(AzureServiceBusMqServerPostMessageTests).Assembly;
            var path = new Uri(assembly.CodeBase).LocalPath;
            var configFile = Path.Combine(Path.GetDirectoryName(path), "settings.config");

            var connectionString = new TextFileSettings(configFile).Get("ConnectionString");

            return new ServiceBusMqServer(connectionString)
            {
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
            LogManager.LogFactory = null;
            var disposeCount = 0;
            using (var appHost = new BasicAppHost(typeof(HelloWithDepService).Assembly)
            {
                ConfigureAppHost = host =>
                {
#if !NETCORE                    
                    RequestContext.UseThreadStatic = true;
#endif
                    host.Container.Register<IDisposableDependency>(c => new DisposableDependency(() =>
                    {
                        Interlocked.Increment(ref disposeCount);
                    }))
                        .ReusedWithin(ReuseScope.Request);
                    host.Container.Register(c => CreateMqServer(host));

                    var mqServer = host.Container.Resolve<IMessageService>();

                    mqServer.RegisterHandler<HelloIntroWithDep>(host.ServiceController.ExecuteMessage);
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
            }
        }
    }

    public class Config
    {
        public const string ServiceStackBaseUri = "http://localhost:20000";
        public const string AbsoluteBaseUri = ServiceStackBaseUri + "/";
        public const string ListeningOn = ServiceStackBaseUri + "/";

        public static string SqlServerBuildDb = "Server=localhost;Database=test;User Id=test;Password=test;";
    }
}
