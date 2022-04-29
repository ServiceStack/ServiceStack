using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Testing;

namespace NetCoreTests;

public class ContainerTests
{
    class AppHost : AppSelfHostBase
    {
        public AppHost() : base(nameof(ContainerTests), typeof(ContainerTests).Assembly)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            AppSettings = new NetCoreAppSettings(Configuration);
        }

        public override void Configure(Container container)
        {
            var String = AppSettings.GetString("String");
            var Num = AppSettings.Get<int>("Num");
        }
    }
        
    [Route("/haskeys")]
    class HasQueryStringKeysTestDto {}
        
    [Route("/numberofkeys")]
    class NumberOfKeysTestDto {}

    class HasKeysService : Service
    {
        public string Get(HasQueryStringKeysTestDto request)
        {
            return this.Request.QueryString.HasKeys().ToString();
        }
            
        public string Get(NumberOfKeysTestDto request)
        {
            return this.Request.QueryString.Count.ToString();
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
        
    [Test]
    public async Task Can_use_haskeys()
    {
        using (var appHost = new AppHost().Init().Start("http://localhost:2000/"))
        {
            var client = new HttpClient();
            var result = await client.GetStringAsync("http://localhost:2000/haskeys?test=foo");
            Assert.That(result.ToLower(), Is.EqualTo("true"));
        }
    }
        
    [Test]
    public async Task Can_use_AllKeys()
    {
        using (var appHost = new AppHost().Init().Start("http://localhost:2000/"))
        {
            var client = new HttpClient();
            var result = await client.GetStringAsync("http://localhost:2000/numberofkeys?test=foo");
            Assert.That(result, Is.EqualTo("1"));
        }
    }
}
