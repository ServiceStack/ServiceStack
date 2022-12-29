using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.FluentValidation;
using ServiceStack.Messaging;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class SGSendSyncGetInternal : IReturn<SGSendSyncGetInternal>
    {
        public bool Throw { get; set; }
        public string Value { get; set; }
    }

    public class SGSendSyncGetExternal : IReturn<SGSendSyncGetExternal>
    {
        public bool Throw { get; set; }
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

    public class SGSendSyncGetSyncObjectInternal : IReturn<SGSendSyncGetSyncObjectInternal>
    {
        public string Value { get; set; }
    }

    public class SGSendSyncGetAsyncObjectExternal : IReturn<SGSendSyncGetAsyncObjectExternal>
    {
        public string Value { get; set; }
    }

    public class SGSyncPostValidationExternal : IReturn<SGSyncPostValidationExternal>
    {
        public string Value { get; set; }
        public string Required { get; set; }
    }

    public class SGSyncPostValidationAsyncExternal : IReturn<SGSyncPostValidationAsyncExternal>
    {
        public string Value { get; set; }
        public string Required { get; set; }
    }

    public class ServiceGatewayServices : Service
    {
        public object Any(SGSendSyncGetInternal request)
        {
            request.Value += "> " + Request.Verb + " " + nameof(SGSendSyncGetInternal);
            return Gateway.Send(request.ConvertTo<SGSyncGetInternal>());
        }

        public object Any(SGSendSyncGetExternal request)
        {
            request.Value += "> " + Request.Verb + " " + nameof(SGSendSyncGetExternal);
            return Gateway.Send(request.ConvertTo<SGSyncGetExternal>());
        }

        public object Any(SGSendSyncPostInternal request)
        {
            request.Value += "> " + Request.Verb + " " + nameof(SGSendSyncPostInternal);
            return Gateway.Send(request.ConvertTo<SGSyncPostInternal>());
        }

        public object Any(SGSendSyncPostExternal request)
        {
            request.Value += "> " + Request.Verb + " " + nameof(SGSendSyncPostExternal);
            return Gateway.Send(request.ConvertTo<SGSyncPostExternal>());
        }

        public object Any(SGSendAllSyncGetAnyInternal request)
        {
            var requests = 3.Times(i => new SGSyncGetAnyInternal
            {
                Value = request.Value + "> " + Request.Verb + " " + nameof(SGSendAllSyncGetAnyInternal) + i
            });

            return Gateway.SendAll(requests);
        }

        public object Any(SGSendAllSyncPostExternal request)
        {
            var requests = 3.Times(i => new SGSyncPostExternal
            {
                Value = request.Value + "> " + Request.Verb + " " + nameof(SGSendAllSyncPostExternal) + i
            });

            return Gateway.SendAll(requests);
        }

        public void Any(SGPublishPostInternalVoid request)
        {
            request.Value += "> " + Request.Verb + " " + nameof(SGPublishPostInternalVoid);
            Gateway.Publish(request.ConvertTo<SGSyncPostInternalVoid>());
        }

        public void Any(SGPublishPostExternalVoid request)
        {
            request.Value += "> " + Request.Verb + " " + nameof(SGPublishPostExternalVoid);
            Gateway.Publish(request.ConvertTo<SGSyncPostExternalVoid>());
        }

        public void Any(SGPublishAllPostInternalVoid request)
        {
            var requests = 3.Times(i => new SGSyncPostInternalVoid
            {
                Value = request.Value + "> " + Request.Verb + " " + nameof(SGPublishAllPostInternalVoid) + i
            });

            Gateway.PublishAll(requests);
        }

        public void Any(SGPublishAllPostExternalVoid request)
        {
            var requests = 3.Times(i => new SGSyncPostExternalVoid
            {
                Value = request.Value + "> " + Request.Verb + " " + nameof(SGPublishAllPostExternalVoid) + i
            });

            Gateway.PublishAll(requests);
        }

        public object Any(SGSendSyncGetSyncObjectInternal request)
        {
            request.Value += "> " + Request.Verb + " " + nameof(SGSendSyncGetSyncObjectInternal);
            return Gateway.Send<object>(request.ConvertTo<SGSyncGetSyncObjectInternal>());
        }

        public object Any(SGSendSyncGetAsyncObjectExternal request)
        {
            request.Value += "> " + Request.Verb + " " + nameof(SGSendSyncGetAsyncObjectExternal);
            return Gateway.SendAsync<object>(request.ConvertTo<SGSyncGetAsyncObjectExternal>());
        }
        
        public object Any(SGSyncPostValidationExternal request)
        {
            request.Value += "> " + Request.Verb + " " + nameof(SGSyncPostValidationExternal);
            try
            {
                return Gateway.Send(request.ConvertTo<SGSyncPostValidationInternal>());
            }
            catch (WebServiceException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new NotImplementedException("Should throw WebServiceException", e);
            }
        }
        
        public async Task<object> Any(SGSyncPostValidationAsyncExternal request)
        {
            await Task.Yield();
            request.Value += "> " + Request.Verb + " " + nameof(SGSyncPostValidationAsyncExternal);
            try
            {
                return await Gateway.SendAsync(request.ConvertTo<SGSyncPostValidationAsyncInternal>());
            }
            catch (WebServiceException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new NotImplementedException("Should throw WebServiceException", e);
            }
        }

    }

    public class SGSyncGetInternal : IReturn<SGSyncGetInternal>, IGet
    {
        public bool Throw { get; set; }
        public string Value { get; set; }
    }

    public class SGSyncGetExternal : IReturn<SGSyncGetExternal>, IGet
    {
        public bool Throw { get; set; }
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

    public class SGSyncGetSyncObjectInternal : IReturn<SGSyncGetSyncObjectInternal>, IGet
    {
        public string Value { get; set; }
    }

    public class SGSyncGetAsyncObjectExternal : IReturn<SGSyncGetAsyncObjectExternal>, IGet
    {
        public string Value { get; set; }
    }

    public class SGSyncPostValidationInternal : IReturn<SGSyncPostValidationInternal>
    {
        public string Value { get; set; }
        public string Required { get; set; }
    }

    public class SGSyncPostValidationInternalValidator : AbstractValidator<SGSyncPostValidationInternal>
    {
        public SGSyncPostValidationInternalValidator()
        {
            RuleFor(x => x.Required).NotEmpty();
        }
    }

    public class SGSyncPostValidationAsyncInternal : IReturn<SGSyncPostValidationAsyncInternal>
    {
        public string Value { get; set; }
        public string Required { get; set; }
    }

    public class SGSyncPostValidationAsyncInternalValidator : AbstractValidator<SGSyncPostValidationAsyncInternal>
    {
        public SGSyncPostValidationAsyncInternalValidator()
        {
            RuleFor(x => x.Required).NotEmpty();
        }
    }

    public class ServiceGatewayInternalServices : Service
    {
        public object Get(SGSyncGetInternal request)
        {
            if (request.Throw)
                throw new ArgumentException("ERROR " + nameof(SGSendSyncGetInternal));

            request.Value += "> GET " + nameof(SGSyncGetInternal);
            return request;
        }

        public object Post(SGSyncPostInternal request)
        {
            request.Value += "> POST " + nameof(SGSyncPostInternal);
            return request;
        }

        public object Any(SGSyncGetAnyInternal request)
        {
            request.Value += "> ANY " + nameof(SGSyncGetAnyInternal);
            return request;
        }

        public void Post(SGSyncPostInternalVoid request)
        {
            request.Value += "> POST " + nameof(SGSyncPostInternalVoid);
        }

        public object Any(SGSyncGetSyncObjectInternal request)
        {
            request.Value += "> GET " + nameof(SGSyncGetSyncObjectInternal);
            return request;
        }

        public object Any(SGSyncPostValidationInternal request)
        {
            request.Value += "> ANY " + nameof(SGSyncPostValidationInternal);
            return request;
        }

        public async Task<object> Any(SGSyncPostValidationAsyncInternal request)
        {
            await Task.Yield();
            request.Value += "> ANY " + nameof(SGSyncPostValidationAsyncInternal);
            return request;
        }
    }

    public class ServiceGatewayExternalServices : Service
    {
        public object Get(SGSyncGetExternal request)
        {
            if (request.Throw)
                throw new ArgumentException("ERROR " + nameof(SGSyncGetExternal));

            request.Value += "> GET " + nameof(SGSyncGetExternal);
            return request;
        }

        public object Post(SGSyncPostExternal request)
        {
            request.Value += "> POST " + nameof(SGSyncPostExternal);
            return request;
        }

        public object Any(SGSyncGetAnyExternal request)
        {
            request.Value += "> ANY " + nameof(SGSyncGetAnyExternal);
            return request;
        }

        public void Post(SGSyncPostExternalVoid request)
        {
            request.Value += "> POST " + nameof(SGSyncPostExternalVoid);
        }

        public async Task<object> Any(SGSyncGetAsyncObjectExternal request)
        {
            await Task.Yield();
            request.Value += "> GET " + nameof(SGSyncGetAsyncObjectExternal);
            return request;
        }
    }

    //AppHosts
    public class MixedServiceGatewayTests : ServiceGatewayTests
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
            public MixedAppHost() : base(nameof(ServiceGatewayTests), typeof(ServiceGatewayServices).Assembly) { }

            public override void Configure(Container container)
            {
                container.Register<IMessageFactory>(c => new MessageFactory());

                container.Register<IServiceGatewayFactory>(x => new MixedServiceGatewayFactory())
                    .ReusedWithin(ReuseScope.None);
                
                Plugins.Add(new ValidationFeature());
                container.RegisterValidator(typeof(SGSyncPostValidationInternalValidator));
            }
        }

        protected override ServiceStackHost CreateAppHost()
        {
            return new MixedAppHost();
        }
    }

    public class AllExternalServiceGatewayTests : ServiceGatewayTests
    {
        class AllExternalAppHost : AppSelfHostBase
        {
            public AllExternalAppHost() : base(nameof(ServiceGatewayTests), typeof(ServiceGatewayServices).Assembly) { }

            public override void Configure(Container container)
            {
                container.Register<IMessageFactory>(c => new MessageFactory());
                var client = new JsonServiceClient(Tests.Config.ListeningOn);
                // client.CaptureHttp(print:true);
                container.Register<IServiceGateway>(c => client);
                
                Plugins.Add(new ValidationFeature());
                container.RegisterValidator(typeof(SGSyncPostValidationInternalValidator));
            }
        }

        protected override ServiceStackHost CreateAppHost()
        {
            return new AllExternalAppHost();
        }
    }

    //Tests
    public class AllInternalServiceGatewayTests : ServiceGatewayTests
    {
        class AllInternalAppHost : AppSelfHostBase
        {
            public AllInternalAppHost() : base(nameof(ServiceGatewayTests), typeof(ServiceGatewayServices).Assembly) { }

            public override void Configure(Container container)
            {
                container.Register<IMessageFactory>(c => new MessageFactory());
                
                Plugins.Add(new ValidationFeature());
                container.RegisterValidator(typeof(SGSyncPostValidationInternalValidator));
            }
        }

        protected override ServiceStackHost CreateAppHost()
        {
            return new AllInternalAppHost();
        }
    }


    public abstract class ServiceGatewayTests
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
        public ServiceGatewayTests()
        {
            appHost = CreateAppHost()
                .Init()
                .Start(Config.ListeningOn);

            client = new JsonServiceClient(Config.ListeningOn);
            // ((JsonServiceClient) client).CaptureHttp(print:true);
        }

        [OneTimeTearDown]
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
        public void Does_throw_original_Exception_in_SGSendSyncInternal()
        {
            try
            {
                var response = client.Get(new SGSendSyncGetInternal { Value = "GET CLIENT", Throw = true });
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                Assert.That(ex.StatusDescription, Is.EqualTo(nameof(ArgumentException)));
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(ArgumentException)));
                Assert.That(ex.ResponseStatus.ErrorCode, Is.EqualTo(nameof(ArgumentException)));
                Assert.That(ex.ResponseStatus.Message, Is.EqualTo("ERROR " + nameof(SGSendSyncGetInternal)));
            }
        }

        [Test]
        public void Does_SGSendSyncGetExternal()
        {
            var response = client.Get(new SGSendSyncGetExternal { Value = "GET CLIENT" });
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSendSyncGetExternal> GET SGSyncGetExternal"));
        }

        [Test]
        public void Does_throw_original_Exception_in_SGSendSyncGetExternal()
        {
            try
            {
                var response = client.Get(new SGSendSyncGetExternal { Value = "GET CLIENT", Throw = true });
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                Assert.That(ex.StatusDescription, Is.EqualTo(nameof(ArgumentException)));
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(ArgumentException)));
                Assert.That(ex.ResponseStatus.ErrorCode, Is.EqualTo(nameof(ArgumentException)));
                Assert.That(ex.ResponseStatus.Message, Is.EqualTo("ERROR " + nameof(SGSyncGetExternal)));
            }
        }

        [Test]
        public void Does_throw_original_ValidationException_in_SGSyncPostValidationExternal()
        {
            try
            {
                var response = client.Get(new SGSyncPostValidationExternal { Value = "GET CLIENT" });
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                Assert.That(ex.StatusDescription, Is.EqualTo("NotEmpty"));
                Assert.That(ex.ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(ex.ResponseStatus.ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(ex.ResponseStatus.Message, Is.EqualTo("'Required' must not be empty."));
            }
        }

        [Test]
        public void Does_throw_original_ValidationException_in_SGSyncPostValidationAsyncExternal()
        {
            try
            {
                var response = client.Get(new SGSyncPostValidationAsyncExternal { Value = "GET CLIENT" });
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(400));
                Assert.That(ex.StatusDescription, Is.EqualTo("NotEmpty"));
                Assert.That(ex.ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(ex.ResponseStatus.ErrorCode, Is.EqualTo("NotEmpty"));
                Assert.That(ex.ResponseStatus.Message, Is.EqualTo("'Required' must not be empty."));
            }
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
                3.Times(i => "GET CLIENT> GET SGSendAllSyncGetAnyInternal{0}> ANY SGSyncGetAnyInternal".Fmt(i))));
        }

        [Test]
        public void Does_SGSendAllSyncPostExternal()
        {
            var response = client.Get(new SGSendAllSyncPostExternal { Value = "GET CLIENT" });
            Assert.That(response.Map(x => x.Value), Is.EquivalentTo(
                3.Times(i => "GET CLIENT> GET SGSendAllSyncPostExternal{0}> POST SGSyncPostExternal".Fmt(i))));
        }

        [Test]
        public void Does_SGPublishPostInternalVoid()
        {
            MessageProducer.Messages.Clear();
            client.Send(new SGPublishPostInternalVoid { Value = "GET CLIENT" });

            Assert.That(MessageProducer.Messages.Count, Is.EqualTo(1));
            var response = MessageProducer.Messages[0] as SGSyncPostInternalVoid;
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGPublishPostInternalVoid"));
        }

        [Test]
        public void Does_SGPublishPostExternalVoid()
        {
            MessageProducer.Messages.Clear();
            client.Send(new SGPublishPostExternalVoid { Value = "POST CLIENT" });

            Assert.That(MessageProducer.Messages.Count, Is.EqualTo(1));
            var response = MessageProducer.Messages[0] as SGSyncPostExternalVoid;
            Assert.That(response.Value, Is.EqualTo("POST CLIENT> POST SGPublishPostExternalVoid"));
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

        [Test]
        public void Does_SGSendSyncGetSyncObjectInternal()
        {
            var response = client.Get(new SGSendSyncGetSyncObjectInternal { Value = "GET CLIENT" });
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSendSyncGetSyncObjectInternal> GET SGSyncGetSyncObjectInternal"));
        }

        [Test]
        public void Does_SGSendSyncGetAsyncObjectExternal()
        {
            var response = client.Get(new SGSendSyncGetAsyncObjectExternal { Value = "GET CLIENT" });
            Assert.That(response.Value, Is.EqualTo("GET CLIENT> GET SGSendSyncGetAsyncObjectExternal> GET SGSyncGetAsyncObjectExternal"));
        }
    }
}