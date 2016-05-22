// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System.Linq;
using NUnit.Framework;
using ServiceStack.Testing;

namespace ServiceStack.ServiceHost.Tests.Routes
{
    [TestFixture]
    public class DynamicRouteAttributeTests
    {
        [Test]
        public void Can_register_routes_dynamically()
        {
            typeof(NewApiRequestDto)
                .AddAttributes(new RouteAttribute("/custom/NewApiRequestDto"))
                .AddAttributes(new RouteAttribute("/custom/NewApiRequestDto/get-only", "GET"));

            using (var appHost = new BasicAppHost(typeof(NewApiRestServiceWithAllVerbsImplemented).Assembly).Init())
            {
                var allVerbs = appHost.RestPaths.First(x => x.Path == "/custom/NewApiRequestDto");
                Assert.That(allVerbs.AllowsAllVerbs);
                Assert.That(allVerbs.AllowedVerbs, Is.Null);

                var getOnlyVerb = appHost.RestPaths.First(x => x.Path == "/custom/NewApiRequestDto/get-only");
                Assert.That(getOnlyVerb.AllowedVerbs.Contains("GET"));
                Assert.That(!getOnlyVerb.AllowedVerbs.Contains("POST"));
            }
        }
    }
}