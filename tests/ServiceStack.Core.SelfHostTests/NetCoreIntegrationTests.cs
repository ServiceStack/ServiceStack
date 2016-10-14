using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.Core.SelfHostTests
{
    public interface IDep
    {
        int Count { get; }
        Dep Dep { get; }
    }
    public class Dep
    {
        private static int count;
        public int Count => count;
        public Dep() { count++; }
    }

    public interface INetCoreSingleton : IDep { }
    public class NetCoreSingleton : INetCoreSingleton
    {
        private static int count;
        public int Count => count;
        public NetCoreSingleton(Dep dep) //.NET Core forcing code-bloat
        {
            count++;
            Dep = dep;
        }
        public Dep Dep { get; set; }
    }

    public interface INetCoreScoped : IDep { }
    public class NetCoreScoped : INetCoreScoped
    {
        internal static int count;
        public int Count => count;
        public NetCoreScoped(Dep dep)
        {
            count++;
            Dep = dep;
        }
        public Dep Dep { get; set; }
    }

    public class NetCoreScopedRequest
    {
        internal static int count;
        public int Count => count;
        public NetCoreScopedRequest(Dep dep)
        {
            count++;
            Dep = dep;
        }
        public Dep Dep { get; set; }
    }

    public interface INetCoreTransient : IDep { }
    public class NetCoreTransient : INetCoreTransient
    {
        private static int count;
        public int Count => count;
        public NetCoreTransient(Dep dep)
        {
            count++;
            Dep = dep;
        }
        public Dep Dep { get; set; }
    }

    public class NetCoreInstance : IDep
    {
        private static int count;
        public int Count => count;
        public NetCoreInstance() { count++; }
        public Dep Dep { get; set; }
    }

    public interface IFunqSingleton : IDep { }
    public class FunqSingleton : IFunqSingleton
    {
        private static int count;
        public int Count => count;
        public FunqSingleton() { count++; }
        public Dep Dep { get; set; }
    }

    public interface IFunqScoped : IDep { }
    public class FunqScoped : IFunqScoped
    {
        private static int count;
        public int Count => count;
        public FunqScoped() { count++; }
        public Dep Dep { get; set; }
    }

    public interface IFunqTransient : IDep { }
    public class FunqTransient : IFunqTransient
    {
        private static int count;
        public int Count => count;
        public FunqTransient() { count++; }
        public Dep Dep { get; set; }
    }

    public class FunqInstance : IDep
    {
        private static int count;
        public int Count => count;
        public FunqInstance() { count++; }
        public Dep Dep { get; set; }
    }

    [TestFixture]
    public class NetCoreIntegrationTests
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddSingleton<INetCoreSingleton, NetCoreSingleton>()
                        .AddSingleton(new NetCoreInstance())
                        .AddScoped<INetCoreScoped, NetCoreScoped>()
                        .AddScoped<NetCoreScopedRequest>()
                        .AddTransient<INetCoreTransient, NetCoreTransient>()
                        .AddTransient<Dep>();
            }

            public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
            {
                loggerFactory.AddConsole();

                app.UseServiceStack(new AppHost());

                app.Run(async context =>
                {
                    context.Request.EnableRewind();
                    await context.Response.WriteAsync("Hello World!!!");
                });
            }
        }

        public class Ioc : IReturn<IocResponse>
        {
            public string Name { get; set; }
        }

        public class IocResponse
        {
            public Dictionary<string, int> Results { get; set; }
        }

        public class IocServices : Service
        {
            public INetCoreSingleton NetCoreSingleton { get; set; }
            public NetCoreInstance NetCoreInstance { get; set; }
            public INetCoreScoped NetCoreScoped { get; set; }
            public INetCoreTransient NetCoreTransient { get; set; }

            public IFunqSingleton FunqSingleton { get; set; }
            public FunqInstance FunqInstance { get; set; }
            public IFunqScoped FunqScoped { get; set; }
            public IFunqTransient FunqTransient { get; set; }

            public object Any(Ioc request)
            {
                if (NetCoreSingleton == null)
                    throw new ArgumentNullException(nameof(NetCoreSingleton));
                if (NetCoreInstance == null)
                    throw new ArgumentNullException(nameof(NetCoreInstance));
                if (NetCoreScoped == null)
                    throw new ArgumentNullException(nameof(NetCoreScoped));
                if (NetCoreTransient == null)
                    throw new ArgumentNullException(nameof(NetCoreTransient));

                if (NetCoreSingleton.Dep == null)
                    throw new ArgumentException(nameof(NetCoreSingleton), "!Dep");
                if (NetCoreInstance.Dep != null)
                    throw new ArgumentException(nameof(NetCoreInstance), "Dep");
                if (NetCoreScoped.Dep == null)
                    throw new ArgumentException(nameof(NetCoreScoped), "!Dep");
                if (NetCoreTransient.Dep == null)
                    throw new ArgumentException(nameof(NetCoreTransient), "!Dep");

                if (FunqSingleton == null)
                    throw new ArgumentNullException(nameof(FunqSingleton));
                if (FunqInstance == null)
                    throw new ArgumentNullException(nameof(FunqInstance));
                if (FunqScoped == null)
                    throw new ArgumentNullException(nameof(FunqScoped));
                if (FunqTransient == null)
                    throw new ArgumentNullException(nameof(FunqTransient));

                if (FunqSingleton.Dep == null)
                    throw new ArgumentException(nameof(FunqSingleton), "!Dep");
                if (FunqInstance.Dep != null)
                    throw new ArgumentException(nameof(FunqInstance), "Dep");
                if (FunqScoped.Dep == null)
                    throw new ArgumentException(nameof(FunqScoped), "!Dep");
                if (FunqTransient.Dep == null)
                    throw new ArgumentException(nameof(FunqTransient), "!Dep");

                var netCoreRequestScope = Request.TryResolve<NetCoreScopedRequest>();
                if (netCoreRequestScope.Dep == null)
                    throw new ArgumentException(nameof(netCoreRequestScope), "!Dep");

                return new IocResponse
                {
                    Results = new Dictionary<string, int>
                    {
                        { "NetCoreSingleton", NetCoreSingleton.Count },
                        { "NetCoreInstance", NetCoreInstance.Count },
                        { "NetCoreScoped", NetCoreScoped.Count },
                        { "NetCoreTransient", NetCoreTransient.Count },
                        { "FunqSingleton", FunqSingleton.Count },
                        { "FunqInstance", FunqInstance.Count },
                        { "FunqScoped", FunqScoped.Count },
                        { "FunqTransient", FunqTransient.Count },
                        { "NetCoreScopedRequest", netCoreRequestScope.Count },
                    }
                };
            }
        }

        public class AppHost : AppHostBase
        {
            public AppHost()
                : base(".NET Core Test", typeof(IocServices).GetAssembly()) { }

            public override void Configure(Container container)
            {
                container.AddSingleton<IFunqSingleton, FunqSingleton>()
                         .AddSingleton(new FunqInstance())
                         .AddScoped<IFunqScoped, FunqScoped>()
                         .AddTransient<IFunqTransient, FunqTransient>();
            }
        }

        class ReqeustScopeStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddScoped<INetCoreScoped, NetCoreScoped>()
                        .AddTransient<Dep>();
            }

            public void Configure(IApplicationBuilder app)
            {
                app.Use((ctx, next) =>
                {
                    var dep = ctx.RequestServices.GetService<INetCoreScoped>();
                    //var dep = app.ApplicationServices.GetService<INetCoreScoped>(); //Request Scope doesn't work here
                    ctx.Response.Body.Write($"Count: {dep.Count}");
                    return Task.FromResult(0);
                });
            }
        }

        [Test]
        public void Does_return_new_Scoped_dependency_per_request()
        {
            using (var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<ReqeustScopeStartup>()
                .UseUrls(Config.AbsoluteBaseUri)
                .Build())
            {
                host.Start();

                5.Times(i => Config.AbsoluteBaseUri.GetStringFromUrl());

                Assert.That(NetCoreScoped.count, Is.GreaterThanOrEqualTo(5));
            }
        }

        [Test]
        public void Does_resolve_deps_in_ConfigureServices()
        {
            using (var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseUrls(Config.AbsoluteBaseUri)
                .Build())
            {
                host.Start();

                var client = new JsonServiceClient(Config.AbsoluteBaseUri);

                var response = client.Get(new Ioc());
                Assert.That(response.Results.Count, Is.EqualTo(9));
                Assert.That(response.Results.Values.ToList().All(x => x == 1));

                4.Times(i => response = client.Get(new Ioc()));

                Assert.That(response.Results.Where(x => x.Key.EndsWith("Singleton")).All(e => e.Value == 1));
                Assert.That(response.Results.Where(x => x.Key.EndsWith("Instance")).All(e => e.Value == 1));

                //Should really be 5 but because deps are resolved for app.ApplicationServices instead of
                //request.RequestServices request scoped deps behave like a singleton
                Assert.That(response.Results["NetCoreScoped"], Is.EqualTo(1));

                Assert.That(response.Results["NetCoreScopedRequest"], Is.EqualTo(5));
                Assert.That(response.Results["FunqScoped"], Is.EqualTo(5));
                Assert.That(response.Results.Where(x => x.Key.EndsWith("Transient")).All(e => e.Value == 5));
            }
        }
    }
}