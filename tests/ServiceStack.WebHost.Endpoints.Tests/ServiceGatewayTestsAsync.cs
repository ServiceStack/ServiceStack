using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class SGSendAsyncGetInternal : IReturn<SGSendAsyncGetInternal>
    {
        public bool Throw { get; set; }
        public string Value { get; set; }
    }

    public class SGSendAsyncGetExternal : IReturn<SGSendAsyncGetExternal>
    {
        public bool Throw { get; set; }
        public string Value { get; set; }
    }

    public class SGSendAsyncPostInternal : IReturn<SGSendAsyncPostInternal>
    {
        public string Value { get; set; }
    }

    public class SGSendAsyncPostExternal : IReturn<SGSendAsyncPostExternal>
    {
        public string Value { get; set; }
    }

    public class SGSendAllAsyncGetAnyInternal : IReturn<List<SGAsyncGetAnyInternal>>
    {
        public string Value { get; set; }
    }

    public class SGSendAllAsyncPostExternal : IReturn<List<SGAsyncPostExternal>>
    {
        public string Value { get; set; }
    }

    public class SGPublishAsyncPostInternalVoid : IReturnVoid, IGet
    {
        public string Value { get; set; }
    }

    public class SGPublishAsyncPostExternalVoid : IReturnVoid, IPost
    {
        public string Value { get; set; }
    }

    public class SGPublishAllAsyncPostInternalVoid : IReturnVoid, IGet
    {
        public string Value { get; set; }
    }

    public class SGPublishAllAsyncPostExternalVoid : IReturnVoid, IPost
    {
        public string Value { get; set; }
    }

    public class ServiceGatewayAsyncServices : Service
    {
        public object Any(SGSendAsyncGetInternal request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGSendAsyncGetInternal).Name;
            return Gateway.SendAsync(request.ConvertTo<SGAsyncGetInternal>());
        }

        public object Any(SGSendAsyncGetExternal request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGSendAsyncGetExternal).Name;
            return Gateway.SendAsync(request.ConvertTo<SGAsyncGetExternal>());
        }

        public object Any(SGSendAsyncPostInternal request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGSendAsyncPostInternal).Name;
            return Gateway.SendAsync(request.ConvertTo<SGAsyncPostInternal>());
        }

        public object Any(SGSendAsyncPostExternal request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGSendAsyncPostExternal).Name;
            return Gateway.SendAsync(request.ConvertTo<SGAsyncPostExternal>());
        }

        public object Any(SGSendAllAsyncGetAnyInternal request)
        {
            var requests = 3.Times(i => new SGAsyncGetAnyInternal
            {
                Value = request.Value + "> " + Request.Verb + " " + typeof(SGSendAllAsyncGetAnyInternal).Name + i
            });

            return Gateway.SendAllAsync(requests);
        }

        public object Any(SGSendAllAsyncPostExternal request)
        {
            var requests = 3.Times(i => new SGAsyncPostExternal
            {
                Value = request.Value + "> " + Request.Verb + " " + typeof(SGSendAllAsyncPostExternal).Name + i
            });

            return Gateway.SendAllAsync(requests);
        }

        public async Task Any(SGPublishAsyncPostInternalVoid request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGPublishAsyncPostInternalVoid).Name;
            await Gateway.PublishAsync(request.ConvertTo<SGAsyncPostInternalVoid>());
        }

        public async Task Any(SGPublishAsyncPostExternalVoid request)
        {
            request.Value += "> " + Request.Verb + " " + typeof(SGPublishAsyncPostExternalVoid).Name;
            await Gateway.PublishAsync(request.ConvertTo<SGAsyncPostExternalVoid>());
        }

        public async Task Any(SGPublishAllAsyncPostInternalVoid request)
        {
            var requests = 3.Times(i => new SGAsyncPostInternalVoid
            {
                Value = request.Value + "> " + Request.Verb + " " + typeof(SGPublishAllAsyncPostInternalVoid).Name + i
            });

            await Gateway.PublishAllAsync(requests);
        }

        public async Task Any(SGPublishAllAsyncPostExternalVoid request)
        {
            var requests = 3.Times(i => new SGAsyncPostExternalVoid
            {
                Value = request.Value + "> " + Request.Verb + " " + typeof(SGPublishAllAsyncPostExternalVoid).Name + i
            });

            await Gateway.PublishAllAsync(requests);
        }
    }

    public class SGAsyncGetInternal : IReturn<SGAsyncGetInternal>, IGet
    {
        public bool Throw { get; set; }
        public string Value { get; set; }
    }

    public class SGAsyncGetExternal : IReturn<SGAsyncGetExternal>, IGet
    {
        public bool Throw { get; set; }
        public string Value { get; set; }
    }

    public class SGAsyncPostInternal : IReturn<SGAsyncPostInternal>, IPost
    {
        public string Value { get; set; }
    }

    public class SGAsyncPostExternal : IReturn<SGAsyncPostExternal>, IPost
    {
        public string Value { get; set; }
    }

    public class SGAsyncGetAnyInternal : IReturn<SGAsyncGetAnyInternal>, IGet
    {
        public string Value { get; set; }
    }

    public class SGAsyncGetAnyExternal : IReturn<SGAsyncGetAnyInternal>, IGet
    {
        public string Value { get; set; }
    }

    public class SGAsyncPostInternalVoid : IReturnVoid, IPost
    {
        public string Value { get; set; }
    }

    public class SGAsyncPostExternalVoid : IReturnVoid, IPost
    {
        public string Value { get; set; }
    }

    public class ServiceGatewayInternalAsyncServices : Service
    {
        public object Get(SGAsyncGetInternal request)
        {
            if (request.Throw)
                throw new ArgumentException("ERROR " + typeof(SGSendAsyncGetInternal).Name);

            request.Value += "> GET " + typeof(SGAsyncGetInternal).Name;
            return request;
        }

        public object Post(SGAsyncPostInternal request)
        {
            request.Value += "> POST " + typeof(SGAsyncPostInternal).Name;
            return Task.FromResult(request);
        }

        public object Any(SGAsyncGetAnyInternal request)
        {
            request.Value += "> ANY " + typeof(SGAsyncGetAnyInternal).Name;
            return request;
        }

        public void Post(SGAsyncPostInternalVoid request)
        {
            request.Value += "> POST " + typeof(SGAsyncPostInternalVoid).Name;
        }
    }

    public class ServiceGatewayExternalAsyncServices : Service
    {
        public async Task<object> Get(SGAsyncGetExternal request)
        {
            await Task.Yield();

            if (request.Throw)
                throw new ArgumentException("ERROR " + typeof(SGAsyncGetExternal).Name);

            request.Value += "> GET " + typeof(SGAsyncGetExternal).Name;
            return await Task.FromResult(request);
        }

        public async Task<object> Post(SGAsyncPostExternal request)
        {
            request.Value += "> POST " + typeof(SGAsyncPostExternal).Name;
            return await Task.FromResult(request);
        }

        public Task Any(SGAsyncGetAnyExternal request)
        {
            request.Value += "> ANY " + typeof(SGAsyncGetAnyExternal).Name;
            return Task.FromResult(request);
        }

        public Task Post(SGAsyncPostExternalVoid request)
        {
            request.Value += "> POST " + typeof(SGAsyncPostExternalVoid).Name;
            return Task.FromResult((object)null);
        }
    }

    //AppHosts
    public class MixedServiceGatewayNativeAsyncTests : ServiceGatewayAsyncTests
    {
        class MixedServiceGatewayFactory : ServiceGatewayFactoryBase
        {
            public override IServiceGateway GetGateway(Type requestType)
            {
                var gateway = requestType.Name.Contains("External")
                    ? new JsonServiceClient(Config.ListeningOn)
                    : (IServiceGateway)localGateway;
                return gateway;
            }
        }

        class MixedAppHost : AppSelfHostBase
        {
            public MixedAppHost() : base(typeof(ServiceGatewayTests).Name, typeof(ServiceGatewayServices).Assembly) { }

            public override void Configure(Container container)
            {
                container.Register<IMessageFactory>(c => new MessageFactory());

                container.Register<IServiceGatewayFactory>(x => new MixedServiceGatewayFactory())
                    .ReusedWithin(ReuseScope.None);
            }
        }

        protected override ServiceStackHost CreateAppHost()
        {
            return new MixedAppHost();
        }
    }

    public class MixedServiceGatewayAsyncTests : ServiceGatewayAsyncTests
    {
        class MixedServiceGatewayFactory : IServiceGatewayFactory, IServiceGateway
        {
            private InProcessServiceGateway localGateway;

            public IServiceGateway GetServiceGateway(IRequest request)
            {
                localGateway = new InProcessServiceGateway(request);
                return this;
            }

            public IServiceGateway GetGateway(Type requestType)
            {
                var gateway = requestType.Name.Contains("External")
                    ? new JsonServiceClient(Config.ListeningOn)
                    : (IServiceGateway) localGateway;
                return gateway;
            }

            public TResponse Send<TResponse>(object requestDto)
            {
                return GetGateway(requestDto.GetType()).Send<TResponse>(requestDto);
            }

            public List<TResponse> SendAll<TResponse>(IEnumerable<object> requestDtos)
            {
                return GetGateway(requestDtos.GetType().GetCollectionType()).SendAll<TResponse>(requestDtos);
            }

            public void Publish(object requestDto)
            {
                GetGateway(requestDto.GetType()).Publish(requestDto);
            }

            public void PublishAll(IEnumerable<object> requestDtos)
            {
                GetGateway(requestDtos.GetType().GetCollectionType()).PublishAll(requestDtos);
            }
        }

        class MixedAppHost : AppSelfHostBase
        {
            public MixedAppHost() : base(typeof(ServiceGatewayTests).Name, typeof(ServiceGatewayServices).Assembly) { }

            public override void Configure(Container container)
            {
                container.Register<IMessageFactory>(c => new MessageFactory());

                container.Register<IServiceGatewayFactory>(x => new MixedServiceGatewayFactory())
                    .ReusedWithin(ReuseScope.None);
            }
        }

        protected override ServiceStackHost CreateAppHost()
        {
            return new MixedAppHost();
        }
    }

    public class AllExternalServiceGatewayAsyncTests : ServiceGatewayAsyncTests
    {
        class AllExternalAppHost : AppSelfHostBase
        {
            public AllExternalAppHost() : base(typeof(ServiceGatewayTests).Name, typeof(ServiceGatewayServices).Assembly) { }

            public override void Configure(Container container)
            {
                container.Register<IMessageFactory>(c => new MessageFactory());
                container.Register<IServiceGateway>(c => new JsonServiceClient(Tests.Config.ListeningOn));
            }
        }

        protected override ServiceStackHost CreateAppHost()
        {
            return new AllExternalAppHost();
        }
    }

    //Tests
    public class AllInternalServiceGatewayAsyncTests : ServiceGatewayAsyncTests
    {
        class AllInternalAppHost : AppSelfHostBase
        {
            public AllInternalAppHost() : base(typeof(ServiceGatewayTests).Name, typeof(ServiceGatewayServices).Assembly) { }

            public override void Configure(Container container)
            {
                container.Register<IMessageFactory>(c => new MessageFactory());
            }
        }

        protected override ServiceStackHost CreateAppHost()
        {
            return new AllInternalAppHost();
        }
    }


    public abstract class ServiceGatewayAsyncTests
    {
        public class MessageFactory : IMessageFactory
        {
            public IMessageProducer CreateMessageProducer()
            {
                return new MessageProducer();
            }

            public IMessageQueueClient CreateMessageQueueClient() { return null; }
            public void Dispose() { }
        }

        public class MessageProducer : IMessageProducer
        {
            public static List<object> Messages = new List<object>();

            public void Publish<T>(T messageBody)
            {
                Messages.Add(messageBody);
            }

            public void Publish<T>(IMessage<T> message) { }
            public void Dispose() { }
        }

        protected abstract ServiceStackHost CreateAppHost();

        readonly IServiceClient client;
        private readonly ServiceStackHost appHost;
        public ServiceGatewayAsyncTests()
        {
            appHost = CreateAppHost()
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
        public void Does_SGSendAsyncInternal()
        {
            var response = client.Get(new SGSendAsyncGetInternal { Value = "GET CLIENT" });
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSendAsyncGetInternal> GET SGAsyncGetInternal"));
        }

        [Test]
        public void Does_throw_original_Exception_in_SGSendAsyncInternal()
        {
            try
            {
                var response = client.Get(new SGSendAsyncGetInternal { Value = "GET CLIENT", Throw = true });
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                Assert.That(ex.StatusDescription, Is.EqualTo(typeof(ArgumentException).Name));
                Assert.That(ex.ErrorCode, Is.EqualTo(typeof(ArgumentException).Name));
                Assert.That(ex.ResponseStatus.ErrorCode, Is.EqualTo(typeof(ArgumentException).Name));
                Assert.That(ex.ResponseStatus.Message, Is.EqualTo("ERROR " + typeof(SGSendAsyncGetInternal).Name));
            }
        }

        [Test]
        public async Task Does_SGSendAsyncGetExternal()
        {
            var response = await client.GetAsync(new SGSendAsyncGetExternal { Value = "GET CLIENT" });
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSendAsyncGetExternal> GET SGAsyncGetExternal"));
        }

        [Test]
        public async Task Does_throw_original_Exception_in_SGSendAsyncGetExternal()
        {
            try
            {
                var response = await client.GetAsync(new SGSendAsyncGetExternal { Value = "GET CLIENT", Throw = true });
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                Assert.That(ex.StatusDescription, Is.EqualTo(typeof(ArgumentException).Name));
                Assert.That(ex.ErrorCode, Is.EqualTo(typeof(ArgumentException).Name));
                Assert.That(ex.ResponseStatus.ErrorCode, Is.EqualTo(typeof(ArgumentException).Name));
                Assert.That(ex.ResponseStatus.Message, Is.EqualTo("ERROR " + typeof(SGAsyncGetExternal).Name));
            }
        }

        [Test]
        public void Does_SGSendAsyncPostInternal()
        {
            var response = client.Get(new SGSendAsyncPostInternal { Value = "GET CLIENT" });
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSendAsyncPostInternal> POST SGAsyncPostInternal"));
        }

        [Test]
        public async Task Does_SGSendAsyncPostExternal()
        {
            var response = await client.GetAsync(new SGSendAsyncPostExternal { Value = "GET CLIENT" });
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSendAsyncPostExternal> POST SGAsyncPostExternal"));
        }

        [Test]
        public void Does_SGSendAllAsyncGetAnyInternal()
        {
            var response = client.Get(new SGSendAllAsyncGetAnyInternal { Value = "GET CLIENT" });
            Assert.That(response.Map(x => x.Value), Is.EquivalentTo(
                3.Times(i => "GET CLIENT> GET SGSendAllAsyncGetAnyInternal{0}> ANY SGAsyncGetAnyInternal".Fmt(i))));
        }

        [Test]
        public async Task Does_SGSendAllAsyncPostExternal()
        {
            var response = await client.GetAsync(new SGSendAllAsyncPostExternal { Value = "GET CLIENT" });
            Assert.That(response.Map(x => x.Value), Is.EquivalentTo(
                3.Times(i => "GET CLIENT> GET SGSendAllAsyncPostExternal{0}> POST SGAsyncPostExternal".Fmt(i))));
        }

        [Test]
        public void Does_SGPublishAsyncPostInternalVoid()
        {
            MessageProducer.Messages.Clear();
            client.Send(new SGPublishAsyncPostInternalVoid { Value = "GET CLIENT" });

            Assert.That(MessageProducer.Messages.Count, Is.EqualTo(1));
            var response = MessageProducer.Messages[0] as SGAsyncPostInternalVoid;
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGPublishAsyncPostInternalVoid"));
        }

        [Test]
        public async Task Does_SGPublishAsyncPostExternalVoid()
        {
            MessageProducer.Messages.Clear();
            await client.SendAsync(new SGPublishAsyncPostExternalVoid { Value = "POST CLIENT" });

            Assert.That(MessageProducer.Messages.Count, Is.EqualTo(1));
            var response = MessageProducer.Messages[0] as SGAsyncPostExternalVoid;
            Assert.That(response.Value, Is.EqualTo("POST CLIENT> POST SGPublishAsyncPostExternalVoid"));
        }

        [Test]
        public void Does_SGPublishAllAsyncPostInternalVoid()
        {
            MessageProducer.Messages.Clear();
            client.Send(new SGPublishAllAsyncPostInternalVoid { Value = "GET CLIENT" });

            Assert.That(MessageProducer.Messages.Count, Is.EqualTo(3));
            Assert.That(MessageProducer.Messages.Map(x => ((SGAsyncPostInternalVoid)x).Value), Is.EquivalentTo(
                3.Times(i => "GET CLIENT> GET SGPublishAllAsyncPostInternalVoid{0}".Fmt(i))));
        }

        [Test]
        public async Task Does_SGPublishAllAsyncPostExternalVoid()
        {
            MessageProducer.Messages.Clear();
            await client.SendAsync(new SGPublishAllAsyncPostExternalVoid { Value = "GET CLIENT" });

            Assert.That(MessageProducer.Messages.Count, Is.EqualTo(3));
            Assert.That(MessageProducer.Messages.Map(x => ((SGAsyncPostExternalVoid)x).Value), Is.EquivalentTo(
                3.Times(i => "GET CLIENT> POST SGPublishAllAsyncPostExternalVoid{0}".Fmt(i))));
        }
    }
}