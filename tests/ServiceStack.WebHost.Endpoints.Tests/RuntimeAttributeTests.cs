// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using Funq;
using NUnit.Framework;
using ServiceStack.Api.Swagger;
using ServiceStack.Auth;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class RuntimeAttributes : IReturn<RuntimeAttributes>
    {
        public int Id { get; set; }
    }

    public class RuntimeAttributeService : Service
    {
        public object Any(RuntimeAttributes request)
        {
            return request;
        }
    }

    public class RuntimeAttributeRequestFilter : RequestFilterAttribute
    {
        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            ((RuntimeAttributes)requestDto).Id++;
        }
    }

    [TestFixture]
    public class RuntimeAttributeTests
    {
        private ServiceStackHost appHost;

        [TestFixtureSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new RuntimeAttributeAppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [TestFixtureTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        public class RuntimeAttributeAppHost : AppSelfHostBase
        {
            public RuntimeAttributeAppHost()
                : base(typeof(RuntimeAttributeTests).Name, typeof(RuntimeAttributeAppHost).Assembly) {}

            public override void Configure(Container container)
            {
                typeof(RuntimeAttributes)
                    .AddAttributes(new RuntimeAttributeRequestFilter());

                typeof(Register)
                    .AddAttributes(new RouteAttribute("/custom-register"))
                    .AddAttributes(new RestrictAttribute(RequestAttributes.Json));

                typeof (ResourceRequest)
                    .AddAttributes(new ExcludeAttribute(Feature.Soap));

                this.RegisterService<RegisterService>("/register");

                Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                    new IAuthProvider[] {
                        new BasicAuthProvider(), 
                    }));
            }
        }

        [Test]
        public void Does_add_CustomAttributes_to_when_added_in_AppHost_constructor()
        {
            string contentType;
            var restPath = RestHandler.FindMatchingRestPath("GET", "/custom-register", out contentType);

            Assert.That(restPath, Is.Not.Null);
            Assert.That(restPath.RequestType, Is.EqualTo(typeof(Register)));

            //Allows JSON
            appHost.ServiceController.AssertServiceRestrictions(typeof(Register), RequestAttributes.Json);

            Assert.Throws<UnauthorizedAccessException>(() =>
                appHost.ServiceController.AssertServiceRestrictions(typeof(Register), RequestAttributes.Xml));
        }

        [Test]
        public void Can_add_RequestFilter_attribute_in_Configure()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            var response = client.Get(new RuntimeAttributes { Id = 1 });

            Assert.That(response.Id, Is.EqualTo(2));
        }
    }
}