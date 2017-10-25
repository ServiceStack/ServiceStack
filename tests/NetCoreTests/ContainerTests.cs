using System;
using Funq;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Testing;

namespace NetCoreTests
{
    public class ContainerTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(ContainerTests), typeof(ContainerTests).Assembly) { }

            public override void Configure(Container container)
            {
            }
        }
        
        [Test]
        public void Can_resolve_dependency_in_multiple_AppHosts()
        {
            using (var appHost = new AppHost().Init().Start("http://localhost:2000/"))
            {
                var logFactory = appHost.TryResolve<Microsoft.Extensions.Logging.ILoggerFactory>();
                var log = logFactory.CreateLogger("categoryName");
            }
            
            using (var appHost = new AppHost().Init().Start("http://localhost:2000/"))
            {
                var logFactory = appHost.TryResolve<Microsoft.Extensions.Logging.ILoggerFactory>();
                var log = logFactory.CreateLogger("categoryName");
            }
        }
    }
}