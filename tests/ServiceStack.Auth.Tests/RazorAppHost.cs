using System;
using System.Collections.Generic;
using System.Threading;
using Funq;
using NUnit.Framework;
using ServiceStack.Razor;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Auth.Tests
{
    public class DataSource
    {
        public string[] Items = new[] { "Eeny", "meeny", "miny", "moe" };
    }
    
    public class RazorAppHost : AppHostHttpListenerBase
    {
        public RazorAppHost()
            : base("Test Razor", typeof(RazorAppHost).Assembly) {}

        public override void Configure(Container container)
        {
            Plugins.Add(new RazorFormat());

            container.Register(new DataSource());
        }
    }
    
    [RestService("/viewmodel/{Id}")]
    public class ViewThatUsesLayoutAndModel
    {
        public string Id { get; set; }
    }

    public class ViewThatUsesLayoutAndModelResponse
    {
        public string Name { get; set; }
        public List<string> Results { get; set; }
    }

    public class ViewService : ServiceBase<ViewThatUsesLayoutAndModel>
    {
        protected override object Run(ViewThatUsesLayoutAndModel request)
        {
            return new ViewThatUsesLayoutAndModelResponse {
                Name = request.Id ?? "Foo",
                Results = new List<string> { "Tom", "Dick", "Harry" }
            };
        }
    }

    [TestFixture]
    public class RazorAppHostTests
    {
        [Test]
        public void Hold_open_for_10Mins()
        {
            using (var appHost = new RazorAppHost())
            {
                appHost.Init();
                appHost.Start("http://localhost:11000/");
                Thread.Sleep(TimeSpan.FromMinutes(10));
            }
        }
    }
}