using System.Collections.Generic;
using System.Linq;
using Funq;
using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class SGSyncCallSyncGetInternal : IReturn<SGSyncCallSyncGetInternal>
    {
        public string Value { get; set; }
    }

    public class SGSyncCallSyncGetExternal : IReturn<SGSyncCallSyncGetExternal>
    {
        public string Value { get; set; }
    }

    public class SGSyncCallSyncPostInternal : IReturn<SGSyncCallSyncPostInternal>
    {
        public string Value { get; set; }
    }

    public class SGSyncCallSyncPostExternal : IReturn<SGSyncCallSyncPostExternal>
    {
        public string Value { get; set; }
    }

    public class SGSyncCallSyncGetAllInternal : IReturn<List<SGSyncGetAnyInternal>>
    {
        public string Value { get; set; }
    }

    public class SGSyncCallSyncPostAllExternal : IReturn<List<SGSyncPostExternal>>
    {
        public string Value { get; set; }
    }

    public class SGSyncCallPostInternalVoid : IReturnVoid, IGet
    {
        public string Value { get; set; }
    }

    public class SGSyncCallPostExternalVoid : IReturnVoid, IPost
    {
        public string Value { get; set; }
    }

    public class ServiceGatewayServices : Service
    {
        public object Any(SGSyncCallSyncGetInternal request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGSyncCallSyncGetInternal).Name;
            return Gateway.Send(request.ConvertTo<SGSyncGetInternal>());
        }

        public object Any(SGSyncCallSyncGetExternal request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGSyncCallSyncGetExternal).Name;
            return Gateway.Send(request.ConvertTo<SGSyncGetExternal>());
        }

        public object Any(SGSyncCallSyncPostInternal request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGSyncCallSyncPostInternal).Name;
            return Gateway.Send(request.ConvertTo<SGSyncPostInternal>());
        }

        public object Any(SGSyncCallSyncPostExternal request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGSyncCallSyncPostExternal).Name;
            return Gateway.Send(request.ConvertTo<SGSyncPostExternal>());
        }

        public object Any(SGSyncCallSyncGetAllInternal request)
        {
            var requests = 3.Times(i => new SGSyncGetAnyInternal
            {
                Value = request.Value + "> " + Request.Verb + " " + typeof(SGSyncCallSyncGetAllInternal).Name + i
            });

            return Gateway.SendAll(requests);
        }

        public object Any(SGSyncCallSyncPostAllExternal request)
        {
            var requests = 3.Times(i => new SGSyncPostExternal
            {
                Value = request.Value + "> " + Request.Verb + " " + typeof(SGSyncCallSyncPostAllExternal).Name + i
            });

            return Gateway.SendAll(requests);
        }

        public void Any(SGSyncCallPostInternalVoid request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGSyncCallPostInternalVoid).Name;
            Gateway.Publish(request.ConvertTo<SGSyncPostInternalVoid>());
        }

        public void Any(SGSyncCallPostExternalVoid request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGSyncCallPostExternalVoid).Name;
            Gateway.Publish(request.ConvertTo<SGSyncPostExternalVoid>());
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
        public void Does_SGSyncCallSyncInternal()
        {
            var response = client.Get(new SGSyncCallSyncGetInternal { Value = "GET CLIENT" });
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSyncCallSyncGetInternal> GET SGSyncGetInternal"));
        }

        [Test]
        public void Does_SGSyncCallSyncGetExternal()
        {
            var response = client.Get(new SGSyncCallSyncGetExternal { Value = "GET CLIENT" });
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSyncCallSyncGetExternal> GET SGSyncGetExternal"));
        }

        [Test]
        public void Does_SGSyncCallSyncPostInternal()
        {
            var response = client.Get(new SGSyncCallSyncPostInternal { Value = "GET CLIENT" });
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSyncCallSyncPostInternal> POST SGSyncPostInternal"));
        }

        [Test]
        public void Does_SGSyncCallSyncPostExternal()
        {
            var response = client.Get(new SGSyncCallSyncPostExternal { Value = "GET CLIENT" });
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSyncCallSyncPostExternal> POST SGSyncPostExternal"));
        }

        [Test]
        public void Does_SGSyncCallSyncGetAllInternal()
        {
            var response = client.Get(new SGSyncCallSyncGetAllInternal { Value = "GET CLIENT" });
            Assert.That(response.Map(x => x.Value), Is.EquivalentTo(
                3.Times(i => "GET CLIENT> GET SGSyncCallSyncGetAllInternal{0}> ANY SGSyncGetAnyInternal".Fmt(i))));
        }

        [Test]
        public void Does_SGSyncCallSyncPostAllExternal()
        {
            var response = client.Get(new SGSyncCallSyncPostAllExternal { Value = "GET CLIENT" });
            Assert.That(response.Map(x => x.Value), Is.EquivalentTo(
                3.Times(i => "GET CLIENT> GET SGSyncCallSyncPostAllExternal{0}> POST SGSyncPostExternal".Fmt(i))));
        }

        [Test]
        public void Does_SGSyncCallPostInternalVoid()
        {
            MessageProducer.Messages.Clear();
            client.Send(new SGSyncCallPostInternalVoid { Value = "GET CLIENT" });

            Assert.That(MessageProducer.Messages.Count, Is.EqualTo(1));
            var response = MessageProducer.Messages[0] as SGSyncPostInternalVoid;
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSyncCallPostInternalVoid"));
        }

        [Test]
        public void Does_SGSyncCallPostExternalVoid()
        {
            MessageProducer.Messages.Clear();
            client.Send(new SGSyncCallPostExternalVoid { Value = "POST CLIENT" });

            Assert.That(MessageProducer.Messages.Count, Is.EqualTo(1));
            var response = MessageProducer.Messages[0] as SGSyncPostExternalVoid;
            Assert.That(response.Value, Is.EqualTo("POST CLIENT> POST SGSyncCallPostExternalVoid"));
        }
    }
}