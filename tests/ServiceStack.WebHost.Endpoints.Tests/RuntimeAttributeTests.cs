// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Testing;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class RuntimeAttributeTests
    {
        public class RuntimeAttributeAppHost : BasicAppHost
        {
            public RuntimeAttributeAppHost()
                : base(typeof (RuntimeAttributeAppHost).Assembly)
            {
                typeof(Register)
                    .AddAttributes(new RouteAttribute("/custom-register"))
                    .AddAttributes(new RestrictAttribute(RequestAttributes.Json));
            }

            public override void Configure(Container container)
            {
                Plugins.Add(new AuthFeature(() => new AuthUserSession(), 
                    new IAuthProvider[] {
                        new BasicAuthProvider(), 
                    }));

                Plugins.Add(new RegistrationFeature());
            }
        }

        [Test]
        public void Does_add_CustomAttributes_to_when_added_in_AppHost_constructor()
        {
            using (var appHost = new RuntimeAttributeAppHost().Init())
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
        }
    }
}