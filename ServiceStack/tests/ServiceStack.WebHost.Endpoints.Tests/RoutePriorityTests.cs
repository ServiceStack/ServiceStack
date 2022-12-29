// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using NUnit.Framework;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/category/priority")]
    public class RoutePriority
    {
        public string Id { get; set; }
    }

    public class RoutePriorityService : IService
    {
        public object Get(RoutePriority request)
        {
            return Run(request, ApplyTo.Get);
        }

        public object Put(RoutePriority request)
        {
            return Run(request, ApplyTo.Put);
        }

        public object Post(RoutePriority request)
        {
            return Run(request, ApplyTo.Post);
        }

        protected virtual object Run(RoutePriority request, ApplyTo method)
        {
            return request.AsTypeString();
        }
    }

    [TestFixture]
    public class RoutePriorityTests
    {
        [Test]
        public void Prefer_user_defined_routes_first()
        {
            using (var appHost = new BasicAppHost
            {
                ConfigureAppHost = host =>
                {
                    host.Routes.AddFromAssembly(typeof(RoutePriorityTests).Assembly);
                },
            }.Init())
            {
                var emptyUrl = new RoutePriority().ToGetUrl();
                Assert.That(emptyUrl, Is.EqualTo("/category/priority"));

                var autoRouteWithIdUrl = new RoutePriority { Id = "foo" }.ToGetUrl();
                Assert.That(autoRouteWithIdUrl, Is.EqualTo("/RoutePriority/foo"));
            }
        }
    }
}