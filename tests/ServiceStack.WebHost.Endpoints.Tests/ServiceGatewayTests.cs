using System.Collections.Generic;
using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class SGSendSyncGetInternal : IReturn<SGSendSyncGetInternal>
    {
        public string Value { get; set; }
    }

    public class SGSendSyncGetExternal : IReturn<SGSendSyncGetExternal>
    {
        public string Value { get; set; }
    }

    public class SGSendSyncPostInternal : IReturn<SGSendSyncPostInternal>
    {
        public string Value { get; set; }
    }

    public class SGSendSyncPostExternal : IReturn<SGSendSyncPostExternal>
    {
        public string Value { get; set; }
    }

    public class SGSendAllSyncGetAnyInternal : IReturn<List<SGSyncGetAnyInternal>>
    {
        public string Value { get; set; }
    }

    public class SGSendAllSyncPostExternal : IReturn<List<SGSyncPostExternal>>
    {
        public string Value { get; set; }
    }

    public class SGPublishPostInternalVoid : IReturnVoid, IGet
    {
        public string Value { get; set; }
    }

    public class SGPublishPostExternalVoid : IReturnVoid, IPost
    {
        public string Value { get; set; }
    }

    public class SGPublishAllPostInternalVoid : IReturnVoid, IGet
    {
        public string Value { get; set; }
    }

    public class SGPublishAllPostExternalVoid : IReturnVoid, IPost
    {
        public string Value { get; set; }
    }

    public class ServiceGatewayServices : Service
    {
        public object Any(SGSendSyncGetInternal request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGSendSyncGetInternal).Name;
            return Gateway.Send(request.ConvertTo<SGSyncGetInternal>());
        }

        public object Any(SGSendSyncGetExternal request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGSendSyncGetExternal).Name;
            return Gateway.Send(request.ConvertTo<SGSyncGetExternal>());
        }

        public object Any(SGSendSyncPostInternal request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGSendSyncPostInternal).Name;
            return Gateway.Send(request.ConvertTo<SGSyncPostInternal>());
        }

        public object Any(SGSendSyncPostExternal request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGSendSyncPostExternal).Name;
            return Gateway.Send(request.ConvertTo<SGSyncPostExternal>());
        }

        public object Any(SGSendAllSyncGetAnyInternal request)
        {
            var requests = 3.Times(i => new SGSyncGetAnyInternal
            {
                Value = request.Value + "> " + Request.Verb + " " + typeof(SGSendAllSyncGetAnyInternal).Name + i
            });

            return Gateway.SendAll(requests);
        }

        public object Any(SGSendAllSyncPostExternal request)
        {
            var requests = 3.Times(i => new SGSyncPostExternal
            {
                Value = request.Value + "> " + Request.Verb + " " + typeof(SGSendAllSyncPostExternal).Name + i
            });

            return Gateway.SendAll(requests);
        }

        public void Any(SGPublishPostInternalVoid request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGPublishPostInternalVoid).Name;
            Gateway.Publish(request.ConvertTo<SGSyncPostInternalVoid>());
        }

        public void Any(SGPublishPostExternalVoid request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGPublishPostExternalVoid).Name;
            Gateway.Publish(request.ConvertTo<SGSyncPostExternalVoid>());
        }

        public void Any(SGPublishAllPostInternalVoid request)
        {
            var requests = 3.Times(i => new SGSyncPostInternalVoid
            {
                Value = request.Value + "> " + Request.Verb + " " + typeof(SGPublishAllPostInternalVoid).Name + i
            });

            Gateway.PublishAll(requests);
        }

        public void Any(SGPublishAllPostExternalVoid request)
        {
            var requests = 3.Times(i => new SGSyncPostExternalVoid
            {
                Value = request.Value + "> " + Request.Verb + " " + typeof(SGPublishAllPostExternalVoid).Name + i
            });

            Gateway.PublishAll(requests);
        }
    }

    public class SGSyncGetInternal : IReturn<SGSyncGetInternal>, IGet
    {
        public string Value { get; set; }
    }

    public class SGSyncGetExternal : IReturn<SGSyncGetExternal>, IGet
    {
        public string Value { get; set; }
    }

    public class SGSyncPostInternal : IReturn<SGSyncPostInternal>, IPost
    {
        public string Value { get; set; }
    }

    public class SGSyncPostExternal : IReturn<SGSyncPostExternal>, IPost
    {
        public string Value { get; set; }
    }

    public class SGSyncGetAnyInternal : IReturn<SGSyncGetAnyInternal>, IGet
    {
        public string Value { get; set; }
    }

    public class SGSyncGetAnyExternal : IReturn<SGSyncGetAnyInternal>, IGet
    {
        public string Value { get; set; }
    }

    public class SGSyncPostInternalVoid : IReturnVoid, IPost
    {
        public string Value { get; set; }
    }

    public class SGSyncPostExternalVoid : IReturnVoid, IPost
    {
        public string Value { get; set; }
    }

    public class ServiceGatewayInternalServices : Service
    {
        public object Get(SGSyncGetInternal request)
        {
            request.Value += "> GET " + typeof(SGSyncGetInternal).Name;
            return request;
        }

        public object Post(SGSyncPostInternal request)
        {
            request.Value += "> POST " + typeof(SGSyncPostInternal).Name;
            return request;
        }

        public object Any(SGSyncGetAnyInternal request)
        {
            request.Value += "> ANY " + typeof(SGSyncGetAnyInternal).Name;
            return request;
        }

        public void Post(SGSyncPostInternalVoid request)
        {
            request.Value += "> POST " + typeof(SGSyncPostInternalVoid).Name;
        }
    }

    public class ServiceGatewayExternalServices : Service
    {
        public object Get(SGSyncGetExternal request)
        {
            request.Value += "> GET " + typeof(SGSyncGetExternal).Name;
            return request;
        }

        public object Post(SGSyncPostExternal request)
        {
            request.Value += "> POST " + typeof(SGSyncPostExternal).Name;
            return request;
        }

        public object Any(SGSyncGetAnyExternal request)
        {
            request.Value += "> ANY " + typeof(SGSyncGetAnyExternal).Name;
            return request;
        }

        public void Post(SGSyncPostExternalVoid request)
        {
            request.Value += "> POST " + typeof(SGSyncPostExternalVoid).Name;
        }
    }

    public class ServiceGatewayTests
    {
        class MessageFactory : IMessageFactory
        {
            public IMessageProducer CreateMessageProducer()
            {
                return new MessageProducer();
            }

            public IMessageQueueClient CreateMessageQueueClient() { return null; }
            public void Dispose() {}
        }

        class MessageProducer : IMessageProducer
        {
            public static List<object> Messages = new List<object>();

            public void Publish<T>(T messageBody)
            {
                Messages.Add(messageBody);
            }

            public void Publish<T>(IMessage<T> message) {}
            public void Dispose() { }
        }

        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(typeof(ServiceGatewayTests).Name, typeof(ServiceGatewayServices).Assembly) { }

            public override void Configure(Container container)
            {
                container.Register<IMessageFactory>(c => new MessageFactory());
            }
        }

        readonly IServiceClient client;
        private readonly ServiceStackHost appHost;
        public ServiceGatewayTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);

            client = new JsonServiceClient(Config.ListeningOn);
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Does_SGSendSyncInternal()
        {
            var response = client.Get(new SGSendSyncGetInternal { Value = "GET CLIENT" });
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSendSyncGetInternal> GET SGSyncGetInternal"));
        }

        [Test]
        public void Does_SGSendSyncGetExternal()
        {
            var response = client.Get(new SGSendSyncGetExternal { Value = "GET CLIENT" });
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSendSyncGetExternal> GET SGSyncGetExternal"));
        }

        [Test]
        public void Does_SGSendSyncPostInternal()
        {
            var response = client.Get(new SGSendSyncPostInternal { Value = "GET CLIENT" });
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSendSyncPostInternal> POST SGSyncPostInternal"));
        }

        [Test]
        public void Does_SGSendSyncPostExternal()
        {
            var response = client.Get(new SGSendSyncPostExternal { Value = "GET CLIENT" });
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSendSyncPostExternal> POST SGSyncPostExternal"));
        }

        [Test]
        public void Does_SGSendAllSyncGetAnyInternal()
        {
            var response = client.Get(new SGSendAllSyncGetAnyInternal { Value = "GET CLIENT" });
            Assert.That(response.Map(x => x.Value), Is.EquivalentTo(
                3.Times(i => "GET CLIENT> GET SGSendSyncGetAllInternal{0}> ANY SGSyncGetAnyInternal".Fmt(i))));
        }

        [Test]
        public void Does_SGSendAllSyncPostExternal()
        {
            var response = client.Get(new SGSendAllSyncPostExternal { Value = "GET CLIENT" });
            Assert.That(response.Map(x => x.Value), Is.EquivalentTo(
                3.Times(i => "GET CLIENT> GET SGSendSyncPostAllExternal{0}> POST SGSyncPostExternal".Fmt(i))));
        }

        [Test]
        public void Does_SGPublishPostInternalVoid()
        {
            MessageProducer.Messages.Clear();
            client.Send(new SGPublishPostInternalVoid { Value = "GET CLIENT" });

            Assert.That(MessageProducer.Messages.Count, Is.EqualTo(1));
            var response = MessageProducer.Messages[0] as SGSyncPostInternalVoid;
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSendPostInternalVoid"));
        }

        [Test]
        public void Does_SGPublishPostExternalVoid()
        {
            MessageProducer.Messages.Clear();
            client.Send(new SGPublishPostExternalVoid { Value = "POST CLIENT" });

            Assert.That(MessageProducer.Messages.Count, Is.EqualTo(1));
            var response = MessageProducer.Messages[0] as SGSyncPostExternalVoid;
            Assert.That(response.Value, Is.EqualTo("POST CLIENT> POST SGSendPostExternalVoid"));
        }

        [Test]
        public void Does_SGPublishAllPostInternalVoid()
        {
            MessageProducer.Messages.Clear();
            client.Send(new SGPublishAllPostInternalVoid { Value = "GET CLIENT" });

            Assert.That(MessageProducer.Messages.Count, Is.EqualTo(3));
            Assert.That(MessageProducer.Messages.Map(x => ((SGSyncPostInternalVoid)x).Value), Is.EquivalentTo(
                3.Times(i => "GET CLIENT> GET SGPublishAllPostInternalVoid{0}".Fmt(i))));
        }

        [Test]
        public void Does_SGPublishAllPostExternalVoid()
        {
            MessageProducer.Messages.Clear();
            client.Send(new SGPublishAllPostExternalVoid { Value = "GET CLIENT" });

            Assert.That(MessageProducer.Messages.Count, Is.EqualTo(3));
            Assert.That(MessageProducer.Messages.Map(x => ((SGSyncPostExternalVoid)x).Value), Is.EquivalentTo(
                3.Times(i => "GET CLIENT> POST SGPublishAllPostExternalVoid{0}".Fmt(i))));
        }
    }
}