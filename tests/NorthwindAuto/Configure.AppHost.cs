using Funq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;

[assembly: HostingStartup(typeof(NorthwindAuto.AppHost))]

namespace NorthwindAuto
{
    public class AppHost : AppHostBase, IHostingStartup
    {
        public void Configure(IWebHostBuilder builder) => builder
            .ConfigureServices((context, services) => 
                // Register Database Connection, see: https://github.com/ServiceStack/ServiceStack.OrmLite#usage
                services.AddSingleton<IDbConnectionFactory>(c =>
                    new OrmLiteConnectionFactory(MapProjectPath("~/northwind.sqlite"), SqliteDialect.Provider)))
            .Configure(app => {
                if (!HasInit) 
                    app.UseServiceStack(new AppHost());
            });
        
        public AppHost() : base("Northwind Auto", typeof(MyServices).Assembly) { }

        // Configure your AppHost with the necessary configuration and dependencies your App needs
        public override void Configure(Container container)
        {
            SetConfig(new HostConfig { DebugMode = true });

            // Register Database Connection, see: https://github.com/ServiceStack/ServiceStack.OrmLite#usage
            container.AddSingleton<IDbConnectionFactory>(c =>
                new OrmLiteConnectionFactory(MapProjectPath("~/northwind.sqlite"), SqliteDialect.Provider));

            Plugins.Add(new AutoQueryFeature {
                MaxLimit = 100,
                GenerateCrudServices = new GenerateCrudServices {}
            });
        }
    }

    [Route("/hello")]
    [Route("/hello/{Name}")]
    public class Hello : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
    }

    public class MyServices : Service
    {
        public object Any(Hello request)
        {
            return new HelloResponse { Result = $"Hello, {request.Name}!" };
        }
    }
}